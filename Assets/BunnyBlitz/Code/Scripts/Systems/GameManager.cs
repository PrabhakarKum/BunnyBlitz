using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BunnyBlitz
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        class CustomCollectibleEntry
        {
            public Sprite IconSprite;
            public int Current;
            public int Max;
        }

        public static GameManager Instance { get; private set; }

        public PlayerBehaviour Player => m_Player;
        public GameHUDHandler HUD => m_HUD;

        public Camera MainCamera { private set; get; }

        [Header("Game Settings")]
        public int StartingLive = 3;

        [Header("Pooling")]
        public PoolingSystem PoolingSystem = new();
        
        [Header("Sounds")]
        public AudioManager AudioManager = new();

        [Header("Data")] 
        public TileToSurfaceTypeLookup TileToSurfaceTypeLookup;
        
        [Header("Scene Reference")] 
        public CinemachineCamera CloseUpCamera;
        public CinemachineCamera FarCamera;
        
        public event Action<int> OnLayerChange;

        [Header("Prefabs")] 
        public GameObject PlayerPrefab;
        public GameHUDHandler HUDPrefab;

        [Header("Graphics")]
        [Range(0.0f, 1.0f)]
        public float BackgroundBlurStrength = 1.0f;

        public CollectibleVfxManager VfxController
        {
            get => m_VfxController;
            private set => m_VfxController = value;
        }

        public SaveSystem SaveSystem { get; private set; } = new();
        public LevelData CurrentLevelData { get; private set; }

        public LevelLayer CurrentLayer => m_CurrentLayer;
        public LevelLayer BackgroundLayer => m_BackgroundLayer;
        public LevelLayer NormalLayer => m_NormalLayer;
        public LevelLayer FrontLayer => m_FrontLayer;

        public string MainCustomCollectibleName => m_MainCustomCollectibleName;

        public Action OnPlayerRespawn;

        private LevelLayer m_BackgroundLayer;
        private LevelLayer m_NormalLayer;
        private LevelLayer m_FrontLayer;
        private LevelLayer m_CurrentLayer;

        private Dictionary<string, CustomCollectibleEntry> m_CustomCollectibles = new();
        private Vector2 m_currentProjectileDirection;
        private CollectibleVfxManager m_VfxController;

        private SpawnPoint m_CurrentSpawnPoint;
        private PlayerBehaviour m_Player;
        private GameHUDHandler m_HUD;

        private BlurRenderPass2D m_BackgroundBlurPass;

        private string m_MainCustomCollectibleName;

        private void Awake()
        {
            if (Instance != null)
            {
                //we already have an instance, so transfer all the new local scene references to the existing one
                Instance.CloseUpCamera = CloseUpCamera;
                Instance.FarCamera = FarCamera;

                Destroy(gameObject);
                return;
            }

            Instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            DontDestroyOnLoad(gameObject);

            m_HUD = Instantiate(HUDPrefab);
            m_HUD.Init();
            m_HUD.FadeOut(true, null);
            DontDestroyOnLoad(m_HUD);

            m_VfxController = GetComponent<CollectibleVfxManager>();
            
            m_BackgroundBlurPass = new BlurRenderPass2D
            {
                renderPassEvent2D = RenderPassEvent2D.AfterRenderingSprites,
                renderPassSortingLayerID = SortingLayer.NameToID("Layer Back"),
                FxMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Custom/SimpleBlurPass")),
                BlurAmount = 1.0f,
                BlurStrength = BackgroundBlurStrength
            };

            RenderPipelineManager.beginCameraRendering += InjectBlurPass;
            
            SaveSystem.Load();
            //reset the live to the defualt starting value between app run, as those are not persistent
            SaveSystem.Data.LiveCount = StartingLive;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        { 
            if (m_VfxController == null)
            {
                Debug.LogWarning("VFX Controller not set");
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //we find if we are a gameplay scene or other
            var levelSelect = FindAnyObjectByType<LevelSelectionScreen>();

            if (levelSelect != null)
            {
                //this is level select, so we disable the player and the gameUI
                if(m_Player != null)
                    m_Player.gameObject.SetActive(false);
                
                m_HUD?.gameObject.SetActive(false);
                return;
            }
            
            CurrentLevelData = FindAnyObjectByType<LevelData>();
            if (CurrentLevelData == null)
            {
                var go = new GameObject("LevelData");
                CurrentLevelData = go.AddComponent<LevelData>();
                CurrentLevelData.LevelName = "Untitled Scene";
            }
            
            TileToSurfaceTypeLookup.InitLookup();
            CurrentLevelData.Init();
            
            //find the level layers
            var layers = FindObjectsByType<LevelLayer>(FindObjectsInactive.Include);
            foreach (var layer in layers)
            {
                switch (layer.Layer)
                {
                    case LevelAuthoring.LayerName.Background:
                        m_BackgroundLayer = layer;
                        break;
                    case LevelAuthoring.LayerName.Normal:
                        m_NormalLayer = layer;
                        break;
                    case LevelAuthoring.LayerName.Front:
                        m_FrontLayer = layer;
                        break;
                }
            }

            StartCoroutine(OnLevelLoaded());
        }

        void InjectBlurPass(ScriptableRenderContext context, Camera camera)
        {
            if(camera.CompareTag("MainCamera"))
                camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(m_BackgroundBlurPass);
        }

        void OnDestroy()
        {
            PoolingSystem.Cleanup();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RenderPipelineManager.beginCameraRendering -= InjectBlurPass;
        }

        // Update is called once per frame
        void Update()
        {
            //for debugging

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                m_Player.transform.position = Vector3.zero;
                m_Player.Init();
                SwitchPlayLayer(m_NormalLayer);
                m_Player.WasRespawn();
            }

            if (MainCamera != null)
            {
                //Set the Listener in the center of the screen at the depth of the player
                var pos = MainCamera.transform.position;
                pos.z = m_Player.transform.position.z;
                AudioManager.Listener.transform.position = pos;
            }
            
            AudioManager.Tick();
        }

        IEnumerator OnLevelLoaded()
        {
            AudioManager.Init();
            
            //wait that the hud is init, the PanelRenderer may need some frame to init
            while (!m_HUD.IsInitialized)
                yield return null;

            HUD.FadeOut(true, null);
            HUD.ShowLevelName(CurrentLevelData.LevelName);
            
            // This fix a regular mistake in level design : a player is left over somewhere as it was used to test lighting etc.
            // in the scene, which lead to having 2 players. So if we already have a player, we delete it to ensure we only have our new spawned player
            var existingPlayers = FindObjectsByType<PlayerBehaviour>(FindObjectsInactive.Include);
            foreach (var player in existingPlayers)
            {
                Destroy(player.gameObject);
            }

            if (m_Player == null)
            {
                var player = Instantiate(PlayerPrefab);
                m_Player = player.GetComponent<PlayerBehaviour>();
                m_Player.gameObject.SetActive(false);
                m_Player.Hide();
            }
            
            m_Player.Init();
            
            //we wait a bit, this will let some setting execute before (like an instant fadeout) before going through
            //the level setup
            yield return new WaitForSeconds(0.1f);
            
            CurrentLevelData.LevelOverrides.ApplyOverride(m_Player);
            
            m_Player?.gameObject.SetActive(true);
            m_HUD?.gameObject.SetActive(true);

            PoolingSystem.Init();
            m_VfxController.Init();

            HUD.StartLevel();

            var cam = GameObject.FindGameObjectWithTag("MainCamera");
            MainCamera = cam.GetComponent<Camera>();

            //find all collectibles and register the custom one
            var oneOfFiveSprites = new Sprite[5];
            var collectibles = FindObjectsByType<CollectibleItem>(FindObjectsInactive.Include);
            foreach (var c in collectibles)
            {
                if (c.ItemType == CollectibleItem.CollectibleType.Custom)
                {
                    if (string.IsNullOrEmpty(m_MainCustomCollectibleName))
                    {
                        m_MainCustomCollectibleName = c.CustomType;
                        m_HUD.InitCustomCollectibleSprite(c.CustomSprite);
                        m_HUD.UpdateMainCustomCollectibleCount(0);
                    }
                    
                    if (!m_CustomCollectibles.TryGetValue(c.CustomType, out var collectibleEntry))
                    {
                        collectibleEntry = new CustomCollectibleEntry() { Current = 0, Max = 0, IconSprite = c.CustomSprite };
                        m_CustomCollectibles[c.CustomType] = collectibleEntry;
                    }

                    collectibleEntry.Max += 1;
                }
                else if (c.ItemType == CollectibleItem.CollectibleType.OneOfFive)
                {
                    if (c.CollectibleIndex >= 0 && c.CollectibleIndex < 5)
                    {
                        oneOfFiveSprites[c.CollectibleIndex] = c.CustomSprite;
                    }
                }
            }
            
            m_HUD.InitOneOfFive(oneOfFiveSprites);

            foreach (var pair in m_CustomCollectibles)
            {
                HUD.RegisterCustomCollectible(pair.Key, pair.Value.IconSprite, pair.Value.Max);
            }

            var allSpawns = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include);
            foreach (var spawn in allSpawns)
            {
                if (spawn.IsStartSpawn)
                {
                    m_CurrentSpawnPoint = spawn;

                    var playerMovement = m_Player.GetComponent<PlayerMovement>();
                    playerMovement.CloseUpCam = CloseUpCamera;

                    CloseUpCamera.Target.TrackingTarget = m_Player.transform;
                    FarCamera.Target.TrackingTarget = m_Player.transform;

                    RespawnPlayer();

                    yield return new WaitForSeconds(1.0f);

                    HUD.HideLevelName();

                    yield break;
                }
            }
                
            
            Debug.LogError("There was no Spawn Point set");
        }

        public void TravelingToLayer(LevelLayer targetLayer, float distanceRatio)
        {
            if (targetLayer == m_BackgroundLayer)
            {
                m_BackgroundBlurPass.BlurAmount = 1.0f - distanceRatio;
            }
            else if(m_CurrentLayer == m_BackgroundLayer)
            {
                //moving away from the bakcground layer, reblur it
                m_BackgroundBlurPass.BlurAmount = distanceRatio;
            }
        }

        public void SwitchPlayLayer(LevelLayer layer)
        {
            m_CurrentLayer = layer;
            //public event with the current SortingLayerID
            var sortGroup = m_CurrentLayer.GetComponent<SortingGroup>().sortingLayerID;
            OnLayerChange?.Invoke(sortGroup);
            //move player plane
            m_Player.gameObject.layer = m_CurrentLayer.gameObject.layer;
            m_Player.GetComponent<TrailRenderer>().Clear();

            if (m_CurrentLayer == m_BackgroundLayer)
            {
                m_CurrentLayer = m_BackgroundLayer;
                m_BackgroundBlurPass.renderPassSortingLayerID = SortingLayer.NameToID("Background");
                m_BackgroundBlurPass.BlurAmount = 1.0f;
                m_Player.MoveTorchToNewLayer(m_BackgroundLayer.FrontDecoRoot, sortGroup);
            } 
            else if (m_CurrentLayer == m_NormalLayer)
            {
                m_BackgroundBlurPass.renderPassSortingLayerID = SortingLayer.NameToID("Layer Back Front Deco");
                m_BackgroundBlurPass.BlurAmount = 1.0f;
                m_CurrentLayer = m_NormalLayer;
                m_Player.MoveTorchToNewLayer(m_NormalLayer.FrontDecoRoot, sortGroup);
            }
            else if (m_CurrentLayer == m_FrontLayer)
            {
                m_BackgroundBlurPass.renderPassSortingLayerID = SortingLayer.NameToID("Layer Normal Front Deco");
                m_BackgroundBlurPass.BlurAmount = 1.0f;
                m_CurrentLayer = m_FrontLayer;
                m_Player.MoveTorchToNewLayer(m_FrontLayer.FrontDecoRoot, sortGroup);
            }

            m_Player.gameObject.transform.SetParent(m_CurrentLayer.transform);
        }

        public void StatsCollectItem(string item)
        {
            if (m_CustomCollectibles.TryGetValue(item, out var collectibleEntry))
            {
                collectibleEntry.Current += 1;
                
                if(item == m_MainCustomCollectibleName)
                    HUD.UpdateMainCustomCollectibleCount(collectibleEntry.Current);
                
                HUD.SetCustomCollectibleCount(item, collectibleEntry.Current, collectibleEntry.Max);
            }
        }

        public int GetCustomCollectedItemCount(string item)
        {
            if (item == null)
                return 0;
            
            return m_CustomCollectibles.TryGetValue(item, out var collectibleEntry) ? collectibleEntry.Current : 0;
        }
        
        public int GetCustomCollectedItemMax(string item)
        {
            if (item == null)
                return 0;
            
            return m_CustomCollectibles.TryGetValue(item, out var collectibleEntry) ? collectibleEntry.Max : 0;
        }

        public void SetNewSpawnPoint(SpawnPoint point)
        {
            m_CurrentSpawnPoint = point;
        }

        public void SwitchCharacterControl(bool enabled)
        {
            m_Player.SwitchInput(enabled);
        }

        public void SetExplicitCaveLightScale(float scale)
        {
            m_Player.SetExplicitMaskScale(scale);
        }

        public void LoadNewLevel(string levelName)
        {
            m_Player.SwitchInput(false);
            m_HUD.FadeOut(false,  () => { SceneManager.LoadScene(levelName); });
        }

        public void RespawnPlayer()
        {
            SwitchPlayLayer(GetLayerOfObject(m_CurrentSpawnPoint.gameObject));
#if UNITY_EDITOR
            //Only in editor we may have a forced spawn position through the menu entry
            if (SessionState.GetBool("IsForcedSpawned", false))
            {
                m_Player.transform.position = SessionState.GetVector3("ForcedSpawnPos", m_Player.transform.position);
                SessionState.SetBool("IsForcedSpawned", false);
            }
            else
            {
#endif
                m_Player.transform.position = m_CurrentSpawnPoint.SpawnPosition != null ? m_CurrentSpawnPoint.SpawnPosition.position : m_CurrentSpawnPoint.transform.position;
#if UNITY_EDITOR
            }
#endif
            
            m_HUD.FadeIn(false, () =>
            {
                m_Player.WasRespawn();
            });

            OnPlayerRespawn?.Invoke();
        }

        public IEnumerator PlayerDeath()
        {
            yield return new WaitForSeconds(1.0f);
            
            m_HUD.FadeOut(false, () =>
            {
                if(m_Player.LivesCount > 0)
                    RespawnPlayer();
                else
                {
                    m_Player.ResetLivesCountTo(3);
                    HUD.ReturnToMainMenu();
                }
            });
        }

        public void StartBossBattle(StaticBossBehaviour boss)
        {
            //Boss healthbar was removed in the last version of the project but kept commented as reference
            //HUD.InitBossHealthbar(boss.Phases.Select(p => p.DamageBeforeStomp).ToArray());
            Player.SetExplicitMaskScale(6.0f);
        }

        public void FinishLevel(Vector3 portalPosition, CinemachineCamera camera, float waitingTime, GameObject[] objectsToEnable,float waitingTimeToUI)
        {
            if(camera != null)
                camera.gameObject.SetActive(true);
            StartCoroutine(MoveThroughEndPortal(portalPosition, waitingTime, objectsToEnable,waitingTimeToUI));
        }

        public void ToggleHUD(bool enabled)
        {
            m_HUD.ToggleVisibility(enabled);
        }

        public IEnumerator MoveThroughEndPortal(Vector3 portalPosition, float waitingTime, GameObject[] objectsToEnable, float waitingTimeToEndUI)
        {
            m_Player.SwitchModelMasked(true);

            yield return new WaitForSeconds(waitingTime);

            m_Player.StopMove();
            m_Player.SwitchInput(false);
            m_Player.GetComponentInChildren<Animator>().SetTrigger("isPortalEnding");
            var spiralEffectBhv = m_Player.GetComponent<SpiralToTarget>();
            if (spiralEffectBhv!=null)
            {
                spiralEffectBhv.targetPosition = new Vector3(portalPosition.x,portalPosition.y,m_Player.transform.position.z-3f);
                spiralEffectBhv.StartSpiralEffect();
            }

            foreach (var o in objectsToEnable)
            {
                o.SetActive(true);
            }
            
            yield return new WaitForSeconds(waitingTimeToEndUI);

            HandleEndLevel();
        }

        void HandleEndLevel()
        {
            string currentLevelName = SceneManager.GetSceneAt(0).name;
            var levelData = SaveSystem.GetOrCreateLevelData(currentLevelName);

            var currentPercentage = CurrentLevelData.GetCompletedPercentage();
            var bestPercentage = levelData.Percentage;

            if (currentPercentage > bestPercentage)
            {
                levelData.Percentage = currentPercentage;
                bestPercentage = currentPercentage;
            }

            m_Player.SaveData();
            SaveSystem.Save();

            m_Player.StopMove();
            m_Player.SwitchInput(false);
            CurrentLevelData.LevelOverrides.RevertValue(m_Player);
            HUD.ShowEndLevel(CurrentLevelData, currentPercentage, bestPercentage);
        }

        public LevelLayer GetLayerOfObject(GameObject obj)
        {
            if (obj.layer == m_BackgroundLayer.gameObject.layer)
                return m_BackgroundLayer;

            if (obj.layer == m_FrontLayer.gameObject.layer)
                return m_FrontLayer;

            return m_NormalLayer;
        }
    }
}
