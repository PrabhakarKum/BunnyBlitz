using UnityEngine;

namespace BunnyBlitz
{
    [RequireComponent(typeof(SquashAndStretch))]
    public class ImpactDeformer : MonoBehaviour
    {
        [Tooltip("How much collision force is needed to trigger the effect")]
        public float impactThreshold = 1f;
    
        [Tooltip("Maximum deformation from impacts")]
        public float maxImpactForce = 2f;

        private SquashAndStretch m_SquashAndStretch;

        void Start()
        {
            m_SquashAndStretch = GetComponent<SquashAndStretch>();
        }

        void OnCollisionEnter(Collision collision)
        {
            float impactForce = collision.relativeVelocity.magnitude;
        
            if (impactForce > impactThreshold)
            {
                // Calculate impact direction and force
                Vector3 direction = collision.contacts[0].normal;
                float force = Mathf.Clamp01(impactForce / maxImpactForce);
            
                // Apply the deformation
                m_SquashAndStretch.AddImpact(direction, force);
            }
        }
    }
}