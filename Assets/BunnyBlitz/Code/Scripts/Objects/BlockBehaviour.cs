using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace BunnyBlitz
{
    public class BlockBehaviour : MonoBehaviour
    {
        public CollectibleItem.CollectibleType CollectibleType;
        public GameObject CustomPrefab;
        public int MinAmount = 1;
        public int MaxAmount = 1;
        public bool Destructible = false;
        public UnityEvent OnCollected;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color hitColor = Color.white;
        [SerializeField] private Color endColor = Color.black;
        [SerializeField] private float hitScale = 1.5f;
        [SerializeField] private float hitDuration = 0.2f;
        [SerializeField] private float hitOffset = 0.5f;
        [SerializeField] private AnimationCurve hitScaleCurve ;
        [SerializeField] private AnimationCurve hitOffsetCurve ;

        [Header("Audio")] 
        public AudioResource HitSound;
        
        private Collider2D[] m_Colliders;
        private GameManager m_GameManager;
        private Vector3 m_VisualInitScale;
        private Vector3 m_InitPosition;
        private float m_hitTimer;
        private bool m_IsHit;
        private Color m_InitColor;
       
        private void Awake()
        {
            m_Colliders = GetComponentsInChildren<Collider2D>();
            m_VisualInitScale = visual.transform.localScale;
            m_InitPosition = visual.transform.position;
            m_InitColor = visual.color;
            m_hitTimer = 0f;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            m_GameManager = GameManager.Instance;
            if (gameObject.CompareTag("Block"))
            {
                if (collision.gameObject.CompareTag(PoolingSystem.ProjectileTag))
                {
                    m_GameManager.VfxController.PlayVfx(VfxInteractDescBase.VfxTypeEnum.Pomegranate, collision.transform.position);
                    GameManager.Instance.PoolingSystem.Return(collision.gameObject);
                    BlockHit();
                }
                if (collision.gameObject.CompareTag("Player"))
                {
                    float cpAngle = Vector2.Angle(Vector2.up, collision.GetContact(0).normal);
                    if (cpAngle > 160f)
                    {
                        BlockHit();
                    }
                }
            }
        }

        void BlockHit()
        {
            GameManager.Instance.AudioManager.PlaySFXAt(HitSound, transform.position);
            PlayVisualFeedback(transform.position);
            if (Destructible)
            {
                Deactivate();
            }
            else
            {
                Invoke(nameof(Deactivate), hitDuration);
            }
        }

        private void Update()
        {
            UpdateHitTimer();
            UpdateHitVisual();
        }

        private void UpdateHitTimer()
        {
            if (!m_IsHit) return;
            
            m_hitTimer += Time.deltaTime;
            if (m_hitTimer >= hitDuration)
            {
                m_IsHit = false;
                m_hitTimer = 0f;
            }
        }

        private void UpdateHitVisual()
        {
            if (!m_IsHit) return;
            
            var normalizedTime = m_hitTimer / hitDuration;
            var gradient = new Gradient()
            {
                alphaKeys = new []
                { 
                    new GradientAlphaKey(1,0)
                },
                colorKeys = new[]
                {
                    new GradientColorKey(m_InitColor, 0),
                    new GradientColorKey(hitColor, 0.15f),
                    new GradientColorKey(Color.white, 0.4f),
                    new GradientColorKey(endColor, 1f),
                }
            };
           
           
            visual.transform.localScale = Vector3.Lerp(m_VisualInitScale, m_VisualInitScale * hitScale ,hitScaleCurve.Evaluate(normalizedTime));
            visual.transform.position = Vector3.Lerp(m_InitPosition, m_InitPosition + new Vector3(0,-hitOffset,0) ,hitOffsetCurve.Evaluate(normalizedTime));
            visual.color = gradient.Evaluate(normalizedTime);;

            if (normalizedTime > 0.99f)
            {
                visual.transform.localScale = m_VisualInitScale;
            }
        }

        private void PlayVisualFeedback(Vector3 position)
        {
           
            m_IsHit = !Destructible;

            if (Destructible)
            {
                visual.enabled = false;
                m_GameManager.VfxController.PlayVfx(VfxInteractDescBase.VfxTypeEnum.BlockDestroy, position);
            }
            else
            {
                m_GameManager.VfxController.PlayVfx(VfxInteractDescBase.VfxTypeEnum.BlockDestroy, position);
                OnCollected.Invoke();
            }
        }
        
        
        private void Deactivate()
        {
            if (m_GameManager != null)
            {
                var spawnAmount = Mathf.Max(Random.Range(MinAmount, MaxAmount + 1), 0);
                var spawnedItems = new GameObject[spawnAmount];
                var spawnOffset = new Vector3(0f, -0.5f, 0f);
                var spawnPosition = transform.position + spawnOffset;
                switch (CollectibleType)
                {
                    case CollectibleItem.CollectibleType.Coin:
                        m_GameManager.PoolingSystem.Spawn(PoolingSystem.CoinTag, spawnAmount, spawnPosition,m_GameManager.CurrentLayer.LayerObjectRoot, spawnedItems);
                        break;
                    case CollectibleItem.CollectibleType.Projectile:
                        m_GameManager.PoolingSystem.Spawn(PoolingSystem.ProjectileCollectableTag, spawnAmount, spawnPosition,m_GameManager.CurrentLayer.LayerObjectRoot, spawnedItems);
                        break;
                    case CollectibleItem.CollectibleType.Health:
                        m_GameManager.PoolingSystem.SpawnHealthCollectible(spawnAmount, spawnPosition, m_GameManager.CurrentLayer.LayerObjectRoot, spawnedItems);
                        break;
                    case CollectibleItem.CollectibleType.Life:
                        m_GameManager.PoolingSystem.SpawnLifeCollectible(spawnAmount, spawnPosition, m_GameManager.CurrentLayer.LayerObjectRoot, spawnedItems);
                        break;
                    default:
                        if (CustomPrefab != null)
                        {
                            for (int i = 0; i < spawnAmount; ++i)
                            {
                                var itm = Instantiate(CustomPrefab, m_GameManager.CurrentLayer.LayerObjectRoot, true);
                                itm.layer = m_GameManager.CurrentLayer.gameObject.layer;
                                itm.transform.position = spawnPosition;

                                spawnedItems[i] = itm;
                            }
                        }
                        break;
                }

                foreach (var itm in spawnedItems)
                {
                    if (itm == null)
                        continue;

                    var itmPhysics = itm.GetComponent<ItemsPhysicsBehaviour>();
                    itmPhysics?.IgnoreColliders(m_Colliders);
                }
                
            }

            if (Destructible)
            {
                //TODO : add destroyed VFX
                Destroy(gameObject);
            }
            else
            {
                this.gameObject.tag = "Untagged";
            }
        }
    }
}