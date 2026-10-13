using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    public class JobDriver_TreeUntie : JobDriver
    {
        private const TargetIndex PrisonerIndex = TargetIndex.A;

        private const int UntieTicks = 90;

        protected Pawn Prisoner => (Pawn)job.GetTarget(PrisonerIndex).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Prisoner, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(PrisonerIndex);
            this.FailOnDespawnedOrNull(PrisonerIndex);
            this.FailOn(() => !TreeBindingRegistry.IsBound(Prisoner));
            yield return Toils_Goto.GotoThing(PrisonerIndex, PathEndMode.Touch);
            Toil untie = Toils_General.WaitWith(PrisonerIndex, UntieTicks, useProgressBar: true, maintainPosture: false, maintainSleep: false);
            untie.FailOnCannotTouch(PrisonerIndex, PathEndMode.Touch);
            yield return untie;
            yield return Toils_General.Do(delegate
            {
                MapComponent_TreeBindings.Untie(Prisoner, UnbindReason.Manual, true);
            });
        }
    }
}
