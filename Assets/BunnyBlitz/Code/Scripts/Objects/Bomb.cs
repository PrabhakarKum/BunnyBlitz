using UnityEngine;
using UnityEngine.VFX;

namespace BunnyBlitz
{
    public class Bomb : HoldableObject
    {
        public float ExplosionTimer = 5.0f;
        public float ExplosionRadius = 5.0f;
        private float m_ExplosionTimer;
        private bool m_WasThrown = false;
        private Animator m_Animatior;
        private int m_SpeedAnimHash = Animator.StringToHash("SpeedMultiplier");
        private int m_VfxTimerID =  Shader.PropertyToID("BombTimer");
        private int m_VfxRadiusID =  Shader.PropertyToID("DamageRadius");
        [SerializeField] private VisualEffect vfx;

         private bool m_RadiusDisplayed = false;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Animatior = GetComponent<Animator>();
            m_ExplosionTimer = ExplosionTimer;
            vfx.SetFloat(m_VfxTimerID, m_ExplosionTimer);
            vfx.SetFloat(m_VfxRadiusID, 0);
            vfx.Play();

            m_RadiusDisplayed = false;
        }

        protected override void Update()
        {
            base.Update();
            
            m_ExplosionTimer -= Time.deltaTime;

            if(!m_RadiusDisplayed && m_ExplosionTimer <= 2.0)
            {
                m_RadiusDisplayed = true;
                vfx.SetFloat(m_VfxRadiusID, ExplosionRadius);
            }

            float ratio = 1.0f - m_ExplosionTimer/ExplosionTimer;
            m_Animatior.SetFloat(m_SpeedAnimHash, 1.0f + ratio * 5.0f);

            if(m_ExplosionTimer < 0)
            {
                Explode();
            }
        }

        protected override void OnThrown()
        {
            base.OnThrown();
            m_WasThrown = true;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (m_WasThrown)
            {
                vfx.SetFloat(m_VfxTimerID, m_ExplosionTimer);
                Explode();
            }
        }

        void Explode()
        {
            var overlaps = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius);
            
            foreach(var c in overlaps)
            {
                var player = c.GetComponentInParent<PlayerBehaviour>();
                if(player != null)
                {
                    player.ReceiveDamage(1, transform.position);
                }
                else
                {
                    var d = c.GetComponent<DamageableCollider>();
                    if(d != null)
                    {
                        d.ExternalDamage(new DamageableCollider.DamageData() {Amount = 1, Other = Colliders[0], Type = DamageableCollider.DamageType.Explosive}); 
                    }
                }
            }
            
            GameManager.Instance.VfxController.PlayVfx(VfxInteractDescBase.VfxTypeEnum.Bomb, transform.position);
            gameObject.SetActive(false);
           
        }

        // Remove without exploding playing the remove VFX
        public void Remove()
        {
            if (Held)
            {
                m_HeldBy.GrabObject(null);
            }
            
            GameManager.Instance.VfxController.PlayVfx(VfxInteractDescBase.VfxTypeEnum.Disappear, transform.position);
            gameObject.SetActive(false);
        }
        

        void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, ExplosionRadius);
        }
    }
}