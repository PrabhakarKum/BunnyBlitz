using UnityEngine;
using UnityEngine.U2D;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Demo Script Usage:
//   When you want multiple SpriteShapes to share a common Spline,
//   attach this script to the secondary objects you would like to 
//   copy the Spline and set the ParentObject to the original object
//   you are copying from.
namespace SpriteShapeExtras
{

    [ExecuteInEditMode]
    public class ConformingSpline : MonoBehaviour
    {
    
        public GameObject m_ParentObject;
        
        [SerializeField]
        [HideInInspector]
        private int m_HashCode;
    
        // Use this for initialization
        void Start()
        {
    
        }
    
        // Update is called once per frame
        void Update()
        {
            if (m_ParentObject != null)
            {
                CopySpline(m_ParentObject, gameObject, this);
            }
        }

        private static void CopySpline(GameObject src, GameObject dst, ConformingSpline conformingSpline)
        {
    #if UNITY_EDITOR
            var parentSpriteShapeController = src.GetComponent<SpriteShapeController>();
            var mirrorSpriteShapeController = dst.GetComponent<SpriteShapeController>();

            if (parentSpriteShapeController != null && mirrorSpriteShapeController != null && parentSpriteShapeController.spline.GetHashCode() != conformingSpline.m_HashCode)
            {
                SerializedObject srcController = new SerializedObject(parentSpriteShapeController);
                SerializedObject dstController = new SerializedObject(mirrorSpriteShapeController);
                SerializedProperty srcSpline = srcController.FindProperty("m_Spline");
                dstController.CopyFromSerializedProperty(srcSpline);
                dstController.ApplyModifiedProperties();
                EditorUtility.SetDirty(mirrorSpriteShapeController);
                var newHash = parentSpriteShapeController.spline.GetHashCode();

                SerializedObject csso = new SerializedObject(conformingSpline);
                var p = csso.FindProperty(nameof(m_HashCode));
                p.intValue = newHash;
                csso.ApplyModifiedProperties();
            }
    #endif
        }
    
    }

}