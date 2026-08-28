using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace BunnyBlitz
{
    [AddComponentMenu("VFX/Property Binders/Spline Position Binder")]
    [VFXBinder("Point Cache/Spline Position Binder")]
    public class VFXSplinePositionBinder : VFXBinderBase
    {
        [VFXPropertyBinding("UnityEngine.Texture2D")]
        public ExposedProperty PositionMapProperty = "PositionMap";
        [VFXPropertyBinding("System.Int32")]
        public ExposedProperty PositionCountProperty = "PositionCount";

        public SplineContainer Spline;
        [Min(2)] public int SampleCount = 64;
        public bool EveryFrame = false;

        private Texture2D m_PositionMap;
        private int m_BakedSampleCount;
        private Color[] m_Pixels;

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateTexture();
        }

        public override bool IsValid(VisualEffect component)
        {
            return Spline != null
                && component.HasTexture(PositionMapProperty)
                && component.HasInt(PositionCountProperty);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            if (EveryFrame || Application.isEditor)
                UpdateTexture();

            component.SetTexture(PositionMapProperty, m_PositionMap);
            component.SetInt(PositionCountProperty, m_BakedSampleCount);
        }

        void UpdateTexture()
        {
            if (Spline == null || Spline.Spline == null || SampleCount < 2)
                return;

            if (m_PositionMap == null || m_PositionMap.width != SampleCount)
            {
                m_PositionMap = new Texture2D(SampleCount, 1, TextureFormat.RGBAFloat, false)
                {
                    name = gameObject.name + "_SplinePositionMap",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                m_Pixels = new Color[SampleCount];
            }

            float step = 1f / (SampleCount - 1);
            for (int i = 0; i < SampleCount; i++)
            {
                float t = i * step;
                Vector3 pos = Spline.EvaluatePosition(t);
                m_Pixels[i] = new Color(pos.x, pos.y, pos.z);
            }

            m_PositionMap.SetPixels(m_Pixels, 0);
            m_PositionMap.Apply();
            m_BakedSampleCount = SampleCount;
        }

        public override string ToString()
        {
            return string.Format("Spline Position Binder ({0} samples)", m_BakedSampleCount);
        }
    }
}
