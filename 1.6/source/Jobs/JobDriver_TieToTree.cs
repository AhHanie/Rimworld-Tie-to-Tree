using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Tie_to_Tree
{
    public class JobDriver_TieToTree : JobDriver
    {
        private const TargetIndex TakeeIndex = TargetIndex.A;

        private const TargetIndex TreeIndex = TargetIndex.B;

        private const TargetIndex CellIndex = TargetIndex.C;

        protected Pawn Takee => (Pawn)job.GetTarget(TakeeIndex).Thing;

        protected Plant Tree => job.GetTarget(TreeIndex).Thing as Plant;

        protected IntVec3 Cell => job.GetTarget(CellIndex).Cell;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Takee.ClearAllReservations();
            return pawn.Reserve(Takee, job, 1, -1, null, errorOnFailed)
                && pawn.Reserve(Tree, job, 1, -1, null, errorOnFailed)
                && pawn.Reserve(Cell, job, 1, -1, null, errorOnFailed);
        }

        private bool TakeeCanStillBeTied()
        {
            Pawn takee = Takee;
            if (takee == null || takee.Dead || !takee.RaceProps.Humanlike)
            {
                return false;
            }

            if (takee.IsPrisonerOfColony)
            {
                return true;
            }

            if (takee.Downed || takee.DevelopmentalStage.Baby())
            {
                return !takee.HostileTo(Faction.OfPlayer) || takee.Downed;
            }

            return takee.CanBeArrestedBy(pawn);
        }

        private bool PlaceIsStillValid()
        {
            Plant tree = Tree;
            if (tree == null)
            {
                return false;
            }

            if (!TreeBindingUtility.IsTreeUsable(tree, pawn, Takee))
            {
                return false;
            }

            return TreeBindingUtility.IsCellUsable(Cell, tree.Map, pawn, Takee);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TakeeIndex);
            this.FailOnDestroyedOrNull(TreeIndex);
            this.FailOnAggroMentalStateAndHostile(TakeeIndex);
            this.FailOn(() => !PlaceIsStillValid());

            AddFinishAction(delegate
            {
                if (pawn.carryTracker.CarriedThing == Takee)
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing _);
                }
            });

            Toil goToTakee = Toils_Goto.GotoThing(TakeeIndex, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TakeeIndex)
                .FailOn(() => !TakeeCanStillBeTied())
                .FailOnSomeonePhysicallyInteracting(TakeeIndex);

            Toil checkAcceptance = ToilMaker.MakeToil("CheckAcceptance");
            checkAcceptance.initAction = delegate
            {
                Pawn takee = Takee;
                if (!TreeBindingUtility.NeedsAcceptanceRoll(takee))
                {
                    return;
                }

                takee.GetLord()?.Notify_PawnAttemptArrested(takee);
                GenClamor.DoClamor(takee, 10f, ClamorDefOf.Harm);
                if (!takee.IsPrisoner && !takee.IsSlave)
                {
                    QuestUtility.SendQuestTargetSignals(takee.questTags, "Arrested", takee.Named("SUBJECT"));
                    if (takee.Faction != null)
                    {
                        QuestUtility.SendQuestTargetSignals(takee.Faction.questTags, "FactionMemberArrested", takee.Faction.Named("FACTION"));
                    }
                }

                if (!takee.CheckAcceptArrest(pawn))
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            };

            Toil startCarrying = Toils_Haul.StartCarryThing(TakeeIndex);
            Toil goToCell = Toils_Goto.GotoCell(CellIndex, PathEndMode.Touch)
                .FailOn(() => !pawn.IsCarryingPawn(Takee));

            yield return Toils_Jump.JumpIf(goToCell, () => pawn.IsCarryingPawn(Takee));
            yield return goToTakee;
            yield return checkAcceptance;
            yield return startCarrying;
            yield return goToCell;
            yield return MakeCaptureToil();
            yield return MakeTieToil();
        }

        private Toil MakeCaptureToil()
        {
            Toil toil = ToilMaker.MakeToil("CapturePrisoner");
            toil.initAction = delegate
            {
                Pawn takee = Takee;
                if (takee.guest == null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                if (takee.guest.Released)
                {
                    takee.guest.Released = false;
                    takee.guest.SetExclusiveInteraction(PrisonerInteractionModeDefOf.MaintainOnly);
                    GenGuest.RemoveHealthyPrisonerReleasedThoughts(takee);
                }

                if (!takee.IsPrisonerOfColony)
                {
                    takee.guest.CapturedBy(Faction.OfPlayer, pawn);
                }

                if (takee.playerSettings == null)
                {
                    takee.playerSettings = new Pawn_PlayerSettings(takee);
                }
            };
            return toil;
        }

        private Toil MakeTieToil()
        {
            Toil toil = ToilMaker.MakeToil("TieToTree");
            toil.initAction = delegate
            {
                Pawn takee = Takee;
                Plant tree = Tree;
                IntVec3 cell = Cell;
                Map map = pawn.Map;
                MapComponent_TreeBindings bindings = MapComponent_TreeBindings.For(map);
                if (bindings == null || tree == null || !takee.IsPrisonerOfColony)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                if (!pawn.carryTracker.TryDropCarriedThing(cell, ThingPlaceMode.Direct, out Thing _))
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                TreeBinding binding = bindings.Bind(takee, tree, cell);
                if (binding == null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                RestUtility.TuckIntoBed(binding.anchor, takee, takee, rescued: false);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
