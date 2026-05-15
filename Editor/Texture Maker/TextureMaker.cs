using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lewiszhao.Unitytools.Editor
{
    /// <summary>
    /// Combined texture authoring tool: channel-pack four grayscale textures into one
    /// RGBA texture (left panel) and split an RGBA texture back into per-channel
    /// grayscale textures (right panel).
    /// <para>
    /// Merge of the previous TextureChannelMixer + TextureChannelSeparator windows.
    /// </para>
    /// </summary>
    public class TextureMaker : EditorWindow
    {
        private enum MixerMode
        {
            Channels,
            RGBPlusAlpha,
        }

        // ===== Shared =====

        private int m_LanguageIndex;
        private readonly string[] m_LanguageOptions = { "English", "中文" };
        private string Language => m_LanguageOptions[m_LanguageIndex];

        private GUIStyle m_Style;
        private Vector2 m_ScrollPosition;

        // ===== Mixer state =====

        private static readonly int s_Metallic = Shader.PropertyToID("_Metallic");
        private static readonly int s_Occlusion = Shader.PropertyToID("_Occlusion");
        private static readonly int s_DetailMask = Shader.PropertyToID("_DetailMask");
        private static readonly int s_Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int s_RGBSource = Shader.PropertyToID("_RGBSource");
        private static readonly int s_AlphaSource = Shader.PropertyToID("_AlphaSource");
        private static readonly int s_SwapRoughness = Shader.PropertyToID("_SwapRoughness");
        private static readonly int s_MixMode = Shader.PropertyToID("_MixMode");

        private MixerMode m_MixerMode = MixerMode.Channels;
        private Texture2D m_Metallic, m_Occlusion, m_DetailMask, m_Smoothness;
        private Texture2D m_RGBSource, m_AlphaSource;
        private Material m_Mat;
        private Texture2D m_Preview;
        private bool m_MixerUseDefaultPath = true;
        private string m_MixerCustomFolder = "";
        private string m_MixerFileName = "";
        private string m_LastAutoFilledName = "";
        private bool m_UseRoughness;

        // ===== Separator state =====

        private Texture2D m_InputTexture;
        private bool m_SepUseDefaultPath = true;
        private string m_SepCustomPath = "";
        private bool m_OutputR = true, m_OutputG = true, m_OutputB = true, m_OutputA = true;

        [MenuItem("Tools/Texture Maker", priority = 1)]
        private static void ShowWindow()
        {
            var window = GetWindow<TextureMaker>(false, "Texture Maker");
            window.titleContent.text = "Texture Maker";
            window.minSize = new Vector2(900, 680);
            window.Show();
        }

        private void OnEnable()
        {
            if (m_Mat != null) return;
            var maskShader = Shader.Find("Hidden/TextureMixer");
            if (maskShader != null)
            {
                m_Mat = new Material(maskShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            else
            {
                Debug.LogError("Missing shader: Hidden/TextureMixer");
            }
        }

        private void OnDestroy()
        {
            if (m_Mat != null) DestroyImmediate(m_Mat);
            if (m_Preview != null) DestroyImmediate(m_Preview);
        }

        private void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                EditorGUILayout.LabelField("Texture Maker", Style);
                GUILayout.FlexibleSpace();
                GUILayout.Label(GetLocalizedText("Language"), GUILayout.Width(70));
                m_LanguageIndex = EditorGUILayout.Popup(m_LanguageIndex, m_LanguageOptions, GUILayout.Width(70));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(490)))
                {
                    DrawMixerPanel();
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    DrawSeparatorPanel();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Create a general header GUIStyle.
        /// </summary>
        private GUIStyle Style
        {
            get
            {
                if (m_Style != null) return m_Style;
                m_Style = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal =
                    {
                        textColor = EditorGUIUtility.isProSkin
                            ? new Color(0.85f, 0.85f, 0.85f)
                            : Color.black
                    }
                };

                return m_Style;
            }
        }

        /// <summary>
        /// Saves a texture as a PNG file and imports it into Unity.
        /// </summary>
        private static void SaveTexture(Texture2D texture, string directory, string fileName)
        {
            var textureBytes = texture.EncodeToPNG();
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllBytes(filePath, textureBytes);
            AssetDatabase.ImportAsset(filePath);
            Debug.Log("Saved: " + filePath);
        }

        #region Mixer

        private void DrawMixerPanel()
        {
            if (!m_Mat)
            {
                GUILayout.Label("Error: Missing shader 'Hidden/TextureMixer'!");
                return;
            }

            EditorGUILayout.LabelField(GetLocalizedText("Input Textures"), Style);
            GetMixerInput();

            EditorGUILayout.LabelField(GetLocalizedText("Preview"), Style);
            PreviewOutput();
            GenerateTextureButton();
        }

        /// <summary>
        /// Draws input texture assignment and output options.
        /// </summary>
        private void GetMixerInput()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(GetLocalizedText("Mode"), GUILayout.Width(60));
            string[] modeLabels = { GetLocalizedText("Channels (RGBA)"), GetLocalizedText("RGB + Alpha") };
            m_MixerMode = (MixerMode)GUILayout.Toolbar((int)m_MixerMode, modeLabels);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (m_MixerMode == MixerMode.Channels)
            {
                m_Metallic = DrawTextureChannel(
                    "R: " + GetLocalizedText("Metallic"),
                    m_Metallic
                );

                m_Occlusion = DrawTextureChannel(
                    "G: " + GetLocalizedText("Occlusion"),
                    m_Occlusion
                );

                m_DetailMask = DrawTextureChannel(
                    "B: " + GetLocalizedText("Detail Mask"),
                    m_DetailMask
                );

                m_Smoothness = DrawTextureChannel(
                    "A: " + GetLocalizedText("Smoothness"),
                    m_Smoothness
                );
            }
            else
            {
                m_RGBSource = DrawTextureChannel(
                    "RGB: " + GetLocalizedText("Color"),
                    m_RGBSource
                );

                m_AlphaSource = DrawTextureChannel(
                    "A: " + GetLocalizedText("Alpha"),
                    m_AlphaSource
                );
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Auto-populate filename from the primary source. Track the last
            // value we filled so we can detect user edits — if the user has
            // typed something different we leave it alone, but if the filename
            // is empty OR still matches the last auto-fill, refresh it from
            // the current source. Lets the field follow texture swaps until
            // the user takes ownership by editing it.
            {
                var src = PrimaryFileNameSource();
                if (src != null)
                {
                    var srcPath = AssetDatabase.GetAssetPath(src);
                    if (!string.IsNullOrEmpty(srcPath))
                    {
                        var srcName = Path.GetFileNameWithoutExtension(srcPath);
                        bool userOwns = !string.IsNullOrEmpty(m_MixerFileName)
                                        && m_MixerFileName != m_LastAutoFilledName;
                        if (!userOwns && srcName != m_MixerFileName)
                        {
                            m_MixerFileName = srcName;
                            m_LastAutoFilledName = srcName;
                        }
                    }
                }
            }

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            m_MixerUseDefaultPath = EditorGUILayout.Toggle(GetLocalizedText("Use Default Path"), m_MixerUseDefaultPath);
            if (!m_MixerUseDefaultPath)
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(
                        GetLocalizedText("Select Folder"),
                        GUILayout.Width(150),
                        GUILayout.Height(20)))
                {
                    var folder = EditorUtility.SaveFolderPanel(
                        GetLocalizedText("Select Folder"),
                        Application.dataPath,
                        "");

                    if (!string.IsNullOrEmpty(folder))
                    {
                        m_MixerCustomFolder = folder;
                        Debug.Log($"Selected Save Folder: {m_MixerCustomFolder}");
                    }
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();

            if (!m_MixerUseDefaultPath && !string.IsNullOrEmpty(m_MixerCustomFolder))
            {
                EditorGUILayout.LabelField(m_MixerCustomFolder, EditorStyles.miniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(GetLocalizedText("Filename"), GUILayout.Width(70));
            m_MixerFileName = EditorGUILayout.TextField(m_MixerFileName);
            GUILayout.Label(".png", GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            m_UseRoughness = EditorGUILayout.Toggle(GetLocalizedText("Use Roughness"), m_UseRoughness);
            GUILayout.FlexibleSpace();
            GUILayout.Label(GetLocalizedText("Inverse Smoothness (A channel)"), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// Source texture whose name seeds the editable Filename field — RGB
        /// source in RGB+A mode, Metallic (R) in channels mode.
        /// </summary>
        private Texture2D PrimaryFileNameSource()
        {
            return m_MixerMode == MixerMode.RGBPlusAlpha ? m_RGBSource : m_Metallic;
        }

        /// <summary>
        /// Draws a texture input field for a specific color channel.
        /// </summary>
        private static Texture2D DrawTextureChannel(string label, Texture2D texture)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label(label, EditorStyles.miniBoldLabel);

            var result = EditorGUILayout.ObjectField(
                texture,
                typeof(Texture2D),
                false,
                GUILayout.Width(80),
                GUILayout.Height(80)
            ) as Texture2D;

            EditorGUILayout.EndVertical();
            return result;
        }

        /// <summary>
        /// Draws preview area and refresh button.
        /// </summary>
        private void PreviewOutput()
        {
            EditorGUILayout.Space(5);

            const int previewSize = 258;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f));

            if (!m_Preview)
            {
                EditorGUI.DrawPreviewTexture(previewRect, Texture2D.grayTexture);
                GUI.Label(previewRect, GetLocalizedText("No Preview"), EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUI.DrawPreviewTexture(previewRect, m_Preview, null, ScaleMode.ScaleToFit);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(GetLocalizedText("Refresh Preview"), GUILayout.Width(150), GUILayout.Height(30)))
            {
                m_Preview = GenerateMaskTexture();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// Handles final texture generation and saving.
        /// </summary>
        private void GenerateTextureButton()
        {
            if (!GUILayout.Button(GetLocalizedText("Generate Texture"), GUILayout.Height(30))) return;
            var tex = GenerateMaskTexture();
            if (!tex) return;

            var savePath = GetMixerSavePath();
            if (string.IsNullOrEmpty(savePath))
            {
                EditorUtility.DisplayDialog("Error", GetLocalizedText("Invalid Save Path"), "OK");
                return;
            }

            if (string.IsNullOrEmpty(m_MixerFileName))
            {
                EditorUtility.DisplayDialog("Error", GetLocalizedText("Invalid Filename"), "OK");
                return;
            }

            SaveTexture(tex, savePath, m_MixerFileName + ".png");
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", GetLocalizedText("Texture Saved"), "OK");
        }

        /// <summary>
        /// Generates combined mask texture using GPU blit.
        /// </summary>
        private Texture2D GenerateMaskTexture()
        {
            Texture2D first = FirstMixerTexture();
            if (first == null)
            {
                EditorUtility.DisplayDialog("Error", GetLocalizedText("No Input Textures"), "OK");
                return null;
            }

            int width = first.width;
            int height = first.height;

            var tempRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear
            };
            var prevRT = RenderTexture.active;
            RenderTexture.active = tempRT;

            m_Mat.SetTexture(s_Metallic, m_Metallic);
            m_Mat.SetTexture(s_Occlusion, m_Occlusion);
            m_Mat.SetTexture(s_DetailMask, m_DetailMask);
            m_Mat.SetTexture(s_Smoothness, m_Smoothness);
            m_Mat.SetTexture(s_RGBSource, m_RGBSource);
            m_Mat.SetTexture(s_AlphaSource, m_AlphaSource);
            m_Mat.SetFloat(s_SwapRoughness, m_UseRoughness ? 1f : 0f);
            m_Mat.SetFloat(s_MixMode, m_MixerMode == MixerMode.RGBPlusAlpha ? 1f : 0f);

            Graphics.Blit(null, tempRT, m_Mat);

            var output = new Texture2D(width, height, TextureFormat.ARGB32, false);
            output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            output.Apply();

            RenderTexture.active = prevRT;
            tempRT.Release();
            DestroyImmediate(tempRT);

            return output;
        }

        /// <summary>
        /// Returns the first non-null input texture for the current mode, used to
        /// pick output dimensions and the default save path.
        /// </summary>
        private Texture2D FirstMixerTexture()
        {
            if (m_MixerMode == MixerMode.RGBPlusAlpha)
                return m_RGBSource != null ? m_RGBSource : m_AlphaSource;
            return m_Metallic ?? m_Occlusion ?? m_DetailMask ?? m_Smoothness;
        }

        /// <summary>
        /// Resolves output save directory based on current mixer settings.
        /// In default mode, uses the primary source texture's folder. In custom
        /// mode, returns whatever folder the user picked.
        /// </summary>
        private string GetMixerSavePath()
        {
            if (m_MixerUseDefaultPath)
            {
                var firstTex = FirstMixerTexture();
                var assetPath = AssetDatabase.GetAssetPath(firstTex);
                return string.IsNullOrEmpty(assetPath) ? null : Path.GetDirectoryName(assetPath);
            }
            return m_MixerCustomFolder;
        }

        #endregion

        #region Separator

        private void DrawSeparatorPanel()
        {
            EditorGUILayout.LabelField(GetLocalizedText("Input Texture"), Style);
            EditorGUILayout.Space(10);
            m_InputTexture =
                (Texture2D)EditorGUILayout.ObjectField(
                    m_InputTexture,
                    typeof(Texture2D),
                    false,
                    GUILayout.Width(80),
                    GUILayout.Height(80));

            EditorGUILayout.Space(10);
            m_SepUseDefaultPath = EditorGUILayout.Toggle(GetLocalizedText("Use Default Path"), m_SepUseDefaultPath);

            EditorGUILayout.Space(5);
            if (!m_SepUseDefaultPath)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(GetLocalizedText("Select Save Path"),
                        GUILayout.Height(30),
                        GUILayout.Width(150)))
                {
                    m_SepCustomPath = EditorUtility.SaveFolderPanel(
                        GetLocalizedText("Select Save Path"),
                        Application.dataPath,
                        "");

                    if (!string.IsNullOrEmpty(m_SepCustomPath))
                    {
                        Debug.Log($"Selected Save Path: {m_SepCustomPath}");
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            if (!m_InputTexture) return;

            EditorGUILayout.LabelField(GetLocalizedText("Output Channels"));
            EditorGUILayout.BeginHorizontal();
            bool allChecked = m_OutputR && m_OutputG && m_OutputB && m_OutputA;
            bool newAll = EditorGUILayout.ToggleLeft(GetLocalizedText("All"), allChecked, GUILayout.Width(60));
            if (newAll != allChecked)
                m_OutputR = m_OutputG = m_OutputB = m_OutputA = newAll;
            m_OutputR = EditorGUILayout.ToggleLeft("R", m_OutputR, GUILayout.Width(40));
            m_OutputG = EditorGUILayout.ToggleLeft("G", m_OutputG, GUILayout.Width(40));
            m_OutputB = EditorGUILayout.ToggleLeft("B", m_OutputB, GUILayout.Width(40));
            m_OutputA = EditorGUILayout.ToggleLeft("A", m_OutputA, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(GetLocalizedText("Save Channels"),
                    GUILayout.Height(30),
                    GUILayout.Width(150)))
            {
                SaveSeparatedChannels();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Separates the input texture into individual channel textures
        /// and saves them as grayscale PNG files.
        /// </summary>
        private void SaveSeparatedChannels()
        {
            if (!m_InputTexture)
            {
                Debug.LogError("No texture selected!");
                return;
            }

            var saveDirectory = m_SepUseDefaultPath
                ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(m_InputTexture))
                : m_SepCustomPath;

            if (string.IsNullOrEmpty(saveDirectory))
            {
                Debug.LogError("Save path is invalid.");
                return;
            }

            if (!m_OutputR && !m_OutputG && !m_OutputB && !m_OutputA)
            {
                EditorUtility.DisplayDialog("Error", GetLocalizedText("No Channel Selected"), "OK");
                return;
            }

            var pixels = m_InputTexture.GetPixels();
            var baseName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(m_InputTexture));

            if (m_OutputR) SaveTexture(CreateChannelTexture(pixels, 0), saveDirectory, baseName + "_R.png");
            if (m_OutputG) SaveTexture(CreateChannelTexture(pixels, 1), saveDirectory, baseName + "_G.png");
            if (m_OutputB) SaveTexture(CreateChannelTexture(pixels, 2), saveDirectory, baseName + "_B.png");
            if (m_OutputA) SaveTexture(CreateChannelTexture(pixels, 3), saveDirectory, baseName + "_A.png");
        }

        /// <summary>
        /// Creates a grayscale texture from a specific color channel.
        /// </summary>
        private Texture2D CreateChannelTexture(Color[] pixels, int channelIndex)
        {
            var newPixels = new Color[pixels.Length];

            for (var i = 0; i < pixels.Length; i++)
            {
                var channelValue = channelIndex switch
                {
                    0 => pixels[i].r,
                    1 => pixels[i].g,
                    2 => pixels[i].b,
                    3 => pixels[i].a,
                    _ => 0
                };

                newPixels[i] = new Color(channelValue, channelValue, channelValue);
            }

            var channelTexture = new Texture2D(m_InputTexture.width, m_InputTexture.height);
            channelTexture.SetPixels(newPixels);
            channelTexture.Apply();
            return channelTexture;
        }

        #endregion

        #region Localization

        private string GetLocalizedText(string key)
        {
            if (Language == "中文")
            {
                return key switch
                {
                    "Input Textures" => "输入纹理",
                    "Input Texture" => "输入纹理",
                    "Metallic" => "金属度",
                    "Occlusion" => "环境遮蔽",
                    "Detail Mask" => "细节遮罩",
                    "Smoothness" => "光滑度",
                    "Language" => "语言",
                    "Use Default Path" => "使用默认路径",
                    "Use Roughness" => "使用粗糙度",
                    "Inverse Smoothness (A channel)" => "反转光滑度（A通道）",
                    "Select Save Path" => "选择保存路径",
                    "Preview" => "预览",
                    "No Preview" => "无预览",
                    "Refresh Preview" => "刷新预览",
                    "Generate Texture" => "生成纹理",
                    "Invalid Save Path" => "无效的保存路径",
                    "No Input Textures" => "无输入纹理",
                    "Texture Saved" => "纹理已保存",
                    "Save Channels" => "保存通道",
                    "Output Channels" => "输出通道",
                    "All" => "全部",
                    "No Channel Selected" => "未选择任何通道",
                    "Mode" => "模式",
                    "Channels (RGBA)" => "通道（RGBA）",
                    "RGB + Alpha" => "RGB + 透明",
                    "Color" => "颜色",
                    "Alpha" => "透明",
                    "Select Folder" => "选择文件夹",
                    "Filename" => "文件名",
                    "Invalid Filename" => "无效的文件名",
                    _ => key
                };
            }

            return key;
        }

        #endregion
    }
}
