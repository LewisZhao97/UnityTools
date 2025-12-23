using UnityEngine;
using UnityEditor;

namespace Lewiszhao.Unitytools.Runtime
{
    public class BoneGizmoDrawer : MonoBehaviour
    {
        [SerializeField] private bool m_ShowBones = true;
        [SerializeField] private Color m_GizmoColor = Color.yellow;
        [SerializeField] private float m_GizmoSize = 0.02f;

        private void OnDrawGizmos()
        {
            if (m_ShowBones)
            {
                DrawBonesRecursive(transform);
            }
        }

        private void DrawBonesRecursive(Transform bone)
        {
            Handles.color = m_GizmoColor;
            Handles.DrawWireDisc(bone.position, bone.forward, m_GizmoSize);
            foreach (Transform child in bone)
            {
                Handles.DrawLine(bone.position, child.position);
                DrawBonesRecursive(child);
            }
        }
    }
}