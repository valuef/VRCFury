using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VF.Model;

namespace VF.Inspector {
    [CustomEditor(typeof(VRCFGlobalCollider), true)]
    public class VRCFGlobalColliderEditor : VRCFuryComponentEditor {
        [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.InSelectionHierarchy)]
        static void DrawGizmo(VRCFGlobalCollider collider, GizmoType gizmoType) {
            var transform = collider.GetTransform();
            var worldRadius = collider.radius * transform.lossyScale.x;

            VRCFuryGizmoUtils.DrawCapsule(
                transform.position,
                Quaternion.identity,
                0,
                worldRadius,
                Color.blue
            );
        }
        
        public override VisualElement CreateEditor() {
            var self = (VRCFGlobalCollider)target;

            var container = new VisualElement();
            
            container.Add(VRCFuryEditorUtils.Prop(serializedObject.FindProperty("rootTransform"), "Root Transform Override"));
            container.Add(VRCFuryEditorUtils.Prop(serializedObject.FindProperty("radius"), "Radius"));

            return container;
        }
    }
}
