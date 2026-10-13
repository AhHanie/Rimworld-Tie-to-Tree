using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    public class FloatMenuOptionProvider_TieToTree : FloatMenuOptionProvider
    {
        private const int MaxMenuTrees = 8;

        protected override bool Drafted => true;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;

        protected override bool RequiresManipulation => true;

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            Pawn captor = context.FirstSelectedPawn;
            if (captor == null || captor.Map == null || !IsPossibleTarget(clickedPawn, captor))
            {
                return null;
            }

            string cannot = "TietoTree.CannotTieToTree".Translate();
            if (captor.InSameExtraFaction(clickedPawn, ExtraFactionType.HomeFaction) || captor.InSameExtraFaction(clickedPawn, ExtraFactionType.MiniFaction))
            {
                return new FloatMenuOption(cannot + ": " + "SameFaction".Translate(clickedPawn), null);
            }

            if (!captor.CanReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly))
            {
                return new FloatMenuOption(cannot + ": " + "NoPath".Translate().CapitalizeFirst(), null);
            }

            if (!TreeBindingUtility.HasAnyCandidate(captor, clickedPawn))
            {
                return new FloatMenuOption(cannot + ": " + "TietoTree.NoTreeNearby".Translate(), null);
            }

            string label = "TietoTree.TieToTree".Translate(clickedPawn.LabelCap, clickedPawn);
            if (TreeBindingUtility.NeedsAcceptanceRoll(clickedPawn) && !clickedPawn.Downed && !clickedPawn.DevelopmentalStage.Baby())
            {
                label = "TietoTree.TieToTreeChance".Translate(clickedPawn.LabelCap, clickedPawn, clickedPawn.GetAcceptArrestChance(captor).ToStringPercent());
            }

            Pawn target = clickedPawn;
            Action openMenu = delegate
            {
                OpenTreeMenu(captor, target);
            };

            return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, openMenu, MenuOptionPriority.High, null, clickedPawn), captor, clickedPawn);
        }

        public static bool IsPossibleTarget(Pawn target, Pawn captor)
        {
            if (!target.Spawned || target.Dead || !target.RaceProps.Humanlike || target.IsSubhuman)
            {
                return false;
            }

            if (TreeBindingUtility.RequiresDraftedCaptor(target) && !captor.Drafted)
            {
                return false;
            }

            if (target.IsPrisonerOfColony)
            {
                return true;
            }

            if (target.Downed || target.DevelopmentalStage.Baby())
            {
                return !target.HostileTo(Faction.OfPlayer) || target.Downed;
            }

            return target.CanBeArrestedBy(captor);
        }

        private static void OpenTreeMenu(Pawn captor, Pawn target)
        {
            List<TreeCandidate> candidates = TreeBindingUtility.FindCandidates(captor, target, MaxMenuTrees);
            if (candidates.Count == 0)
            {
                Messages.Message("TietoTree.CannotTieToTree".Translate() + ": " + "TietoTree.NoTreeNearby".Translate(), target, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < candidates.Count; i++)
            {
                TreeCandidate candidate = candidates[i];
                string label = "TietoTree.TreeOption".Translate(TreeBindingUtility.DescribeTree(candidate.tree).CapitalizeFirst(), Mathf.RoundToInt(Mathf.Sqrt(candidate.distance)));
                options.Add(new FloatMenuOption(label, delegate
                {
                    StartJob(captor, target, candidate);
                }, candidate.tree, Color.white, MenuOptionPriority.Default, delegate(Rect _)
                {
                    TargetHighlighter.Highlight(new GlobalTargetInfo(candidate.tree), arrow: true, colonistBar: false, circleOverlay: true);
                    TargetHighlighter.Highlight(new GlobalTargetInfo(candidate.cell, candidate.tree.Map), arrow: false, colonistBar: false, circleOverlay: true);
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options, "TietoTree.PickTreeTitle".Translate()));
        }

        private static void StartJob(Pawn captor, Pawn target, TreeCandidate candidate)
        {
            if (!TreeBindingUtility.IsTreeUsable(candidate.tree, captor, target) || !TreeBindingUtility.IsCellUsable(candidate.cell, candidate.tree.Map, captor, target))
            {
                Messages.Message("TietoTree.CannotTieToTree".Translate() + ": " + "TietoTree.TreeNoLongerValid".Translate(), target, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            Job job = JobMaker.MakeJob(TietoTreeDefOf.TietoTree_TieToTree, target, candidate.tree, candidate.cell);
            job.count = 1;
            captor.jobs.TryTakeOrderedJob(job, JobTag.Misc);

            if (TreeBindingUtility.NeedsAcceptanceRoll(target) && target.Faction != null && target.Faction != Faction.OfPlayer && !target.Faction.Hidden && !target.Faction.HostileTo(Faction.OfPlayer))
            {
                Messages.Message("MessageCapturingWillAngerFaction".Translate(target.Named("PAWN")).AdjustedFor(target), target, MessageTypeDefOf.CautionInput, historical: false);
            }
        }
    }
}
