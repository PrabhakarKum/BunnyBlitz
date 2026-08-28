using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace BunnyBlitz
{
    public class CollectibleItem : MonoBehaviour
    {
        public enum CollectibleType
        {
            Life,
            Health,
            Coin,
            Projectile,
            Custom,
            OneOfFive
        }

        public CollectibleType ItemType;
        public string CustomType;

        //Used by one of Five collectible to separate each version of the collectible
        public int CollectibleIndex = 0;

        public Sprite CustomSprite;
        public int Amount = 1;
    }
}


#if UNITY_EDITOR
namespace BunnyBlitz.Editor
{
    [CustomEditor(typeof(CollectibleItem))]
    public class CollectibleItemEditor : UnityEditor.Editor
    {
        private SerializedProperty m_ItemTypeProperty;
        private SerializedProperty m_CustomTypeProperty;
        private SerializedProperty m_CustomSpriteProperty;
        private SerializedProperty m_AmountProperty;
        private SerializedProperty m_CollectibleIndexProperty;

        private PropertyField m_CustomTypeField;
        private PropertyField m_CustomSpriteField;
        private PropertyField m_CollectibleIndexField;

        private void OnEnable()
        {
            m_ItemTypeProperty = serializedObject.FindProperty(nameof(CollectibleItem.ItemType));
            m_CustomTypeProperty = serializedObject.FindProperty(nameof(CollectibleItem.CustomType));
            m_CustomSpriteProperty = serializedObject.FindProperty(nameof(CollectibleItem.CustomSprite));
            m_AmountProperty = serializedObject.FindProperty(nameof(CollectibleItem.Amount));
            m_CollectibleIndexProperty = serializedObject.FindProperty(nameof(CollectibleItem.CollectibleIndex));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var itemTypeField = new PropertyField(m_ItemTypeProperty);
            itemTypeField.RegisterValueChangeCallback(evt =>
            {
                UpdateVisibility((CollectibleItem.CollectibleType)evt.changedProperty.enumValueIndex);
            });

            m_CustomTypeField = new PropertyField(m_CustomTypeProperty);

            m_CustomSpriteField = new PropertyField(m_CustomSpriteProperty);

            root.Add(itemTypeField);
            root.Add(m_CustomTypeField);
            root.Add(m_CustomSpriteField);

            var itmCountField = new PropertyField(m_AmountProperty);
            root.Add(itmCountField);

            m_CollectibleIndexField = new PropertyField(m_CollectibleIndexProperty);
            root.Add(m_CollectibleIndexField);

            UpdateVisibility((CollectibleItem.CollectibleType)m_ItemTypeProperty.enumValueIndex);

            return root;
        }

        void UpdateVisibility(CollectibleItem.CollectibleType currentType)
        {
            m_CustomTypeField.style.display = currentType == CollectibleItem.CollectibleType.Custom
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            m_CustomSpriteField.style.display =
                currentType == CollectibleItem.CollectibleType.Custom ||
                currentType == CollectibleItem.CollectibleType.OneOfFive
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            m_CollectibleIndexField.style.display = currentType == CollectibleItem.CollectibleType.OneOfFive
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}
#endif
