using UnityEngine;
using UnityEngine.Rendering;

namespace BunnyBlitz
{
    public class ShadowCameraFarClipSetter : MonoBehaviour
    {
        private Camera m_ShadowCamera;
        private RenderTexture m_ShadowTexture;

        private bool m_TextureNeedRebuild = false;

        private void OnEnable()
        {
            m_ShadowCamera = GetComponent<Camera>();
            CreateTexture();
            m_ShadowCamera.enabled = true;
            RenderPipelineManager.beginCameraRendering += UpdateCam;
        }
        
        private void OnDisable()
        {
            m_ShadowCamera.enabled = false;
            m_ShadowCamera.targetTexture = null;
            RenderPipelineManager.beginCameraRendering -= UpdateCam;
            m_ShadowTexture.Release();
        }

        void Update()
        {
            if(m_TextureNeedRebuild)
                CreateTexture();
        }

        private void UpdateCam(ScriptableRenderContext context, Camera cam)
        {
            if (cam != m_ShadowCamera || GameManager.Instance == null || GameManager.Instance.Player == null)
                return;

            var rt = cam.targetTexture;
            if(rt.width != Screen.width || rt.height != Screen.height)
                m_TextureNeedRebuild = true;   
            
            float zDiff = GameManager.Instance.Player.transform.position.z - transform.position.z;
            m_ShadowCamera.farClipPlane = zDiff;
        }

        void CreateTexture()
        {
            m_ShadowCamera.targetTexture = null;
            if(m_ShadowTexture != null)
                m_ShadowTexture.Release();
            
            m_ShadowTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
            m_ShadowCamera.targetTexture = m_ShadowTexture;
            
            Shader.SetGlobalTexture("_BackShadowTex", m_ShadowTexture);

            m_TextureNeedRebuild = false;
        }
    }
}
