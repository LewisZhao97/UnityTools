using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Lewiszhao.Unitytools.Editor
{
    public class MissingReferencesFinder : EditorWindow
    {
        private struct BrokenReference
        {
            public GameObject GameObject;
            public string ComponentName;
            public string PropertyName;
            public string PropertyLabel;
            public string Path;
            public string ErrorType;
        }

        private readonly List<BrokenReference> m_BrokenReferences = new();
        private Vector2 m_ScrollPosition;

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

        [MenuItem("Tools/Find Missing References", priority = 3)]
        public static void ShowWindow()
        {
            var window = GetWindow<MissingReferencesFinder>(false, "Missing References Finder");
            window.titleContent.text = "Missing References Finder";
            window.minSize = new Vector2(450, 241);
            window.maxSize = new Vector2(550, 450);
            window.Show();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Label(GetLocalizedText("Scanner"), Style);
                GUILayout.Label(GetLocalizedText("This includes: Missing Scripts and the Missing attribute"), EditorStyles.miniLabel);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(GetLocalizedText("Language"), GUILayout.Width(70));
                m_LanguageIndex = EditorGUILayout.Popup(m_LanguageIndex, m_LanguageOptions, GUILayout.Width(70));
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                if (GUILayout.Button(GetLocalizedText("Scan in current scene"), GUILayout.Height(30)))
                {
                    ScanScene();
                }

                GUILayout.Space(10);

                if (m_BrokenReferences.Count > 0)
                {
                    GUILayout.Label(GetLocalizedText("Scan complete: Finding ") + m_BrokenReferences.Count + GetLocalizedText(" missing reference(s):"), EditorStyles.boldLabel);
                    GUILayout.Label(GetLocalizedText("Please find the corresponding resource based on the variable name."), EditorStyles.helpBox);

                    m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);

                    foreach (var refer in m_BrokenReferences)
                    {
                        DrawResultItem(refer);
                    }

                    GUILayout.EndScrollView();
                }
                else
                {
                    GUILayout.Label(GetLocalizedText("The list is empty (please click to scan)"), EditorStyles.centeredGreyMiniLabel);
                }
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

        private void DrawResultItem(BrokenReference refer)
        {
            EditorGUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            var icon = EditorGUIUtility.IconContent("GameObject Icon").image;
            var content = new GUIContent(refer.GameObject.name, icon);
            var leftBoldLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                imagePosition = ImagePosition.ImageLeft,
                fixedHeight = 20
            };

            if (GUILayout.Button(content, leftBoldLabel))
            {
                Selection.activeGameObject = refer.GameObject;
                EditorGUIUtility.PingObject(refer.GameObject);
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(GetLocalizedText("Path: ") + refer.Path, EditorStyles.miniLabel);

            var errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal =
                {
                    textColor = new Color(1f, 0.4f, 0.4f)
                }
            };

            if (refer.ErrorType == "Missing Script")
            {
                GUILayout.Label(GetLocalizedText("⚠ Error: Script file missing"), errorStyle);
            }
            else
            {
                GUILayout.Label(GetLocalizedText("⚠ Component: ") + refer.ComponentName, EditorStyles.label);
                var style = new GUIStyle(EditorStyles.label)
                {
                    normal =
                    {
                        textColor = Color.yellow
                    }
                };
                GUILayout.Label(GetLocalizedText("⚠ Name (Inspector): ") + refer.PropertyLabel, style); 
                GUILayout.Label(GetLocalizedText("⚠ Property: ") + refer.PropertyName + GetLocalizedText(" has a missing reference"), errorStyle);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        private void ScanScene()
        {
            m_BrokenReferences.Clear();

            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (GameObject g in rootObjects)
            {
                CheckGameObjectRecursive(g);
            }

            Debug.Log(GetLocalizedText("Scan complete: Finding ") + m_BrokenReferences.Count + GetLocalizedText(" missing reference(s)"));
        }

        private void CheckGameObjectRecursive(GameObject go)
        {
            CheckMissingScripts(go);
            CheckBrokenProperties(go);

            foreach (Transform child in go.transform)
            {
                CheckGameObjectRecursive(child.gameObject);
            }
        }

        private void CheckMissingScripts(GameObject go)
        {
            var components = go.GetComponents<Component>();

            foreach (var t in components)
            {
                if (!t)
                {
                    m_BrokenReferences.Add(new BrokenReference
                    {
                        GameObject = go,
                        ComponentName = "Missing Script",
                        PropertyName = "N/A",
                        PropertyLabel = "N/A",
                        Path = GetFullPath(go),
                        ErrorType = "Missing Script"
                    });
                }
            }
        }

        private void CheckBrokenProperties(GameObject go)
        {
            var components = go.GetComponents<Component>();

            foreach (var c in components)
            {
                if (!c) continue;

                var so = new SerializedObject(c);
                var sp = so.GetIterator();

                while (sp.NextVisible(true))
                {
                    if (sp.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                    {
                        m_BrokenReferences.Add(new BrokenReference
                        {
                            GameObject = go,
                            ComponentName = c.GetType().Name,
                            PropertyName = sp.name,
                            PropertyLabel = sp.displayName,
                            Path = GetFullPath(go),
                            ErrorType = "Missing Reference"
                        });
                    }
                }
            }
        }

        private static string GetFullPath(GameObject go)
        {
            var path = "/" + go.name;
            while (go.transform.parent)
            {
                go = go.transform.parent.gameObject;
                path = "/" + go.name + path;
            }

            return path;
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
                    "Path: " => "路径： ",
                    "This includes: Missing Scripts and the Missing attribute" => "包括：Missing Scripts 和 属性中的 Missing",
                    "Scanner" => "引用扫描器",
                    "Scan in current scene" => "开始扫描当前场景",
                    "The list is empty (please click to scan)" => "列表为空(请点击扫描)",
                    "Scan complete: Finding " => "扫描完成，找到 ",
                    " missing reference(s)" => " 个丢失引用。",
                    " missing reference(s):" => " 个丢失项：",
                    "⚠ Error: Script file missing" => "⚠ 错误: 挂载的脚本文件丢失",
                    "⚠ Component: " => "⚠ 组件： ",
                    "⚠ Name (Inspector): " => "⚠ 名称 (检查器)： ",
                    "⚠ Property: " => "⚠ 属性： ",
                    " has a missing reference" => "指向了丢失的资源",
                    "Please find the corresponding resource based on the variable name." => "请根据变量名称查找对应资源。",
                    _ => key
                };
            }

            return key;
        }

        #endregion
    }
}