using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace BunnyBlitz
{
    public class Checkpoint : MonoBehaviour
    {
        public string OnMessage;
        public Color OnColor;
        public string OffMessage;
        //public Color OffColor;

        [Header("Reference")]
        public SpawnPoint AttachedSpawnPoint;
        public UIDocument FrontDocument;
        public UIDocument BackDocument;

        [Header("Audio")]
        public AudioSource EffectSound;

        private bool m_Activated = false;
        private Animator m_Animator;
        private Light2D m_Light;
        private Label m_FrontMessage;
        private Label m_BackMessage;

        void OnEnable()
        {
            m_Animator = GetComponentInChildren<Animator>();
            m_Light = GetComponentInChildren<Light2D>();

            m_FrontMessage = FrontDocument.rootVisualElement.Q<Label>();
            m_BackMessage = BackDocument.rootVisualElement.Q<Label>();

            m_FrontMessage.text = OffMessage;
            m_BackMessage.text = OnMessage;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerBehaviour>();

            if (player != null)
            {
                GameManager.Instance.SetNewSpawnPoint(AttachedSpawnPoint);
                if(!m_Activated)
                {
                    m_Activated = true;
                    m_Animator.SetTrigger("Activate");
                    m_Light.color = OnColor;

                    EffectSound.Play();
                }
            }
        }
    }
}