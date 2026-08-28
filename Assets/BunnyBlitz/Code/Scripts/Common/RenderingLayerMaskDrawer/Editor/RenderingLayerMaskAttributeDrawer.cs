using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BunnyBlitz.Editor
{
    [CustomPropertyDrawer(typeof(RenderingLayerMaskAttribute))]
    class RenderingLayerMaskAttributeDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                // Integer is expected. Everything else is ignored.
                return;
            }
        
            EditorGUI.LabelField(position, label);

            position.x += EditorGUIUtility.labelWidth;
            position.width -= EditorGUIUtility.labelWidth;

            string[] renderingLayerMaskNames = GetRenderingLayerMaskNames();
            int[] renderingLayerMasksIDs = GetRenderingLayerMasksIDs(renderingLayerMaskNames);
    
            EditorGUI.BeginChangeCheck();
            uint a = (uint)(EditorGUI.MaskField(position, property.intValue, renderingLayerMaskNames));

            if (EditorGUI.EndChangeCheck())
            {
                property.longValue = a;
            }

        }

        /**
     * Retrieves list of rendering layer mask names.
     *
     * @return List of rendering layer mask names.
     */
        private string[] GetRenderingLayerMaskNames()
        {
            return RenderingLayerMask.GetDefinedRenderingLayerNames().ToArray();
        }

        /**
     * Retrieves list of rendering layer mask IDs.
     *
     * @return List of rendering layer mask IDs.
     */
        private int[] GetRenderingLayerMasksIDs(string[] names)
        {
            return Enumerable.Range(0, names.Length).ToArray();
        }
    }
}