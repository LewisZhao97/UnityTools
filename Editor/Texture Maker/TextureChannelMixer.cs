using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lewiszhao.Unitytools.Editor
{
    public class TextureChannelMixer : EditorWindow
    {
        private static readonly int s_Metallic = Shader.PropertyToID("_Metallic");
        private static readonly int s_Occlusion = Shader.PropertyToID("_Occlusion");
        private static readonly int s_DetailMask = Shader.PropertyToID("_DetailMask");
        private static readonly int s_Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int s_SwapRoughness = Shader.PropertyToID("_SwapRoughness");

        private Texture2D m_Metallic, m_Occlusion, m_DetailMask, m_Smoothness;
        private Material m_Mat;
        private Texture2D m_Preview;

        private bool m_UseDefaultPath = true;
        private string m_CustomPath = "";
        private bool m_UseRoughness;

        private int m_LanguageIndex;
        private readonly string[] m_LanguageOptions = { "English", "中文" };
        private string Language => m_LanguageOptions[m_LanguageIndex];
        private Vector2 m_ScrollPosition;
        private GUIStyle m_Style;

        [MenuItem("Tools/Texture Maker Tools/Texture Channel Mixer", priority = 1)]
        private static void ShowTextureMakerWindow()
        {
            var window = GetWindow<TextureChannelMixer>(false, "Texture Channel Mixer");
            window.titleContent.text = "Texture Channel Mixer";
            window.minSize = new Vector2(600, 500);
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

        private static Texture2D DrawTextureChannel(string label, Texture2D current)
        {
            EditorGUI.BeginChangeCheck();
            var newTex = TextureField(label, current);
            return EditorGUI.EndChangeCheck() ? newTex : current;
        }

        private static Texture2D TextureField(string label, Texture2D texture)
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

        private static void SaveTexture(Texture2D texture, string directory, string fileName)
        {
            var textureBytes = texture.EncodeToPNG();
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllBytes(filePath, textureBytes);
            AssetDatabase.ImportAsset(filePath);
            Debug.Log("Saved: " + filePath);
        }

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

        private string GetLocalizedText(string key)
        {
            if (Language == "中文")
            {
                return key switch
                {
                    "Input Textures" => "输入纹理",
                    "Metallic" => "金属度",
                    "Occlusion" => "环境遮蔽",
                    "Detail Mask" => "细节蒙版",
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

        private void OnDestroy()
        {
            if (m_Mat != null) DestroyImmediate(m_Mat);
            if (m_Preview != null) DestroyImmediate(m_Preview);
        }
    }
}