using UnityEngine;
using UnityEngine.VFX;

namespace BunnyBlitz
{
    public class CollectibleVfxManager : MonoBehaviour
    {
        [SerializeField] private VfxInteractTable vfxScriptDesc;
    
        public void Init()
        {
            VFXPoolsWarmUp();
        }

        private void VFXPoolsWarmUp()
        {
            foreach (VfxInteractDescBase vfxDescBase in vfxScriptDesc.vfxInteractTables)
            {
                var go = new GameObject($"ref_{vfxDescBase.vfxAsset.name}", typeof(VisualEffect));
                var vfx = go.GetComponent<VisualEffect>();
                vfx.visualEffectAsset = vfxDescBase.vfxAsset;
                go.transform.SetParent(transform);
                vfxDescBase.vfxEffect = vfx;
                go.SetActive(false);
            
                GameManager.Instance.PoolingSystem.InitNewPool(go, vfxDescBase.poolInitCount);
            }
        }


        public void PlayVfx(VfxInteractDescBase.VfxTypeEnum vfxType, Vector3 spawnPos)
        {
            if(this == null)
                return;
        
            PlayVfx(spawnPos, vfxType);
        }

        private void PlayVfx(Vector3 spawnPos, VfxInteractDescBase.VfxTypeEnum vfxType)
        {
            var vfxDesc = vfxScriptDesc.vfxInteractTables.Find(x => x.VfxType == vfxType);
            if (vfxDesc != null)
            {
                //Slightly offset the VFX on the Z axis so it get culled by the shadow camera. As they are not depth sorted,
                //their actual depth is not relevant for their visual, and this is a quick way to cull them from shadow
                StartCoroutine(vfxDesc.SpawnPlayAndReleaseAsync(spawnPos + Vector3.forward * 10.0f));
            }
            else
            {
                Debug.LogError($"{vfxType} not found");
            }
        }
    }
}