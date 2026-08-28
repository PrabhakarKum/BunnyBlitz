using UnityEngine;
using UnityEngine.Audio;

namespace BunnyBlitz
{
    public class ItemsPhysicsBehaviour : MonoBehaviour
    {    
        public float impulseXmin = 3f;
        public float impulseXmax = 3f;
        [System.Serializable]
        public enum directionX { Right, Left, RandomLeftRight,Custom};
        [Tooltip("If it's 0 it won't die on its own")]
        public float destroyTimer = 7f; //if 0 they dont die until collected
  
        public directionX dirX;
        public Vector2 CustomDir
        {
            get  => m_CustomDir;
            set
            {
                m_CustomDir = value;
                Push();
            }
        }
        public float maxVelocity = 30f;

        public AudioResource ContactSound;

        private Rigidbody2D rb;
        private Collider2D[] m_Colliders;
        private MeshRenderer[] m_Renderers;
        private Vector2 m_CustomDir;

        private float m_RemainingTimeBeforeDestroy = 0.0f;
        private float m_BlinkingTimer = 0.0f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            m_Colliders = GetComponentsInChildren<Collider2D>();
            m_Renderers = GetComponentsInChildren<MeshRenderer>();
        }
    
        void OnEnable()
        {
            Push();
            if (destroyTimer>0)
            {
                m_RemainingTimeBeforeDestroy = destroyTimer;
            }
            else
            {
                m_RemainingTimeBeforeDestroy = -1.0f;
            }
           
        }
        
        void Update()
        {
            if (m_RemainingTimeBeforeDestroy >= 0)
            {
                m_RemainingTimeBeforeDestroy -= Time.deltaTime;
                if (m_RemainingTimeBeforeDestroy <= 0)
                {
                    foreach (var r in m_Renderers)
                    {
                        r.enabled = true;
                    }
                    AutoDestroy();
                    return;
                }
                
                if (m_RemainingTimeBeforeDestroy <= 1.0f)
                {
                    m_BlinkingTimer -= Time.deltaTime;
                    if (m_BlinkingTimer <= 0.0f)
                    {
                        m_BlinkingTimer = 0.04f;
                        foreach (var r in m_Renderers)
                        {
                            r.enabled = !r.enabled;
                        }
                    }
                }
            }

            // Check if the current speed exceeds the maximum speed
            if (rb.linearVelocity.magnitude > maxVelocity)
            {
                // Clamp the velocity by normalizing it and multiplying by the max speed
                rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
            }
        }
        
        public void Push()
        {
            Vector2 dir = Vector2.zero;
            switch (dirX)
            {
                case directionX.Left:
                    dir = Vector2.left;
                    break;
                case directionX.Right:
                    dir = Vector2.right;
                    break;
                case directionX.RandomLeftRight:
                    dir = Vector2.left;
                    if (Random.value < 0.5f)
                    {
                        dir = Vector2.right;
                    }
                    break;
                case directionX.Custom:
                    dir = CustomDir;
                    break;
            }

            rb.AddForce(Random.Range(impulseXmin, impulseXmax) * dir, ForceMode2D.Impulse);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            GameManager.Instance.AudioManager.PlaySFXAt(ContactSound, transform.position);
        }

        public void IgnoreColliders(Collider2D[] colliders)
        {
            foreach (var c in colliders)
            {
                if (c.isTrigger)
                    continue;

                foreach (var oc in m_Colliders)
                {
                    if (oc.isTrigger)
                        continue;
                    
                    Physics2D.IgnoreCollision(c, oc, true);
                }
            }
        }

        private void AutoDestroy()
        {
            if (gameObject.activeSelf)
            {
                if (TryGetComponent(out TrailRenderer tr))
                {
                    tr.Clear();
                }
                GameManager.Instance.PoolingSystem.Return(gameObject);
            }
        }
    }
}