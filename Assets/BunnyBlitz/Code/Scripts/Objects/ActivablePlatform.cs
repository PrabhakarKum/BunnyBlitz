using UnityEngine;
using UnityEngine.Splines;

namespace BunnyBlitz
{
    public class ActivablePlatform : MonoBehaviour
    {
        public AudioSource PlatformAudio;
        public GameObject[] ObjectsToToggle;

        public enum ActivationTrigger
        {
            OnStart,
            OnPlayerContact
        }

        public ActivationTrigger ActivatedOn = ActivationTrigger.OnStart;
        
        private SplineAnimate m_SplineAnimate;

        private void OnEnable()
        {
            m_SplineAnimate = GetComponentInChildren<SplineAnimate>();
        }

        private void Start()
        {
            if(ActivatedOn != ActivationTrigger.OnStart)
            {
                ToggleActivation(false);
            }
            else
            {
                ToggleActivation(true);
            }
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if(ActivatedOn == ActivationTrigger.OnPlayerContact && other.gameObject.CompareTag("Player"))
            {
                ToggleActivation(true);
            }
        }

        void ToggleActivation(bool activated)
        {
            if(activated)
            {
                m_SplineAnimate.Play();
                PlatformAudio.Play();
            }
            else
            {
                PlatformAudio.Stop();
                m_SplineAnimate.Pause();
            }

            foreach(var o in ObjectsToToggle)
            {
                o.SetActive(activated);
            }
        }
    }
}