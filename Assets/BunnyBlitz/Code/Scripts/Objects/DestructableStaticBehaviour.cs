using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace BunnyBlitz
{
    public class DestructableStaticBehaviour : MonoBehaviour
    {
        public int hitPoints = 1;
        public SpriteRenderer sr;
        private bool m_ShieldFromDamage;
        private Color m_InitColor;
        public Color colorOnDamage;
        public ParticleSystem destroyVfx;

        public AudioResource DestroySound;

        void Awake()
        {
            m_InitColor = sr.color;
        }

        void OnCollisionEnter2D(Collision2D collider)
        {
            switch (collider.gameObject.tag)
            {
                case PoolingSystem.ProjectileTag:
                    GameManager.Instance.VfxController.PlayVfx(VfxInteractDescBase.VfxTypeEnum.Pomegranate, collider.transform.position);
                    GameManager.Instance.PoolingSystem.Return(collider.gameObject);
                    if (!m_ShieldFromDamage)
                    {
                        StartCoroutine(DamageEnemy(1));
                    }
                    break;
            }
        }

        public void TriggerDestroyed()
        {
            StartCoroutine(Destroy());
        }

        IEnumerator Destroy()
        {
            if (destroyVfx != null)
            {
                //destroyVfx.Play();
                destroyVfx.transform.gameObject.SetActive(true);
            }
            sr.enabled = false;
            GetComponent<Collider2D>().enabled = false;
            GameManager.Instance.AudioManager.PlaySFXAt(DestroySound, transform.position);
            yield return new WaitForSeconds(destroyVfx.main.duration);
            Destroy(this.gameObject);
        }

        private IEnumerator DamageEnemy(int value)
        {
            m_ShieldFromDamage = true;
            hitPoints -= value;
            sr.color = colorOnDamage;
            if (hitPoints <= 0)
            {
                yield return Destroy();
            }
            else
            {
                //trigger damage anim
                // Pause execution and resume after the 
                yield return new WaitForSeconds(0.2f);
                m_ShieldFromDamage = false;
                sr.color = m_InitColor;
            }
        }

    }
}