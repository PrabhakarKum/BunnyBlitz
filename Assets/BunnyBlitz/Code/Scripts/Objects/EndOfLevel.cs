using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace BunnyBlitz
{
    public class EndOfLevel : MonoBehaviour
    {
        public CinemachineCamera EndOfLevelCamera;
        public float TimeBeforeEndOfLevel = 10.0f;
        public float TimeAfterEndOfLevelAndBeforeUI = 10.0f;
        public GameObject spiralEffectTargetPosition;
        
        public Volume EndOfLevelVolume;
        public float EndOfLevelVolumeTransitionTime = 2.0f;
        public float PortalAudioFadeTime = 0.5f;

        public GameObject[] ObjectToEnableOnEnter;
        public GameObject[] ObjectToEnableOnPortalActivation;

        public UnityEvent OnPlayerEntered;

        public AudioSource IdleSource;
        public AudioSource ActiveSource;
        public AudioResource ActivatingAudio;

        private bool m_WasActivated = false;
        private float m_RemainingTransitionTime = 0.0f;
        private float m_PortalAudioFadeTimer = 0.0f;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            //we can only activate end of level once
            if (m_WasActivated)
                return;
            
            var player = other.gameObject.GetComponentInParent<PlayerBehaviour>();

            if (player != null)
            {
                m_WasActivated = true;
                
                OnPlayerEntered?.Invoke();
                
                GameManager.Instance.AudioManager.PlaySFXAt(ActivatingAudio, transform.position);
                ActiveSource.volume = 0.0f;
                ActiveSource.Play();

                foreach (var obj in ObjectToEnableOnEnter)
                {
                    obj.SetActive(true);
                }

                if (EndOfLevelVolume != null)
                {
                    EndOfLevelVolume.weight = 0.0f;
                    EndOfLevelVolume.gameObject.SetActive(true);
                    m_RemainingTransitionTime = EndOfLevelVolumeTransitionTime;
                }

                m_PortalAudioFadeTimer = PortalAudioFadeTime;
                GameManager.Instance.FinishLevel(spiralEffectTargetPosition.transform.position, EndOfLevelCamera, TimeBeforeEndOfLevel, ObjectToEnableOnPortalActivation,TimeAfterEndOfLevelAndBeforeUI);
            }
        }

        void Update()
        {
            if (m_RemainingTransitionTime > 0.0f)
            {
                m_RemainingTransitionTime -= Time.deltaTime;
                float ratio = Mathf.Clamp01(1.0f - m_RemainingTransitionTime / EndOfLevelVolumeTransitionTime);
                EndOfLevelVolume.weight = ratio;
            }

            if (m_PortalAudioFadeTimer > 0.0f)
            {
                m_PortalAudioFadeTimer -= Time.deltaTime;
                ActiveSource.volume = 1.0f - (m_PortalAudioFadeTimer/PortalAudioFadeTime);
            }
        }
    }
}