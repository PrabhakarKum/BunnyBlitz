using UnityEngine;

namespace BunnyBlitz
{
    public static class Helpers
    {
        public static void RecursiveLayerSet(Transform root, int layerIdx)
        {
            root.gameObject.layer = layerIdx;
            foreach (Transform child in root)
            {
                RecursiveLayerSet(child, layerIdx);
            }
        }
    }
}