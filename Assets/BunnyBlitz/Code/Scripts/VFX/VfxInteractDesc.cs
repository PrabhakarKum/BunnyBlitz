using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

namespace BunnyBlitz
{
    [Serializable]
    public class VfxInteractDescBase
    {
        public enum VfxTypeEnum
        {
            CoinPickup,
            BlockInteract,
            BlockDestroy,
            SpringInteract,
            OneOfFiveInteract,
            Bomb,
            Pomegranate,
            Disappear,
            WaterSplash
        }
    
        public VfxTypeEnum VfxType;
        [NonSerialized] public VisualEffect vfxEffect;
        public VisualEffectAsset vfxAsset;
        public int poolInitCount = 10;
        [SerializeField] private float effectDuration = 2f;
    
        public IEnumerator SpawnPlayAndReleaseAsync(Vector3 spawnPos)
        {
            if (vfxAsset != null)
            {
                var spawnVfx = GameManager.Instance.PoolingSystem.Spawn(vfxEffect.gameObject, spawnPos, GameManager.Instance.CurrentLayer.LayerObjectRoot);
                spawnVfx.GetComponent<VisualEffect>().Play();
                yield return new WaitForSeconds(effectDuration);
                GameManager.Instance.PoolingSystem.Return(spawnVfx.gameObject);
            }
            else
            {
                Debug.LogError("Visual Effect is missing");
            }
        }
    }
}