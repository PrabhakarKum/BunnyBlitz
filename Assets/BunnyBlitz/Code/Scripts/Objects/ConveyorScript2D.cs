using System.Collections.Generic;
using UnityEngine;

namespace BunnyBlitz
{
    [RequireComponent(typeof(Collider2D))]
    public class ConveyorScript2D : MonoBehaviour
    {
        /// <summary>
        /// Implement this and the conveyor will calculate and set the force it wants to apply.
        /// </summary>
        public interface IConveyorTarget
        {
            public void AddImpulse(Vector2 impulse);
        }
    
        [Range(-100f, 100f)] public float Speed;
        [Range(0f, 100f)] public float ForceScale = 1f;

        private Collider2D m_Collider;
        private readonly List<ContactPoint2D> m_Contacts = new();
        private readonly List<Rigidbody2D> m_ContactBodies = new();

        private void Start()
        {
            m_Collider = GetComponent<Collider2D>();
            if (!m_Collider)
                Debug.LogWarning($"{typeof(ConveyorScript2D)} needs a Collider2D to operator.", this);
        }

        private void Update()
        {
            if (!m_Collider || Physics2D.simulationMode != SimulationMode2D.Update)
                return;
        
            UpdateSurfaceContacts(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!m_Collider || Physics2D.simulationMode != SimulationMode2D.Update)
                return;
        
            UpdateSurfaceContacts(Time.deltaTime);
        }

        private void UpdateSurfaceContacts(float deltaTime)
        {
            var contactCount = m_Collider.GetContacts(m_Contacts);
            if (contactCount == 0 || ForceScale < float.Epsilon)
                return;

            // Find any Conveyor Targets.
            m_ContactBodies.Clear();
            foreach (var contact in m_Contacts)
            {
                // See if the body is already in the list.
                Rigidbody2D body;
                Vector2 normal;
                if (contact.collider == m_Collider)
                {
                    body = contact.otherRigidbody;
                    normal = contact.normal;
                }
                else
                {
                    body = contact.rigidbody;
                    normal = -contact.normal;
                }

                // Skip if we already handled this body.
                if (m_ContactBodies.Contains(body))
                    continue;

                // Flag as handled.
                m_ContactBodies.Add(body);
            
                // Skip if no conveyor target found.
                var conveyorTarget = body.GetComponent<IConveyorTarget>();
                if (conveyorTarget == null)
                    continue;

                // Calculate the tangent.
                var tangent = -Vector2.Perpendicular(normal);
            
                // Calculate the required impulse.
                var bodyVelocity = body.linearVelocity;
                var tangentImpulseSpeed = Speed - Vector2.Dot(tangent, bodyVelocity);
                var impulseScale = tangentImpulseSpeed * ForceScale;// * body.mass;
                var impulse = tangent * impulseScale;

                // Add the impulse to the target.
                conveyorTarget.AddImpulse(impulse);
            }
        
            // Clear the contacts.
            m_Contacts.Clear();
        }
    }
}