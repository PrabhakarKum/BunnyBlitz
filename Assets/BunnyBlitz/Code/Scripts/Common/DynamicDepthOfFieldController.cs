using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
// Required for Volume and DepthOfField

namespace BunnyBlitz
{
    public class DynamicDepthOfFieldController : MonoBehaviour
    {
        [Header("Setup")]
        public Volume postProcessVolume; // Assign your scene's Post Processing Volume here
        public Transform targetToFocusOn; // The object you want to keep in focus

        [Header("Focus Settings")]
        public float focusSpeed = 8f; // How quickly the focus adapts
        public float minFocusDistance = 1f; // Minimum distance for focus
        public float maxFocusDistance = 50f; // Maximum distance for focus

        private DepthOfField depthOfField;
        private Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;

            if (postProcessVolume == null || postProcessVolume.profile == null)
            {
                Debug.LogError("Post Processing Volume or its Profile is not assigned.");
                enabled = false; // Disable the script if setup is incomplete
                return;
            }

            // Attempt to get the DepthOfField override from the Volume Profile
            if (!postProcessVolume.profile.TryGet(out depthOfField))
            {
                Debug.LogError("Depth of Field override not found on the assigned Volume Profile. Please add it.");
                enabled = false; // Disable the script
                return;
            }

            // Ensure the Depth of Field effect is active and in Bokeh mode for this script's logic
            if (!depthOfField.active || depthOfField.mode.value != DepthOfFieldMode.Bokeh)
            {
                Debug.LogWarning("Depth of Field is not active or not in Bokeh mode. Script might not behave as expected. Consider setting Mode to Bokeh in the Volume Profile.");
                // You could optionally force it:
                depthOfField.active = true;
                depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
            }
        }

        void Update()
        {
            if (targetToFocusOn == null || depthOfField == null || mainCamera == null)
                return;

            // Calculate the distance from the camera to the target
            float distanceToTarget = Vector3.Distance(mainCamera.transform.position, targetToFocusOn.position);
            distanceToTarget = Mathf.Clamp(distanceToTarget, minFocusDistance, maxFocusDistance);

            // Smoothly interpolate the focus distance towards the target distance
            // depthOfField.focusDistance is a ClampedFloatParameter, so we access/override its .value
            float newFocusDistance = Mathf.Lerp(depthOfField.focusDistance.value, distanceToTarget, Time.deltaTime * focusSpeed);
        
            depthOfField.focusDistance.Override(newFocusDistance);
        }
    }
}
// Required for URP specific DepthOfField