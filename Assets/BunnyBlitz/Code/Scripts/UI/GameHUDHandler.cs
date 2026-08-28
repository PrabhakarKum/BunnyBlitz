using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BunnyBlitz
{
    [DefaultExecutionOrder(-300)]
    public class GameHUDHandler : MonoBehaviour
    {
        public bool IsInitialized => m_UIVersion != 0;
        
        public VisualTreeAsset CustomCollectibleEntry;

        [Header("Healthbars")] 
        public UIDocument HealthbarRootPrefab;
        public HealthbarHandler HealthbarHandlerPrefab;
        
        //public VisualTreeAsset BossHealthbarSegment;
        //public VisualTreeAsset BossHealthbarSubSegment;

        private PanelRenderer m_PanelRenderer;
        private VisualElement m_PanelRoot;

        private VisualElement m_VisualBlocker;

        private VisualElement m_TitleBanner;
        private Label m_TitleText;

        private Label m_CoinCounter;
        //private Label m_GemCounter;
        private Label m_ProjectileCounter;
        private VisualElement[] m_HeartsContainers;
        private Label m_LiveCounter;

        private VisualElement m_CustomCollectibleIcon;
        private Label m_CustomCollectibleCounter;
        private Image[] m_OneOfFiveIcones;

        private Button m_PauseButton;
        private VisualElement m_PauseRoot;
        private Button m_PauseReturnToMainMenuButton;

        private VisualElement m_PopupLifeContainer;
        private Label m_PopupLifeCounter;

        private ScrollView m_CustomCollectibleScrollview;

        private VisualElement m_EndOfLevelPopup;
        private Label m_EndOfLevelTitle;
        private Label m_EnemyKilledCounter;
        private Label m_OneOfFiveCounter;
        private Label m_BestCompletePercentage;
        private Label m_CompletePercentageText;
        private Button m_CompleteReturnToMainMenuButton;

        private VisualElement m_BossHealthZone;
        private VisualElement m_BossHealthBackground;

        private System.Action m_FadeCallback = null;

        private Dictionary<string, Label> m_CollectibleToLabelMap = new();

        private UIDocument m_HealthbarRoot;
        private List<HealthbarHandler> m_HealthbarEntries = new();
        private Queue<int> m_FreeHealthbar = new();
        
        private int m_UIVersion = 0;

        void Awake()
        {
            m_PanelRenderer = GetComponent<PanelRenderer>();
            m_PanelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        void OnDestroy()
        {
            m_PanelRenderer.UnregisterUIReloadCallback(OnUIReload);
            
            m_UIVersion = 0;
            
            //the UI Reload wasn't called since the OnEnable, so we don't have reference to element to cleanup
            if(m_PauseButton == null)
                return;
            
            m_PauseButton.clicked -= TogglePause;
            m_CompleteReturnToMainMenuButton.clicked -= ReturnToMainMenu;
            m_PauseReturnToMainMenuButton.clicked -= ReturnToMainMenu;
            m_VisualBlocker.UnregisterCallback<TransitionEndEvent>(TransitionEnd);
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if(m_UIVersion == version)
                return;

            m_UIVersion = version;

            m_PanelRoot = rootElement;
            m_VisualBlocker = rootElement.Q<VisualElement>("visual-blocker");

            m_CoinCounter = rootElement.Q<VisualElement>("coin-zone").Q<Label>("counter");
            //m_GemCounter = rootElement.Q<VisualElement>("gem-zone").Q<Label>("counter");
            m_ProjectileCounter = rootElement.Q<VisualElement>("projectile-zone").Q<Label>("counter");

            const int heartCount = 3;
            m_HeartsContainers = new VisualElement[heartCount];
            for (int i = 0; i < heartCount; ++i)
            {
                m_HeartsContainers[i] = rootElement.Q<VisualElement>("heart"+(i+1));
            }

            m_LiveCounter = rootElement.Q<VisualElement>("life-zone").Q<Label>("counter");
            
            m_CustomCollectibleIcon = rootElement.Q<VisualElement>("custom-zone").Q<VisualElement>("icone");
            m_CustomCollectibleCounter = rootElement.Q<VisualElement>("custom-zone").Q<Label>("counter");
            
            m_OneOfFiveIcones = new Image[5];
            var oneOfFiveElementNames = new[] { "U-image", "N-image", "I-image", "T-image", "Y-image" };
            for (int i = 0; i < 5; ++i)
                m_OneOfFiveIcones[i] = rootElement.Q<Image>(oneOfFiveElementNames[i]);

            m_TitleBanner = rootElement.Q<VisualElement>("title-banner");
            m_TitleText = rootElement.Q<Label>("title-text");
            
            HideLevelName();

            m_PauseButton = rootElement.Q<Button>("pause-button");
            m_PauseRoot = rootElement.Q<VisualElement>("pause-menu-root");

            m_PauseReturnToMainMenuButton = m_PauseRoot.Q<Button>("pause-banner-main-menu-button");
            m_PauseReturnToMainMenuButton.clicked += ReturnToMainMenu;

            m_CustomCollectibleScrollview = m_PauseRoot.Q<ScrollView>("pause-custom-collectable-list");

            m_PopupLifeContainer = rootElement.Q<VisualElement>("life-counter-popup");
            m_PopupLifeCounter = m_PopupLifeContainer.Q<Label>("life-count");

            m_EndOfLevelPopup = rootElement.Q<VisualElement>("level-end-popup");
            m_EndOfLevelTitle = m_EndOfLevelPopup.Q<Label>("level-end-title");
            m_EnemyKilledCounter = m_EndOfLevelPopup.Q<Label>("enemy-count");
            m_OneOfFiveCounter = m_EndOfLevelPopup.Q<Label>("oneoffive-count");
            m_BestCompletePercentage = m_EndOfLevelPopup.Q<Label>("level-end-best-completion");
            m_CompletePercentageText = m_EndOfLevelPopup.Q<Label>("level-end-completion");

            m_CompleteReturnToMainMenuButton = m_EndOfLevelPopup.Q<Button>("level-end-button-main-menu");
            m_CompleteReturnToMainMenuButton.clicked += ReturnToMainMenu;

            m_PopupLifeContainer.style.display = DisplayStyle.None;
            m_PauseRoot.style.display = DisplayStyle.None;
            m_EndOfLevelPopup.style.display = DisplayStyle.None;
            
            m_BossHealthZone = rootElement.Q<VisualElement>("boss-health-zone");
            m_BossHealthBackground = m_BossHealthZone.Q<VisualElement>("boss-health-background");
            
            //Boss healthbar was removed in the last version of the project but kept commented as reference
            //HideBossHealthbar();
            
            m_PauseButton.clicked += TogglePause;
            m_VisualBlocker.RegisterCallback<TransitionEndEvent>(TransitionEnd);
        }

        public void Init()
        {
            m_HealthbarRoot = Instantiate(HealthbarRootPrefab);
            DontDestroyOnLoad(m_HealthbarRoot);
            m_HealthbarRoot.transform.position = Vector3.zero;
            
            //creating default healthbar entries
            m_HealthbarEntries = new List<HealthbarHandler>();
            const int startCount = 10;
            for (int i = 0; i < startCount; i++)
            {
                CreateNewHealthbar();
            }
        }

        void TransitionEnd(TransitionEndEvent evt)
        {
            // the callback may want to queue another transition, which would lead to the callback being called again
            // this ensure that the queuing will see m_FadeCallback is null and so won't try to call it again
            var clbk = m_FadeCallback;
            m_FadeCallback = null;
            clbk?.Invoke();
        }

        public void TogglePause()
        {
            if (Time.timeScale > 0.5f)
            {
                Time.timeScale = 0.0f;
                m_PauseRoot.style.display = DisplayStyle.Flex;
            }
            else
            {
                Time.timeScale = 1.0f;
                m_PauseRoot.style.display = DisplayStyle.None;
            }
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1.0f;
            m_PauseRoot.style.display = DisplayStyle.None;
            m_EndOfLevelPopup.style.display = DisplayStyle.None;
            GameManager.Instance.Player.SaveData();
            SceneManager.LoadScene("LevelSelect");
        }

        public void UpdateCoin(int count)
        {
            m_CoinCounter.text = count.ToString();
        }

        public void InitOneOfFive(Sprite[] sprites)
        {
            for (int i = 0; i < 5; ++i)
            {
                if(i < sprites.Length && sprites[i] != null)
                    m_OneOfFiveIcones[i].sprite = sprites[i];
                
                m_OneOfFiveIcones[i].SetEnabled(false);
            }
        }

        public void InitCustomCollectibleSprite(Sprite sprite)
        {
            m_CustomCollectibleIcon.style.backgroundImage = Background.FromSprite(sprite);
        }
        
        
        public void UpdateMainCustomCollectibleCount(int count)
        {
            m_CustomCollectibleCounter.text = count.ToString();
        }

        public void UpdateOnOfFive(bool[] collected)
        {
            for (int i = 0; i < collected.Length; ++i)
            {
                if(i < collected.Length && collected[i])
                    m_OneOfFiveIcones[i].SetEnabled(true);
                else
                    m_OneOfFiveIcones[i].SetEnabled(false);
            }
        }

        public void UpdateProjectile(int count)
        {
            m_ProjectileCounter.text = count.ToString();
        }

        public void UpdateHealth(int heartAmount)
        {
            for (int i = 0; i < 3; ++i)
            {
                if (heartAmount < (i+1))
                {
                    m_HeartsContainers[i].RemoveFromClassList("heart-full");
                    m_HeartsContainers[i].AddToClassList("heart-empty");
                }
                else
                {
                    m_HeartsContainers[i].AddToClassList("heart-full");
                    m_HeartsContainers[i].RemoveFromClassList("heart-empty");
                }
            }
        }

        public void UpdateLives(int count, bool showPopup)
        {
            m_PopupLifeCounter.text = $"x{count + 1}";
            if (showPopup)
            {
                m_PopupLifeContainer.style.display = DisplayStyle.Flex;
                m_PopupLifeContainer.schedule.Execute(() =>
                {
                    m_PopupLifeCounter.text = $"x{count}";
                    m_LiveCounter.text = $"x{count.ToString()}";
                    m_PopupLifeContainer.schedule.Execute(() => { m_PopupLifeContainer.style.display = DisplayStyle.None; })
                        .StartingIn(1000);
                }).StartingIn(1000);
            }
            else
            {
                m_LiveCounter.text = $"x{count.ToString()}";
            }
        }

        public void ShowLevelName(string name)
        {
            m_TitleText.text = name;
            m_TitleBanner.style.scale = new Vector3(1.5f,1f,1f);
            m_TitleText.style.scale = Vector3.one;
        }

        public void HideLevelName()
        {
            m_TitleBanner.style.scale = new Vector3(1f,0f,0f);
            m_TitleText.style.scale = Vector3.zero;
        }

        public void FadeOut(bool instant, System.Action onFadeFinished)
        {
            if (!isActiveAndEnabled || m_VisualBlocker == null)
            {
                onFadeFinished?.Invoke();
                return;
            }
            
            if (instant || m_VisualBlocker.style.opacity.value > 0.9f)
            {
                var clbk = m_FadeCallback;
                m_FadeCallback = null;
                clbk?.Invoke();

                onFadeFinished?.Invoke();
                m_VisualBlocker.AddToClassList("instant-fade");
            }
            else
            {
                var clbk = m_FadeCallback;
                m_FadeCallback = onFadeFinished;
                clbk?.Invoke();

                m_VisualBlocker.RemoveFromClassList("instant-fade");
            }

            m_VisualBlocker.style.opacity = 1.0f;
        }

        public void FadeIn(bool instant, System.Action onFadeFinished)
        {
            if (!isActiveAndEnabled || m_VisualBlocker == null)
            {
                onFadeFinished?.Invoke();
                return;
            }
            
            if (instant || m_VisualBlocker.style.opacity.value < 0.01f)
            {
                var clbk = m_FadeCallback;
                m_FadeCallback = null;
                clbk?.Invoke();

                onFadeFinished?.Invoke();
                m_VisualBlocker.AddToClassList("instant-fade");
            }
            else
            {
                var clbk = m_FadeCallback;
                m_FadeCallback = onFadeFinished;
                clbk?.Invoke();

                m_VisualBlocker.RemoveFromClassList("instant-fade");
            }

            m_VisualBlocker.style.opacity = 0.0f;
        }

        public bool IsMouseOverElement()
        {
            var screenPosition = Mouse.current.position.value;
            screenPosition.y = Screen.height - screenPosition.y;
            var panelPosition = RuntimePanelUtils.ScreenToPanel(m_PanelRoot.panel, screenPosition);
            var element = m_PanelRoot.panel.Pick(panelPosition);
            return element != null;
        }

        public void StartLevel()
        {
            m_CustomCollectibleScrollview.contentContainer.Clear();
            m_CollectibleToLabelMap.Clear();
        }

        public void RegisterCustomCollectible(string itemType, Sprite sprite, int max)
        {
            var newEntry = CustomCollectibleEntry.CloneTree();
            var textElement = newEntry.Q<Label>("collectible-count");
            var spriteElement = newEntry.Q<VisualElement>("collectible-icon");

            m_CollectibleToLabelMap.Add(itemType, textElement);
            m_CustomCollectibleScrollview.Add(newEntry);

            textElement.text = $"0/{max}";
            spriteElement.style.backgroundImage = Background.FromSprite(sprite);
        }

        public void SetCustomCollectibleCount(string itemType, int amount, int max)
        {
            if (m_CollectibleToLabelMap.TryGetValue(itemType, out var element))
            {
                element.text = $"{amount}/{max}";
            }
        }

        public void ShowEndLevel(LevelData currentLevel, int completion, int bestCompletion)
        {
            m_EndOfLevelPopup.style.display = DisplayStyle.Flex;

            var mainCustom =
                GameManager.Instance.GetCustomCollectedItemCount(GameManager.Instance.MainCustomCollectibleName);
            var mainCustomMax =
                GameManager.Instance.GetCustomCollectedItemMax(GameManager.Instance.MainCustomCollectibleName);

            m_EndOfLevelTitle.text = $"{currentLevel.LevelName}";

            m_OneOfFiveCounter.text = $"{mainCustom:D2}/{mainCustomMax:D2}";
            m_EnemyKilledCounter.text = $"{currentLevel.EnemyDefeated:D2}/{currentLevel.EnemyCount:D2}";

            m_BestCompletePercentage.text = $"{bestCompletion}";            
            m_CompletePercentageText.text = $"{completion}";
        }

        public void ToggleVisibility(bool visible)
        {
            m_PanelRoot.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
        
        // ===  Healthbar UI
        void CreateNewHealthbar()
        {
            var newInstance = Instantiate(HealthbarHandlerPrefab, m_HealthbarRoot.transform);
            newInstance.transform.position = Vector3.zero;
            newInstance.gameObject.SetActive(false);
            m_HealthbarEntries.Add(newInstance);
            m_FreeHealthbar.Enqueue(m_HealthbarEntries.Count - 1);
        }
        
        public int GetHealthbar()
        {
            if (m_FreeHealthbar.Count == 0)
                CreateNewHealthbar();
            
            int id = m_FreeHealthbar.Dequeue();
            m_HealthbarEntries[id].gameObject.SetActive(true);
            
            return id;
        }

        public void ReturnHealthbar(int id)
        {
            if(id > m_HealthbarEntries.Count || m_HealthbarEntries[id] == null)
                return;
            
            m_HealthbarEntries[id].gameObject.SetActive(false);
            
            //use a coroutine as this can be called from a OnDisable function, and you cannot change trasnform hierarchy
            //of disabled object on OnDisable
            StartCoroutine(HealthbarReturnCoroutine(m_HealthbarEntries[id].transform));
            m_FreeHealthbar.Enqueue(id);
        }

        // allow 
        IEnumerator HealthbarReturnCoroutine(Transform transform)
        {
            yield return null;
            transform.SetParent(null);
        }

        public void SetHealthbarPosition(int id, Vector3 position, Transform parent)
        {
            m_HealthbarEntries[id].gameObject.SetActive(true);
            m_HealthbarEntries[id].transform.SetParent(parent);
            m_HealthbarEntries[id].transform.position = position;
        }

        public void SetHealthbarValue(int id, float ratio)
        {
            m_HealthbarEntries[id].SetBarFill(ratio * 100);
        }
        
        // BOSS HEALTHBAR
        
        // Boss healthbar have been removed in the last version of the sample, but the function handling it have been
        // kept here commented as reference
        
        // public void InitBossHealthbar(int[] subsegmentCounts)
        // {
        //     m_BossHealthZone.style.display = DisplayStyle.Flex;
        //     m_BossHealthBackground.Clear();
        //     
        //     foreach(var subCount in subsegmentCounts)
        //     {
        //         BossHealthbarSegment.CloneTree(m_BossHealthBackground);
        //         
        //         var segment = m_BossHealthBackground.Children().Last();
        //         segment.style.flexGrow = 1.0f;
        //
        //         RefillLastSegment(subCount);
        //         
        //         m_BossHealthBackground.Add(segment);
        //     }
        // }
        //
        // //refill the current last segmenet of hte healthbar, usefull when the boss go back to shooting phase
        // public void RefillLastSegment(int count)
        // {
        //     var segment = m_BossHealthBackground.Children().Last();
        //     segment.style.flexGrow = 1.0f;
        //
        //     for (int i = 0; i < count; ++i)
        //     {
        //         BossHealthbarSubSegment.CloneTree(segment);
        //     }
        // }
        //
        // public void HideBossHealthbar()
        // {
        //     m_BossHealthZone.style.display = DisplayStyle.None;
        // }
        //
        // public void RemoveBossHealthSubSegment(int count)
        // {
        //     if(m_BossHealthBackground.childCount == 0)
        //         return;
        //
        //     var segment = m_BossHealthBackground.Children().Last();
        //     for (int i = 0; i < count; ++i)
        //     {
        //         if (segment.childCount > 0)
        //             segment.RemoveAt(segment.childCount - 1);
        //         else
        //             break;
        //     }
        // }
        //
        // public void RemoveBossHealthSegment(int count)
        // {
        //     for (int i = 0; i < count; ++i)
        //     {
        //         if (m_BossHealthBackground.childCount > 0)
        //             m_BossHealthBackground.RemoveAt(m_BossHealthBackground.childCount - 1);
        //         else
        //             break;
        //     }
        // }
    }
}