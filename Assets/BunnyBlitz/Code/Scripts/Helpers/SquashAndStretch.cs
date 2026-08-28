using UnityEngine;

namespace BunnyBlitz
{
    public class SquashAndStretch : MonoBehaviour
    {
        [Header("Effect Settings")]
        [Tooltip("How much the object can be squashed/stretched")]
        public float deformAmount = 0.3f;
    
        [Tooltip("How fast the object returns to its original shape")]
        public float springForce = 10f;
    
        [Tooltip("How quickly the squash/stretch effect settles")]
        public float damping = 5f;
    
        [Tooltip("Minimum velocity needed to trigger the effect")]
        public float velocityThreshold = 0.1f;
    
        [Tooltip("How much the movement affects the deformation")]
        public float responsiveness = 1f;
    
        [Header("Axis Settings")]
        [Tooltip("Axis along which stretching occurs (usually the direction of movement)")]
        public Vector3 stretchAxis = Vector3.up;
    
        [Tooltip("Maintain volume during deformation")]
        public bool preserveVolume = true;

        private Vector3 m_OriginalScale;
        private Vector3 m_CurrentVelocity;
        private Vector3 m_TargetScale;
        private Vector3 m_LastPosition;
        private bool m_IsDeforming = false;

        void Start()
        {
            m_OriginalScale = transform.localScale;
            m_TargetScale = m_OriginalScale;
            m_LastPosition = transform.position;
        }

        void Update()
        {
            // Calculate movement velocity
            Vector3 velocity = (transform.position - m_LastPosition) / Time.deltaTime;
            float speedSqr = velocity.sqrMagnitude;
            m_LastPosition = transform.position;

            // Check if we should trigger squash/stretch
            if (speedSqr > velocityThreshold * velocityThreshold)
            {
                // Calculate stretch direction based on movement
                Vector3 direction = velocity.normalized;
                float stretchFactor = Mathf.Clamp01(speedSqr * responsiveness);
            
                // Calculate stretch and squash scales
                float stretch = 1f + (deformAmount * stretchFactor);
                float squash = preserveVolume ? 1f / Mathf.Sqrt(stretch) : 1f - (deformAmount * stretchFactor * 0.5f);

                // Create target scale
                Vector3 newScale = m_OriginalScale;
            
                // Apply stretch along movement direction
                Vector3 scaleMod = Vector3.one;
                scaleMod += direction * (stretch - 1f);
            
                // Apply squash perpendicular to movement
                Vector3 perpendicular1 = Vector3.Cross(direction, Vector3.up).normalized;
                if (perpendicular1 == Vector3.zero)
                    perpendicular1 = Vector3.right;
                Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular1).normalized;
            
                scaleMod += (perpendicular1 + perpendicular2) * (squash - 1f);
            
                m_TargetScale = Vector3.Scale(m_OriginalScale, scaleMod);
                m_IsDeforming = true;
            }
            else if (!m_IsDeforming)
            {
                m_TargetScale = m_OriginalScale;
            }

            // Apply spring physics to scale
            if (m_IsDeforming)
            {
                Vector3 scaleVelocity = Vector3.zero;
                transform.localScale = Vector3.SmoothDamp(
                    transform.localScale, 
                    m_TargetScale, 
                    ref scaleVelocity, 
                    1f / springForce, 
                    Mathf.Infinity, 
                    Time.deltaTime
                );

                // Check if we've settled back to original scale
                if (Vector3.Distance(transform.localScale, m_TargetScale) < 0.001f)
                {
                    if (m_TargetScale == m_OriginalScale)
                    {
                        m_IsDeforming = false;
                        transform.localScale = m_OriginalScale;
                    }
                }
            }
        }

        public void AddImpact(Vector3 direction, float force)
        {
            direction = direction.normalized;
            float stretch = 1f + (deformAmount * force);
            float squash = preserveVolume ? 1f / Mathf.Sqrt(stretch) : 1f - (deformAmount * force * 0.5f);

            Vector3 scaleMod = Vector3.one;
            scaleMod += direction * (stretch - 1f);

            Vector3 perpendicular1 = Vector3.Cross(direction, Vector3.up).normalized;
            if (perpendicular1 == Vector3.zero)
                perpendicular1 = Vector3.right;
            Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular1).normalized;
        
            scaleMod += (perpendicular1 + perpendicular2) * (squash - 1f);
        
            m_TargetScale = Vector3.Scale(m_OriginalScale, scaleMod);
            m_IsDeforming = true;
        }
    }
}