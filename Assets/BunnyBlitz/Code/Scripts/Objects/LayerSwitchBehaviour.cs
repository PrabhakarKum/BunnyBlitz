using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Splines;
using UnityEngine.VFX;

namespace BunnyBlitz
{
    public enum LayerSwitchDirection
    {
        FrontToBack,
        BackToFront
    }

    [ExecuteAlways]
    public class LayerSwitchBehaviour : MonoBehaviour
    {
        [SerializeField] private LayerSwitchDirection m_Direction = LayerSwitchDirection.FrontToBack;
        public LayerSwitchDirection Direction => m_Direction;

        public SplineAnimate splineForAnimation;

        [Header("VFX Feedback")]
        [SerializeField] private VisualEffect m_LayerSwitchVFX;
        [SerializeField] private Volume m_FeedbackVolume;
        [SerializeField] private AnimationCurve m_VolumeWeightCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve m_LensDistortionIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private string m_AlphaGlobalPropertyName = "_Alpha";
        [SerializeField] private AnimationCurve m_AlphaCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private string m_VFXAlphaPropertyName = "Alpha";
        [SerializeField] private AnimationCurve m_VFXAlphaCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private string m_VFXSplineTimePropertyName = "SplineTime";
        [Range(0f, 1f)]
        [SerializeField] private float m_FeedbackAmount;

        [Header("Audio")] 
        [SerializeField] private AudioResource ActivatedAudio;

        public float FeedbackAmount => m_FeedbackAmount;

        private PlayerBehaviour m_Playerbehaviour;
        private Vector3 m_InitPos;
        private bool m_WasSplinePlaying;

        private static LayerSwitchBehaviour s_ActiveOwner;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            m_InitPos = transform.position;
            BindSplineToVFX();
        }

        void OnValidate()
        {
            ApplyFeedbackToVolume();
            BindSplineToVFX();
        }

        void BindSplineToVFX()
        {
            if (m_LayerSwitchVFX == null || splineForAnimation == null)
                return;

            var binder = m_LayerSwitchVFX.GetComponent<VFXSplinePositionBinder>();
            if (binder != null && binder.Spline != splineForAnimation.Container)
                binder.Spline = splineForAnimation.Container;
        }

        void ApplyFeedbackToVolume()
        {
            // Per-instance: each component drives its own VFX
            if (m_LayerSwitchVFX != null
                && !string.IsNullOrEmpty(m_VFXAlphaPropertyName)
                && m_LayerSwitchVFX.HasFloat(m_VFXAlphaPropertyName))
            {
                float vfxAlpha = Mathf.Clamp01(m_VFXAlphaCurve.Evaluate(m_FeedbackAmount));
                m_LayerSwitchVFX.SetFloat(m_VFXAlphaPropertyName, vfxAlpha);
            }

            if (m_LayerSwitchVFX != null
                && !string.IsNullOrEmpty(m_VFXSplineTimePropertyName)
                && m_LayerSwitchVFX.HasFloat(m_VFXSplineTimePropertyName))
            {
                m_LayerSwitchVFX.SetFloat(m_VFXSplineTimePropertyName, m_FeedbackAmount);
            }

            // Shared outputs (volume + shader global): only one owner at a time
            bool hasFeedback = m_FeedbackAmount > 0f;
            if (hasFeedback && s_ActiveOwner == null)
            {
                s_ActiveOwner = this;
            }
            else if (!hasFeedback && s_ActiveOwner == this)
            {
                ReleaseSharedOutputs();
                return;
            }

            if (s_ActiveOwner != this) return;

            if (m_FeedbackVolume != null)
            {
                m_FeedbackVolume.enabled = hasFeedback;
                m_FeedbackVolume.weight = Mathf.Clamp01(m_VolumeWeightCurve.Evaluate(m_FeedbackAmount));

                if (m_FeedbackVolume.profile != null
                    && m_FeedbackVolume.profile.TryGet(out LensDistortion lensDistortion))
                {
                    float intensity = Mathf.Clamp(m_LensDistortionIntensityCurve.Evaluate(m_FeedbackAmount), -1f, 1f);
                    if (m_Direction == LayerSwitchDirection.BackToFront)
                        intensity = -intensity;
                    lensDistortion.intensity.Override(intensity);
                }
            }

            if (!string.IsNullOrEmpty(m_AlphaGlobalPropertyName))
            {
                float alpha = Mathf.Clamp01(m_AlphaCurve.Evaluate(m_FeedbackAmount));
                Shader.SetGlobalFloat(Shader.PropertyToID(m_AlphaGlobalPropertyName), alpha);
            }
        }

