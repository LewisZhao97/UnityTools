using Lewiszhao.Unitytools.Runtime;
using UnityEditor;
using UnityEngine.UIElements;

namespace Lewiszhao.Unitytools.Editor
{
    [CustomEditor(typeof(BoneGizmoDrawer))]
    public class BoneGizmoDrawerEditor : UnityEditor.Editor
    {
        // public override void OnInspectorGUI()
        // {
        //     var boneGizmoDrawer = (BoneGizmoDrawer)target;
        //
        //     EditorGUI.BeginChangeCheck();
        //     boneGizmoDrawer.ShowBones = EditorGUILayout.Toggle("Show Bones", boneGizmoDrawer.ShowBones);
        //     boneGizmoDrawer.GizmoColor = EditorGUILayout.ColorField("Gizmo Color", boneGizmoDrawer.GizmoColor);
        //     boneGizmoDrawer.GizmoSize = EditorGUILayout.FloatField("Gizmo Size", boneGizmoDrawer.GizmoSize);
        //     if (EditorGUI.EndChangeCheck())
        //     {
        //         SceneView.RepaintAll();
        //     }
        // }

        public VisualTreeAsset VisualTreeAsset;

        public override VisualElement CreateInspectorGUI()
        {
            var root = VisualTreeAsset.CloneTree();
            return root;
        }
    }
}