using HarmonyLib;
using RimWorld;
using Verse;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(GenGuest), nameof(GenGuest.PrisonerRelease))]
    public static class Patch_GenGuest_PrisonerRelease
    {
        public static void Prefix(Pawn p)
        {
            if (TreeBindingRegistry.Count == 0 || !TreeBindingRegistry.TryGet(p, out TreeBinding binding))
            {
                return;
            }

            MapComponent_TreeBindings.Untie(binding, UnbindReason.Released, true);
        }
    }
}
