using HarmonyLib;
using RimWorld;
using Verse;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus))]
    public static class Patch_PawnGuestTracker_SetGuestStatus
    {
        public static void Postfix(Pawn ___pawn)
        {
            if (TreeBindingRegistry.Count == 0 || !TreeBindingRegistry.TryGet(___pawn, out TreeBinding binding))
            {
                return;
            }

            if (!___pawn.IsPrisonerOfColony)
            {
                MapComponent_TreeBindings.Untie(binding, UnbindReason.StatusChanged, true);
            }
        }
    }
}
