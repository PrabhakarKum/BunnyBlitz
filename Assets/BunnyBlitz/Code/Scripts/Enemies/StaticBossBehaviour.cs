using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace BunnyBlitz
{
    public class StaticBossBehaviour : MonoBehaviour
    {
        public enum State
        {
            Waiting,
            Shot,
            Stomp
        }

    #region DATA DEFINITION

        [System.Serializable]
        public class ShootingPhase
        {
            public float MinimumSpeed = 1.0f;
            public float MaximumSpeed = 2.0f;
            public float MinShootingAngle = -10.0f;
            public float MaxShootingAngle = 10.0f;
            public float MinimumTimeBetweenShot = 1.0f;
            public float MaximumTimeBetweenShot = 3.0f;

            
            [FormerlySerializedAs("TurretSpeed")] 
            public float TurretMoveSpeed = 1.0f;
            [FormerlySerializedAs("TurrentRotateSpeed")] 
            public float TurretRotateSpeed = 10.0f;

            public float GetRandomTime()
            {
                return Random.Range(MinimumTimeBetweenShot,MaximumTimeBetweenShot);
            }

            public float GetRandomSpeed()
            {
                return Random.Range(MinimumSpeed, MaximumSpeed);
            }
        }

        [System.Serializable]
        public class StompingPhase
        {
            public int NumberOfObject = 5;
            public int NumberOfBomb = 1;
            public GameObject[] PossibleObjects;
        }

        [System.Serializable]
        public class BossPhase
        {
            public ShootingPhase Shoot;
            public StompingPhase Stomp;
            public int DamageBeforeStomp;
        }

        [System.Serializable]
        public class TurretSettings
        {
            public Transform TurretRoot;
            public Vector3 LowestLocalPoint;
            public Vector3 HighestLocalPoint;
        }
    #endregion

        public List<Collider2D> Colliders => m_Colliders;

        public GameObject BossCamera;

        [Header("References")]
        [Tooltip("Used when stomping to rise it into the air before falling down")]
        public Transform BossVisualRoot;
        public Transform ProjectileOriginPoint;
        public StaticBossProjectile ProjectilePrefab;
        public Animator BossAnimator;
        public Animator TurretAnimator; 
        
        public float SecondDelayBeforeProjectileSpawn = 0.2f;
        public TurretSettings Turret;
        
        public Transform StompSpawnPosition;
        public float StompSpawnSize = 2.0f;
        public float InvincibilityTimerAfterHit = 1.0f;

        [Header("Timing")] 
        public AnimationCurve StompCurve;

        [Header("Events")]
        public UnityEvent OnBossDeath;
        public UnityEvent OnPlayerRespawned;

        [Header("Audio")] 
        public AudioResource ShootingVocalizationAudio;
        public AudioResource ShootingAudio;
        public AudioResource JumpInitiatedAudio;
        public AudioResource LandingAudio;
        public AudioResource DamagedAudio;
        public AudioResource DeathAudio;

        [Header("PhaseData")]
        public BossPhase[] Phases; 

        private State m_CurrentState = State.Waiting;
        private int m_CurrentPhase = 0;
        
        private float m_ShootingTimer = 0.0f;
        private int m_RemainingPhaseHealthpoint = 0;

        private bool m_CameraShaking = false;

        private int m_ShotSinceLastProjectilesCountCheck = 0;
        private bool m_StompForProjectile = false;
        private float m_ProjectileStompTimer = 0.0f;

        private float m_InvicibilityTimer = 0.0f;
        private float m_BlinkingTimer = 0.0f;
        private int m_BlinkingColor = 0;
        
        private List<Renderer> m_MeshRenderers = new List<Renderer>();
        private List<Collider2D> m_Colliders = new List<Collider2D>();

        //Boss pools all its relevant gameobject itself as it's specific and not common like the common enemies
        private Queue<StaticBossProjectile> m_ProjectilePools = new();
        
        //we're keeping track of everything the boss have spawn to be able to "remove" it when the player respawn in
        //in order to reset the boss fight ot the beginning
        private List<Bomb> m_LiveBombs = new();

        //Keep track of projectile in the 
        private List<StaticBossProjectile> m_LiveProjectiles = new();

        private Vector3 m_StartTurretWorld;
        private Vector3 m_EndTurretWorld;
        private int m_TurrentMoveDirection = 1;
        private int m_TurrentRotateDirection = 1;

        private Vector3 m_AscendingStartingPosition;
        private float m_DescendingVelocity = 0.0f;

        private bool m_IsInFight = false;
        private int HitScaleID = Shader.PropertyToID("_HitScale");

        public void StartBossFight()
        {
            StartCoroutine(BossStart());
        }

        void OnEnable()
        {
            GameManager.Instance.OnPlayerRespawn += PlayerWasRespawned;

            var meshRenderers = GetComponentsInChildren<MeshRenderer>();
            var skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            
            m_MeshRenderers.AddRange(meshRenderers);
            m_MeshRenderers.AddRange(skinnedMeshRenderers);
            
            foreach (var r in m_MeshRenderers)
            {
                r.material.SetFloat(HitScaleID, 0.0f);
            }
            
            GetComponentsInChildren(m_Colliders);
        }

        void OnDisable()
        {
            GameManager.Instance.OnPlayerRespawn -= PlayerWasRespawned;
        }

        void Awake()
        {
            for(int i = 0; i < 16; ++i)
            {
                var inst = Instantiate(ProjectilePrefab, transform);
                inst.Init(this);
                inst.gameObject.SetActive(false);
                m_ProjectilePools.Enqueue(inst);
            }

            var dz = GetComponentsInChildren<DamageableCollider>();
            foreach (var zone in dz)
            {
                zone.OnDamage += OnDamaged;
                zone.OnRetaliate += OnRetaliate;
            }

            if(Turret.TurretRoot)
            {
                m_StartTurretWorld = Turret.TurretRoot.TransformPoint(Turret.LowestLocalPoint); 
                m_EndTurretWorld = Turret.TurretRoot.TransformPoint(Turret.HighestLocalPoint);
            }
            
        }

        void Update()
        {
            if(!m_IsInFight)
                return;
                
            switch(m_CurrentState)
            {
                case State.Shot:
                    ShootingUpdate();
                    break;
                case State.Stomp:
                    StompUpdate();
                    break;
                default:
                    break;
            }
        }


        void OnDamaged(DamageableCollider.DamageData data)
        {
            if (m_CurrentState == State.Stomp)
            {
                if (data.Type == DamageableCollider.DamageType.Explosive)
                {
                    GameManager.Instance.AudioManager.PlaySFXAt(DamagedAudio, BossAnimator.transform.position);
                    //Boss healthbar was removed in the last version of the project but kept commented as reference
                    //GameManager.Instance.HUD.RemoveBossHealthSegment(1);
                    BossAnimator.SetTrigger("hit");
                    
                    //cleanup all bomb leftover
                    foreach (var bomb in m_LiveBombs)
                    {
                        bomb.Remove();
                    }
                    
                    StartCoroutine(GoToNextPhase());
                }
            }
            else if(m_CurrentState == State.Shot)
            {
                if (m_InvicibilityTimer > 0.0f)
                    return;
                
                GameManager.Instance.AudioManager.PlaySFXAt(DamagedAudio, BossAnimator.transform.position);
                StartCoroutine(CameraShake(0.5f));
                m_RemainingPhaseHealthpoint -= data.Amount;
                BossAnimator.SetTrigger("hit");
                m_InvicibilityTimer = InvincibilityTimerAfterHit;
                
                //Boss healthbar was removed in the last version of the project but kept commented as reference
                //GameManager.Instance.HUD.RemoveBossHealthSubSegment(data.Amount);

                if (m_RemainingPhaseHealthpoint <= 0)
                {
                    StartCoroutine(StartStomping());
                }
            }
        }

        void OnRetaliate(DamageableCollider.DamageData data)
        {
            var player = data.Other.gameObject.GetComponent<PlayerBehaviour>();

            if (player != null)
            {
                player.ReceiveDamage(1 ,transform.position);
            }
        }

        IEnumerator BossStart()
        {
            m_IsInFight = true;
            GameManager.Instance.StartBossBattle(this);
            BossAnimator.SetBool("isDead", false);
            
            BossCamera.SetActive(true);
            
            const float startTime = 2.0f;
            StartCoroutine(CameraShake(startTime));
            yield return new WaitForSeconds(startTime);

            m_CurrentPhase = 0;
            StartPhase();
        }

        IEnumerator GoToNextPhase()
        {
            m_CurrentPhase++;
            if (m_CurrentPhase >= Phases.Length)
            {
                EndBossFight(true);
                Destroy(this);
                foreach (var c in m_Colliders)
                {
                    c.enabled = false;
                }
            }
            else
            {
                m_CurrentState = State.Waiting;
                yield return StartCoroutine(CameraShake(2.0f));
                StartPhase();
            }
        }

        void StartPhase()
        {
            foreach (var r in m_MeshRenderers)
            {
                r.material.SetFloat(HitScaleID, 0.0f);
            }
            
            m_CurrentState = State.Shot;
            m_ShootingTimer = -1;
            m_RemainingPhaseHealthpoint = Phases[m_CurrentPhase].DamageBeforeStomp;
            m_ShotSinceLastProjectilesCountCheck = 0;
        }

        IEnumerator DelayedShot(Vector3 dir, float speed)
        {
            yield return new WaitForSeconds(SecondDelayBeforeProjectileSpawn);
            var inst = m_ProjectilePools.Dequeue();
            inst.Spawned(ProjectileOriginPoint.position, dir, speed);
            m_LiveProjectiles.Add(inst);
        }

        void ShootingUpdate()
        {
            HandleDamagedBlinking();
            
            m_ShootingTimer -= Time.deltaTime;

            if(m_ShootingTimer < 0.0f)
            {
                //if we have 3 shot already, check if the player still have projectile to not be softlocked.
                //if the player is out of projectile go into a special stomp phase that spawn projectile
                if (m_ShotSinceLastProjectilesCountCheck > 3)
                {
                    if (GameManager.Instance.Player.ProjectilesCount == 0)
                    {
                        m_ShotSinceLastProjectilesCountCheck = 0;
                        m_StompForProjectile = true;
                        StartCoroutine(StartStomping());
                        return;
                    }
                }

                if(!Turret.TurretRoot)
                    return;
                
                
                
                var currentPhase = Phases[m_CurrentPhase];
                float speed = currentPhase.Shoot.GetRandomSpeed();

                Vector3 dir = -ProjectileOriginPoint.right;
                dir = Quaternion.Euler(0, 0, Turret.TurretRoot.rotation.eulerAngles.z) * dir;
                
                TurretAnimator?.SetTrigger("shoot");
                m_ShootingTimer = Phases[m_CurrentPhase].Shoot.GetRandomTime();

                StartCoroutine(DelayedShot(dir, speed));

                m_ShotSinceLastProjectilesCountCheck += 1;
                
                GameManager.Instance.AudioManager.PlaySFXAt(ShootingVocalizationAudio, BossAnimator.transform.position);
                GameManager.Instance.AudioManager.PlaySFXAt(ShootingAudio, ProjectileOriginPoint.position);
            }
            else if(Turret.TurretRoot)
            {
                var target = m_TurrentMoveDirection > 0 ? m_EndTurretWorld : m_StartTurretWorld;
                var p = Vector3.MoveTowards(Turret.TurretRoot.position, target, Phases[m_CurrentPhase].Shoot.TurretMoveSpeed * Time.deltaTime);

                if(p == target)
                    m_TurrentMoveDirection = -m_TurrentMoveDirection;
                
                Turret.TurretRoot.position = p;

                //rotation
                var currentRotation = Turret.TurretRoot.rotation;
                var angle = currentRotation.eulerAngles.z;
                var targetAngle = m_TurrentRotateDirection > 0 ? Phases[m_CurrentPhase].Shoot.MaxShootingAngle : Phases[m_CurrentPhase].Shoot.MinShootingAngle;
                var newAngle = Mathf.MoveTowardsAngle(angle, targetAngle, Time.deltaTime * Phases[m_CurrentPhase].Shoot.TurretRotateSpeed);

                if(Mathf.Approximately(newAngle, targetAngle))
                {
                    m_TurrentRotateDirection = -m_TurrentRotateDirection;
                }

                Turret.TurretRoot.rotation = Quaternion.Euler(currentRotation.eulerAngles.x, currentRotation.eulerAngles.y, newAngle);
            }
        }

        void StompUpdate()
        {
            if (m_StompForProjectile)
            {
                //special stomp that just wait a bit before going back to shooting
                m_ProjectileStompTimer += Time.deltaTime;
                if (m_ProjectileStompTimer < 2.0f) 
                    return;
                
                m_CurrentState = State.Shot;
                m_ProjectileStompTimer = 0.0f;
                m_StompForProjectile = false;
                return;
            }
            
            HandleStompBlinking();
            
            for(int i = 0; i < m_LiveBombs.Count; ++i)
            {
                if(!m_LiveBombs[i].gameObject.activeSelf)
                {
                    m_LiveBombs.RemoveAt(i);
                    i--;
                }
            }

            if(m_LiveBombs.Count == 0)
            {
                //no more bomb that are not exploded, we go back to the beginning of the phase
                //if a bomb touched and damaged the boss, it will have gone to another state by now
                //Boss healthbar was removed in the last version of the project but kept commented as reference
                //GameManager.Instance.HUD.RefillLastSegment(Phases[m_CurrentPhase].DamageBeforeStomp);
                StartPhase();
            }
        }

        IEnumerator StartStomping()
        {
            while (m_InvicibilityTimer > 0.0f)
            {
                HandleDamagedBlinking();
                yield return null;
            }

            m_BlinkingTimer = 0.1f;
            
            foreach (var r in m_MeshRenderers)
            {
                r.material.SetFloat(HitScaleID, 0.0f);
            }
            
            m_CurrentState = State.Waiting;
            GameManager.Instance.AudioManager.PlaySFXAt(JumpInitiatedAudio, BossAnimator.transform.position);

            //rise
            BossAnimator.SetTrigger("stomp");
            m_AscendingStartingPosition = BossVisualRoot.position;
            float stompTimer = 0.0f;
            float length = StompCurve.keys[StompCurve.length - 1].time;
            while (stompTimer < length)
            {
                var y = StompCurve.Evaluate(stompTimer);
                var pos = m_AscendingStartingPosition + Vector3.up * y;
                BossVisualRoot.position = pos;
                stompTimer += Time.deltaTime;
                yield return null;
            }

            BossVisualRoot.position = m_AscendingStartingPosition;

            //now come back down but ascelerating this time
            while(true)
            {
                m_DescendingVelocity += 9.8f * Time.deltaTime;
                BossVisualRoot.position = Vector3.MoveTowards(BossVisualRoot.position, m_AscendingStartingPosition, m_DescendingVelocity * Time.deltaTime);
                if(BossVisualRoot.position == m_AscendingStartingPosition)
                    break;
                
                yield return null;
            }
            
            GameManager.Instance.AudioManager.PlaySFXAt(LandingAudio, BossAnimator.transform.position);
            yield return StartCoroutine(CameraShake(1.0f));
            //spawn the object
            m_CurrentState = State.Stomp;

            GameObject[] spawnedBomb = new GameObject[5];

            var stompData = Phases[m_CurrentPhase].Stomp;
            var objectSpacing = StompSpawnSize / (stompData.NumberOfObject - 1);
            var possiblePosition = new List<int>();
            for(int i = 0; i < stompData.NumberOfObject; ++i)
                possiblePosition.Add(i);
            
            var spawnStart = StompSpawnPosition.position - StompSpawnPosition.right * StompSpawnSize * 0.5f;

            // only spawn bomb is this is not a projectile spawn stomp that happen when the player is out of projectiles
            if (!m_StompForProjectile)
            {
                for (int i = 0; i < stompData.NumberOfBomb; ++i)
                {
                    int bombPosIdx = Random.Range(0, possiblePosition.Count);
                    var bombPos = spawnStart + StompSpawnPosition.right * possiblePosition[bombPosIdx] * objectSpacing;

                    GameManager.Instance.PoolingSystem.SpawnBomb(1, bombPos,
                        GameManager.Instance.CurrentLayer.LayerObjectRoot, spawnedBomb);
                    m_LiveBombs.Add(spawnedBomb[0].GetComponent<Bomb>());
                    possiblePosition.RemoveAt(bombPosIdx);
                }
            }

            //now we fill the remaining position with any type of item from the possible items
            while (possiblePosition.Count > 0)
            {
                var pos = possiblePosition[0];
                possiblePosition.RemoveAt(0);
                var objPos = spawnStart + StompSpawnPosition.right * pos * objectSpacing;
                
                if (!m_StompForProjectile)
                {
                    var randObj = stompData.PossibleObjects[Random.Range(0, stompData.PossibleObjects.Length)];
                    var inst = GameManager.Instance.PoolingSystem.Spawn(randObj, objPos,
                        GameManager.Instance.CurrentLayer.LayerObjectRoot);

                    if (inst == null)
                    {
                        //the object requested is not pooled by the game, so instantiate a new version
                        //TODO : pool this new object? This should extremly rarely happen and there will only be half a dozen of them in the whole boss fight
                        inst = Instantiate(randObj, transform, false);
                        inst.transform.position = objPos;
                    }
                }
                else
                {
                    GameManager.Instance.PoolingSystem.SpawnProjectileCollectible(1, objPos, GameManager.Instance.CurrentLayer.LayerObjectRoot, null);
                }
            }
        }

        IEnumerator CameraShake(float shakeTime)
        {
            // if we're already the camera for now we ignore any new shaking request, TODO: expend the shaking time if a new one pop up
            if(!m_CameraShaking)
            {
                m_CameraShaking = true;
                const float shakeAmount = 0.5f;
                float startTime = Time.time;
                Vector3 originalPosition = BossCamera.transform.position;

                while(Time.time - startTime < shakeTime)
                {
                    BossCamera.transform.position = originalPosition + new Vector3(Random.Range(-shakeAmount, shakeAmount), Random.Range(-shakeAmount, shakeAmount), 0.0f);
                    yield return new WaitForSeconds(0.01f);
                }

                BossCamera.transform.position = originalPosition;
                m_CameraShaking = false;
            }
        }

        public void ReturnProjectile(StaticBossProjectile projectile)
        {
            projectile.gameObject.SetActive(false);
            m_ProjectilePools.Enqueue(projectile);
            m_LiveProjectiles.Remove(projectile);
        }

        public void EndBossFight(bool bossDefeated)
        {
            CleanupBossArea();
            
            GameManager.Instance.Player.SetExplicitMaskScale(1.0f);
            BossCamera.SetActive(false);
            //Boss healthbar was removed in the last version of the project but kept commented as reference
            //GameManager.Instance.HUD.HideBossHealthbar();
            m_IsInFight = false;
            
            foreach (var r in m_MeshRenderers)
            {
                r.material.SetFloat(HitScaleID, 0.0f);
            }
            
            if (bossDefeated)
            {
                BossAnimator.SetBool("isDead", true);
                OnBossDeath?.Invoke();
                GameManager.Instance.AudioManager.PlaySFXAt(DeathAudio, BossAnimator.transform.position);
            }
        }

        void CleanupBossArea()
        {
            // Destroy all remaining spawned bomb
            foreach (var bomb in m_LiveBombs)
            {
                
                Destroy(bomb.gameObject);
            }
            m_LiveBombs.Clear();

            //return all projectile so none are left in flight
            while (m_LiveProjectiles.Count > 0)
            {
                var proj = m_LiveProjectiles[0];
                if (!proj.Return())
                {
                    m_LiveProjectiles.Remove(proj);
                }
            }
        }

        void PlayerWasRespawned()
        {
            if(!m_IsInFight)
                return;
            
            OnPlayerRespawned?.Invoke();
            CleanupBossArea();
            
            m_CurrentState = State.Waiting;
            EndBossFight(false);
        }

        void HandleDamagedBlinking()
        {
            if (m_InvicibilityTimer > 0.0f)
            {
                m_InvicibilityTimer -= Time.deltaTime;
                m_BlinkingTimer -= Time.deltaTime;
                if (m_InvicibilityTimer <= 0.0f)
                {
                    foreach (var r in m_MeshRenderers)
                    {
                        r.material.SetFloat(HitScaleID, 0.0f);
                    }
                }
                else if (m_BlinkingTimer <= 0.0f)
                {
                    m_BlinkingTimer = 0.1f;
                    foreach (var r in m_MeshRenderers)
                    {
                        r.material.SetFloat(HitScaleID, m_BlinkingColor);
                    }

                    m_BlinkingColor = 1 - m_BlinkingColor;
                }
            }
        }

        void HandleStompBlinking()
        {
            m_BlinkingTimer -= Time.deltaTime;
            if (m_BlinkingTimer <= 0.0f)
            {
                m_BlinkingTimer = 0.1f;
                foreach (var r in m_MeshRenderers)
                {
                    r.material.SetFloat(HitScaleID, m_BlinkingColor);
                }

                m_BlinkingColor = 1 - m_BlinkingColor;
            }
        }

        void OnDrawGizmos()
        {
            if (StompSpawnPosition != null)
            {
                var min = StompSpawnPosition.position - StompSpawnPosition.right * StompSpawnSize * 0.5f;
                var max = StompSpawnPosition.position + StompSpawnPosition.right * StompSpawnSize * 0.5f;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(min, max);
                Gizmos.DrawSphere(min, 0.1f);
                Gizmos.DrawSphere(max, 0.1f);
            }

            if(Turret.TurretRoot != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(Turret.TurretRoot.TransformPoint(Turret.LowestLocalPoint), Turret.TurretRoot.TransformPoint(Turret.HighestLocalPoint));
            }
        }
    }
}