#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;

namespace BunnyBlitz.Editor
{
    [ExecuteInEditMode]
    public class ResortSprite : MonoBehaviour
    {
        Renderer spriteRenderer;
        SortingGroup sortGroup;

        void Start()
        {
            spriteRenderer = GetComponent<Renderer>();
            sortGroup = GetComponent<SortingGroup>();
        }
    

        void Update()
        {
            if(sortGroup)
                sortGroup.sortingOrder = (int)(-10 * transform.position.z);
            else
                spriteRenderer.sortingOrder = (int)(-10 * transform.position.z);
        }
    
    }
}

#endif
