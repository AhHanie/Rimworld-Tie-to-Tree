using HarmonyLib;
using Verse;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
    public static class Patch_Pawn_DeSpawn
    {
        public static void Prefix(Pawn __instance)
        {
            if (TreeBindingRegistry.Count == 0 || !TreeBindingRegistry.TryGet(__instance, out TreeBinding binding))
            {
                return;
            }

            MapComponent_TreeBindings.Untie(binding, UnbindReason.PawnLeft, true);
        }
    }
}
