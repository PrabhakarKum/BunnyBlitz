using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace BunnyBlitz
{
    public class AutoPlayerVFXBinding : MonoBehaviour
    {
        public bool NeedSphere => m_HaveSphere;
        public bool NeedVelocity => m_HaveVelocity;
    
        private VisualEffect m_VFX;

        private bool m_HaveSphere;
        private bool m_HaveVelocity;

        private const string SphereTransformName = "Sphere_transform_position";
        private const string SphereRadiusName = "Sphere_radius";
        private const string VelocityName = "Velocity";
        private int m_SpherePositionID = Shader.PropertyToID(SphereTransformName);
        private int m_SphereRadiusID = Shader.PropertyToID(SphereRadiusName);
        private int m_VelocityID= Shader.PropertyToID(VelocityName);
    
        private void Awake()
        {
            m_VFX = GetComponent<VisualEffect>();

            var propList = new List<VFXExposedProperty>();
            m_VFX.visualEffectAsset.GetExposedProperties(propList);

            foreach (var property in propList)
            {
                if (property.name == SphereTransformName)
                {
                    m_HaveSphere = true;
                }
                else if (property.name == VelocityName)
                {
                    m_HaveVelocity = true;
                }
            }
        }

        public void SetSphere(SphereCollider collider)
        {
            m_VFX.SetVector3(m_SpherePositionID, collider.transform.position);
            m_VFX.SetFloat(m_SphereRadiusID, collider.transform.localScale.x * collider.radius);
        }

        public void SetVelocity(Vector3 velocity)
        {
            m_VFX.SetVector3(m_VelocityID, velocity);
        }
    }
}