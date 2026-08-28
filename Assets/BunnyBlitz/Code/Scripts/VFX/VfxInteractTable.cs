using System.Collections.Generic;
using UnityEngine;

namespace BunnyBlitz
{
    [CreateAssetMenu(fileName = "NewVfxInteractTable", menuName = "Visual Effects/Sample/VFX Interact Table")]
    public class VfxInteractTable : ScriptableObject
    {
        public List<VfxInteractDescBase> vfxInteractTables = new ();
    }
}