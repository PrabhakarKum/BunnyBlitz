using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.VFX;

namespace BunnyBlitz
{
    public class SpringBehaviour : MonoBehaviour
    {
        [SerializeField] private VisualEffect vfx;
        public float springImpulse = 140f;

        [Header("Audio")]
        public AudioResource SpringAudio;

        private Animator m_Animator;

        void OnEnable()
        {
            m_Animator = GetComponentInChildren<Animator>();
        }
    

        void OnCollisionEnter2D(Collision2D collision)
        {
            float cpAngle = Vector2.Angle(Vector2.up, collision.GetContact(0).normal) + transform.localEulerAngles.z;

            //impulse the character or object in the dir that the spring is facing
            if (cpAngle > 160f)
            {
                //Debug.Log(this.gameObject.name + "spring activated");
                float degrees = transform.localEulerAngles.z + 90f;
                float radians = degrees * Mathf.Deg2Rad;

                Vector2 v = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                collision.rigidbody.AddForce(v * springImpulse, ForceMode2D.Impulse);
                vfx.Play();

                var player = collision.gameObject.GetComponent<PlayerBehaviour>();
                player?.BouncedOnSpring();

                GameManager.Instance.AudioManager.PlaySFXAt(SpringAudio, transform.position);

                if (m_Animator)
                    m_Animator.SetTrigger("Bounce");
            }
        }
    }
}