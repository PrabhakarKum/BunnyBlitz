using UnityEngine;

namespace BunnyBlitz
{
    [System.Serializable]
    public class VfxGroundId 
    {
        public uint vfxIdentifier; // The string name for your VFX
        public PhysicsMaterial2D physicsMaterial; // The 2D physics material
    }
}