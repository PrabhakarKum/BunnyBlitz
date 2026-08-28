using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BunnyBlitz
{
    public class SpawnPoint : MonoBehaviour
    {
        public bool IsStartSpawn = false;
        [Tooltip("If this is null, this will use the Spawn Point Position")]
        public Transform SpawnPosition;

#if UNITY_EDITOR
        private void OnValidate()
        {
            EditorGUIUtility.SetIconForObject(gameObject, EditorGUIUtility.IconContent("sv_label_3").image as Texture2D);
        }
#endif
    }
}
