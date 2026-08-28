using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BunnyBlitz
{
    public class PlayerInput : MonoBehaviour
    {
        public InputActionAsset InputAsset;
        public event Action OnAttackPerformed;

        public InputAction Move => m_MoveAction;
        public InputAction Jump => m_JumpAction;
        public InputAction Attack => m_AttackAction;

        public InputAction Pause { get; private set; }

        private InputActionMap m_PlayerInputMap;
        private InputAction m_MoveAction;
        private InputAction m_JumpAction;
        private InputAction m_AttackAction;

        private void Awake()
        {
            m_PlayerInputMap = InputAsset.FindActionMap("Player");
            m_MoveAction = m_PlayerInputMap.FindAction("Move");
            m_JumpAction = m_PlayerInputMap.FindAction("Jump");
            m_AttackAction = m_PlayerInputMap.FindAction("Attack");
            Pause = m_PlayerInputMap.FindAction("Pause");
        }

        private void OnEnable()
        {
            m_PlayerInputMap.Enable();
            m_AttackAction.performed += AttackOnperformed;
        }

        private void OnDisable()
        {
            m_AttackAction.performed -= AttackOnperformed;
            m_PlayerInputMap.Disable();
        }

        private void AttackOnperformed(InputAction.CallbackContext context)
        {
            OnAttackPerformed?.Invoke();
        }
    }
}