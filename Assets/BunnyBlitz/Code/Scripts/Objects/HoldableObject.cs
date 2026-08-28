using System.Collections.Generic;
using UnityEngine;

namespace BunnyBlitz
{
    /// <summary>
    /// An object with this component can be "picked up" automatically by the player on contact, and then thrown
    /// </summary>
    public class HoldableObject : MonoBehaviour
    {
        public bool Held => m_HeldBy != null;
        public Collider2D[] Colliders => m_Colliders;
        public Rigidbody2D Rb => m_Rigidbody2D;
        
        private Rigidbody2D m_Rigidbody2D;
        private Collider2D[] m_Colliders;
        protected PlayerBehaviour m_HeldBy = null;

        private List<Collider2D> m_IgnoredColliders = new();

        private float m_ThrownTimer = -1.0f;
        
        protected virtual void OnEnable()
        {
            m_Rigidbody2D = GetComponent<Rigidbody2D>();
            m_Colliders = GetComponentsInChildren<Collider2D>();
            m_ThrownTimer = -1.0f;
        }

        protected virtual void OnDisable()
        {
            m_HeldBy?.GrabObject(null);
            m_HeldBy = null;
        }

        public void Throw(Vector2 force)
        {
            Release();
            m_Rigidbody2D.AddForce(force, ForceMode2D.Impulse);
            OnThrown();
        }

        public void GrabBy(PlayerBehaviour player)
        {
            m_HeldBy = player;
            m_HeldBy.GrabObject(this);
            m_Rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            DisableColliders();
            OnGrabbed();
        }

        public void Release()
        {
            m_HeldBy?.GrabObject(null);
            m_HeldBy = null;
            transform.SetParent(GameManager.Instance.CurrentLayer.LayerObjectRoot);
            m_Rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
            m_Rigidbody2D.linearVelocity = Vector2.zero;
            //we reenable collision with the launcher after a while
            m_ThrownTimer = 0.5f;
        }
        
        protected virtual void OnGrabbed()
        {
            
        }

        protected virtual void OnThrown()
        {
            
        }

        protected virtual void Update()
        {
            if(m_ThrownTimer > 0.0f)
            {
                m_ThrownTimer -= Time.deltaTime;
                if(m_ThrownTimer <= 0.0f)
                    EnableColliders();
            }
        } 

        private void DisableColliders()
        {
            if (m_HeldBy == null)
                return;

            m_IgnoredColliders.AddRange(m_HeldBy.Colliders);
            foreach(var c in m_Colliders)
            {
                foreach(var oc in m_HeldBy.Colliders)
                {
                    Physics2D.IgnoreCollision(oc, c, true);
                }
            }
        }

        private void EnableColliders()
        {
            foreach(var c in m_Colliders)
            {
                foreach(var oc in m_IgnoredColliders)
                    Physics2D.IgnoreCollision(oc, c, false);
            }

            m_IgnoredColliders.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                if (m_HeldBy == null)
                {
                    var player = other.GetComponentInParent<PlayerBehaviour>();
                    if (player != null && player.HeldObject == null)
                    {
                        GrabBy(player);
                    }
                }
            }
        }
    }
}