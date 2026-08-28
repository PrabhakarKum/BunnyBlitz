using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

namespace BunnyBlitz.Editor
{
    public static class AuthoringEditorUtils
    {
        [MenuItem("Bunny Blitz/Select Level Data", priority = 0)]
        static void SelectLevelData()
        {
            var data = Object.FindAnyObjectByType<LevelData>();
            if (data == null)
            {
                var newObj = new GameObject("LevelData");
                data = newObj.AddComponent<LevelData>();
                Undo.RegisterCreatedObjectUndo(newObj, "New LevelData");
            }

            Selection.activeGameObject = data.gameObject;
        }

        [MenuItem("Bunny Blitz/Select Game Manager Prefab", priority = 0)]
        static void SelectGameManager()
        {
            var found = AssetDatabase.FindAssets("t:GameObject GameManager");
            if (found.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(found[0]);
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log(obj.name, obj);
                Selection.activeGameObject = obj;
            }
        }
        
        [MenuItem("Bunny Blitz/Center Start Spawn", priority = 0)]
        static void CenterSpawnPoint()
        {
            var founds = GameObject.FindObjectsByType<SpawnPoint>();
            foreach (var spawn in founds)
            {
                if (spawn.IsStartSpawn)
                {
                    var pos = SceneView.GetAllSceneCameras()[0].transform.position;
                    pos.z = 0.0f;
                    spawn.transform.position = pos;
                    break;
                }
            }
        }

        [MenuItem("Bunny Blitz/Play From Scene View Position", priority = 0)]
        static void PlayFromThere()
        {
            var pos = SceneView.GetAllSceneCameras()[0].transform.position;
            pos.z = 0.0f;
            SessionState.SetBool("IsForcedSpawned", true);
            SessionState.SetVector3("ForcedSpawnPos", pos);
            EditorApplication.EnterPlaymode();
        }
    }

    [Overlay(typeof(SceneView), "Level Stat", true)]
    public class LevelStatInfo : Overlay
    {
        private Label m_EnemiesCount;
        private Label m_CoinCounter;
        private Label m_SpecialCollectibleCounter;
        private Label m_OneOfFiveCounter;
        private Label m_SpringCounter;
        private Label m_MovingPlatformCounter;
        
        private HelpBox m_ErrorHelpBox;
        
        public override void OnCreated()
        {
            base.OnCreated();
            EditorApplication.hierarchyChanged += UpdateStat;
        }

        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();
            EditorApplication.hierarchyChanged -= UpdateStat;
        }

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();

            m_EnemiesCount = new Label("Enemies");
            root.Add(m_EnemiesCount);
            
            m_CoinCounter = new Label("Coins");
            root.Add(m_CoinCounter);
            
            m_SpecialCollectibleCounter = new Label("Special Collectibles");
            root.Add(m_SpecialCollectibleCounter);
            
            m_OneOfFiveCounter = new Label("One of Five Collectibles");
            root.Add(m_OneOfFiveCounter);
            
            m_SpringCounter = new Label("Springs");
            root.Add(m_SpringCounter);
            
            m_MovingPlatformCounter = new Label("Moving Platforms");
            root.Add(m_MovingPlatformCounter);

            m_ErrorHelpBox = new HelpBox();
            m_ErrorHelpBox.messageType = HelpBoxMessageType.Error;
            root.Add(m_ErrorHelpBox);
            m_ErrorHelpBox.style.display = DisplayStyle.None;

            UpdateStat();
            
