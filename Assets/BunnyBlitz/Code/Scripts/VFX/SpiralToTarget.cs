// 11/11/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

namespace BunnyBlitz
{
    public class SpiralToTarget : MonoBehaviour
    {
        public Vector3 targetPosition; // The target position to move to
        public AnimationCurve scaleOverTime;
        public float duration = 5f;    // Duration of the spiral movement in seconds
        public float rotations = 3f;   // Number of spiral rotations
        public float rotationSpeed = 15f;   // Number of spiral rotations

        private Vector3 m_StartPosition;
        private float m_ElapsedTime = 0f;

        void Awake()
        {
            enabled = false;
        }
        public void StartSpiralEffect()
        {
            // Store the starting position of the object
            m_StartPosition = transform.position;
            enabled = true;
        }

        void Update()
        {
            // Increment the elapsed time
            m_ElapsedTime += Time.deltaTime;

            // Calculate the progress (0 to 1) based on the duration
            float progress = Mathf.Clamp01(m_ElapsedTime / duration);

            // Calculate the scale progress based on the duration of the anim curve
            Keyframe lastFrame = scaleOverTime[ scaleOverTime.length - 1 ];
            transform.localScale = Vector3.one * scaleOverTime.Evaluate(progress*lastFrame.time);

            // Calculate the current radius based on the progress
            float currentRadius = Mathf.Lerp(0, Vector3.Distance(m_StartPosition, targetPosition), progress);

            // Calculate the angle for the spiral movement
            float angle = Mathf.Lerp(0, rotations * 2 * Mathf.PI, progress);

            // Calculate the direction towards the target
            Vector3 direction = (targetPosition - m_StartPosition).normalized;

            // Calculate the perpendicular vector for the spiral
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;

            // Calculate the spiral position
            Vector3 spiralOffset = Mathf.Cos(angle) * perpendicular * currentRadius + Mathf.Sin(angle) * Vector3.Cross(perpendicular, direction) * currentRadius;

            // Rotate towards the target
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Update the position of the object
            transform.position = m_StartPosition + direction * currentRadius + spiralOffset;

            // Stop the movement after the duration
            if (m_ElapsedTime >= duration)
            {
                transform.position = targetPosition; // Ensure the object ends exactly at the target position
                enabled = false; // Disable the script
            }
        }
    }
}