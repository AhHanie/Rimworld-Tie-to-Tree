using RimWorld;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    public class JobGiver_TiedToTree : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!TreeBindingRegistry.TryGet(pawn, out TreeBinding binding))
            {
                return null;
            }

            if (!pawn.Spawned || pawn.Position != binding.cell || binding.anchor == null || !binding.anchor.Spawned)
            {
                return null;
            }

            Job current = pawn.CurJob;
            if (current != null && current.def == JobDefOf.LayDown && current.targetA.Thing == binding.anchor)
            {
                return current;
            }

            if (!TreeBindingRegistry.CanUseAnchor(binding.anchor, pawn))
            {
                return null;
            }

            return JobMaker.MakeJob(JobDefOf.LayDown, binding.anchor);
        }
    }
}
