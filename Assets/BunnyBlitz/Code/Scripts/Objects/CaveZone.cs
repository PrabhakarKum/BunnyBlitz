using UnityEngine;

namespace BunnyBlitz
{
    public class CaveZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerBehaviour>();
            if (player != null)
            {
                player.EnterCave();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerBehaviour>();
            if (player != null)
            {
                player.ExitCave();
            }
        }
    }
}