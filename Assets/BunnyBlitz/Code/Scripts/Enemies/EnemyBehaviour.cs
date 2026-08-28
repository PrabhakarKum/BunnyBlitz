using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace BunnyBlitz
{
    public class EnemyBehaviour : MonoBehaviour
    {
        [Header("Core values")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [Range(1, 20)]
        public int m_HitPoints = 1;
        [Range(1, 10)]
        public int m_AttackPoints = 1;
        [Range(0.01f, 10f)]
        public float m_RecoveryFromDamageTime = 2f;
        [Range(1f, 1000f)]
        public float m_DelayInit = 1f;

        [Header("Audio")]
        public AudioResource DamagedSound;
        public AudioResource DeathSound;

        protected Rigidbody2D Rb;
        protected GameObject EnemyObject;

        private SkinnedMeshRenderer[] m_VisualObjects;
    
        [Header("VFX")]
        [SerializeField] private VisualEffect vfxDamage;
        [SerializeField] private VisualEffect vfxDeath;
        [SerializeField] private Vector2 pushForce = new Vector2(5f, 20f);
        
        private const float DeathDuration = 3.0f;
        private float m_DeathTimer;
        private uint m_MeshData = 0x00000000 ;

        protected bool m_IsDead;
        protected int m_CurrentHealth;
        protected int m_HealthbarId = -1;
        
        protected bool m_ShieldFromDamage;

        protected float m_BlinkingTimer = 0.0f;

        protected Animator m_Animator;
        
        private int m_IsHitHash = Animator.StringToHash("isHit");
        private int m_IsDeadHash = Animator.StringToHash("isDead");
    
        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            m_VisualObjects =  EnemyObject.transform.GetComponentsInChildren<SkinnedMeshRenderer>(false) ;

            var dz = GetComponentsInChildren<DamageableCollider>();
            foreach (var zone in dz)
            {
                zone.OnDamage += OnDamaged;
                zone.OnRetaliate += OnRetaliate;
            }

            m_CurrentHealth = m_HitPoints;
        }

        protected virtual void OnEnable()
        {
            m_Animator = GetComponentInChildren<Animator>();
        }

        protected virtual void OnDisable()
        {
            if (m_HealthbarId != -1)
            {
                GameManager.Instance.HUD.ReturnHealthbar(m_HealthbarId);
                m_HealthbarId = -1;
            }
        }

        void OnDamaged(DamageableCollider.DamageData data)
        {
            if (!m_ShieldFromDamage)
            {
                StartCoroutine(DamageEnemy(data.Amount));
            }
            
            var player = data.Other.gameObject.GetComponent<PlayerBehaviour>();

            if (player != null)
            {
                player.BouncingOnEnemy();
            }
        }

        void OnRetaliate(DamageableCollider.DamageData data)
        {
            if (m_ShieldFromDamage)
            {
                return;
            }
            
            var player = data.Other.gameObject.GetComponent<PlayerBehaviour>();

            if (player != null)
            {
                player.ReceiveDamage(m_AttackPoints,transform.position);
            }
        }

        private IEnumerator DamageEnemy(int value)
        {
            m_Animator.SetTrigger(m_IsHitHash);
            m_ShieldFromDamage = true;
            m_BlinkingTimer = 0.0f;
            m_CurrentHealth -= value;

            if (m_HealthbarId == -1)
            {
                m_HealthbarId = GameManager.Instance.HUD.GetHealthbar();
                GameManager.Instance.HUD.SetHealthbarPosition(m_HealthbarId, transform.position + Vector3.up, transform);
            }
            
            GameManager.Instance.AudioManager.PlaySFXAt(DamagedSound, transform.position);
            GameManager.Instance.HUD.SetHealthbarValue(m_HealthbarId, m_CurrentHealth/(float)m_HitPoints);
            PlayFeedback();
            if (m_CurrentHealth <= 0)
            {
                GameManager.Instance.AudioManager.PlaySFXAt(DeathSound, transform.position);
                GameManager.Instance.HUD.ReturnHealthbar(m_HealthbarId);
                m_HealthbarId = -1;
                PlayFeedbackAndDestroy();
            }
            else
            {
                // trigger damage anim
                // Pause execution and resume after the time set
                yield return new WaitForSeconds(m_RecoveryFromDamageTime);
                m_ShieldFromDamage = false;
                foreach (var o in m_VisualObjects)
                {
                    o.enabled = true;
                }
            }
        }

        private void PlayFeedback()
        {
            PlayFeedbackSkinnedMesh(); 
            PlayVFX(vfxDamage);
        }

        private void PlayFeedbackSkinnedMesh(bool isDead = false)
        {
            var timeInMs = (int)(Time.timeSinceLevelLoad * 1000);
            m_MeshData = HelpersRSUV.EncodeDataIntoBits(m_MeshData, timeInMs, 0, 24);
            //m_MeshData = HelpersRSUV.SetBit(m_MeshData, 24, isDead);
            
            foreach (var skinnedMesh in m_VisualObjects)
            {
                skinnedMesh.SetShaderUserValue(m_MeshData);
            }
        }

        private void PlayVFX(VisualEffect vfx)
        {
            vfx.Play();
        }

        private void PlayFeedbackAndDestroy()
        {
            //PlayFeedbackSkinnedMesh(true);
            PlayVFX(vfxDeath);
            RandomImpulse();
            m_IsDead = true;
            m_ShieldFromDamage = false;
            m_Animator?.SetBool(m_IsDeadHash, true);

            OnDeath();
        }

        protected virtual void OnDeath()
        {
            
        }

        private void DestroySelf()
        {
            GameManager.Instance.CurrentLevelData.EnemyDestroyed();
            Destroy(gameObject);
        }

        protected virtual void Update()
        {
            if (transform.position.y < -200)
            {
                DestroySelf();
                return;
            }

            if (m_ShieldFromDamage)
            {
                m_BlinkingTimer -= Time.deltaTime;
                if (m_BlinkingTimer < 0.0f)
                {
                    m_BlinkingTimer = 0.1f;
                    foreach (var o in m_VisualObjects)
                    {
                        o.enabled = !o.enabled;
                    }
                }
            }

            if (!m_IsDead) return;
            
            m_DeathTimer += Time.deltaTime;
            
            if (m_DeathTimer >= DeathDuration)
            {
                DestroySelf();
            }
        }

        private void RandomImpulse()
        {
            var dir = Vector3.up * 10.0f + Vector3.right * Random.Range(-1.0f, 1.0f);
            Rb.bodyType = RigidbodyType2D.Dynamic;
            Rb.excludeLayers = ~0;
            Rb.linearDamping = 0.0f;
            Rb.gravityScale = 5.0f;
            Rb.AddForce(dir, ForceMode2D.Impulse);
        }
    }
}