using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace BunnyBlitz
{
    [System.Serializable]
    public class AudioManager
    {
        [System.Serializable]
        public class SurfaceSoundMatchUp : ISerializationCallbackReceiver
        {
            [System.Serializable]
            public class SurfaceSound
            {
                public SurfaceType SurfaceType;
                public AudioResource Sound;
            }
            
            public AudioResource Default;
            public List<SurfaceSound> SurfaceSounds = new();

            private Dictionary<SurfaceType, SurfaceSound> m_TypeToSoundLookup = new();

            public AudioResource GetSoundForSurface(SurfaceType surfaceType)
            {
                if(surfaceType != null && m_TypeToSoundLookup.TryGetValue(surfaceType, out var surfaceSound))
                {
                    return surfaceSound.Sound;
                }

                return Default;
            }

            public void OnBeforeSerialize()
            {
                
            }

            public void OnAfterDeserialize()
            {
                m_TypeToSoundLookup.Clear();
                foreach (var sound in SurfaceSounds)
                {
                    if(sound.SurfaceType != null)
                        m_TypeToSoundLookup.Add(sound.SurfaceType, sound);
                }
            }
        }

        public AudioSource[] AmbientSources;
        public AudioSource[] CaveAmbientSources;
        
        public AudioSource SFXAudioPrefab;

        public AudioListener Listener { private set; get; }
        
        private Queue<AudioSource> m_SFXSourcePool = new();

        private bool m_IsFading = false;
        private bool m_FadeToCave = false;

        //save as tuple with the original volume as item 2
        private (AudioSource, float)[] m_AmbientSources;
        private (AudioSource, float)[] m_CaveAmbientSources;
        
        public void Init()
        {
            //We can have left over from older level
            m_SFXSourcePool.Clear();
            
            const int SourceCount = 64;

            for (int i = 0; i < SourceCount; ++i)
            {
                var inst = Object.Instantiate(SFXAudioPrefab);
                inst.loop = false;
                inst.Stop();
                m_SFXSourcePool.Enqueue(inst);
            }

            var go = new GameObject();
            Listener = go.AddComponent<AudioListener>();

            m_CaveAmbientSources = new (AudioSource, float)[CaveAmbientSources.Length];
            for (int i = 0; i < CaveAmbientSources.Length; ++i)
            {
                m_CaveAmbientSources[i] = (CaveAmbientSources[i], CaveAmbientSources[i].volume);
                m_CaveAmbientSources[i].Item1.volume = 0.0f;
            }
            
            m_AmbientSources = new (AudioSource, float)[AmbientSources.Length];
            for (int i = 0; i < AmbientSources.Length; ++i)
            {
                m_AmbientSources[i] = (AmbientSources[i], AmbientSources[i].volume);
            }
        }

        public void Tick()
        {
            const float fadeSpeed = 2.0f;
            if (!m_IsFading) return;
            
            if (m_FadeToCave)
            {
                bool allCaveSourceDone = true;
                foreach (var source in m_CaveAmbientSources)
                {
                    source.Item1.volume = Mathf.MoveTowards(source.Item1.volume, source.Item2, Time.deltaTime * fadeSpeed);
                    allCaveSourceDone &= source.Item1.volume > 0.99f;
                }

                bool allAmbientDone = true;
                foreach (var source in m_AmbientSources)
                {
                    source.Item1.volume = Mathf.MoveTowards(source.Item1.volume, 0.0f, Time.deltaTime * fadeSpeed);
                    allAmbientDone &= source.Item1.volume < 0.01f;
                }

                if (allAmbientDone && allCaveSourceDone)
                    m_IsFading = false;
            }
            else
            {
                bool allCaveSourceDone = true;
                foreach (var source in m_CaveAmbientSources)
                {
                    source.Item1.volume = Mathf.MoveTowards(source.Item1.volume, 0.0f, Time.deltaTime * fadeSpeed);
                    allCaveSourceDone &= source.Item1.volume < 0.01f;
                }

                bool allAmbientDone = true;
                foreach (var source in m_AmbientSources)
                {
                    source.Item1.volume = Mathf.MoveTowards(source.Item1.volume, source.Item2, Time.deltaTime * fadeSpeed);
                    allAmbientDone &= source.Item1.volume > 0.99f;
                }

                if (allAmbientDone && allCaveSourceDone)
                    m_IsFading = false;
            }
        }

        public void FadeToCave(bool fade)
        {
            m_FadeToCave = fade;
            m_IsFading = true;
        }

        /// <summary>
        /// Play the given audio clip variation non spatialized
        /// </summary>
        /// <param name="clip"></param>
        public void PlaySFX(AudioResource clip)
        {
            if(clip == null || m_SFXSourcePool.Count == 0)
                return;

            var usedSource = m_SFXSourcePool.Dequeue();
            PlaySFXAt(usedSource, clip, Vector3.zero,  0.0f);
            m_SFXSourcePool.Enqueue(usedSource);
        }

        /// <summary>
        /// Play the given audio clip variation spatialized at the given position
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="position"></param>
        public void PlaySFXAt(AudioResource clip, Vector3 position)
        {
            if(clip == null || m_SFXSourcePool.Count == 0)
                return;
            
            var usedSource = m_SFXSourcePool.Dequeue();
            PlaySFXAt(usedSource, clip, position);
            m_SFXSourcePool.Enqueue(usedSource);
        }

        public void PlaySFXWithSource(AudioSource source, AudioResource clip)
        {
            PlaySFXAt(source, clip, source.transform.position);
        }

        public void PlaySFXAt(AudioSource source, AudioResource clip, Vector3 position, float spatialBlend = 1.0f)
        {
            if(clip == null)
                return;

            source.Stop();
            source.resource = clip;
            source.transform.position = position;
            source.spatialBlend = spatialBlend;
            source.Play();
        }
    }
}