using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace BunnyBlitz
{
    public class LevelSelectionScreen : MonoBehaviour
    {
        [Serializable]
        public class LevelEntry
        {
            public string SceneName;
            public Texture2D ScenePreview;
        }
        
        public Transform LogoRoot;
        public float CameraMoveAmount = 0.5f;
        public float LogoMaxXAngle = 45.0f;
        public float LogoMaxYAngle = 45.0f;
        public bool RotateLogoAroundY = false;
        public bool RotateLogoAroundX = true;
        
        public VisualTreeAsset LevelSelectTemplate;
        public LevelEntry[] Levels;

        private Camera m_Camera;
        private PanelRenderer m_PanelRenderer;

        private Vector3 m_CameraAnchorPoint;
        private Quaternion m_OrignalLogoRotation;

        private int m_UIVersion = 0;

        private void OnEnable()
        {
            SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
            
            m_Camera = FindAnyObjectByType<Camera>();
            m_CameraAnchorPoint = m_Camera.transform.position;

            if(LogoRoot)
            {
                m_OrignalLogoRotation = LogoRoot.transform.rotation;
            }

            m_PanelRenderer = GetComponent<PanelRenderer>();
            m_PanelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            m_PanelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if(m_UIVersion == version)
                return;

            m_UIVersion = version;
            
            var levelScroller = rootElement.Q<ScrollView>("level-scroller");
            foreach (var level in Levels)
            {
                var template = LevelSelectTemplate.CloneTree();
                template.AddToClassList("level-select-template-root");

                template.AddManipulator(new Clickable(() =>
                {
                    GameManager.Instance.HUD.gameObject.SetActive(true);
                    GameManager.Instance.HUD.FadeOut(true, () => { SceneManager.LoadScene(level.SceneName); });
                }));

                var entry = template.Q<VisualElement>("level-entry");
                entry.style.backgroundImage = Background.FromTexture2D(level.ScenePreview);
                
                if (level.SceneName != null)
                {
                    var levelData = GameManager.Instance.SaveSystem.GetOrCreateLevelData(level.SceneName);
                    var label = template.Q<Label>("level-completion-amount");
                    label.text = $"{levelData.Percentage}%";
                }

                levelScroller.Add(template);
            }
        }

        private void Update()
        {
            var mousePos = Mouse.current.position.ReadValue();
            var centerToCursor = mousePos - new Vector2(Screen.width / 2, Screen.height / 2);
            centerToCursor.y *= -1;
            var ratio = centerToCursor.magnitude / Screen.height;
            
            m_Camera.transform.position = m_CameraAnchorPoint + ((Vector3)centerToCursor).normalized * (ratio * CameraMoveAmount);

            if(LogoRoot)
            {   
                var logoScreenCenter = m_Camera.WorldToScreenPoint(LogoRoot.position);
                var cursorToLogoCenter = (Vector3)mousePos - logoScreenCenter;

                var height = Mathf.Min(logoScreenCenter.y, Screen.height - logoScreenCenter.y);
                var width = Mathf.Min(logoScreenCenter.x, Screen.width - logoScreenCenter.x);

                float xRatio = Mathf.Clamp(cursorToLogoCenter.x / width, -1.0f, 1.0f);
                float yRatio = Mathf.Clamp(cursorToLogoCenter.y / height, -1.0f, 1.0f);
                var rot = m_OrignalLogoRotation;
                if(RotateLogoAroundY)
                    rot *= Quaternion.AngleAxis(LogoMaxYAngle * xRatio, Vector3.up);
                if(RotateLogoAroundX)
                    rot *= Quaternion.AngleAxis(LogoMaxXAngle * yRatio, Vector3.right);
                
                LogoRoot.transform.rotation = rot;
            }
        }
    }
}

