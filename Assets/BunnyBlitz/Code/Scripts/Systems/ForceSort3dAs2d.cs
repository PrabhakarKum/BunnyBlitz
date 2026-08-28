using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace BunnyBlitz
{
    [RequireComponent(typeof(SortingGroup))]
    public class ForceSort3dAs2d : MonoBehaviour
    {
        SortingGroup m_SortingGroup;
        Type m_RenderAs2DType;
        MethodInfo m_R2DInitFunction;
        PropertyInfo m_R2DMaterialProperty;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            m_SortingGroup = GetComponent<SortingGroup>();

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly =>
            {
                return assembly.GetName().Name == "UnityEngine.RenderAs2DModule";
            });

            foreach (var a in loadedAssemblies)
            {
                var ts = a.GetTypes().Where(t => t.Name == "RenderAs2D");
                if (ts.Count() > 0)
                {
                    m_RenderAs2DType = ts.First();

                    m_R2DInitFunction = m_RenderAs2DType.GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);
                    m_R2DMaterialProperty = m_RenderAs2DType.GetProperty("material");
                }
            }

            RecursiveCheck(transform);
        }

        void RecursiveCheck(Transform root)
        {
#if UNITY_EDITOR
            Renderer r = root.GetComponent<Renderer>();

            if (r != null)
            {
                var r2d = root.gameObject.GetComponent(m_RenderAs2DType);

                if (!r2d)
                {
                    Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/RenderAs2D-Flattening.mat");
                    var newRenderAs2D = root.gameObject.AddComponent(m_RenderAs2DType);
                    m_R2DInitFunction.Invoke(newRenderAs2D, new[] { m_SortingGroup });
                    m_R2DMaterialProperty.SetValue(newRenderAs2D, mat);
                    newRenderAs2D.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                }
            }
#endif
            foreach (Transform t in root)
            {
                RecursiveCheck(t);
            }
        }
    }
}