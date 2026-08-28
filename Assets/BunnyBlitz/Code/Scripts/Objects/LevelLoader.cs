using UnityEngine;

namespace BunnyBlitz
{
    [RequireComponent(typeof(Collider2D))]
    public class LevelLoader : MonoBehaviour
    {
        public string LevelName = "LevelName";
    
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.GetComponentInParent<PlayerBehaviour>() == null)
                return;

            GameManager.Instance.LoadNewLevel(LevelName);
        
        }
    }
}