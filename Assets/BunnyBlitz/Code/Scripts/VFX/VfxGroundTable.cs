using System.Collections.Generic;
using UnityEngine;

namespace BunnyBlitz
{
    [CreateAssetMenu(fileName = "NewVfxGroundTable", menuName = "Visual Effects/Sample/VFX Ground Table")]
    public class VfxGroundTable : ScriptableObject
    {
        public List<VfxGroundId> vfxGroundTable = new List<VfxGroundId>();
   
        public uint GetGroundVfxId(PhysicsMaterial2D physMaterial)
        {
            return vfxGroundTable.Find(x => x.physicsMaterial == physMaterial)?.vfxIdentifier ?? 0;
        }
    }
}