            return root;
        }

        void UpdateStat()
        {
            //it seems that sometime in some rare reload case of the editor, the label are destroyed, but this still get called
            //probably some timing between will be destroyed where the callback is unregistered and it being called with destroyed
            //label. So just double checking we do have our label to fill here
            if(m_EnemiesCount == null)
                return;

            var collectible = Object.FindObjectsByType<CollectibleItem>(FindObjectsInactive.Exclude);

            int coinCount = 0;
            int specialCount = 0;
            bool[] oneOfFivePresent = new[] { false, false, false, false, false };
            string errorMessage = "";
            
            foreach (var c in collectible)
            {
                if (c.ItemType == CollectibleItem.CollectibleType.Coin)
                    coinCount += 1;
                else if (c.ItemType == CollectibleItem.CollectibleType.Custom)
                    specialCount += 1;
                else if (c.ItemType == CollectibleItem.CollectibleType.OneOfFive)
                {
                    if (c.CollectibleIndex < 0 || c.CollectibleIndex >= 5)
                        errorMessage += "There is a OneOfFive with an index outside of the 0-4 range\n";
                    else
                    {
                        if (oneOfFivePresent[c.CollectibleIndex])
                        {
                            errorMessage += $"There is multiple One Of Five with index {c.CollectibleIndex}";
                        }
                        else
                        {
                            oneOfFivePresent[c.CollectibleIndex] = true;
                        }
                    }
                }
            }

            var enemies = Object.FindObjectsByType<EnemyBehaviour>(FindObjectsInactive.Exclude);
            m_EnemiesCount.text = $"Enemies: {enemies.Length}";
            
            m_CoinCounter.text = "Coins: " + coinCount;
            m_SpecialCollectibleCounter.text = "Special Collectible: " + specialCount;
            
            string oneOfFive = "Five Collectible : ";
            for (int i = 0; i < 5; ++i)
            {
                oneOfFive += oneOfFivePresent[i] ? "O" : "X";
            }
            m_OneOfFiveCounter.text = oneOfFive;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                m_ErrorHelpBox.text = errorMessage;
                m_ErrorHelpBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_ErrorHelpBox.style.display = DisplayStyle.None;
            }

            m_SpringCounter.text = "Springs: " + Object.FindObjectsByType<SpringBehaviour>().Length;
            m_MovingPlatformCounter.text =
                "Platform: " + Object.FindObjectsByType<ActivablePlatform>().Length;

        }
        
        // SplineShape Utilities
        [MenuItem("Bunny Blitz/SpriteShape/Set Selected SpriteShape Spline Details to 64", priority=10)]
        static void SetSpriteShapeDetailTo64()
        {
            Undo.SetCurrentGroupName("Setting Spline Details");
            int group = Undo.GetCurrentGroup();
            foreach (var o in Selection.gameObjects)
            {
                var controller = o.GetComponent<SpriteShapeController>();
                if (controller != null)
                {
                    Undo.RegisterCompleteObjectUndo(controller, "SetSplineDetail");
                    controller.splineDetail = 64;
                }
            }
            Undo.CollapseUndoOperations(group);
        }
        
        [MenuItem("Bunny Blitz/SpriteShape/Set Selected SpriteShape Spline Details to 128", priority=12)]
        static void SetSpriteShapeDetailTo128()
        {
            Undo.SetCurrentGroupName("Setting Spline Details");
            int group = Undo.GetCurrentGroup();
            foreach (var o in Selection.gameObjects)
            {
                var controller = o.GetComponent<SpriteShapeController>();
                if (controller != null)
                {
                    Undo.RegisterCompleteObjectUndo(controller, "SetSplineDetail");
                    controller.splineDetail = 128;
                }
            }
            
            Undo.CollapseUndoOperations(group);
        }
        
        [MenuItem("Bunny Blitz/SpriteShape/Reset Selected SpriteShape Spline Scale", priority=15)]
        static void ResetSpriteShapeScale()
        {
            Undo.SetCurrentGroupName("Reset SpriteShape scale");
            int group = Undo.GetCurrentGroup();
            foreach (var o in Selection.gameObjects)
            {
                var controller = o.GetComponent<SpriteShapeController>();
                if (controller != null)
                {
                    Undo.RegisterCompleteObjectUndo(controller, "Reset Sprite Shape Scale");
                    ResetControllerScale(controller);
                }
            }
            
            Undo.CollapseUndoOperations(group);
        }

        static void ResetControllerScale(SpriteShapeController controller)
        {
            
            var spline = controller.spline;
            var pointCount = spline.GetPointCount();
            var scale = controller.transform.localScale;
            for (int i = 0; i < pointCount; ++i)
            {
                var localPts = spline.GetPosition(i);
                localPts.Scale(controller.transform.localScale);
                spline.SetPosition(i, localPts);

                var lt = spline.GetLeftTangent(i);
                lt.Scale(scale);
                spline.SetLeftTangent(i, lt);
                
                var rt = spline.GetRightTangent(i);
                rt.Scale(scale);
                spline.SetRightTangent(i, rt);
            }

            Undo.RegisterCompleteObjectUndo(controller.transform, "Set scale");
            controller.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(controller.gameObject);
        }
    }
}