#if UNITY_EDITOR
namespace BunnyBlitz.Editor
{
    [CustomEditor(typeof(LevelSelectionScreen))]
    public class LevelSelectionEditor : UnityEditor.Editor
    {
        private SerializedProperty m_CameraMoveProperty;
        private SerializedProperty m_LogoRootProperty;
        private SerializedProperty m_MaxRotateAngleXProperty;
        private SerializedProperty m_MaxRotateAngleYProperty;
        private SerializedProperty m_RotateAroundXProperty;
        private SerializedProperty m_RotateAroundYProperty;
        private SerializedProperty m_LevelSelectTemplateProperty;
        private SerializedProperty m_LevelsProperty;

        private void OnEnable()
        {
            m_LevelSelectTemplateProperty =
                serializedObject.FindProperty(nameof(LevelSelectionScreen.LevelSelectTemplate));
            m_LevelsProperty = serializedObject.FindProperty(nameof(LevelSelectionScreen.Levels));
            m_CameraMoveProperty = serializedObject.FindProperty(nameof(LevelSelectionScreen.CameraMoveAmount));

            m_LogoRootProperty = serializedObject.FindProperty(nameof(LevelSelectionScreen.LogoRoot));
            m_MaxRotateAngleXProperty = serializedObject.FindProperty(nameof(LevelSelectionScreen.LogoMaxXAngle));
            m_MaxRotateAngleYProperty = serializedObject.FindProperty(nameof(LevelSelectionScreen.LogoMaxYAngle));
            m_RotateAroundXProperty = serializedObject.FindProperty(nameof(LevelSelectionScreen.RotateLogoAroundX));
            m_RotateAroundYProperty = serializedObject.FindProperty(nameof(LevelSelectionScreen.RotateLogoAroundY));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            root.Add(new PropertyField(m_CameraMoveProperty));

            root.Add(new PropertyField(m_LogoRootProperty));
            root.Add(new PropertyField(m_MaxRotateAngleXProperty));
            root.Add(new PropertyField(m_MaxRotateAngleYProperty));
            root.Add(new PropertyField(m_RotateAroundXProperty));
            root.Add(new PropertyField(m_RotateAroundYProperty));


            var levelSelectTemplate = new PropertyField(m_LevelSelectTemplateProperty);
            root.Add(levelSelectTemplate);

            var levelList = new ListView();
            levelList.reorderable = true;
            levelList.showBoundCollectionSize = false;
            levelList.allowAdd = true;
            levelList.allowRemove = true;
            levelList.showAddRemoveFooter = true;
            levelList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            levelList.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;

            levelList.makeItem = () =>
            {
                var elem = new VisualElement();

                var sceneSelection = new DropdownField();
                sceneSelection.RegisterValueChangedCallback(evt =>
                {
                    var prop = sceneSelection.userData as SerializedProperty;
                    prop.stringValue = evt.newValue;
                    prop.serializedObject.ApplyModifiedProperties();
                });
                sceneSelection.name = "scene-idx-dropdown";
                sceneSelection.label = "Scene";
                elem.Add(sceneSelection);

                var scenePreview = new PropertyField();
                scenePreview.name = "scene-preview";
                scenePreview.label = "Preview";
                elem.Add(scenePreview);
                return elem;
            };

            levelList.bindItem = (elem, i) =>
            {
                var itm = m_LevelsProperty.GetArrayElementAtIndex(i);
                var sceneNameProp = itm.FindPropertyRelative(nameof(LevelSelectionScreen.LevelEntry.SceneName));

                var list = new List<string>();
                foreach (var s in EditorBuildSettings.scenes)
                {
                    list.Add(System.IO.Path.GetFileNameWithoutExtension(s.path));
                }

                var selectedScene = sceneNameProp.stringValue;

                var sceneDropDown = elem.Q<DropdownField>();
                sceneDropDown.userData = sceneNameProp;
                sceneDropDown.choices = list;
                sceneDropDown.SetValueWithoutNotify(selectedScene);

                var scenePreviewField = elem.Q<PropertyField>("scene-preview");
                scenePreviewField.BindProperty(
                    itm.FindPropertyRelative(nameof(LevelSelectionScreen.LevelEntry.ScenePreview)));
            };

            levelList.BindProperty(m_LevelsProperty);
            root.Add(levelList);

            return root;
        }
    }
}
#endif