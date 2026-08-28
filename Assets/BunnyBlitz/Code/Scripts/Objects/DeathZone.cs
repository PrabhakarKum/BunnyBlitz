using UnityEngine;

namespace BunnyBlitz
{
    public class DeathZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerBehaviour>();
        
            if(player != null)
                player.PlayerDead();
        }
    }
}