using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    public class JobDriver_FeedTiedInfant : JobDriver
    {
        private const TargetIndex BabyIndex = TargetIndex.A;

        private const TargetIndex BabyFoodIndex = TargetIndex.B;

        private float initialFoodPercentage;

        private float bottleNutrition;

        private float totalBottleNutrition;

        private float initialNutritionNeeded;

        protected Pawn Baby => (Pawn)job.GetTarget(BabyIndex).Thing;

        protected Thing BabyFood => job.GetTarget(BabyFoodIndex).Thing;

        protected bool Bottle => job.GetTarget(BabyFoodIndex).IsValid;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(Baby, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            if (!Bottle)
            {
                return pawn.Reserve(pawn, job, 1, -1, null, errorOnFailed);
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(BabyIndex);
            this.FailOnDespawnedOrNull(BabyIndex);
            AddFailCondition(() => !TreeBindingRegistry.IsBound(Baby));
            AddFailCondition(() => !ChildcareUtility.CanSuckle(Baby, out ChildcareUtility.BreastfeedFailReason? _));
            AddFinishAction(delegate
            {
                RestoreBabyRest();
            });

            if (Bottle)
            {
                AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
                Toil failIfNoBabyFoodInInventory = ToilMaker.MakeToil("FailIfNoBabyFoodInInventory");
                failIfNoBabyFoodInInventory.FailOn(() => FoodUtility.BestFoodInInventory(pawn, Baby) == null);
                yield return Toils_Jump.JumpIf(failIfNoBabyFoodInInventory, () => !job.GetTarget(BabyFoodIndex).IsValid || pawn.inventory.Contains(BabyFood));
                yield return Toils_Ingest.ReserveFoodFromStackForIngesting(BabyFoodIndex, Baby).FailOnDestroyedNullOrForbidden(BabyFoodIndex);
                yield return Toils_Goto.GotoThing(BabyFoodIndex, PathEndMode.ClosestTouch).FailOnDestroyedNullOrForbidden(BabyFoodIndex);
                yield return Toils_Haul.TakeToInventory(BabyFoodIndex, delegate(Thing food)
                {
                    int wanted = FoodUtility.WillIngestStackCountOf(Baby, food.def, FoodUtility.NutritionForEater(Baby, food));
                    return Mathf.Min(food.stackCount, wanted);
                }).FailOnDestroyedNullOrForbidden(BabyFoodIndex);
                yield return failIfNoBabyFoodInInventory;
            }
            else
            {
                AddFailCondition(() => !ChildcareUtility.CanBreastfeed(pawn, out ChildcareUtility.BreastfeedFailReason? _));
            }

            yield return Toils_Goto.GotoThing(BabyIndex, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(BabyIndex)
                .FailOnSomeonePhysicallyInteracting(BabyIndex);
            yield return Bottle ? MakeBottleToil() : MakeBreastfeedToil();
        }

        private void RestoreBabyRest()
        {
            Pawn baby = Baby;
            if (baby == null || !TreeBindingRegistry.TryGet(baby, out TreeBinding binding) || !binding.IsIntact(baby.Map))
            {
                return;
            }

            MapComponent_TreeBindings.RestoreRest(binding);
        }

        private Toil MakeBreastfeedToil()
        {
            Toil toil = ToilMaker.MakeToil("FeedTiedInfantBreast");
            toil.initAction = delegate
            {
                Baby.jobs.StartJob(ChildcareUtility.MakeBabySuckleJob(pawn), JobCondition.InterruptForced);
                initialFoodPercentage = Baby.needs.food.CurLevelPercentage;
            };
            toil.tickIntervalAction = delegate(int delta)
            {
                bool keepGoing = ChildcareUtility.SuckleFromLactatingPawn(Baby, pawn, delta);
                pawn.GainComfortFromCellIfPossible(delta);
                if (!keepGoing)
                {
                    ReadyForNextToil();
                }
            };
            toil.AddFinishAction(delegate
            {
                if (Baby.Dead)
                {
                    return;
                }

                if (Baby.needs.food.CurLevelPercentage - initialFoodPercentage > 0.6f)
                {
                    Baby.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.BreastfedMe, pawn);
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.BreastfedBaby, Baby);
                }

                if (Baby.CurJobDef == JobDefOf.BabySuckle)
                {
                    Baby.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            });
            toil.AddFailCondition(() => !ChildcareUtility.CanBreastfeed(pawn, out ChildcareUtility.BreastfeedFailReason? _));
            toil.handlingFacing = true;
            toil.WithProgressBar(BabyIndex, () => Baby.needs.food.CurLevelPercentage);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.WithEffect(EffecterDefOf.Breastfeeding, BabyIndex);
            return toil;
        }

        private Toil MakeBottleToil()
        {
            Toil toil = ToilMaker.MakeToil("FeedTiedInfantBottle");
            toil.initAction = delegate
            {
                initialNutritionNeeded = Baby.needs.food.NutritionWanted;
                initialFoodPercentage = Baby.needs.food.CurLevelPercentage;
                Baby.jobs.StartJob(ChildcareUtility.MakeBabySuckleJob(pawn), JobCondition.InterruptForced);
            };
            toil.tickIntervalAction = delegate(int delta)
            {
                float perTick = Baby.needs.food.MaxLevel / 5000f;
                float step = Mathf.Min(Baby.needs.food.NutritionWanted, perTick * delta);
                bottleNutrition += step;
                totalBottleNutrition += step;
                pawn.GainComfortFromCellIfPossible(delta);
                Baby.ideo?.IncreaseIdeoExposureIfBabyTick(pawn.Ideo);
                while (true)
                {
                    Thing food = FoodUtility.BestFoodInInventory(pawn, Baby);
                    if (food == null)
                    {
                        ReadyForNextToil();
                        break;
                    }

                    if (bottleNutrition >= Baby.needs.food.NutritionWanted)
                    {
                        float eaten = food.Ingested(Baby, bottleNutrition);
                        Baby.records.AddTo(RecordDefOf.NutritionEaten, eaten);
                        bottleNutrition -= eaten;
                        Baby.needs.food.CurLevel = Mathf.Clamp(Baby.needs.food.CurLevel + eaten, 0f, Baby.needs.food.MaxLevel);
                        if (Baby.needs.food.CurLevel >= Baby.needs.food.MaxLevel)
                        {
                            ReadyForNextToil();
                            break;
                        }
                    }
                    else
                    {
                        float serving = FoodUtility.NutritionForEater(Baby, food);
                        if (bottleNutrition < serving)
                        {
                            break;
                        }

                        float eaten = food.Ingested(Baby, serving);
                        Baby.records.AddTo(RecordDefOf.NutritionEaten, eaten);
                        bottleNutrition -= eaten;
                        Baby.needs.food.CurLevel = Mathf.Clamp(Baby.needs.food.CurLevel + eaten, 0f, Baby.needs.food.MaxLevel);
                    }
                }
            };
            toil.AddFinishAction(delegate
            {
                if (Baby.needs != null && Baby.needs.food.CurLevelPercentage - initialFoodPercentage > 0.6f)
                {
                    Baby.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.FedMe, pawn);
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.FedBaby, Baby);
                }

                if (Baby.CurJobDef == JobDefOf.BabySuckle)
                {
                    Baby.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            });
            toil.handlingFacing = true;
            toil.WithProgressBar(BabyIndex, () => initialNutritionNeeded <= 0f ? 1f : totalBottleNutrition / initialNutritionNeeded);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.WithEffect(EffecterDefOf.Breastfeeding, BabyIndex);
            return toil;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initialFoodPercentage, "initialFoodPercentage", 0f);
            Scribe_Values.Look(ref bottleNutrition, "bottleNutrition", 0f);
            Scribe_Values.Look(ref totalBottleNutrition, "totalBottleNutrition", 0f);
            Scribe_Values.Look(ref initialNutritionNeeded, "initialNutritionNeeded", 0f);
        }
    }
}
