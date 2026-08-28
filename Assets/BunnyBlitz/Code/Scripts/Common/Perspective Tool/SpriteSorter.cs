using System;
using System.Collections.Generic;
using BunnyBlitz.Editor;
using UnityEngine;

namespace MindjarDesign.PerspectiveTools
{

    [System.Serializable]
    public struct PerspectiveElement
    {
        public SpriteRenderer rend;
        public float depth;
    }

    public class SpriteSorter : MonoBehaviour
    {
        public Gradient spriteDepthGradient;
    
        [SerializeField]
        private List<PerspectiveElement> perspectiveElements = new List<PerspectiveElement>();
       
        [SortingLayer]
        public int newSortingLayer = 0;

        [RenderingLayerMask]
        public uint newRenderingLayerMask = 0;
        
        [SerializeField]
        Material sortingMaterial;

         public bool sort;
        
        
        // Start is called before the first frame update
        void Start()
        {

        }

        //Assign sorting layer to an gameobject
        void AssignSortingLayer()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }
        
        
        [ContextMenu("Sort Renderers")]
        public void SortRenderers()
        {
            Console.Clear();
            var rendLayers = RenderingLayerMask.GetDefinedRenderingLayerNames();
            
            perspectiveElements = new List<PerspectiveElement>();
            
            var elements = transform.root.GetComponentsInChildren<Transform>();
            
            foreach (Transform t in elements)
            {
                PerspectiveElement p = default;
                if (t.GetComponent<SpriteRenderer>() != null)
                {
                    p = new PerspectiveElement
                    {
                        rend = t.GetComponent<SpriteRenderer>(),
                        depth = t.position.z
                    };
                    //Debug.Log( LayerMask.LayerToName(t.gameObject.layer));
                    perspectiveElements.Add(p);
                }
            }
            perspectiveElements.Sort(SortDescending);

            for (int i = 0; i < perspectiveElements.Count; i++)
            {
              perspectiveElements[i].rend.sortingOrder = i;
              perspectiveElements[i].rend.sortingLayerID = newSortingLayer;
              perspectiveElements[i].rend.renderingLayerMask = (uint)newRenderingLayerMask;
              if(sortingMaterial != null)
              {
                  perspectiveElements[i].rend.material = sortingMaterial;
              }
              
              //var gradpos = MMMaths.Remap(perspectiveElements[i].depth,perspectiveElements[0].depth, perspectiveElements[^1].depth, 0, 1 );
              //perspectiveElements[i].rend.color = spriteDepthGradient.Evaluate(gradpos);

            }
            
        }
        
        // Sort floats descending
        public int SortDescending(PerspectiveElement x,PerspectiveElement y)
        {
            if (x.depth > y.depth)
            {
                return -1;
            }
            else if(x.depth < y.depth)
            {
                return 1;
            }
            return 0;
        }
        
        // Sort floats ascending
        public int SortAscending(PerspectiveElement x,PerspectiveElement y)
        {
            if (x.depth < y.depth)
            {
                return -1;
            }
            else if(x.depth > y.depth)
            {
                return 1;
            }
            return 0;
        }
    }
}
