using System;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace BunnyBlitz
{
    public class LevelData : MonoBehaviour
    {
        public string LevelName;
        public float KillZ = -100.0f;

        [Header("Override value")] public LevelOverrideValue LevelOverrides = new();

        public int EnemyCount { get; private set; }
        public int EnemyDefeated { get; private set; }
        public bool[] OneOfFiveCollected { get; private set; } = new bool[5];

        public void Init()
        {
            for (int i = 0; i < 5; ++i)
            {
                OneOfFiveCollected[i] = false;
            }

            EnemyCount = FindObjectsByType<EnemyBehaviour>(FindObjectsInactive.Include).Length;
            EnemyDefeated = 0;
        }

        public void EnemyDestroyed()
        {
            EnemyDefeated += 1;
        }

        public void OneOfFivePickedUp(int index)
        {
            if (index < 0 || index >= 5)
                return;

            OneOfFiveCollected[index] = true;
            GameManager.Instance.HUD.UpdateOnOfFive(OneOfFiveCollected);
        }

        public int GetCompletedPercentage()
        {
            int collected = 0;
            foreach (var c in OneOfFiveCollected)
            {
                if (c)
                    collected += 1;
            }

            return Mathf.RoundToInt(
                Mathf.Clamp01((collected / 5.0f) * 0.5f + (EnemyDefeated / (float)EnemyCount) * 0.5f) * 100);
        }
    }

    [Serializable]
    public class LevelOverrideValue
    {
        public bool OverridePlayerSpeed = false;
        public float PlayerSpeed = 22;

        public bool OverrideGravityValue = false;
        public float GravityMultiplier = 1.0f;

        private float m_OldPlayerSpeed;
        private float m_OldGravityMultiplier;

        public void ApplyOverride(PlayerBehaviour player)
        {
            if (OverridePlayerSpeed)
            {
                m_OldPlayerSpeed = player.Movement.XspeedLimit;
                player.Movement.XspeedLimit = PlayerSpeed;
            }

            if (OverrideGravityValue)
            {
                m_OldGravityMultiplier = player.Rigidbody.gravityScale;
                player.Rigidbody.gravityScale = GravityMultiplier;
            }
        }

        public void RevertValue(PlayerBehaviour player)
        {
            if (OverridePlayerSpeed)
                player.Movement.XspeedLimit = m_OldPlayerSpeed;

            if (OverrideGravityValue)
                player.Rigidbody.gravityScale = m_OldGravityMultiplier;
        }
    }
}

#if UNITY_EDITOR
namespace BunnyBlitz.Editor
{
    [CustomPropertyDrawer(typeof(LevelOverrideValue))]
    public class LevelOverrideValueDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var speedOverrideProperty = property.FindPropertyRelative(nameof(LevelOverrideValue.OverridePlayerSpeed));
            var speedValueProperty = property.FindPropertyRelative(nameof(LevelOverrideValue.PlayerSpeed));
            var gravityOverrideProperty =
                property.FindPropertyRelative(nameof(LevelOverrideValue.OverrideGravityValue));
            var gravityValueProperty = property.FindPropertyRelative(nameof(LevelOverrideValue.GravityMultiplier));


            var root = new VisualElement();

            var overrideSpeedCheck = new PropertyField(speedOverrideProperty);
            root.Add(overrideSpeedCheck);

            var overrideSpeedValue = new PropertyField(speedValueProperty);
            overrideSpeedValue.style.display = speedOverrideProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            root.Add(overrideSpeedValue);

            overrideSpeedCheck.RegisterValueChangeCallback(evt =>
            {
                overrideSpeedValue.style.display =
                    evt.changedProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

            var overrideGravityCheck = new PropertyField(gravityOverrideProperty);
            root.Add(overrideGravityCheck);

            var overrideGravityValue = new PropertyField(gravityValueProperty);
            overrideGravityValue.style.display =
                gravityOverrideProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            root.Add(overrideGravityValue);

            overrideGravityCheck.RegisterValueChangeCallback(evt =>
            {
                overrideGravityValue.style.display =
                    evt.changedProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

            return root;
        }
    }
}
#endif