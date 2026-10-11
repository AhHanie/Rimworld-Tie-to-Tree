using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    public class FloatMenuOptionProvider_UntieFromTree : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;

        protected override bool RequiresManipulation => true;

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            Pawn actor = context.FirstSelectedPawn;
            if (actor == null || !TreeBindingRegistry.IsBound(clickedPawn))
            {
                return null;
            }

            string label = "TietoTree.UntieFromTree".Translate(clickedPawn.LabelCap, clickedPawn);
            if (!actor.CanReach(clickedPawn, PathEndMode.Touch, Danger.Deadly))
            {
                return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null);
            }

            Pawn target = clickedPawn;
            Action action = delegate
            {
                Job job = JobMaker.MakeJob(TietoTreeDefOf.TietoTree_TreeUntie, target);
                actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            };

            return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action, MenuOptionPriority.High, null, clickedPawn), actor, clickedPawn);
        }
    }
}