        void ReleaseSharedOutputs()
        {
            if (m_FeedbackVolume != null)
            {
                m_FeedbackVolume.weight = 0f;
                m_FeedbackVolume.enabled = false;
            }

            if (!string.IsNullOrEmpty(m_AlphaGlobalPropertyName))
            {
                Shader.SetGlobalFloat(Shader.PropertyToID(m_AlphaGlobalPropertyName), 0f);
            }

            if (s_ActiveOwner == this)
                s_ActiveOwner = null;
        }

        void OnDisable()
        {
            if (s_ActiveOwner == this)
                ReleaseSharedOutputs();
        }

        void GrabPlayer(PlayerBehaviour player)
        {
            m_Playerbehaviour = player;
            m_Playerbehaviour.TransportingLayerStart();
            GetComponentInChildren<DragTarget>().AddTarget(m_Playerbehaviour.transform.gameObject);
            splineForAnimation.Play();
            
            GameManager.Instance.AudioManager.PlaySFX(ActivatedAudio);

            if (m_LayerSwitchVFX != null)
                m_LayerSwitchVFX.Play();
        }

        void ReturnPlayer()
        {
            if (m_LayerSwitchVFX != null)
                m_LayerSwitchVFX.Stop();

            GameManager.Instance.SwitchPlayLayer(ClosestGameplayLayer());
            m_Playerbehaviour.TransportingLayerEnd();
            m_Playerbehaviour = null;
            GetComponentInChildren<DragTarget>().RemoveTarget();
            ResetLayerSwitch();
        }

        void ResetLayerSwitch()
        {
            transform.position = m_InitPos;
            splineForAnimation.Restart(false);
        }

        LevelLayer ClosestGameplayLayer()
        {
            LevelLayer returnLayer = null;
            float smallestDifference = 99999f;
            List<LevelLayer> gameplayLayersCheck = new List<LevelLayer>
            {
                GameManager.Instance.BackgroundLayer,
                GameManager.Instance.NormalLayer,
                GameManager.Instance.FrontLayer
            };
            foreach (LevelLayer layer in gameplayLayersCheck)
            {
                float compareDifference = Mathf.Abs(this.transform.position.z - layer.transform.position.z);
                if (compareDifference < smallestDifference)
                {
                    smallestDifference = compareDifference;
                    returnLayer = layer;
                }

            }
            return returnLayer;

        }

        // Update is called once per frame
        void Update()
        {
            if (!Application.isPlaying)
            {
                // Editor preview: spline's play/pause/reset drives the feedback
                if (splineForAnimation != null)
                {
                    m_FeedbackAmount = Mathf.Clamp01(splineForAnimation.NormalizedTime);

                    bool isPlaying = splineForAnimation.IsPlaying;
                    if (m_LayerSwitchVFX != null)
                    {
                        if (isPlaying && !m_WasSplinePlaying)
                            m_LayerSwitchVFX.Play();
                        else if (!isPlaying && m_WasSplinePlaying)
                            m_LayerSwitchVFX.Stop();
                    }
                    m_WasSplinePlaying = isPlaying;
                }
                ApplyFeedbackToVolume();
                return;
            }

            //we grabbed the player
            if (m_Playerbehaviour != null)
            {
                if (splineForAnimation.IsPlaying)
                {
                    m_FeedbackAmount = Mathf.Clamp01(splineForAnimation.NormalizedTime);
                    GameManager.Instance.TravelingToLayer(ClosestGameplayLayer(), splineForAnimation.NormalizedTime);
                }
                else
                {
                    ReturnPlayer();
                }
            }
            else
            {
                m_FeedbackAmount = 0f;
            }

            ApplyFeedbackToVolume();
        }

        void OnTriggerEnter2D(Collider2D collision)
        {
            if (m_Playerbehaviour==null)
            {
                switch (collision.transform.tag)
                {
                    case "Player":
                        float compareDifferenceZ = Mathf.Abs(this.transform.position.z - collision.transform.position.z);
                        //only transport if the Z difference is less than x
                        if (collision.gameObject.TryGetComponent<PlayerBehaviour>(out PlayerBehaviour pb) && compareDifferenceZ < 10f)
                        {
                            GrabPlayer(pb);
                        }
                        break;
                    case "Untagged":
                        break;
                }
            }

        }
    }
}
