using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace BunnyBlitz
{
    public class PhysicIgnore : MonoBehaviour
    {
        static Dictionary<string, List<PhysicIgnore>> s_PhysicIgnoreMap = new();

        public string[] Tags;
        public string[] IgnoreTags;

        private Collider2D[] m_Colliders;

        void Awake()
        {
            m_Colliders = GetComponentsInChildren<Collider2D>();
        }

        void OnEnable()
        {
            foreach (var t in Tags)
            {
                if (!s_PhysicIgnoreMap.ContainsKey(t))
                {
                    s_PhysicIgnoreMap.Add(t, new());
                }

                s_PhysicIgnoreMap[t].Add(this);
            }

            SetupIgnoreList();
        }

        void OnDisable()
        {
            foreach (var t in Tags)
            {
                s_PhysicIgnoreMap[t].Remove(this);
            }
        }

        void SetupIgnoreList()
        {
            foreach (var it in IgnoreTags)
            {
                if (s_PhysicIgnoreMap.TryGetValue(it, out var list))
                {
                    foreach (var pi in list)
                    {
                        if (pi == this)
                            continue;

                        pi.IgnoreColliders(m_Colliders);
                    }
                }
            }
        }

        public void IgnoreColliders(Collider2D[] colliders)
        {
            foreach (var c in colliders)
            {
                foreach (var sc in m_Colliders)
                {
                    Physics2D.IgnoreCollision(c, sc);
                }
            }
        }
    }
}

#if UNITY_EDITOR
namespace BunnyBlitz.Editor
{
    [CustomEditor(typeof(PhysicIgnore))]
    public class PhysicIgnoreEditor : UnityEditor.Editor
    {
        SerializedProperty m_TagsProperty;
        SerializedProperty m_IgnoreTagsProperty;

        void OnEnable()
        {
            m_TagsProperty = serializedObject.FindProperty(nameof(PhysicIgnore.Tags));
            m_IgnoreTagsProperty = serializedObject.FindProperty(nameof(PhysicIgnore.IgnoreTags));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            MaskField tagsField = new MaskField("Self Tags");
            tagsField.choices = new List<string>(InternalEditorUtility.tags);
            SyncMaskFieldWithArray(tagsField, m_TagsProperty);
            tagsField.RegisterValueChangedCallback(evt =>
            {
                MaskFieldToSerializedProperty(tagsField, m_TagsProperty);
                serializedObject.ApplyModifiedProperties();
            });

            root.Add(tagsField);

            MaskField ignoredTagsField = new MaskField("Ignored Tags");
            ignoredTagsField.choices = new List<string>(InternalEditorUtility.tags);
            SyncMaskFieldWithArray(ignoredTagsField, m_IgnoreTagsProperty);
            ignoredTagsField.RegisterValueChangedCallback(evt =>
            {
                MaskFieldToSerializedProperty(ignoredTagsField, m_IgnoreTagsProperty);
                serializedObject.ApplyModifiedProperties();
            });

            root.Add(ignoredTagsField);

            return root;
        }

        void SyncMaskFieldWithArray(MaskField field, SerializedProperty arrayProp)
        {
            int mask = 0;
            for (int p = 0; p < arrayProp.arraySize; ++p)
            {
                var s = arrayProp.GetArrayElementAtIndex(p).stringValue;

                for (int i = 0; i < field.choices.Count; ++i)
                {
                    if (field.choices[i] == s)
                    {
                        mask |= 1 << i;
                    }
                }
            }

            field.SetValueWithoutNotify(mask);
        }

        void MaskFieldToSerializedProperty(MaskField field, SerializedProperty property)
        {
            int mask = field.value;
            property.ClearArray();
            for (var i = 0; i < field.choices.Count; ++i)
            {
                if ((mask & (1 << i)) != 0)
                {
                    property.InsertArrayElementAtIndex(property.arraySize);
                    property.GetArrayElementAtIndex(property.arraySize - 1).stringValue = field.choices[i];
                }
            }
        }
    }
}
#endif