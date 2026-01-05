using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lewiszhao.Unitytools.Editor
{
    /// <summary>
    /// Texture channel packing tool for Unity Editor.
    /// <para>
    /// <see cref="TextureChannelMixer"/> allows users to combine multiple grayscale textures
    /// into a single RGBA texture by assigning each input texture to a specific color channel.
    /// </para>
    /// <para>
    /// Typical use cases include:
    /// <list type="bullet">
    /// <item>Packing Metallic / Occlusion / DetailMask / Smoothness maps</item>
    /// <item>Creating optimized PBR mask textures</item>
    /// <item>Reducing texture count for performance optimization</item>
    /// </list>
    /// </para>
    /// <para>
    /// The tool provides real-time preview, optional roughness inversion,
    /// and flexible output path control.
    /// </para>
    /// </summary>
    public class TextureChannelMixer : EditorWindow
    {
        /// <summary>
        /// Shader property ID for metallic channel input. (R channel)
        /// </summary>
        private static readonly int s_Metallic = Shader.PropertyToID("_Metallic");

        /// <summary>
        /// Shader property ID for occlusion channel input. (G channel)
        /// </summary>
        private static readonly int s_Occlusion = Shader.PropertyToID("_Occlusion");

        /// <summary>
        /// Shader property ID for detail mask channel input. (B channel)
        /// </summary>
        private static readonly int s_DetailMask = Shader.PropertyToID("_DetailMask");

        /// <summary>
        /// Shader property ID for smoothness channel input. (A channel)
        /// </summary>
        private static readonly int s_Smoothness = Shader.PropertyToID("_Smoothness");

        /// <summary>
        /// Shader property ID used to toggle roughness inversion.
        /// </summary>
        private static readonly int s_SwapRoughness = Shader.PropertyToID("_SwapRoughness");

        /// <summary>
        /// Input textures.
        /// </summary>
        private Texture2D m_Metallic, m_Occlusion, m_DetailMask, m_Smoothness;

        /// <summary>
        /// Internal material using hidden texture mixing shader.
        /// </summary>
        private Material m_Mat;

        /// <summary>
        /// Generated preview texture.
        /// </summary>
        private Texture2D m_Preview;

        /// <summary>
        /// Whether to save generated texture to the same directory as input textures.
        /// </summary>
        private bool m_UseDefaultPath = true;

        /// <summary>
        /// Custom save path selected by user.
        /// </summary>
        private string m_CustomPath = "";

        /// <summary>
        /// If true, smoothness channel will be inverted and treated as roughness.
        /// </summary>
        private bool m_UseRoughness;

        /// <summary>
        /// Current language selection index.
        /// </summary>
        private int m_LanguageIndex;

        /// <summary>
        /// Supported UI language options.
        /// </summary>
        private readonly string[] m_LanguageOptions = { "English", "中文" };

        /// <summary>
        /// Current UI language.
        /// </summary>
        private string Language => m_LanguageOptions[m_LanguageIndex];

        /// <summary>
        /// Allows you to scroll down if the content exceeds the boundaries.
        /// </summary>
        private Vector2 m_ScrollPosition;

        /// <summary>
        /// Header GUI style.
        /// </summary>
        private GUIStyle m_Style;

        /// <summary>
        /// Opens the Texture Channel Mixer editor window.
        /// </summary>
        [MenuItem("Tools/Texture Maker Tools/Texture Channel Mixer", priority = 1)]
        private static void ShowTextureMakerWindow()
        {
            var window = GetWindow<TextureChannelMixer>(false, "Texture Channel Mixer");
            window.titleContent.text = "Texture Channel Mixer";
            window.minSize = new Vector2(600, 630);
            window.Show();
        }

        /// <summary>
        /// Initializes internal material using hidden mixing shader.
        /// </summary>
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

        /// <summary>
        /// Draws the main editor GUI.
        /// Handles input texture assignment, preview, and texture generation.
        /// </summary>
        private void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            if (!m_Mat)
            {
                GUILayout.Label("Error: Missing shader 'Hidden/TextureMixer'!");
                return;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(GetLocalizedText("Input Textures"), Style);
                GUILayout.FlexibleSpace();
                GUILayout.Label(GetLocalizedText("Language"), GUILayout.Width(70));
                m_LanguageIndex = EditorGUILayout.Popup(m_LanguageIndex, m_LanguageOptions, GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();
                GetInput();
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(GetLocalizedText("Preview"), Style);
                PreviewOutput();
                GenerateTextureButton();
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

        #region Input Settings

        /// <summary>
        /// Draws input texture assignment and output options.
        /// </summary>
        private void GetInput()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

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

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            m_UseDefaultPath = EditorGUILayout.Toggle(GetLocalizedText("Use Default Path"), m_UseDefaultPath);
            if (!m_UseDefaultPath)
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(
                        GetLocalizedText("Select Save Path"),
                        GUILayout.Width(150),
                        GUILayout.Height(20)))
                {
                    m_CustomPath = EditorUtility.SaveFilePanel(
                        GetLocalizedText("Select Save Path"),
                        Application.dataPath,
                        "Texture.png",
                        "png");

                    if (!string.IsNullOrEmpty(m_CustomPath))
                    {
                        Debug.Log($"Selected Save Path: {m_CustomPath}");
                    }
                }

                GUILayout.FlexibleSpace();
            }

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
        /// Draws a texture input field for a specific color channel.
        /// </summary>
        /// <param name="label">Channel label.</param>
        /// <param name="texture">Current texture.</param>
        /// <returns>Updated texture reference.</returns>
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

        #endregion

        #region Preview and Generate

        /// <summary>
        /// Draws preview area and refresh button.
        /// </summary>
        private void PreviewOutput()
        {
            EditorGUILayout.Space(5);

            const int previewSize = 258;

            var previewX = (position.width - previewSize) / 2;
            var previewY = GUILayoutUtility.GetLastRect().yMax + 10;
            var previewRect = new Rect(previewX, previewY, previewSize, previewSize);

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

            EditorGUILayout.Space(previewSize + 35);
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

            var savePath = GetSavePath();
            if (string.IsNullOrEmpty(savePath))
            {
                EditorUtility.DisplayDialog("Error", GetLocalizedText("Invalid Save Path"), "OK");
                return;
            }

            var texName = m_UseDefaultPath
                ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(m_Metallic))
                : Path.GetFileNameWithoutExtension(m_CustomPath);
            SaveTexture(tex, savePath, texName + ".png");
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", GetLocalizedText("Texture Saved"), "OK");
        }

        /// <summary>
        /// Generates combined mask texture using GPU blit.
        /// </summary>
        /// <returns>Generated RGBA texture.</returns>
        private Texture2D GenerateMaskTexture()
        {
            if (!m_Metallic && !m_Occlusion && !m_DetailMask && !m_Smoothness)
            {
                EditorUtility.DisplayDialog("Error", GetLocalizedText("No Input Textures"), "OK");
                return null;
            }

            int width = 2048, height = 2048;
            if (m_Metallic)
            {
                width = m_Metallic.width;
                height = m_Metallic.height;
            }
            else if (m_Occlusion)
            {
                width = m_Occlusion.width;
                height = m_Occlusion.height;
            }
            else if (m_DetailMask)
            {
                width = m_DetailMask.width;
                height = m_DetailMask.height;
            }
            else if (m_Smoothness)
            {
                width = m_Smoothness.width;
                height = m_Smoothness.height;
            }

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
            m_Mat.SetFloat(s_SwapRoughness, m_UseRoughness ? 1f : 0f);

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
        /// Saves a texture as a PNG file and imports it into Unity.
        /// </summary>
        /// <param name="texture">
        /// Texture to be saved.
        /// </param>
        /// <param name="directory">
        /// Target directory path.
        /// </param>
        /// <param name="fileName">
        /// Output file name including extension.
        /// </param>
        private static void SaveTexture(Texture2D texture, string directory, string fileName)
        {
            var textureBytes = texture.EncodeToPNG();
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllBytes(filePath, textureBytes);
            AssetDatabase.ImportAsset(filePath);
            Debug.Log("Saved: " + filePath);
        }

        /// <summary>
        /// Resolves output save directory based on current settings.
        /// </summary>
        /// <returns>Directory path.</returns>
        private string GetSavePath()
        {
            var firstTex = m_Metallic ?? m_Occlusion ?? m_DetailMask ?? m_Smoothness;
            var assetPath = AssetDatabase.GetAssetPath(firstTex);
            var saveDirectory = m_UseDefaultPath
                ? Path.GetDirectoryName(assetPath)
                : Path.GetDirectoryName(m_CustomPath);
            return saveDirectory;
        }

        #endregion

        #region Localization

        /// <summary>
        /// Returns localized UI text based on current language selection.
        /// </summary>
        /// <param name="key">Original English key.</param>
        /// <returns>Localized string.</returns>
        private string GetLocalizedText(string key)
        {
            if (Language == "中文")
            {
                return key switch
                {
                    "Input Textures" => "输入纹理",
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
                    _ => key
                };
            }

            return key;
        }

        #endregion

        /// <summary>
        /// Cleans up temporary resources when window is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (m_Mat != null) DestroyImmediate(m_Mat);
            if (m_Preview != null) DestroyImmediate(m_Preview);
        }
    }
}