//using System.Numerics;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace BunnyBlitz
{
    public class PlayerBehaviour : MonoBehaviour
    {
        private class TempPhysicIgnore
        {
            public Collider2D IgnoredCollider;
            public float IgnoringTimer;
        }
        
        public enum PlayerState
        {
            Alive,
            Dead
        }

        public PlayerMovement Movement { get; private set; }
        public Rigidbody2D Rigidbody { get; private set; }
        public SortingGroup SortingGroup { get; private set; }
        public bool SpawnVFX { get; private set; } = true;
        public Animator Animator => m_CurrentAnimator;
        public Collider2D[] Colliders => m_Colliders;
        public HoldableObject HeldObject => m_HeldObject;

        public bool IsInWater => m_InWater;

        public int LivesCount => m_CurrentLivesCount;
        
        public int ProjectilesCount => m_CurrentProjectileCount;
        
        public event Action<bool> OnMaskingChanged;

        [Header("Gameplay")] 
        public int StartingProjectileCount = 2;
        public int StartingHealth = 5;
        [SerializeField] private float shieldDamageDuration = 1.5f;
        [SerializeField] private float m_HeldObjectThrowingForce = 3.0f;
        
        [Header("Gameplay Reference")]
        public float shootingAngleRaise = 0.3f; //1 is up, 0 is model direction
        public float hitPushBackIntensity = 13f; //how much bounce back
        public GameObject CaveLightMask;
        public float CaveLightScaleSpeed = 0.5f;
        public Transform VFXCollider;
        public Transform HoldingTransform;

        [Header("Character Animation")] 
        public Transform modelHolder;
        public float delayShootingProjectile = 0.2f; //to match the animation
        public Transform modelProjectilePosition;
        public VisualEffect SpawnEffect;
        private uint _meshData = 0x00000000;


        [Header("Events")]
        public UnityEvent OnCaveEnter;
        public UnityEvent OnCaveExit;

        [Header("Audio")]
        public AudioResource DamageAudio;
        public AudioResource DeathAudio;
        public AudioResource RespawnAudio;

        public AudioResource ProjectilePickupAudio;
        public AudioResource ProjectileThrowAudio;
        
        public AudioResource HealthPickupAudio;
        public AudioResource LifePickupAudio;
        public AudioResource CarrotPickupAudio;
        public AudioResource CollectiblePickupAudio;

        public AudioResource TorchLitUpAudio;
        public AudioResource WaterEnterSound;
        
        private bool m_ShieldFromDamage;
    
        private CollectibleVfxManager m_CollectibleVfx;
        private PlayerInput m_PlayerInput;

        private PlayerState m_CurrentState = PlayerState.Alive;

        private int m_CurrentProjectileCount;
        private int m_CurrentHealth;
        private int m_CurrentLivesCount;
        private int m_Coins;

        private Vector3 m_DeathVelocity;
        
        private HoldableObject m_HeldObject;

        private Collider2D[] m_Colliders;
        private SphereCollider m_VfxSphereCollider;
        private Vector3 m_OriginalVfxColliderPosition;

        private Animator m_CurrentAnimator;

        private SkinnedMeshRenderer[] m_Meshes;

        private bool m_SwitchingCaveLightMask = false;
        private float m_SwitchingCaveMaskSpeed = 0.0f;
        private Vector3 m_CaveLightMaskOriginalScale;
        private Vector3 m_CaveLightMaskScaleTarget = Vector3.one;
        private Light2D[] m_MaskLights;
        private (float, float)[] m_MaskLightsStartRadius;
        private (float, float)[] m_MaskLightsSwitchSpeed;
        private (float, float)[] m_MaskLightsTargetRadius;

        private AutoPlayerVFXBinding[] m_VFXBindings;
        
        private bool m_InWater = false;
        
        private SortingGroup m_SortingGroup;

        private List<TempPhysicIgnore> m_TempIgnoredCollider = new();
        
        private int m_DeadHash = Animator.StringToHash("isDead");
        private int m_CarryingHash = Animator.StringToHash("isCarrying");
        
        private void Awake()
        {
            m_CurrentAnimator = modelHolder.GetComponentInChildren<Animator>();
            m_Meshes = m_CurrentAnimator.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            m_Colliders = GetComponentsInChildren<Collider2D>(true);

            m_CaveLightMaskOriginalScale = CaveLightMask.transform.localScale;
            m_SwitchingCaveMaskSpeed = m_CaveLightMaskOriginalScale.magnitude/CaveLightScaleSpeed;

            m_VfxSphereCollider = VFXCollider.GetComponent<SphereCollider>();
            m_OriginalVfxColliderPosition = VFXCollider.transform.localPosition;
            
            SpawnEffect.Stop();
            
            m_MaskLights = CaveLightMask.GetComponentsInChildren<Light2D>();
            m_MaskLightsStartRadius = new (float, float)[m_MaskLights.Length];
            m_MaskLightsSwitchSpeed = new (float, float)[m_MaskLights.Length];
            m_MaskLightsTargetRadius = new (float, float)[m_MaskLights.Length];
            
            for(int i = 0; i < m_MaskLights.Length; ++i)
            {
                var l = m_MaskLights[i];
                m_MaskLightsStartRadius[i] = (l.pointLightInnerRadius, l.pointLightOuterRadius);
                m_MaskLightsSwitchSpeed[i] = (l.pointLightInnerRadius/CaveLightScaleSpeed, l.pointLightOuterRadius/CaveLightScaleSpeed);
                
                l.pointLightInnerRadius = 0.0f;
                l.pointLightOuterRadius = 0.0f;
            }

            m_SortingGroup = GetComponentInChildren<SortingGroup>();

            SwitchModelMasked(false);
        }

        public void Init()
        {
            m_CollectibleVfx = GameManager.Instance.VfxController;
            m_ShieldFromDamage = false;
            
            //if the saved counter is -1, that mean it wasn't saved yet, so we use the start value
            m_CurrentProjectileCount = GameManager.Instance.SaveSystem.Data.ProjectileCount < 0 ? 
                StartingProjectileCount: 
                GameManager.Instance.SaveSystem.Data.ProjectileCount;
            m_CurrentHealth = StartingHealth;

            m_CurrentLivesCount = GameManager.Instance.SaveSystem.Data.LiveCount;
            m_Coins = 0;

            GameManager.Instance.HUD.UpdateProjectile(m_CurrentProjectileCount);
            GameManager.Instance.HUD.UpdateHealth(m_CurrentHealth);
            GameManager.Instance.HUD.UpdateOnOfFive(new bool[]{false, false, false, false, false});
            GameManager.Instance.HUD.UpdateCoin(m_Coins);
            GameManager.Instance.HUD.UpdateLives(m_CurrentLivesCount, false);
            
            CaveLightMask.transform.localScale = new Vector3(0,0,0);
            
            //find all VFX that need binding
            m_VFXBindings = FindObjectsByType<AutoPlayerVFXBinding>(FindObjectsInactive.Include);

            SwitchInput(false);
        }


        public void SaveData()
        {
            GameManager.Instance.SaveSystem.Data.ProjectileCount = m_CurrentProjectileCount;
            GameManager.Instance.SaveSystem.Data.LiveCount = m_CurrentLivesCount;
        }
        
        // Update is called once per frame
        void Update()
        {
            CaveLightMask.transform.position = transform.position;
            
            if (m_CurrentState == PlayerState.Dead)
            {
                DeathAnimationUpdate();
            }
            
            if (m_SwitchingCaveLightMask)
            {
                var currentScale = CaveLightMask.transform.localScale;
                var nextScale = Vector3.MoveTowards(currentScale, m_CaveLightMaskScaleTarget, Time.deltaTime * m_SwitchingCaveMaskSpeed);

                for (int i = 0; i < m_MaskLights.Length; ++i)
                {
                    var l = m_MaskLights[i];

                    l.pointLightInnerRadius = Mathf.MoveTowards(l.pointLightInnerRadius, m_MaskLightsTargetRadius[i].Item1, Time.deltaTime * m_MaskLightsSwitchSpeed[i].Item1);
                    l.pointLightOuterRadius = Mathf.MoveTowards(l.pointLightOuterRadius, m_MaskLightsTargetRadius[i].Item2, Time.deltaTime * m_MaskLightsSwitchSpeed[i].Item2);

                }

                CaveLightMask.transform.localScale = nextScale;
                if (nextScale == m_CaveLightMaskScaleTarget)
                {
                    m_SwitchingCaveLightMask = false;
                    
                    //making sure to set our lights radius to the target
                    for (int i = 0; i < m_MaskLights.Length; ++i)
                    {
                        var l = m_MaskLights[i];

                        l.pointLightInnerRadius = m_MaskLightsTargetRadius[i].Item1;
                        l.pointLightOuterRadius = m_MaskLightsTargetRadius[i].Item2;

                    }
                }
            }
            
            //count down ignored collider and remove the one that are done
            for (int i = 0; i < m_TempIgnoredCollider.Count; ++i)
            {
                var data = m_TempIgnoredCollider[i];
                data.IgnoringTimer -= Time.deltaTime;
                if (data.IgnoringTimer < 0)
                {
                    m_TempIgnoredCollider.RemoveAt(i);
                    i--;
                    foreach (var c in m_Colliders)
                    {
                        if(c.isTrigger)
                            continue;

                        Physics2D.IgnoreCollision(c, data.IgnoredCollider, false);
                    }
                }
            }
            
            if (m_PlayerInput.Pause.WasPressedThisFrame())
            {
                GameManager.Instance.HUD.TogglePause();
                return;
            }

            if (m_CurrentState != PlayerState.Alive)
            {
                return;
            }

            if (m_PlayerInput.Attack.WasPressedThisFrame() &&
                !GameManager.Instance.HUD.IsMouseOverElement() && Time.timeScale > 0.001f) //we check that there are projectiles in the game stats
            {
                ShootProjectile();
            }
            
            if(transform.position.y < GameManager.Instance.CurrentLevelData.KillZ)
            {
                PlayerDead();
            }
        }

        private void DeathAnimationUpdate()
        {
            m_DeathVelocity.y -= 15.0f * Time.deltaTime;

            Movement.modelHolder.transform.localPosition += m_DeathVelocity * Time.deltaTime;
        }

        private void LateUpdate()
        {
            if(m_VFXBindings == null)
                return;
            
            foreach (var binding in m_VFXBindings)
            {
                if (binding.gameObject.activeInHierarchy)
                {
                    if (binding.NeedSphere)
                    {
                        binding.SetSphere(m_VfxSphereCollider);
                    }

                    if (binding.NeedVelocity)
                    {
                        binding.SetVelocity(Rigidbody.linearVelocity);
                    }
                }
            }
        }

        void ShootProjectile()
        {
            if (m_HeldObject != null)
            {
                //Updates to animation controller
                if (m_CurrentAnimator != null)
                {
                    m_CurrentAnimator.SetTrigger("hasShotProjectile");
                }
                
                //if we have an held object we throw it instead
                float radians = modelHolder.localEulerAngles.z * Mathf.Deg2Rad;
                float sign = (modelHolder.localEulerAngles.y > 180) ? 1 : -1;
                Vector2 modelAngle = new Vector2(Mathf.Cos(radians) * sign, Mathf.Sin(radians));
                m_HeldObject.Throw(Vector2.Lerp(modelAngle, Vector2.up, shootingAngleRaise).normalized * m_HeldObjectThrowingForce);
            }
            else
            {
                if (m_CurrentProjectileCount <= 0)
                    return;
                
                //Updates to animation controller
                if (m_CurrentAnimator != null)
                {
                    m_CurrentAnimator.SetTrigger("hasShotProjectile");
                }
                
                //TODO : if performane proiling show this is expensive in allocation, make the projectile array static in the class
                int projectileAmount = 1;
                var projectiles = new GameObject[projectileAmount];

                float radians = modelHolder.localEulerAngles.z * Mathf.Deg2Rad;
                float sign = (modelHolder.localEulerAngles.y > 180) ? 1 : -1;
                Vector2 modelAngle = new Vector2(Mathf.Cos(radians) * sign, Mathf.Sin(radians));
                GameManager.Instance.PoolingSystem.SpawnProjectile(PoolingSystem.ProjectileTag, projectileAmount,
                    modelProjectilePosition.position,
                    GameManager.Instance.CurrentLayer.LayerObjectRoot, Vector2.Lerp(modelAngle, Vector2.up,
                        shootingAngleRaise), projectiles); //middle point between up and model angle * facing direction
                m_CurrentProjectileCount -= projectileAmount;

                for (int i = 0; i < projectileAmount; ++i)
                {
                    var itmPhysic = projectiles[i].GetComponent<ItemsPhysicsBehaviour>();
                    itmPhysic.IgnoreColliders(m_Colliders);
                }

                GameManager.Instance.HUD.UpdateProjectile(m_CurrentProjectileCount);
                
                GameManager.Instance.AudioManager.PlaySFXAt(ProjectileThrowAudio, transform.position);
            }
        }

        void OnTriggerEnter2D(Collider2D collision)
        {
            //Check if this is a collectible
            var collectibleItem = collision.gameObject.GetComponentInParent<CollectibleItem>();

           if (collectibleItem != null)
           {
                switch (collectibleItem.ItemType)
                {
                    case CollectibleItem.CollectibleType.Coin:
                        m_CollectibleVfx.PlayVfx(VfxInteractDescBase.VfxTypeEnum.CoinPickup, collision.gameObject.transform.position);
                        int hundreds = m_Coins / 100;
                        m_Coins += 1;
                        int postHundreds = m_Coins / 100;
                        if (hundreds != postHundreds)
                            m_CurrentLivesCount += 1;
                        GameManager.Instance.HUD.UpdateCoin(m_Coins);
                        GameManager.Instance.AudioManager.PlaySFX(CarrotPickupAudio);
                        break;
                    case CollectibleItem.CollectibleType.Projectile:
                        m_CollectibleVfx.PlayVfx(VfxInteractDescBase.VfxTypeEnum.CoinPickup, collision.gameObject.transform.position);
                        m_CurrentProjectileCount += 1;
                        GameManager.Instance.HUD.UpdateProjectile(m_CurrentProjectileCount);
                        GameManager.Instance.AudioManager.PlaySFXAt(ProjectilePickupAudio, transform.position);
                        break;
                    case CollectibleItem.CollectibleType.Health:
                        m_CollectibleVfx.PlayVfx(VfxInteractDescBase.VfxTypeEnum.CoinPickup, collision.gameObject.transform.position);
                        m_CurrentHealth = Mathf.Min(m_CurrentHealth + 1, StartingHealth);
                        GameManager.Instance.HUD.UpdateHealth(m_CurrentHealth);
                        GameManager.Instance.AudioManager.PlaySFX(HealthPickupAudio);
                        break;
                    case CollectibleItem.CollectibleType.Life:
                        m_CollectibleVfx.PlayVfx(VfxInteractDescBase.VfxTypeEnum.CoinPickup, collision.gameObject.transform.position);
                        m_CurrentLivesCount += 1;
                        GameManager.Instance.HUD.UpdateLives(m_CurrentLivesCount, false);
                        GameManager.Instance.AudioManager.PlaySFX(LifePickupAudio);
                        break;
                    case CollectibleItem.CollectibleType.OneOfFive:
                        m_CollectibleVfx.PlayVfx(VfxInteractDescBase.VfxTypeEnum.OneOfFiveInteract, collision.gameObject.transform.position);
                        GameManager.Instance.CurrentLevelData?.OneOfFivePickedUp(collectibleItem.CollectibleIndex);
                        GameManager.Instance.AudioManager.PlaySFX(CollectiblePickupAudio);
                        break;
                    case CollectibleItem.CollectibleType.Custom:
                        m_CollectibleVfx.PlayVfx(VfxInteractDescBase.VfxTypeEnum.CoinPickup, collision.gameObject.transform.position);
                        GameManager.Instance.StatsCollectItem(collectibleItem.CustomType);
                        GameManager.Instance.AudioManager.PlaySFX(CollectiblePickupAudio);
                        break;
                }
                
                GameManager.Instance.PoolingSystem.Return(collision.gameObject);
            }
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.collider.CompareTag("ProjectileEnemy"))
            {
                GameManager.Instance.PoolingSystem.Return(other.gameObject);
                ReceiveDamage(1, other.transform.position);
            }
        }


        public void TransportingLayerStart()
        {
            Rigidbody.linearVelocity = Vector2.zero;
            LayerMask mask = ~0; //everything
            Rigidbody.excludeLayers = mask;
        }

        public void TransportingLayerEnd()
        {
            Rigidbody.AddForceY(8f, ForceMode2D.Impulse);

            LayerMask mask = 0; //nothing
            Rigidbody.excludeLayers = mask;
        }

        public void BouncedOnSpring()
        {
            Movement.Bounced();
            if (m_CurrentAnimator != null)
            {
                m_CurrentAnimator.SetTrigger("hasBounced");
            }
        }

        public void BouncingOnEnemy()
        {
            Movement.Bounced();
            Rigidbody.linearVelocityY = 0;
            Rigidbody.AddForceY(14f, ForceMode2D.Impulse);
            //Updates to animation controller
            if (m_CurrentAnimator != null)
            {
                m_CurrentAnimator.SetTrigger("hasBounced");
            }
        }

        public void GrabObject(HoldableObject obj)
        {
            if (m_HeldObject != null)
            {
                m_HeldObject.transform.SetParent(null);
            }
            
            m_HeldObject = obj;
            m_CurrentAnimator.SetBool(m_CarryingHash, m_HeldObject != null);
            if(m_HeldObject != null)
            {
                m_HeldObject.transform.SetParent(HoldingTransform);
                m_HeldObject.transform.localPosition = Vector3.zero;
            }
        }

        public void ReceiveDamage(int amount, Vector3 damageOrigin)
        {
            if (m_ShieldFromDamage || m_CurrentHealth <= 0) return;
            
            m_CurrentHealth -= amount;
            if (m_CurrentHealth <= 0)
            {
                PlayerDead();
                m_CurrentHealth = 0;
            }
            else
            {
                StartCoroutine(PlayerDamage(damageOrigin));
            }
            
            GameManager.Instance.AudioManager.PlaySFXAt(DamageAudio, transform.position);
            GameManager.Instance.HUD.UpdateHealth(m_CurrentHealth);
        }
        
        public IEnumerator PlayerDamage(Vector3 origin)
        {
            m_ShieldFromDamage = true;
            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.AddForce(-(origin - transform.position).normalized * hitPushBackIntensity, ForceMode2D.Impulse);
            PlayFeedbackSkinnedMesh();
            yield return new WaitForSeconds(shieldDamageDuration);
            modelHolder.gameObject.SetActive(true);
            m_ShieldFromDamage = false;
            StopFeedbackSkinnedMesh();
        }

        private void PlayFeedbackSkinnedMesh()
        {
            var timeInMs = (int)(Time.timeSinceLevelLoad * 1000);
            _meshData = HelpersRSUV.EncodeDataIntoBits(_meshData, timeInMs, 0, 24);
            _meshData = HelpersRSUV.SetBit(_meshData, 24, true);
            foreach (var skinnedMesh in m_Meshes)
            {
                skinnedMesh.SetShaderUserValue(_meshData);
            }
        }
        private void StopFeedbackSkinnedMesh()
        {
            _meshData = HelpersRSUV.SetBit(_meshData, 24, false);
            foreach (var skinnedMesh in m_Meshes)
            {
                skinnedMesh.SetShaderUserValue(_meshData);
            }
        }

        private void ToggleMeshVisibility(bool visible)
        {
            foreach (var mesh in m_Meshes)
            {
                mesh.enabled = visible;
            }
        }

        public void Hide()
        {
            ToggleMeshVisibility(false);
        }

        public void MoveTorchToNewLayer(Transform frontDecoRoot, int sortingLayerId)
        {
            CaveLightMask.transform.SetParent(frontDecoRoot);
            m_SortingGroup.sortingLayerID = sortingLayerId;
        }

        public void PlayerDead()
        {
            //visual
            m_CurrentLivesCount--;
            m_CurrentState = PlayerState.Dead;
            SwitchInput(false);
            GameManager.Instance.AudioManager.PlaySFXAt(DeathAudio, transform.position);
            GameManager.Instance.HUD.UpdateLives(m_CurrentLivesCount, true);
            StartCoroutine(GameManager.Instance.PlayerDeath());

            m_DeathVelocity = (Vector3.up * 2.0f + Random.Range(-1.0f, 1.0f) * Vector3.right * 5.0f);

            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.bodyType = RigidbodyType2D.Kinematic;

            m_CurrentAnimator.SetBool(m_DeadHash, true);
        }

        public void ResetLivesCountTo(int life)
        {
            m_CurrentLivesCount = life;
        }

        public void WasRespawn()
        {
            m_CurrentAnimator.SetBool(m_DeadHash, false);
            
            m_DeathVelocity = Vector3.zero;
            Movement.modelHolder.transform.localPosition = Vector3.zero;
            
            ToggleMeshVisibility(true);
            Rigidbody.bodyType = RigidbodyType2D.Dynamic;
            
            if(m_HeldObject != null)
                Destroy(m_HeldObject.gameObject);
            
            m_HeldObject = null;
            Movement.ResetMovement();
            m_CurrentState = PlayerState.Alive;
            m_CurrentHealth = StartingHealth;
            GameManager.Instance.HUD.UpdateHealth(m_CurrentHealth);

            StartCoroutine(WaitForSpawnAnim());
        }

        IEnumerator WaitForSpawnAnim()
        {
            _meshData = HelpersRSUV.EncodeDataIntoBits(_meshData, 0, 0, 24);
            _meshData = HelpersRSUV.SetBit(_meshData, 25, true);
            foreach (var skinnedMesh in m_Meshes)
            {
                skinnedMesh.SetShaderUserValue(_meshData);
            }

            //Disable gravity so the player don't fall during respawn animation
            var gravScale = Rigidbody.gravityScale;
            Rigidbody.gravityScale = 0f;
            Movement.enabled = false;
            
            SpawnEffect.Play();

            GameManager.Instance.AudioManager.PlaySFX(RespawnAudio);
            
            float spawnTime = 1.0f;
            float spawnTimer = spawnTime;
            while (spawnTimer > 0f)
            {
                yield return null;
                spawnTimer -= Time.deltaTime;
                float ratio = 1.0f - (spawnTimer / spawnTime);
                _meshData = HelpersRSUV.EncodeDataIntoBits(_meshData, (int)(ratio * 1000), 0, 24);
                foreach (var skinnedMesh in m_Meshes)
                {
                    skinnedMesh.SetShaderUserValue(_meshData);
                }
            }
            
            Rigidbody.gravityScale = gravScale;
            Movement.enabled = true;
            
            SpawnEffect.Stop();
            _meshData = HelpersRSUV.SetBit(_meshData, 25, false);
            foreach (var skinnedMesh in m_Meshes)
            {
                skinnedMesh.SetShaderUserValue(_meshData);
            }
            SwitchInput(true);
        }

        public void SwitchInput(bool enabled)
        {
            m_PlayerInput.enabled = enabled;
        }

        public void StopMove()
        {
            Rigidbody.linearVelocity = Vector2.zero;
        }

        public void EnterCave()
        {
            m_SwitchingCaveLightMask = true;
            m_CaveLightMaskScaleTarget = m_CaveLightMaskOriginalScale;

            for (int i = 0; i < m_MaskLights.Length; ++i)
            {
                m_MaskLightsTargetRadius[i] = m_MaskLightsStartRadius[i];
            }
            
            GameManager.Instance.AudioManager.FadeToCave(true);
            GameManager.Instance.AudioManager.PlaySFXAt(TorchLitUpAudio, transform.position);

            OnCaveEnter?.Invoke();
        }

        public void ExitCave()
        {
            m_SwitchingCaveLightMask = true;
            m_CaveLightMaskScaleTarget = Vector3.zero;
            for (int i = 0; i < m_MaskLights.Length; ++i)
            {
                m_MaskLightsTargetRadius[i] = (0,0);
            }

            GameManager.Instance.AudioManager.FadeToCave(false);
            OnCaveExit?.Invoke();
        }

        public void SetExplicitMaskScale(float explicitMaskScale)
        {
            m_SwitchingCaveLightMask = true;
            m_CaveLightMaskScaleTarget = m_CaveLightMaskOriginalScale * explicitMaskScale;
            
            for (int i = 0; i < m_MaskLights.Length; ++i)
            {
                m_MaskLightsTargetRadius[i] = (m_MaskLightsStartRadius[i].Item1 * explicitMaskScale, m_MaskLightsStartRadius[i].Item2 * explicitMaskScale);
            }
        }

        public void EnteringWater(Vector3 point)
        {
            GameManager.Instance.AudioManager.PlaySFXAt(WaterEnterSound, point);
            GameManager.Instance.VfxController.PlayVfx(VfxInteractDescBase.VfxTypeEnum.WaterSplash, point);
            m_InWater = true;
        }

        public void ExitWater(Vector3 point)
        {
            GameManager.Instance.VfxController.PlayVfx(VfxInteractDescBase.VfxTypeEnum.WaterSplash, point);
            m_InWater = false;
        }

        /// <summary>
        /// Will set the player colliders to ignore the given collider for a given time. Can be useful for thing like
        /// going through one way platform.
        /// </summary>
        /// <param name="Collider"></param>
        /// <param name="IgnoreTime"></param>
        public void IgnoreCollider(Collider2D Collider, float IgnoreTime)
        {
            foreach (var c in m_Colliders)
            {
                if (c.isTrigger)
                    continue;
                
                Physics2D.IgnoreCollision(c, Collider, true);
            }
            
            m_TempIgnoredCollider.Add(new TempPhysicIgnore()
            {
                IgnoredCollider = Collider,
                IgnoringTimer = IgnoreTime
            });
        }

        public void SwitchModelMasked(bool masked)
        {
            foreach (var mesh in m_Meshes)
            {
                mesh.SetSpriteMaskInteraction(masked ? SpriteMaskInteraction.VisibleOutsideMask : SpriteMaskInteraction.None);
            }
            
            if (masked)
            {
                foreach (var mesh in m_Meshes)
                {
                    mesh.SetSpriteMaskInteraction(SpriteMaskInteraction.VisibleInsideMask);
                }

                SpawnVFX = false;
                VFXCollider.localPosition = Vector3.up * 20000;
            }
            else
            {
                foreach (var mesh in m_Meshes)
                {
                    mesh.SetSpriteMaskInteraction(SpriteMaskInteraction.None);
                }

                SpawnVFX = true;
                VFXCollider.localPosition = m_OriginalVfxColliderPosition;
            }
            
            OnMaskingChanged?.Invoke(masked);
        }

        void OnEnable()
        {
            m_PlayerInput = GetComponent<PlayerInput>();
            Rigidbody = GetComponent<Rigidbody2D>();
            Movement = GetComponent<PlayerMovement>();
            SortingGroup = GetComponent<SortingGroup>();
            SwitchInput(true);
        }

        void OnDisable()
        {
            SwitchInput(false);
        }
    }
}