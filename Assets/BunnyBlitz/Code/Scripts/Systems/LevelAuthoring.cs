using System;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace BunnyBlitz
{
    [ExecuteInEditMode]
    public class LevelAuthoring: MonoBehaviour
    {
        public enum LayerName
        {
            Background = 0,
            Normal = 1,
            Front = 2,
            All = 3
        }
        
#if UNITY_EDITOR
        [Serializable]
        public class Layer
        {
            public Transform Root;

            public bool IsValid()
            {
                return Root != null;
            }
        }

        [SerializeField] 
        public GameObject BackLayerPrefab;
        [SerializeField] 
        public GameObject NormalLayerPrefab;
        [SerializeField] 
        public GameObject FrontLayerPrefab;

        public float ZBackLayerDistance = 120.0f;
        
        [HideInInspector] 
        [SerializeField] 
        private Layer BackgroundLayer;

        [HideInInspector] 
        [SerializeField] 
        private Layer NormalLayer;
        
        [HideInInspector] 
        [SerializeField] 
        private Layer FrontLayer;
        
        private GameObject m_FrontLayerDecoTilemap;
        
        private void OnEnable()
        {
            LevelAuthoringSceneTool.DisplayModeChanged += UpdateLayerDisplay;
            LevelAuthoringSceneTool.FrontDecoTileDisplayChanged += UpdateFrontDecoDisplay;
            
            m_FrontLayerDecoTilemap = GameObject.Find("Normal_FrontTileDeco");
        }

        private void OnDisable()
        {
            LevelAuthoringSceneTool.DisplayModeChanged -= UpdateLayerDisplay;
            LevelAuthoringSceneTool.FrontDecoTileDisplayChanged -= UpdateFrontDecoDisplay;
        }

        private void UpdateLayerDisplay()
        {
            var displayedLayer = (LayerName)SessionState.GetInt("DisplayedLevelLayer", (int)LayerName.Front);
            ShowLayers(displayedLayer);
        }

        private void UpdateFrontDecoDisplay(bool hidden)
        {
            if (!hidden)
            {
                SceneVisibilityManager.instance.Show(m_FrontLayerDecoTilemap, true);
            }
            else
            {
                SceneVisibilityManager.instance.Hide(m_FrontLayerDecoTilemap, true);
            }
        }

        private void OnValidate()
        {
            transform.position = Vector3.zero;
            
            if (BackgroundLayer == null || NormalLayer == null || !BackgroundLayer.IsValid() || !NormalLayer.IsValid())
            {
                EditorApplication.update += RecreateLayers;
                return;
            }

            BackgroundLayer.Root.localPosition = new Vector3(0, 0, ZBackLayerDistance);
        }

        private Layer CreateLayer(string name, GameObject prefab)
        {
            var layer = new Layer();
            layer = new Layer();
            layer.Root = (PrefabUtility.InstantiatePrefab(prefab, transform) as GameObject).transform;
            PrefabUtility.UnpackPrefabInstance(layer.Root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            layer.Root.gameObject.name = name;
            return layer;
        }

        private void RecreateLayers()
        {
            if (BackgroundLayer == null || !BackgroundLayer.IsValid())
            {
                BackgroundLayer = CreateLayer("BackgroundLayer", BackLayerPrefab);
                BackgroundLayer.Root.transform.localPosition = new Vector3(0, 0, ZBackLayerDistance);
            }

            if (NormalLayer == null || !NormalLayer.IsValid())
            {
                NormalLayer = CreateLayer("NormalLayer", NormalLayerPrefab);
            }
            
            if (FrontLayer == null || !FrontLayer.IsValid())
            {
                FrontLayer = CreateLayer("FrontLayer", FrontLayerPrefab);
            }

            EditorApplication.update -= RecreateLayers;
        }

        // Will hide all the layer in front of the shown one
        public void ShowLayers(LayerName layer)
        {
            switch (layer)
            {
                case LayerName.Background:
                    SceneVisibilityManager.instance.Show(BackgroundLayer.Root.gameObject, true);
                    SceneVisibilityManager.instance.Hide(NormalLayer.Root.gameObject, true);
                    SceneVisibilityManager.instance.Hide(FrontLayer.Root.gameObject, true);
                    Selection.activeObject = BackgroundLayer.Root.gameObject.GetComponentInChildren<Grid>();
                    break;
                case LayerName.Normal:
                    SceneVisibilityManager.instance.Hide(BackgroundLayer.Root.gameObject, true);
                    SceneVisibilityManager.instance.Show(NormalLayer.Root.gameObject, true);
                    SceneVisibilityManager.instance.Hide(FrontLayer.Root.gameObject, true);
                    Selection.activeObject = NormalLayer.Root.gameObject.GetComponentInChildren<Grid>();
                    break;
                case LayerName.Front:
                    SceneVisibilityManager.instance.Hide(BackgroundLayer.Root.gameObject, true);
                    SceneVisibilityManager.instance.Hide(NormalLayer.Root.gameObject, true);
                    SceneVisibilityManager.instance.Show(FrontLayer.Root.gameObject, true);
                    Selection.activeObject = FrontLayer.Root.gameObject.GetComponentInChildren<Grid>();
                    break;
                case LayerName.All:
                    SceneVisibilityManager.instance.Show(BackgroundLayer.Root.gameObject, true);
                    SceneVisibilityManager.instance.Show(NormalLayer.Root.gameObject, true);
                    SceneVisibilityManager.instance.Show(FrontLayer.Root.gameObject, true);
                    break;
            }

            UpdateFrontDecoDisplay(SessionState.GetBool("FrontDecoHidden", false));
        }

        public LayerName ShownLayer()
        {
            if (FrontLayer.Root.gameObject.activeInHierarchy)
                return LayerName.Front;
            
            if (NormalLayer.Root.gameObject.activeInHierarchy)
                return LayerName.Normal;
            
            return LayerName.Background;
        }

        public void SetExpand()
        {
            
        }
#endif
    }

    
#if UNITY_EDITOR
    [CustomEditor(typeof(LevelAuthoring))]
    public class LevelAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty m_ZDistanceProperty;
        
        private void OnEnable()
        {
            m_ZDistanceProperty = serializedObject.FindProperty(nameof(LevelAuthoring.ZBackLayerDistance));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var propertyField = new PropertyField(m_ZDistanceProperty);
            root.Add(propertyField);
            
            return root;
        }
    }
    
    [Overlay(typeof(SceneView), "LevelAuthoringOverlay", "Level Authoring", defaultDisplay = true)]
    public class LevelAuthoringSceneTool : Overlay
    {
        public static Action DisplayModeChanged;
        public static Action<bool> FrontDecoTileDisplayChanged;
        
        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;

            var hideFrontLayerToggle = new Toggle("Hide Front Cave Decor");
            
            var frontHidden = SessionState.GetBool("FrontDecoHidden", false);
            hideFrontLayerToggle.SetValueWithoutNotify(frontHidden);
            hideFrontLayerToggle.RegisterValueChangedCallback(evt =>
            {
                FrontDecoTileDisplayChanged?.Invoke(evt.newValue);
                SessionState.SetBool("FrontDecoHidden", evt.newValue);
            });
            
            root.Add(hideFrontLayerToggle);
            
            OverlayToolbar toolbar = new OverlayToolbar();
            root.Add(toolbar);

            var showAllLayer = new EditorToolbarToggle
            {
                text = "All"
            };
            toolbar.Add(showAllLayer);
            
            var showFrontLayer = new EditorToolbarToggle
            {
                text = "Front"
            };
            toolbar.Add(showFrontLayer);
            
            var showNormalLayer = new EditorToolbarToggle
            {
                text = "Normal"
            };
            toolbar.Add(showNormalLayer);
                        
            var showBackgroundLayer = new EditorToolbarToggle
            {
                text = "Background"
            };
            toolbar.Add(showBackgroundLayer);

            toolbar.SetupChildrenAsButtonStrip();
            
            var displayedLayer = (LevelAuthoring.LayerName)SessionState.GetInt("DisplayedLevelLayer", (int)LevelAuthoring.LayerName.Front);
            
            showBackgroundLayer.SetValueWithoutNotify(displayedLayer == LevelAuthoring.LayerName.Background);
            showNormalLayer.SetValueWithoutNotify(displayedLayer == LevelAuthoring.LayerName.Normal);
            showFrontLayer.SetValueWithoutNotify(displayedLayer == LevelAuthoring.LayerName.Front);
            showAllLayer.SetValueWithoutNotify(displayedLayer == LevelAuthoring.LayerName.All);

            showBackgroundLayer.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    var newDisplayedLayer = LevelAuthoring.LayerName.Background;
                    
                    showNormalLayer.SetValueWithoutNotify(false);
                    showFrontLayer.SetValueWithoutNotify(false);
                    showAllLayer.SetValueWithoutNotify(false);

                    SessionState.SetInt("DisplayedLevelLayer", (int)newDisplayedLayer);
                    DisplayModeChanged?.Invoke();
                }
            });
            showNormalLayer.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    var newDisplayedLayer = LevelAuthoring.LayerName.Normal;
                    
                    showBackgroundLayer.SetValueWithoutNotify(false);
                    showFrontLayer.SetValueWithoutNotify(false);
                    showAllLayer.SetValueWithoutNotify(false);
                    
                    SessionState.SetInt("DisplayedLevelLayer", (int)newDisplayedLayer);
                    DisplayModeChanged?.Invoke();
                }
            });
            showFrontLayer.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    var newDisplayedLayer = LevelAuthoring.LayerName.Front;
                    
                    showBackgroundLayer.SetValueWithoutNotify(false);
                    showNormalLayer.SetValueWithoutNotify(false);
                    showAllLayer.SetValueWithoutNotify(false);
                    
                    SessionState.SetInt("DisplayedLevelLayer", (int)newDisplayedLayer);
                    DisplayModeChanged?.Invoke();
                }
            });
            showAllLayer.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    var newDisplayedLayer = LevelAuthoring.LayerName.All;
                    
                    showBackgroundLayer.SetValueWithoutNotify(false);
                    showNormalLayer.SetValueWithoutNotify(false);
                    showFrontLayer.SetValueWithoutNotify(false);
                    
                    SessionState.SetInt("DisplayedLevelLayer", (int)newDisplayedLayer);
                    DisplayModeChanged?.Invoke();
                }
            });
            
            return root;
        }
    }
#endif
}

