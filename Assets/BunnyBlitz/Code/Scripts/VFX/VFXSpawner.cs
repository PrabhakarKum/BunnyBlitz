using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

namespace BunnyBlitz
{
    public class VFXSpawner : MonoBehaviour
    {
        [SerializeField] private VisualEffectAsset vfxAsset;
        [SerializeField] private PlayerInput playerInput;
        private VisualEffect m_VisualEffect;
    
   
        private void OnEnable()
        {
            playerInput.OnAttackPerformed += OnAttackPerformed;
        }

        private void Start()
        {
            m_VisualEffect = new GameObject($"ref_{vfxAsset.name}").AddComponent<VisualEffect>();
            m_VisualEffect.visualEffectAsset = vfxAsset;
            m_VisualEffect.transform.SetParent(transform);
            m_VisualEffect.gameObject.SetActive(false);
        }
        private void OnAttackPerformed()
        {
            if (Camera.main != null)
            {
                Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                StartCoroutine(SpawnPlayAndRelease(worldPos));
            }
        }

        private IEnumerator SpawnPlayAndRelease(Vector3 spawnPos)
        {
            if (vfxAsset != null)
            {
                var spawnVfx = GameManager.Instance.PoolingSystem.Spawn(m_VisualEffect.gameObject, spawnPos, GameManager.Instance.CurrentLayer.LayerObjectRoot).GetComponent<VisualEffect>();
                spawnVfx.Play();
                yield return new WaitForSeconds(2f);
                GameManager.Instance.PoolingSystem.Return(spawnVfx.gameObject);
            }
        }

        private void OnDisable()
        {
            playerInput.OnAttackPerformed -= OnAttackPerformed;
        }
    }
}