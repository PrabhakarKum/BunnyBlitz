using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace BunnyBlitz
{
    public class SurfaceType : ScriptableObject
    {
        public TileBase[] Tiles;
    }
}

#if UNITY_EDITOR
namespace BunnyBlitz.Editor
{
    [CustomEditor(typeof(SurfaceType))]
    public class SurfaceTypeEditor : UnityEditor.Editor
    {
        private SerializedProperty m_NameProperty;
        private SerializedProperty m_TilesPropery;

        private void OnEnable()
        {
            m_NameProperty = serializedObject.FindProperty("m_Name");
            m_TilesPropery = serializedObject.FindProperty("Tiles");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var nameField = new PropertyField();
            nameField.BindProperty(m_NameProperty);

            var propertyField = new PropertyField();
            propertyField.BindProperty(m_TilesPropery);

            propertyField.RegisterCallback<DragEnterEvent>(evt =>
            {
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj.GetType().IsSubclassOf(typeof(TileBase)))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        evt.StopPropagation();
                    }
                }
            });

            propertyField.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj.GetType().IsSubclassOf(typeof(TileBase)))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        evt.StopPropagation();
                    }
                }
            });

            propertyField.RegisterCallback<DragPerformEvent>(evt =>
            {
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj.GetType() == typeof(SurfaceType))
                    {
                        m_TilesPropery.InsertArrayElementAtIndex(m_TilesPropery.arraySize);
                        m_TilesPropery.GetArrayElementAtIndex(m_TilesPropery.arraySize - 1).objectReferenceValue = obj;
                    }
                }

                serializedObject.ApplyModifiedProperties();
            });

            root.Add(nameField);
            root.Add(propertyField);
            return root;
        }
    }
}
#endif