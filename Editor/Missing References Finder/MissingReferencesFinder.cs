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
            public string Path;
            public string ErrorType;
        }

        private readonly List<BrokenReference> m_BrokenReferences = new();
        private Vector2 m_ScrollPosition;

        [MenuItem("Tools/Find Missing References")]
        public static void ShowWindow()
        {
            GetWindow<MissingReferencesFinder>("Missing References");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("扫描当前场景中的丢失引用", EditorStyles.boldLabel);
            GUILayout.Label("包括：Missing Scripts 和 属性中的 Missing", EditorStyles.miniLabel);

            GUILayout.Space(10);

            if (GUILayout.Button("开始扫描", GUILayout.Height(30)))
            {
                ScanScene();
            }

            GUILayout.Space(10);

            if (m_BrokenReferences.Count > 0)
            {
                GUILayout.Label($"找到 {m_BrokenReferences.Count} 个丢失项:", EditorStyles.boldLabel);

                m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);

                foreach (var refer in m_BrokenReferences)
                {
                    DrawResultItem(refer);
                }

                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("列表为空 (请点击扫描)", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private static void DrawResultItem(BrokenReference refer)
        {
            EditorGUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(EditorGUIUtility.IconContent("GameObject Icon")), GUILayout.Width(20),
                GUILayout.Height(20));
            if (GUILayout.Button($"{refer.GameObject.name}", EditorStyles.boldLabel))
            {
                Selection.activeGameObject = refer.GameObject;
                EditorGUIUtility.PingObject(refer.GameObject);
            }

            GUILayout.EndHorizontal();

            GUILayout.Label($"Path: {refer.Path}", EditorStyles.miniLabel);

            var errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal =
                {
                    textColor = new Color(1f, 0.4f, 0.4f)
                }
            };

            if (refer.ErrorType == "Missing Script")
            {
                GUILayout.Label($"⚠ Error: 挂载的脚本文件丢失 (Missing Script)", errorStyle);
            }
            else
            {
                GUILayout.Label($"⚠ Component: [{refer.ComponentName}]", EditorStyles.label);
                GUILayout.Label($"⚠ Property: [{refer.PropertyName}] 指向了丢失的资源", errorStyle);
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

            Debug.Log($"扫描完成，找到 {m_BrokenReferences.Count} 个丢失引用。");
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
                            PropertyName = sp.displayName + " (" + sp.name + ")",
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
    }
}