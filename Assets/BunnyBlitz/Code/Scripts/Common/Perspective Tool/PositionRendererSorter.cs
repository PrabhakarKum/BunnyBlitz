using System;
using UnityEngine;

namespace BunnyBlitz
{
    public class PositionRendererSorter : MonoBehaviour
    {
        //[MMInspectorButton("ConvertCameraProjectionType")]
        public bool changeFrustum;

        [SerializeField] private double FieldOfView = 17.41374f;
    

        private float timer;
        private float timerMax = .1f;
        private Renderer[] myRenderer;

        private Camera cam;

        private void Awake() {
            cam = Camera.main;
        
        }

        private void LateUpdate() {
            timer -= Time.deltaTime;
            if (timer <= 0f) {
                timer = timerMax;
                //myRenderer.sortingOrder = (int)(sortingOrderBase - transform.position.y - offset);
                //CalculateFrustum();
            }
        }

        public void CalculateFrustum()
        {
            float distance = cam.transform.position.z;
            //double frustumHeight = Mathf.Abs(distance) * Math.Tan(cam.fieldOfView * Math.PI/180.0);
            double frustumHeight = 2f * Mathf.Abs(distance) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cam.orthographic = true;
        
            cam.orthographicSize = (float)frustumHeight / 2.0f; 
            //var frustumWidth = frustumHeight * cam.aspect;
        }

        public float CalculatePerspectiveDistance()
        {
            double vert = FieldOfView / cam.aspect;
            double dist = cam.orthographicSize / Math.Tan( FieldOfView * 0.5 * Math.PI/180.0);
            return (float)dist * -1.0f;
        }

        [ContextMenu("Change Camera Projection Type")]
        [ExecuteInEditMode]
        public void ConvertCameraProjectionType()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }
        
            if (cam.orthographic)
            {
                var camPosition = cam.transform.position;
                camPosition.z = CalculatePerspectiveDistance();
                cam.transform.position = camPosition;
                cam.orthographic = false;
            }
            else
            {
                //FieldOfView = cam.fieldOfView;
                CalculateFrustum();
            }
        }



    }
}
//using MoreMountains.Tools;