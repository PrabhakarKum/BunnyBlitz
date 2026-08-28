using System.Collections.Generic;
using UnityEngine;

namespace BunnyBlitz
{
    public class StaticBossProjectile : MonoBehaviour
    {
        public float MaxLifetime = 5.0f;
        public int MaxBounce = 3;

        private StaticBossBehaviour m_Owner;
        private Rigidbody2D m_Rigidbody2d;

        private List<Collider2D> m_Colliders = new();

        private float m_Speed = 1.0f;
        private float m_LifetimeCounter = 0.0f;
        private float m_BossCollisionIgnoreTimer = 0.0f;
        private int m_BounceCount = 0;

        void Awake()
        {
            m_Rigidbody2d = GetComponent<Rigidbody2D>();
            m_Rigidbody2d.useFullKinematicContacts = true;
            m_Rigidbody2d.bodyType = RigidbodyType2D.Kinematic;
        }

        public void Init(StaticBossBehaviour owner)
        {
            m_Owner = owner;
            GetComponentsInChildren(m_Colliders);
        }

        public void Spawned(Vector3 position, Vector3 direction, float speed)
        {
            transform.position = position;
            transform.right = direction;
            m_LifetimeCounter = MaxLifetime;
            m_BounceCount = 0;
            m_Speed = speed;

            // Disable collision between the boss and the projectile so it can fly "out" of the boss colliders
            foreach(var oc in m_Owner.Colliders)
            {
                foreach(var lc in m_Colliders)
                {
                    Physics2D.IgnoreCollision(oc, lc, true);
                }
            }

            // Once that timer reach zero, we re-enable collision between the boss and projectile so it can bounce
            m_BossCollisionIgnoreTimer = 1.0f;

            gameObject.SetActive(true);
        }

        void Update()
        {
            if(m_BossCollisionIgnoreTimer > 0)
            {
                m_BossCollisionIgnoreTimer -= Time.deltaTime;
                if(m_BossCollisionIgnoreTimer < 0)
                {
                    foreach(var oc in m_Owner.Colliders)
                    {
                        foreach(var lc in m_Colliders)
                        {
                            Physics2D.IgnoreCollision(oc, lc, false);
                        }
                    }
                }
            }

            m_LifetimeCounter -= Time.deltaTime;
            if(m_LifetimeCounter < 0.0f)
            {
                Return();
                return;
            }

            transform.position += m_Speed * Time.deltaTime * transform.right;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            var player = collision.gameObject.GetComponentInParent<PlayerBehaviour>(); 
            if(player != null)
            {
                player.ReceiveDamage(1, collision.contacts[0].point);
                Return();
            }
            else if (collision.gameObject.CompareTag(PoolingSystem.ProjectileTag))
            {
                GameManager.Instance.VfxController.PlayVfx(VfxInteractDescBase.VfxTypeEnum.Pomegranate, collision.transform.position);
                GameManager.Instance.PoolingSystem.Return(collision.gameObject);
            }
            else if(collision.rigidbody == null || collision.rigidbody.bodyType == RigidbodyType2D.Static)
            {
                m_BounceCount += 1;
                if (m_BounceCount >= MaxBounce)
                {
                    Return();
                }
                else
                {
                    //if it's a rigidbody less collider 
                    var newDir = Vector3.Reflect(transform.right, collision.contacts[0].normal);
                    transform.right = newDir;
                }
            }
        }

        // Return true if the projectile was able to be returned, false otherwise (used to cleanup up straight shot)
        public bool Return()
        {
            GameManager.Instance.VfxController.PlayVfx(VfxInteractDescBase.VfxTypeEnum.Disappear, transform.position);
            if(m_Owner != null && gameObject.activeSelf)
            {
                m_Owner.ReturnProjectile(this);
                return true;
            }

            return false;
        }
    }
}