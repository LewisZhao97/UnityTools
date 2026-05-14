using UnityEditor;
using UnityEngine;
using System.IO;

namespace Lewiszhao.Unitytools.Editor
{
    /// <summary>
    /// Texture channel separation tool for Unity Editor.
    /// <para>
    /// <see cref="TextureChannelSeparator"/> allows users to split a single RGBA texture
    /// into four individual grayscale textures, each representing one color channel
    /// (R, G, B, A).
    /// </para>
    /// <para>
    /// Typical use cases include:
    /// <list type="bullet">
    /// <item>Extracting packed PBR mask textures</item>
    /// <item>Debugging individual texture channels</item>
    /// <item>Converting channel-packed textures back to standalone maps</item>
    /// </list>
    /// </para>
    /// </summary>
    public class TextureChannelSeparator : EditorWindow
    {
        /// <summary>
        /// Source texture to be separated into individual channels.
        /// </summary>
        private Texture2D m_InputTexture;

        /// <summary>
        /// Whether to save separated textures to the same directory as the input texture.
        /// </summary>
        private bool m_UseDefaultPath = true;

        /// <summary>
        /// Custom save path selected by user.
        /// </summary>
        private string m_CustomPath = "";

        /// <summary>
        /// Current UI language index.
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
        /// Header GUI style.
        /// </summary>
        private GUIStyle m_Style;

        /// <summary>
        /// Opens the Texture Channel Separator editor window.
        /// </summary>
        [MenuItem("Tools/Texture Maker Tools/Texture Channel Separator", priority = 2)]
        public static void ShowTextureSeparatorWindow()
        {
            var window = GetWindow<TextureChannelSeparator>(false, "Texture Channel Separator");
            window.titleContent.text = "Texture Channel Separator";
            window.minSize = new Vector2(350, 241);
            window.Show();
        }

        /// <summary>
        /// Draws the main editor GUI.
        /// Handles input texture selection, save path options,
        /// and channel separation execution.
        /// </summary>
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
                        m_CustomPath = EditorUtility.SaveFolderPanel(
                            GetLocalizedText("Select Save Path"),
                            Application.dataPath,
                            "");

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

        /// <summary>
        /// Creates a grayscale texture from a specific color channel.
        /// </summary>
        /// <param name="pixels">
        /// Pixel array of the source texture.
        /// </param>
        /// <param name="channelIndex">
        /// Color channel index:
        /// <list type="bullet">
        /// <item>0 = Red</item>
        /// <item>1 = Green</item>
        /// <item>2 = Blue</item>
        /// <item>3 = Alpha</item>
        /// </list>
        /// </param>
        /// <returns>
        /// A new <see cref="Texture2D"/> containing the extracted channel as grayscale.
        /// </returns>
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