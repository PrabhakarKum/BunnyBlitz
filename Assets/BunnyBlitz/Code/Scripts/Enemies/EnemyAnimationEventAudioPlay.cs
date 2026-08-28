using UnityEngine;
using UnityEngine.Audio;

namespace BunnyBlitz
{
    public class EnemyAnimationEventAudioPlay : MonoBehaviour
    {
        public AudioSource FootFallSource;
        public AudioResource FootFallAudio;
        public AudioSource ChompSource;
        public AudioResource ChompAudio;
        public AudioSource WindFlapSource;
        public AudioResource WindFlapAudio;

        void FootFall()
        {
            GameManager.Instance.AudioManager.PlaySFXWithSource(FootFallSource, FootFallAudio);
        }

        void WindFlap()
        {
            GameManager.Instance.AudioManager.PlaySFXWithSource(WindFlapSource, WindFlapAudio);
        }

        void Chomp()
        {
            GameManager.Instance.AudioManager.PlaySFXWithSource(ChompSource, ChompAudio);
        }
    }
}