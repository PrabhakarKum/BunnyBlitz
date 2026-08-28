using UnityEngine;

namespace BunnyBlitz
{
	public class DragTarget : MonoBehaviour
	{
		[Range(0f, 10f)] public float DragOffset = 1f;
	
		private TargetJoint2D m_TargetJoint;
		private Rigidbody2D m_Rigidbody2D;
	
		public void AddTarget(GameObject body)
		{
			// Turn-off rotation constraints.
			m_Rigidbody2D = body.GetComponent<Rigidbody2D>();
			m_Rigidbody2D.constraints &= ~RigidbodyConstraints2D.FreezeRotation;
		
			// Add a target joint.
			m_TargetJoint = body.AddComponent<TargetJoint2D>();
			m_TargetJoint.autoConfigureTarget = false;
			m_TargetJoint.dampingRatio = 0.0f;
			m_TargetJoint.frequency = 1000.0f;
			m_TargetJoint.maxForce = 1000000000;
			m_TargetJoint.anchor = m_Rigidbody2D.centerOfMass + Vector2.up * DragOffset;
			m_TargetJoint.target = m_Rigidbody2D.position;
		}
	
		public void RemoveTarget()
		{
			// Destroy the joint.
			Destroy (m_TargetJoint);
			m_TargetJoint = null;
			
			// Turn-on rotation constraints, reset rotation and linear velocity.
			m_Rigidbody2D.SetRotation(0.0f);
			m_Rigidbody2D.linearVelocity = Vector2.zero;
			m_Rigidbody2D.angularVelocity = 0.0f;
			
			m_Rigidbody2D.transform.rotation = Quaternion.identity;
			m_Rigidbody2D.constraints |= RigidbodyConstraints2D.FreezeRotation;
			m_Rigidbody2D = null;
		}

		private void Update()
		{
			// Update the joint target.
			if (m_TargetJoint)
			{
				var newPosition = transform.position;
				m_TargetJoint.target = newPosition;

				var targetPosition = m_Rigidbody2D.position;
				m_TargetJoint.transform.position = new Vector3(targetPosition.x, targetPosition.y, newPosition.z);
			}
		}
	}
}