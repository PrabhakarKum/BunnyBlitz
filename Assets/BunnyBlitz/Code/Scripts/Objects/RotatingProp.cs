using UnityEngine;

namespace BunnyBlitz.Editor
{
    public class RotatingProp : MonoBehaviour
    {
        public Rigidbody2D m_rigidbodyThis;
        public float rotatingSpeed;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            m_rigidbodyThis = GetComponent<Rigidbody2D>();
        }

        // Update is called once per frame
        void Update()
        {
            //m_rigidbodyThis.AddTorque(rotatingSpeed,ForceMode2D.Force);//this for dynamic objects
            m_rigidbodyThis.angularVelocity = rotatingSpeed;
        }
    }
}