using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace BunnyBlitz
{
    public class LightFlicker : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Light2D light2d;
        [Header("Time")]
        [SerializeField] private float timeScale;
        [Header("Transform")]
        [SerializeField] private float positionJitterScale;
        [SerializeField] private float rotationJitterScale;
        [Header("Intensity")]
        [SerializeField] private float intensityJitterScale;
    
        private float m_XSeed;
        private float m_YSeed;
        private float m_ZSeed;
        private Vector3 m_Noise;
        private float m_initialLightIntensity;


        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            m_initialLightIntensity  = light2d.intensity; 
            Random.InitState((int)EntityId.ToULong(gameObject.GetEntityId()));
            m_XSeed = Random.value*248;
            m_YSeed = Random.value*248;
            m_ZSeed = Random.value*248;
        }
    
    
        private void Update()
        {
            var x = Time.time * timeScale + m_XSeed;
            var y = Time.time * timeScale + m_YSeed;
            var z = Time.time * timeScale + m_ZSeed;

            m_Noise = PerlinNoise3D(new Vector3(x, y, z), 2, 1) * 2 - Vector3.one;
            SetLightIntensity();
        }
    
    
        private Vector3 PerlinNoise3D(Vector3 uv, int octaves, float freq)
        {
            Vector3 output = Vector3.zero;
            for (int i = 0; i < octaves; i++)
            {
                output.x += Mathf.PerlinNoise1D(uv.x * freq * (i + 1));
                output.y += Mathf.PerlinNoise1D(uv.y * freq * (i + 1));
                output.z += Mathf.PerlinNoise1D(uv.z * freq * (i + 1));
            }
            return output;
        }
    
    
        private void SetLightIntensity()
        {
            light2d.intensity = m_initialLightIntensity + m_Noise.x * intensityJitterScale ;
        }
    
    
    }
}