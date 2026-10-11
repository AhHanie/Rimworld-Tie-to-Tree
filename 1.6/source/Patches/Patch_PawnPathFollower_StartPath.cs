using HarmonyLib;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath))]
    public static class Patch_PawnPathFollower_StartPath
    {
        public static bool Prefix(Pawn_PathFollower __instance, Pawn ___pawn, LocalTargetInfo dest)
        {
            if (TreeBindingRegistry.Count == 0 || !TreeBindingRegistry.TryGet(___pawn, out TreeBinding binding))
            {
                return true;
            }

            if (dest.Cell == binding.cell || dest.Thing == binding.anchor)
            {
                return true;
            }

            __instance.StopDead();
            if (___pawn.jobs == null || ___pawn.jobs.curJob == null)
            {
                return false;
            }

            if (___pawn.InMentalState && !___pawn.Downed)
            {
                Logger.Message(___pawn.LabelShort + " tried to leave the tree during a mental state; ending the state.");
                ___pawn.MentalState.RecoverFromState();
                if (___pawn.jobs.curJob != null)
                {
                    ___pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }

                return false;
            }

            ___pawn.jobs.curDriver?.Notify_PatherFailed();
            return false;
        }
    }
}
