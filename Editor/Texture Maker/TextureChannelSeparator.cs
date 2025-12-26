using UnityEditor;
using UnityEngine;
using System.IO;

namespace Lewiszhao.Unitytools.Editor
{
    public class TextureChannelSeparator : EditorWindow
    {
        private Texture2D m_InputTexture;
        private bool m_UseDefaultPath = true;
        private string m_CustomPath = "";
        private int m_LanguageIndex;
        private readonly string[] m_LanguageOptions = { "English", "中文" };
        private string Language => m_LanguageOptions[m_LanguageIndex];
        private GUIStyle m_Style;

        [MenuItem("Tools/Texture Maker Tools/Texture Channel Separator", priority = 2)]
        public static void ShowTextureSeparatorWindow()
        {
            var window = GetWindow<TextureChannelSeparator>(false, "Texture Channel Separator");
            window.titleContent.text = "Texture Channel Separator";
            window.minSize = new Vector2(350, 241);
            window.Show();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    GetLocalizedText("Input Texture"), Style, GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(GetLocalizedText("Language"), GUILayout.Width(70));
                m_LanguageIndex = EditorGUILayout.Popup(m_LanguageIndex, m_LanguageOptions, GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(10);
                m_InputTexture =
                    (Texture2D)EditorGUILayout.ObjectField(
                        m_InputTexture,
                        typeof(Texture2D),
                        false,
                        GUILayout.Width(80),
                        GUILayout.Height(80));
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);
                m_UseDefaultPath = EditorGUILayout.Toggle(GetLocalizedText("Use Default Path"), m_UseDefaultPath);

                EditorGUILayout.Space(5);
                if (!m_UseDefaultPath)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(GetLocalizedText("Select Save Path"),
                            GUILayout.Height(30),
                            GUILayout.Width(150)))
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
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(5);

                if (!m_InputTexture) return;
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

        private void SaveSeparatedChannels()
        {
            if (!m_InputTexture)
            {
                Debug.LogError("No texture selected!");
                return;
            }

            var saveDirectory = m_UseDefaultPath
                ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(m_InputTexture))
                : m_CustomPath;

            if (string.IsNullOrEmpty(saveDirectory))
            {
                Debug.LogError("Save path is invalid.");
                return;
            }

            var pixels = m_InputTexture.GetPixels();

            var rChannel = CreateChannelTexture(pixels, 0);
            var gChannel = CreateChannelTexture(pixels, 1);
            var bChannel = CreateChannelTexture(pixels, 2);
            var aChannel = CreateChannelTexture(pixels, 3);

            var baseName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(m_InputTexture));

            SaveTexture(rChannel, saveDirectory, baseName + "_R.png");
            SaveTexture(gChannel, saveDirectory, baseName + "_G.png");
            SaveTexture(bChannel, saveDirectory, baseName + "_B.png");
            SaveTexture(aChannel, saveDirectory, baseName + "_A.png");
        }

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

        private static void SaveTexture(Texture2D texture, string directory, string fileName)
        {
            var textureBytes = texture.EncodeToPNG();
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllBytes(filePath, textureBytes);
            AssetDatabase.ImportAsset(filePath);
            Debug.Log("Saved: " + filePath);
        }

        #region Localization

        private string GetLocalizedText(string key)
        {
            if (Language == "中文")
            {
                return key switch
                {
                    "Input Texture" => "输入纹理",
                    "Use Default Path" => "使用默认路径",
                    "Language" => "语言",
                    "Select Save Path" => "选择保存路径",
                    "Custom Save Path: " => "自定义保存路径： ",
                    "Save Channels" => "保存通道",
                    _ => key
                };
            }

            return key;
        }

        #endregion
    }
}