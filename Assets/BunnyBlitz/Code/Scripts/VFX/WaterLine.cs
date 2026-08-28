using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace BunnyBlitz
{
    [ExecuteInEditMode]
    public class WaterLine : MonoBehaviour
    {
        SpriteShapeRenderer m_Renderer;
        int m_ReflectionOffsetProperty = Shader.PropertyToID("_Reflection_Offset");

        MaterialPropertyBlock m_PropertyBlock;
        private Vector3 m_WorldTop;

        void OnEnable()
        {
            m_Renderer = GetComponent<SpriteShapeRenderer>();

            if(m_PropertyBlock == null)
            {
                m_PropertyBlock = new MaterialPropertyBlock();
            }

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            
            var bound = m_Renderer.GetBounds()[0];
            
            var topOfBoundLocal = bound.center + Vector3.up * bound.extents.y; 
            m_WorldTop = m_Renderer.transform.TransformPoint(topOfBoundLocal);
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if(m_Renderer == null)
                return;

            var viewportPoint = cam.WorldToViewportPoint(m_WorldTop);
            float y = (viewportPoint.y - 0.5f) * 2.0f;

            m_PropertyBlock.SetVector(m_ReflectionOffsetProperty, new Vector2(0, y));
            m_Renderer.SetPropertyBlock(m_PropertyBlock);
        }
    
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var playerBehaviour = other.GetComponent<PlayerBehaviour>();

                var playerPosition = other.transform.position;
                playerPosition.y = m_WorldTop.y;
                playerBehaviour?.EnteringWater(playerPosition);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var playerBehaviour = other.GetComponent<PlayerBehaviour>();
                var playerPosition = other.transform.position;
                playerPosition.y = m_WorldTop.y;
                playerBehaviour?.ExitWater(playerPosition);
            }
        }
    }
}