using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace BunnyBlitz
{
    [CreateAssetMenu(fileName = "TileToSurfaceTypeLookup", menuName = "Scriptable Objects/TileToSurfaceTypeLookup")]
    public class TileToSurfaceTypeLookup : ScriptableObject
    {
        public SurfaceType[] SurfaceTypes;

        private Dictionary<TileBase, SurfaceType> m_TileToSurfaceLookup;

        public void InitLookup()
        {
            m_TileToSurfaceLookup = new();
            foreach (var st in SurfaceTypes)
            {
                foreach (var t in st.Tiles)
                {
                    m_TileToSurfaceLookup.Add(t, st);
                }
            }
        }

        public bool GetSurfaceForTile(TileBase tile, out SurfaceType type)
        {
            if (tile == null)
            {
                type = null;
                return false;
            }

            return m_TileToSurfaceLookup.TryGetValue(tile, out type);
        }
    }
}

#if UNITY_EDITOR
namespace BunnyBlitz.Editor
{
    [CustomEditor(typeof(TileToSurfaceTypeLookup))]
    public class TileToSurfaceTypeLookupEditor : UnityEditor.Editor
    {
        private SerializedProperty m_SurfaceTypeProperty;

        private void OnEnable()
        {
            m_SurfaceTypeProperty = serializedObject.FindProperty("SurfaceTypes");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var list = new ListView();
            list.BindProperty(m_SurfaceTypeProperty);
            root.Add(list);

            list.showAddRemoveFooter = true;
            list.showBoundCollectionSize = false;
            list.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            list.onAdd += list =>
            {
                var newEntry = CreateInstance<SurfaceType>();
                AssetDatabase.AddObjectToAsset(newEntry, target);
                m_SurfaceTypeProperty.InsertArrayElementAtIndex(m_SurfaceTypeProperty.arraySize);
                m_SurfaceTypeProperty.GetArrayElementAtIndex(m_SurfaceTypeProperty.arraySize - 1).objectReferenceValue =
                    newEntry;
                serializedObject.ApplyModifiedProperties();
            };

            list.onRemove += list =>
            {
                Undo.DestroyObjectImmediate(m_SurfaceTypeProperty.GetArrayElementAtIndex(list.selectedIndex)
                    .objectReferenceValue);
                m_SurfaceTypeProperty.DeleteArrayElementAtIndex(list.selectedIndex);
                serializedObject.ApplyModifiedProperties();
                list.selectedIndex = -1;
                list.RefreshItems();
            };

            list.makeItem += () =>
            {
                var root = new InspectorElement();
                return root;
            };

            list.unbindItem += (element, i) => { element.Unbind(); };

            list.bindItem += (element, i) =>
            {
                if (i >= m_SurfaceTypeProperty.arraySize)
                    return;

                var elm = m_SurfaceTypeProperty.GetArrayElementAtIndex(i);
                if (elm.objectReferenceValue == null)
                    return;

                var so = new SerializedObject(elm.objectReferenceValue);
                (element as InspectorElement).Bind(so);
            };

            return root;
        }
    }
}
#endif