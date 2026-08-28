using UnityEngine;
using UnityEngine.Audio;

namespace BunnyBlitz
{
    public class DamageZone : MonoBehaviour
    {
        public int DamageAmount;
        public AudioResource ContactSound;
        private void OnCollisionEnter2D(Collision2D other)
        {
            var player = other.gameObject.GetComponentInParent<PlayerBehaviour>();

            if (player != null)
            {
                if (ContactSound != null)
                {
                    GameManager.Instance.AudioManager.PlaySFXAt(ContactSound, transform.position);
                }
                
                player.ReceiveDamage(DamageAmount, other.contacts[0].point);
                player.BouncingOnEnemy();
                
                return;
            }

            var destructable = other.gameObject.GetComponentInParent<DestructableStaticBehaviour>();
            if (destructable != null)
            {
                destructable.TriggerDestroyed();
            }
        }
    }
}