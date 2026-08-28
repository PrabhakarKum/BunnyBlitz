using UnityEngine;
#if ENABLE_INPUT_SYSTEM
#endif

namespace BunnyBlitz
{
    /// <summary>
    /// Will disable the on screen control if not on ios/android
    /// </summary>
    public class MobileDisableControlOnScreen : MonoBehaviour
    {

#if !(UNITY_IOS || UNITY_ANDROID)
        void Awake()
        {
            gameObject.SetActive(false);
        }
#endif

    }
}
