using UnityEngine;
using UnityEngine.Events;

namespace BunnyBlitz
{
    [RequireComponent(typeof(Collider2D))]
    public class CallbackOnTrigger : MonoBehaviour
    {
        public UnityEvent EventsOnTriggerEnter;
        public UnityEvent EventsOnTriggerExit;

        [Tooltip("If this is true, the trigger disable itself after being entered OR exited once")]
        public bool SingleUse = false;

        [Tooltip("If this is true, on the GameManager respawning the player, this get reenabled")]
        public bool ResetOnRespawn = false;

        void Awake()
        {
            if(ResetOnRespawn)
            {
                GameManager.Instance.OnPlayerRespawn += ResetOnRespawnCallback;
            }
        }
        
        void OnDestroy()
        {
            if(ResetOnRespawn)
            {
                GameManager.Instance.OnPlayerRespawn -= ResetOnRespawnCallback;
            }
        }

        void ResetOnRespawnCallback()
        {
            gameObject.SetActive(true);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if(other.gameObject.CompareTag("Player"))
            {
                if(EventsOnTriggerEnter.GetPersistentEventCount() > 0)
                {
                    EventsOnTriggerEnter.Invoke();

                    //singe use get disable after on Enter if there is no OnExit event
                    if(SingleUse && EventsOnTriggerExit.GetPersistentEventCount() == 0)
                        gameObject.SetActive(false);
                }
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if(other.gameObject.CompareTag("Player"))
            {
                if(EventsOnTriggerExit.GetPersistentEventCount() > 0)
                {
                    EventsOnTriggerExit.Invoke();

                    if(SingleUse)
                        gameObject.SetActive(false);
                }
            }
        }
    }
}