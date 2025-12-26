using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lewiszhao.Unitytools.Editor
{
    /// <summary>
    /// Ramp map creation tool for Unity Editor.
    /// <para>
    /// This editor window allows artists and technical artists to create custom ramp textures
    /// directly inside Unity using multiple <see cref="Gradient"/> definitions.
    /// </para>
    /// <para>
    /// The tool supports:
    /// <list type="bullet">
    /// <item>Multiple ramp rows generated from gradient list</item>
    /// <item>Real-time preview inside the editor window</item>
    /// <item>Previewing the ramp texture on a user-assigned material</item>
    /// <item>Saving ramp textures to common image formats (TGA / PNG / JPG)</item>
    /// <item>Saving and loading ramp configuration via <see cref="RampMapData"/> assets</item>
    /// </list>
    /// </para>
    /// <para>
    /// This tool is mainly designed for cartoon shading, stylized lighting,
    /// or any shading workflow that relies on ramp textures.
    /// </para>
    /// </summary>
    public class RampMapCreator : EditorWindow
    {
        #region Fields and Properties

        /// <summary>
        /// Shader property ID for preview ramp texture.
        /// </summary>
        private static readonly int s_RampPreviewTex = Shader.PropertyToID("_RampPreviewTex");

        /// <summary>
        /// Shader property ID for ramp sampling step.
        /// </summary>
        private static readonly int s_SampleRoll = Shader.PropertyToID("_SampleRoll");

        /// <summary>
        /// Shader property ID for total ramp row count.
        /// </summary>
        private static readonly int s_RollNum = Shader.PropertyToID("_RollNum");

        /// <summary>
        /// Width (in pixels) of each ramp row.
        /// </summary>
        private int m_RampMapWidth = 32;

        /// <summary>
        /// Height (in pixels) of each ramp row.
        /// </summary>
        private int m_RampMapHeight = 4;

        /// <summary>
        /// Generated ramp texture used for preview and saving.
        /// </summary>
        private Texture2D m_RampMap;

        /// <summary>
        /// Output ramp texture file name (without extension).
        /// </summary>
        private string m_RampName = "NewRampMap";

        /// <summary>
        /// Supported output texture formats.
        /// </summary>
        private readonly string[] m_MapFormat = { "TGA", "PNG", "JPG" };

        /// <summary>
        /// Selected format index.
        /// </summary>
        private int m_MapIndex;

        /// <summary>
        /// File extension string resolved from selected format.
        /// </summary>
        private string m_Format = ".tga";

        /// <summary>
        /// The preview texture displayed on the right.
        /// </summary>
        private Texture2D m_PreviewTex;

        /// <summary>
        /// Preview scale multiplier for displaying ramp texture in editor.
        /// </summary>
        private float m_PreWidth = 1;

        /// <summary>
        /// Number of ramp rows used when previewing on material.
        /// </summary>
        private int m_Roll = 4;

        /// <summary>
        /// Texture wrap mode applied when importing saved ramp texture.
        /// </summary>
        private TextureWrapMode m_TextureWrapMode = TextureWrapMode.Clamp;

        /// <summary>
        /// Texture filter mode applied when importing saved ramp texture.
        /// </summary>
        private FilterMode m_FilterMode = FilterMode.Point;

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

        private GUIStyle m_Style;

        /// <summary>
        /// Gradient list used to generate each ramp row.
        /// Each gradient corresponds to one horizontal strip in the final ramp texture.
        /// </summary>
        [SerializeField] protected List<Gradient> m_Gradients = new();

        /// <summary>
        /// Material used for ramp preview inside the editor.
        /// </summary>
        [SerializeField] protected Material m_PreviewMaterial;

        /// <summary>
        /// SerializedObject wrapper for gradient list editing.
        /// </summary>
        private SerializedObject m_SerializedGradientObject;

        /// <summary>
        /// SerializedProperty reference to gradient list.
        /// </summary>
        private SerializedProperty m_GradientProperty;

        #endregion

        /// <summary>
        /// Opens the Ramp Map Creator editor window.
        /// </summary>
        [MenuItem("Tools/Ramp Map Tools/Create Ramp Texture", priority = 1),
         MenuItem("Assets/Create/Ramp Map Tools/Create from asset/Create Ramp Texture", priority = 120)]
        private static void ShowRampGeneratorWindow()
        {
            var window = GetWindow<RampMapCreator>(false, "Ramp Map Creator");
            window.titleContent = new GUIContent("Ramp Map Creator");
            window.minSize = new Vector2(630, 400);
            window.Show();
        }

        /// <summary>
        /// Initializes serialized properties and loads data
        /// from selected <see cref="RampMapData"/> asset if available.
        /// </summary>
        private void OnEnable()
        {
            m_Gradients ??= new List<Gradient>();

            m_SerializedGradientObject = new SerializedObject(this);
            m_GradientProperty = m_SerializedGradientObject.FindProperty("m_Gradients");

            if (Selection.activeObject == null) return;
            var obj = Selection.activeObject;

            if (obj is RampMapData asset)
            {
                m_RampName = asset.RampMapName;
                m_RampMapWidth = asset.RampMapWidth;
                m_RampMapHeight = asset.RampMapHeight;
                m_Gradients = new List<Gradient>(asset.Gradients);
            }
            else
            {
                Debug.LogWarning("Selected asset file is not a RampMapData");
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
        /// Draws the main editor window GUI.
        /// The layout is divided into configuration panel (left)
        /// and preview panel (right).
        /// </summary>
        private void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(270)))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(GetLocalizedText("Configurations"), Style, GUILayout.MinWidth(0));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(GetLocalizedText("Language"), GUILayout.Width(70));
                    m_LanguageIndex = EditorGUILayout.Popup(m_LanguageIndex, m_LanguageOptions, GUILayout.Width(70));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2);
                    DrawAssetGUI();
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField(GetLocalizedText("Preview"), Style);
                    m_RampMap = CreateRamp();
                    SceneView.RepaintAll();

                    if (m_RampMap)
                    {
                        PreviewTex();
                    }

                    Save();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws configuration GUI including:
        /// gradient list, texture size, config save/load,
        /// and preview settings.
        /// </summary>
        private void DrawAssetGUI()
        {
            m_SerializedGradientObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_GradientProperty);

            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedGradientObject.ApplyModifiedProperties();
            }

            m_RampMapWidth = EditorGUILayout.IntField(GetLocalizedText("Width for each line"), m_RampMapWidth);
            m_RampMapHeight = EditorGUILayout.IntField(GetLocalizedText("Height for each line"), m_RampMapHeight);

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(GetLocalizedText("Load Config"), GUILayout.Width(130)))
            {
                var path = EditorUtility.OpenFilePanel(GetLocalizedText("Load RampMapData"), Application.dataPath,
                    "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    LoadConfig(path);
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(GetLocalizedText("Save Config"), GUILayout.Width(130)))
            {
                var path = EditorUtility.SaveFilePanel(GetLocalizedText("Save RampMapData"), Application.dataPath,
                    "NewRampMapData",
                    "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    SaveConfig(path);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField(GetLocalizedText("Preview Ramp"), EditorStyles.boldLabel);

            var labelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = 100;
            m_PreWidth = EditorGUILayout.Slider(GetLocalizedText("Preview Width"), m_PreWidth, 0.1f, 15);
            m_Roll = EditorGUILayout.IntSlider(GetLocalizedText("Preview Roll"), m_Roll, 1, m_Gradients.Count);
            EditorGUIUtility.labelWidth = labelWidth;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(GetLocalizedText("Preview Material"), EditorStyles.boldLabel);
            m_PreviewMaterial = (Material)EditorGUILayout.ObjectField(
                m_PreviewMaterial,
                typeof(Material),
                false);

            if (m_Gradients.Count <= 0)
                return;

            if (!m_PreviewMaterial)
                return;

            m_PreviewMaterial.SetInt(s_RollNum, m_Gradients.Count);
            m_PreviewMaterial.SetInt(s_SampleRoll, m_Roll);
            m_PreviewMaterial.SetTexture(s_RampPreviewTex, m_RampMap);
        }

        /// <summary>
        /// Generates a temporary ramp texture based on current gradient list.
        /// Each gradient is converted into one horizontal strip in the texture.
        /// </summary>
        /// <returns>
        /// A newly generated <see cref="Texture2D"/> used for preview and export.
        /// </returns>
        private Texture2D CreateRamp()
        {
            if (m_Gradients == null || m_Gradients.Count == 0)
            {
                return new Texture2D(1, 1);
            }

            var ramp = new Texture2D(m_RampMapWidth, m_RampMapHeight * m_Gradients.Count, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };

            var cols = new Color[m_RampMapWidth];

            var inv = 1f / (m_RampMapWidth - 1);

            for (var i = 0; i < m_Gradients.Count; i++)
            {
                if (m_Gradients[i] == null) continue;

                var start = m_RampMapHeight * i;
                var end = start + m_RampMapHeight;

                for (var x = 0; x < m_RampMapWidth; x++)
                {
                    var t = x * inv;
                    cols[x] = m_Gradients[i].Evaluate(t);
                }

                for (var y = start; y < end; y++)
                {
                    ramp.SetPixels(0, y, m_RampMapWidth, 1, cols);
                }
            }

            ramp.wrapMode = TextureWrapMode.Clamp;
            ramp.filterMode = FilterMode.Point;
            ramp.Apply();
            return ramp;
        }

        /// <summary>
        /// Draws the ramp texture preview inside the editor window.
        /// </summary>
        private void PreviewTex()
        {
            var rect = EditorGUILayout.GetControlRect(true, m_RampMapHeight * m_Gradients.Count * m_PreWidth);
            EditorGUI.DrawPreviewTexture(rect, m_RampMap);
        }

        /// <summary>
        /// Draws save options and handles ramp texture export.
        /// The texture will be saved and reimported with specified import settings.
        /// </summary>
        private void Save()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(GetLocalizedText("Texture Name:"), GUILayout.Width(90));
            m_RampName = EditorGUILayout.TextField(m_RampName);

            EditorGUILayout.LabelField(GetLocalizedText("Format:"), GUILayout.Width(60));
            m_MapIndex = EditorGUILayout.Popup(m_MapIndex, m_MapFormat, GUILayout.Width(60));

            m_Format = m_MapIndex switch
            {
                0 => ".tga",
                1 => ".png",
                2 => ".jpg",
                _ => m_Format
            };
            EditorGUILayout.EndHorizontal();

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.Space(15);
            m_TextureWrapMode =
                (TextureWrapMode)EditorGUILayout.EnumPopup(GetLocalizedText("Wrap Mode:"), m_TextureWrapMode,
                    GUILayout.Width(170));
            m_FilterMode = (FilterMode)EditorGUILayout.EnumPopup(GetLocalizedText("Filter Mode:"), m_FilterMode,
                GUILayout.Width(170));
            EditorGUIUtility.labelWidth = labelWidth;

            EditorGUILayout.Space(15);

            if (!GUILayout.Button(GetLocalizedText("Save Texture"), GUILayout.Height(30))) return;
            var path = EditorUtility.SaveFolderPanel(GetLocalizedText("Save Texture"), "", "");
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Save path is invalid.");
                return;
            }

            var imgData = m_MapIndex switch
            {
                0 => m_RampMap.EncodeToTGA(),
                1 => m_RampMap.EncodeToPNG(),
                2 => m_RampMap.EncodeToJPG(),
                _ => null
            };

            var filePath = Path.Combine(path, m_RampName + m_Format);
            if (imgData != null) File.WriteAllBytes(filePath, imgData);

            if (filePath.StartsWith(Application.dataPath))
            {
                var relativePath = "Assets" + filePath[Application.dataPath.Length..];
                AssetDatabase.Refresh();

                var importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.wrapMode = m_TextureWrapMode;
                    importer.filterMode = m_FilterMode;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }

                AssetDatabase.Refresh();
                Debug.Log("Texture saved successful: " + relativePath);
            }
            else
            {
                Debug.LogWarning("The texture must be saved in the project's Assets directory.");
            }
        }

        /// <summary>
        /// Loads ramp configuration from a <see cref="RampMapData"/> asset.
        /// </summary>
        /// <param name="path">Absolute file path to asset.</param>
        private void LoadConfig(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            var relativePath = "Assets" + path[Application.dataPath.Length..];
            var asset = AssetDatabase.LoadAssetAtPath<RampMapData>(relativePath);

            if (asset)
            {
                m_RampName = asset.RampMapName;
                m_RampMapWidth = asset.RampMapWidth;
                m_RampMapHeight = asset.RampMapHeight;
                m_Gradients = new List<Gradient>(asset.Gradients);
                m_SerializedGradientObject.Update();
            }
            else
            {
                Debug.LogError("Load failed: Invalid ramp map data file.");
            }
        }

        /// <summary>
        /// Saves current ramp configuration as a <see cref="RampMapData"/> asset.
        /// </summary>
        /// <param name="path">Absolute file path to save asset.</param>
        private void SaveConfig(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            var relativePath = "Assets" + path[Application.dataPath.Length..];
            var asset = CreateInstance<RampMapData>();

            asset.RampMapName = m_RampName;
            asset.RampMapWidth = m_RampMapWidth;
            asset.RampMapHeight = m_RampMapHeight;
            asset.Gradients = new List<Gradient>(m_Gradients);

            AssetDatabase.CreateAsset(asset, relativePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Config saved successful: " + relativePath);
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
                    "Language" => "语言",
                    "Configurations" => "配置",
                    "Preview" => "预览",
                    "Width for each line" => "行宽",
                    "Height for each line" => "行高",
                    "Load Config" => "导入配置",
                    "Save Config" => "保存配置",
                    "Load RampMapData" => "导入渐变纹理数据",
                    "Save RampMapData" => "保存渐变纹理数据",
                    "Preview Ramp" => "预览渐变",
                    "Preview Width" => "预览宽度",
                    "Preview Roll" => "预览行数",
                    "Texture Name:" => "纹理名称：",
                    "Format:" => "纹理格式：",
                    "Wrap Mode:" => "环绕模式：",
                    "Filter Mode:" => "过滤模式：",
                    "Save Texture" => "保存纹理",
                    "Preview Material" => "预览材质",
                    "Preview On Material" => "从材质预览",
                    _ => key
                };
            }

            return key;
        }

        #endregion
    }
}