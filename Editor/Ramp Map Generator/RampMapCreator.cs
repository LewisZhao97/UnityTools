using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lewiszhao.Unitytools.Editor
{
    public class RampMapCreator : EditorWindow
    {
        private static readonly int s_RampPreviewTex = Shader.PropertyToID("_RampPreviewTex");
        private static readonly int s_SampleStep = Shader.PropertyToID("_SampleStep");
        private static readonly int s_RampY = Shader.PropertyToID("_RampY");

        [MenuItem("Tools/Ramp Map Tools/Create Ramp Texture", priority = 1),
         MenuItem("Assets/Create/Ramp Map Tools/Create from asset/Create Ramp Texture", priority = 120)]
        private static void ShowWindow()
        {
            var window = GetWindow<RampMapCreator>();
            window.titleContent = new GUIContent("RampMapCreator");
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        [SerializeField] protected List<Gradient> m_Gradients = new();

        private SerializedObject m_SerializedObject;
        private SerializedProperty m_AssetListProperty;

        private void OnEnable()
        {
            m_Gradients ??= new List<Gradient>();

            m_SerializedObject = new SerializedObject(this);
            m_AssetListProperty = m_SerializedObject.FindProperty("m_Gradients");

            m_PreShader = Shader.Find(nameof(Editor) + "/RampPreview");
            if (m_PreShader != null)
            {
                m_PreMaterial = CoreUtils.CreateEngineMaterial(m_PreShader);
            }

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

        private int m_RampMapWidth = 32;
        private int m_RampMapHeight = 4;
        private Texture2D m_RampMap;
        private string m_RampName = "NewRampMap";

        private readonly string[] m_MapFormat = { "TGA", "PNG", "JPG" };
        private int m_MapIndex;
        private string m_Format = ".tga";

        private Texture2D m_PreviewTex;
        private Material m_PreMaterial;
        private Shader m_PreShader;
        private float m_PreWidth = 1;
        private int m_Step = 4;
        private float m_RampY;

        private TextureWrapMode m_TextureWrapMode = TextureWrapMode.Clamp;
        private FilterMode m_FilterMode = FilterMode.Point;

        private Vector2 m_ScrollPos;

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(250)))
                {
                    DrawAssetGUI();
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    m_RampMap = CreateRamp();
                    SceneView.RepaintAll();

                    if (m_RampMap)
                    {
                        PreviewTex();
                    }

                    Save();
                }
            }
        }

        private void DrawAssetGUI()
        {
            m_SerializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_AssetListProperty);

            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedObject.ApplyModifiedProperties();
            }

            m_RampMapWidth = EditorGUILayout.IntField("Width for each line", m_RampMapWidth);
            m_RampMapHeight = EditorGUILayout.IntField("Height for each line", m_RampMapHeight);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Load Config"))
            {
                var path = EditorUtility.OpenFilePanel("Load RampMapData", Application.dataPath, "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    LoadConfig(path);
                }
            }

            if (GUILayout.Button("Save Config"))
            {
                var path = EditorUtility.SaveFilePanel("Save RampMapData", Application.dataPath, "NewRampMapData",
                    "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    SaveConfig(path);
                }
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Preview Ramp", EditorStyles.boldLabel);

            var labelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = 100;
            m_PreWidth = EditorGUILayout.Slider("Preview Width", m_PreWidth, 0.1f, 15);
            m_Step = EditorGUILayout.IntSlider("Preview Step", m_Step, 1, 10);
            m_RampY = EditorGUILayout.Slider("Sample Y Axis", m_RampY, 0, 1);
            EditorGUIUtility.labelWidth = labelWidth;

            if (GUI.changed)
            {
                if (m_PreShader && !m_PreMaterial)
                {
                    m_PreMaterial = CoreUtils.CreateEngineMaterial(m_PreShader);
                }

                if (m_RampMap && m_PreMaterial)
                {
                    Shader.SetGlobalTexture(s_RampPreviewTex, m_RampMap);
                    m_PreMaterial.SetInt(s_SampleStep, m_Step);
                    m_PreMaterial.SetFloat(s_RampY, m_RampY);
                    m_PreviewTex = AssetPreview.GetAssetPreview(m_PreMaterial);
                }
            }

            if (m_PreviewTex)
            {
                GUILayout.Box(m_PreviewTex, GUILayout.Width(200), GUILayout.Height(200));
            }
        }

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

        private void PreviewTex()
        {
            var rect = EditorGUILayout.GetControlRect(true, m_RampMapHeight * m_Gradients.Count * m_PreWidth);
            EditorGUI.DrawPreviewTexture(rect, m_RampMap);
        }

        private void Save()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Texture Name:", GUILayout.Width(100));
            m_RampName = EditorGUILayout.TextField(m_RampName);

            EditorGUILayout.LabelField("Format:", GUILayout.Width(60));
            m_MapIndex = EditorGUILayout.Popup(m_MapIndex, m_MapFormat, GUILayout.Width(60));

            m_Format = m_MapIndex switch
            {
                0 => ".tga",
                1 => ".png",
                2 => ".jpg",
                _ => m_Format
            };
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);
            m_TextureWrapMode =
                (TextureWrapMode)EditorGUILayout.EnumPopup("Wrap Mode:", m_TextureWrapMode, GUILayout.Width(220));
            m_FilterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode:", m_FilterMode, GUILayout.Width(220));

            EditorGUILayout.Space(15);

            if (!GUILayout.Button("Save Texture", GUILayout.Height(30))) return;
            var path = EditorUtility.SaveFolderPanel("Save Texture", "", "");
            if (string.IsNullOrEmpty(path)) return;

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
                m_SerializedObject.Update();
            }
            else
            {
                Debug.LogError("Load failed: Invalid ramp map data file.");
            }
        }

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
    }
}