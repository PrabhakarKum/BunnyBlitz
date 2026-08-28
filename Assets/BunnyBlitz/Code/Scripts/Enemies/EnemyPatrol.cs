using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BunnyBlitz
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyPatrol : EnemyBehaviour
    {
        [Header("Flying enemy values")]
        public bool isFlyingEnemy;
        public float sineWaveSpeed;
        public float sineWaveFrequency;

        [Header("Move values")]
        public bool mirrorAccelerationCurve;
        public AnimationCurve accelerationCurve = new AnimationCurve(new Keyframe(0f, 1.0f, 0f, 0f), new Keyframe(1f, 1.0f, 0f, 0f));
        public List<Vector2> walkTowardsLocalPoints = new List<Vector2>();
        [Tooltip("If true, enemy goes to 1st point after the last. If false, walk the path backward to go back to the beginning when reaching the end")]
        public bool LoopingPath = true;
        public float idleTimeBetweenWalkPoints = 1f;
        private int m_CurrentListPos;
        public Rigidbody2D.SlideMovement SlideMovement = new Rigidbody2D.SlideMovement();
        public float speed;
        public bool patrolOn;

        [Header("Attack values")]
        public Vector2 m_EnemyProjectileDirection;
        [Range(0f, 1f)]
        public float chancesOfAttackingWhenIdle = 1f;
        public float MaxPlayerDistanceForAttack = 10.0f;
        public Vector3 offset = new Vector3(0f, 2f, 0f);

        [Header("Visual")]
        public GameObject m_EnemyModel;
        public int modelFacingDir;
        public float rotationLerpFactor = 0.03f;

        private float m_GroundAngle;
        private bool m_IsWaitingInPosition;
        private bool m_FlipAccelerationCurveThisTurn;
        private float m_IdleTimeBetweenWalkPointsTimer;
        private float m_nFrames = 10f;
        private Collider2D[] m_Colliders;
        private Vector3[] m_FixedWorldPoints;
        private int m_PathDirection = 1;
        private bool m_WillAttack;

        private int m_FlyingHash = Animator.StringToHash("Flying");

        protected override void Awake()
        {
            EnemyObject = m_EnemyModel;
            m_Colliders = GetComponentsInChildren<Collider2D>();
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Animator.SetBool(m_FlyingHash, isFlyingEnemy);
        }


        public IEnumerator Start()
        {
            //we fix the local point in world space on start. It's practical to have local point at authoring time (because the path will move with the enemy)
            //but as the enemy will move, its patrol point would move with it! So we compute what are the world point to use
            m_FixedWorldPoints = new Vector3[walkTowardsLocalPoints.Count];
            for (int i = 0; i < walkTowardsLocalPoints.Count; ++i)
            {
                m_FixedWorldPoints[i] = transform.TransformPoint(walkTowardsLocalPoints[i]);
            }

            yield return new WaitForSeconds(m_DelayInit);


            if (UnityEngine.Random.Range(0f, 1f) < chancesOfAttackingWhenIdle)
            {
                m_WillAttack = true;
            }
            else
            {
                m_WillAttack = false;
            }
            patrolOn = true;


        }

        protected override void Update()
        {
            base.Update();
            
            //if the patrol is not on, we are dead or we're shielded from damage (just got hit)
            if (!patrolOn || m_IsDead || m_ShieldFromDamage)
            {
                return;
            }

            Vector3 worldPosWalkTowardsPoint = transform.position;
            Vector3 worldPosWalkPreviousPoint = transform.position;
            Vector2 dirVelocity = Vector2.zero;
            Vector3 effectiveSpeed = Vector3.zero;
            if (m_FixedWorldPoints.Length > 1 && !m_IsWaitingInPosition)
            {

                worldPosWalkTowardsPoint = m_FixedWorldPoints[m_CurrentListPos];
                int previousListPos = m_CurrentListPos - 1;
                if (previousListPos < 0)
                {
                    previousListPos = m_FixedWorldPoints.Length - 1;
                }
                worldPosWalkPreviousPoint = m_FixedWorldPoints[previousListPos];

                if (isFlyingEnemy)
                {
                    var totalDistanceVector = (Vector2)worldPosWalkTowardsPoint - (Vector2)worldPosWalkPreviousPoint;
                    float totalDistance = Mathf.Abs(totalDistanceVector.magnitude);
                    var distanceCompletedVector = Rb.position - (Vector2)worldPosWalkPreviousPoint;
                    float distanceCompleted = Mathf.Abs(distanceCompletedVector.magnitude);
                    float acceleration = Mathf.Clamp(accelerationCurve.Evaluate(distanceCompleted / totalDistance), 0.05f, 1f); //make sure not stop completed or going over value
                    if (m_FlipAccelerationCurveThisTurn)
                    {
                        acceleration = Mathf.Clamp(accelerationCurve.Evaluate(1f - (distanceCompleted / totalDistance)), 0.05f, 1f);

                    }
                    dirVelocity = ((Vector2)worldPosWalkTowardsPoint - Rb.position).normalized; //flying enemy
                    Vector2 perpendicularDirection = new Vector2(-dirVelocity.y, dirVelocity.x);
                    float waveValue = Mathf.Sin(Time.time * sineWaveFrequency);
                    Vector2 waveVelocity = perpendicularDirection * waveValue * sineWaveSpeed;
                    effectiveSpeed = dirVelocity * speed * acceleration + waveVelocity;
                    Rb.linearVelocity = effectiveSpeed;
                }
                else
                {
                    float totalDistance = Mathf.Abs(worldPosWalkTowardsPoint.x - worldPosWalkPreviousPoint.x);
                    float distanceCompleted = Mathf.Abs(Rb.position.x - worldPosWalkPreviousPoint.x);
                    float acceleration = Mathf.Clamp(accelerationCurve.Evaluate(distanceCompleted / totalDistance), 0.1f, 1f); //make sure not stop completed or going over value
                    if (m_FlipAccelerationCurveThisTurn)
                    {
                        acceleration = Mathf.Clamp(accelerationCurve.Evaluate(1f - (distanceCompleted / totalDistance)), 0.1f, 1f);
                    }

                    float VelocityY = 0f;
                    if (Rb.linearVelocityY > 0)
                    {
                        VelocityY = Rb.linearVelocityY;
                    }
                    dirVelocity = new Vector2(Mathf.Sign(worldPosWalkTowardsPoint.x - Rb.position.x), VelocityY); //ground enemy, y velocity is 0 or higher
                    effectiveSpeed = dirVelocity * speed * acceleration;
                    var slide = Rb.Slide(effectiveSpeed, Time.deltaTime, SlideMovement);
                    
                    m_GroundAngle = Vector2.SignedAngle(Vector2.up, slide.surfaceHit.normal);

                    var slideHitAngle = Vector3.Angle(Vector2.up, slide.slideHit.normal);
                    if (slideHitAngle >= 45.0f)
                    {
                        //we handle if we hit the player in a special way, because the collision with the player can be
                        //detected before an actual collision occur
                        PlayerBehaviour player =
                            slide.slideHit.collider.gameObject.GetComponentInParent<PlayerBehaviour>();
                        if (player != null)
                        {
                            player.ReceiveDamage(1, Rb.position);
                        }
                        
                        Rb.linearVelocity = Vector2.zero;
                        m_CurrentListPos++;
                        if (m_CurrentListPos > m_FixedWorldPoints.Length - 1)
                        {
                            m_CurrentListPos = 0;
                        }
                    }
                }
            }

            //rotation 3d model
            if (dirVelocity.x > 0)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = true;
                }
                modelFacingDir = 1;
            }
            else if (dirVelocity.x < 0)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = false;
                }
                modelFacingDir = -1;
            }

            var rotationAmount = (1.0f - Mathf.Clamp01(modelFacingDir)) * 180.0f + modelFacingDir * 40.0f;
            var facingRotation = Quaternion.AngleAxis(rotationAmount, Vector3.up);
            var groundRotation = Quaternion.AngleAxis(m_GroundAngle, Vector3.forward);

            EnemyObject.transform.localRotation = Quaternion.Slerp(groundRotation * facingRotation, EnemyObject.transform.localRotation, Mathf.Pow(rotationLerpFactor, Time.deltaTime)); //smoothing would be needed

            m_Animator.SetFloat("xSpeed", Mathf.Abs(effectiveSpeed.x));
            //Check every n frames

            if (Time.frameCount % m_nFrames == 0)
            {
                float distance = 99f;
                if (isFlyingEnemy)
                {
                    distance = ((Vector2)worldPosWalkTowardsPoint - Rb.position).magnitude; //distance on the x axi
                }
                else
                {
                    distance = Math.Abs(worldPosWalkTowardsPoint.x - Rb.position.x);

                }
                if (distance < 0.25f) //close enough to change dir
                {
                    Rb.linearVelocity = Vector2.zero;
                    m_IsWaitingInPosition = true;
                    m_IdleTimeBetweenWalkPointsTimer += Time.deltaTime * m_nFrames;
                    if (m_IdleTimeBetweenWalkPointsTimer < idleTimeBetweenWalkPoints)
                    {
                        if (m_IdleTimeBetweenWalkPointsTimer >= idleTimeBetweenWalkPoints * 0.5f) //check in the middle of idle time if it attacks
                        {
                            if (m_WillAttack)
                            {
                                EnemyAttack();
                            }
                        }
                        
                        return;
                    }

                    //SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();

                    //for next move
                    if (mirrorAccelerationCurve)
                    {
                        if (m_FlipAccelerationCurveThisTurn)
                        {
                            m_FlipAccelerationCurveThisTurn = false;
                        }
                        else { m_FlipAccelerationCurveThisTurn = true; }
                    }

                    m_IsWaitingInPosition = false;

                    m_CurrentListPos += m_PathDirection;
                    if (m_CurrentListPos > m_FixedWorldPoints.Length - 1)
                    {
                        if (LoopingPath)
                            m_CurrentListPos = 0;
                        else
                        {
                            m_CurrentListPos = m_FixedWorldPoints.Length - 2;
                            m_PathDirection *= -1;
                        }
                    }
                    else if (m_CurrentListPos < 0)
                    {
                        if (LoopingPath)
                            m_CurrentListPos = m_FixedWorldPoints.Length - 1;
                        else
                        {
                            m_CurrentListPos = 1;
                            m_PathDirection *= -1;
                        }
                    }

                    m_IdleTimeBetweenWalkPointsTimer = 0;
                    var distanceToPlayer = GameManager.Instance.Player.transform.position - transform.position;
                    if (UnityEngine.Random.Range(0f, 1f) < chancesOfAttackingWhenIdle && GameManager.Instance.CurrentLayer.gameObject.layer == gameObject.layer &&
                        distanceToPlayer.magnitude < MaxPlayerDistanceForAttack) //only attack if its in the same play layer and distance ti player is smaller than the defined one in the properties
                    {
                        m_WillAttack = true;
                    }
                    else
                    {
                        m_WillAttack = false;
                    }


                }

            }

        }

        private void EnemyAttack()
        {
            var prj = new GameObject[5];
            GameManager.Instance.PoolingSystem.SpawnProjectile("ProjectileEnemy", 1, transform.position,
                GameManager.Instance.GetLayerOfObject(gameObject).LayerObjectRoot, (GameManager.Instance.Player.transform.position + offset - transform.position).normalized, prj); //point between up and model angle * player

            var itmPhysic = prj[0].GetComponent<ItemsPhysicsBehaviour>();
            itmPhysic.IgnoreColliders(m_Colliders);
            //Debug.Log($"{this.name} attacking now");
            m_WillAttack = false;
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            m_Animator.SetBool(m_FlyingHash, false);
        }

        void OnDrawGizmosSelected()
        {
            //when we are not playing the edito take care of drawing the lines
            if (!Application.isPlaying)
                return;

            var endIdx = LoopingPath? m_FixedWorldPoints.Length : m_FixedWorldPoints.Length - 1;

            for (int i = 0; i < endIdx; ++i)
            {

                var start = m_FixedWorldPoints[i];
                var end = i < m_FixedWorldPoints.Length - 1 ? m_FixedWorldPoints[i + 1] : m_FixedWorldPoints[0];

                Vector3 startToEnd = end - start;
                Vector3 perpenticular = Vector3.Cross(startToEnd.normalized, Vector3.forward);

                Gizmos.DrawLine(start, end);
                Gizmos.DrawLine(start + startToEnd * 0.5f, start + startToEnd * 0.5f + startToEnd.normalized * -0.2f + perpenticular * 0.2f);
                Gizmos.DrawLine(start + startToEnd * 0.5f, start + startToEnd * 0.5f + startToEnd.normalized * -0.2f + perpenticular * -0.2f);
            }
        }
    }
}

