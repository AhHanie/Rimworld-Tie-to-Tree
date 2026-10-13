using HarmonyLib;
using Verse;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing))]
    public static class Patch_PawnRenderer_LayingFacing
    {
        public static void Postfix(Pawn ___pawn, ref Rot4 __result)
        {
            if (TreeBindingRegistry.Count != 0 && TreeBindingRegistry.IsBound(___pawn))
            {
                __result = Rot4.South;
            }
        }
    }
}
