using HarmonyLib;
using UnityEngine;
using Verse;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class Patch_PawnRenderer_RenderPawnAt
    {
        public static void Postfix(Pawn ___pawn, Vector3 drawLoc)
        {
            if (TreeBindingRegistry.Count == 0 || !TreeBindingRegistry.TryGet(___pawn, out TreeBinding binding))
            {
                return;
            }

            TreeRopeDrawer.Draw(___pawn, binding, drawLoc);
        }
    }
}
