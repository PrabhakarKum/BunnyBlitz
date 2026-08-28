using UnityEngine;

namespace BunnyBlitz
{
    //Ensure that it run at the very last step, so that everything is spawn before moving their layer
    // (e.g. the spline instantiation spawning prefab along them)
    [DefaultExecutionOrder(9999)]
    public class LevelLayer : MonoBehaviour
    {
        [Tooltip("Use to place the light source mask (mostly used for front of the cave)")]
        public Transform FrontDecoRoot;
        public Transform LayerObjectRoot;
        public LevelAuthoring.LayerName Layer;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            switch (Layer)
            {
                case LevelAuthoring.LayerName.Background:
                    Helpers.RecursiveLayerSet(transform, LayerMask.NameToLayer("BackgroundLayer"));
                    break;
                case LevelAuthoring.LayerName.Normal:
                    Helpers.RecursiveLayerSet(transform, LayerMask.NameToLayer("NormalLayer"));
                    break;
                case LevelAuthoring.LayerName.Front:
                    Helpers.RecursiveLayerSet(transform, LayerMask.NameToLayer("ForegroundLayer"));
                    break;
            }

            if (FrontDecoRoot == null)
            {
                //Try to retrieve the front deco if it wasn't explicitly set
                var layerFrontDecoName = Layer switch
                {
                    LevelAuthoring.LayerName.Background => "Background_FrontTileDeco",
                    LevelAuthoring.LayerName.Normal => "Normal_FrontTileNormal",
                    LevelAuthoring.LayerName.Front => "Front_FrontTileFront",
                    LevelAuthoring.LayerName.All => "All Layers",
                    _ => "Unknown Case"
                };

                FrontDecoRoot = GameObject.Find(layerFrontDecoName)?.transform;
            }
        }

        private void OnEnable()
        {
            if (LayerObjectRoot == null)
            {
                //check if maybe there was already a gameobject that was forgotten to be assigned
                var obj = transform.Find("LayerGameObjects");
                LayerObjectRoot = obj != null ? obj : new GameObject("LayerGameObjects").transform;
                LayerObjectRoot.SetParent(transform);
                LayerObjectRoot.gameObject.layer = gameObject.layer;
            }
        }
    }
}