#if UNITY_EDITOR
namespace BunnyBlitz.Editor
{
    [CustomEditor(typeof(EnemyPatrol))]
    public class EnemyPatrolEditor : UnityEditor.Editor
    {
        EnemyPatrol m_Target;
        SerializedProperty m_WalkingPointProperty;
        SerializedProperty m_LoopingPathProperty;

        void OnEnable()
        {
            m_Target = target as EnemyPatrol;
            m_WalkingPointProperty = serializedObject.FindProperty(nameof(EnemyPatrol.walkTowardsLocalPoints));
            m_LoopingPathProperty = serializedObject.FindProperty(nameof(EnemyPatrol.LoopingPath));
        }

        void OnSceneGUI()
        {
            //Cannot modify during gameplay, OnGizmoSelected will draw the path
            if (Application.isPlaying)
                return;

            for (int i = 0; i < m_WalkingPointProperty.arraySize; ++i)
            {
                bool drawLine = true;
                var entry = m_WalkingPointProperty.GetArrayElementAtIndex(i);
                SerializedProperty next = null;
                if (i < m_WalkingPointProperty.arraySize - 1)
                {
                    next = m_WalkingPointProperty.GetArrayElementAtIndex(i + 1);
                }
                else
                {
                    drawLine = m_LoopingPathProperty.boolValue;
                    next = m_WalkingPointProperty.GetArrayElementAtIndex(0);
                }

                Vector3 startPoint = m_Target.transform.TransformPoint(entry.vector2Value);
                Vector3 endPoint = m_Target.transform.TransformPoint(next.vector2Value);

                Vector3 startToEnd = endPoint - startPoint;
                Vector3 perpenticular = Vector3.Cross(startToEnd.normalized, Vector3.forward);

                if (drawLine)
                {
                    Handles.DrawLine(startPoint, endPoint);
                    Handles.DrawLine(startPoint + startToEnd * 0.5f, startPoint + startToEnd * 0.5f + startToEnd.normalized * -0.2f + perpenticular * 0.2f);
                    Handles.DrawLine(startPoint + startToEnd * 0.5f, startPoint + startToEnd * 0.5f + startToEnd.normalized * -0.2f + perpenticular * -0.2f);
                }

                EditorGUI.BeginChangeCheck();
                var newValue = Handles.PositionHandle(startPoint, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    entry.vector2Value = m_Target.transform.InverseTransformPoint(newValue);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
#endif