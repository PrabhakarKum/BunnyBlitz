using System.Collections;
using UnityEngine;

namespace BunnyBlitz
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyRolling : EnemyBehaviour
    {

        [Header("Move values")]
        public float speed;
        public float rotationSpeed;
        public bool patrolOn;

        [Header("Visual")]
        public GameObject m_EnemyModel;
        
        protected override void Awake()
        {
            EnemyObject = m_EnemyModel;
            base.Awake();
        }

        public IEnumerator Start()
        {
            yield return new WaitForSeconds(m_DelayInit);
            patrolOn = true;
        }

        protected override void Update()
        {
            base.Update();
            if (!patrolOn || m_IsDead)
            {
                return;
            }
            Rb.linearVelocityX = speed;
            Rb.rotation += Time.deltaTime * rotationSpeed;

        }
        
        void OnCollisionEnter2D(Collision2D collision)
        {
            float contactAngle = Vector2.Angle(Vector2.up, collision.GetContact(0).normal);
            if (!collision.transform.CompareTag(PoolingSystem.ProjectileTag))
            {
                //changing direction 
                if (contactAngle > 45f)
                {
                    speed = -speed;
                }

            }
        }


    }
}