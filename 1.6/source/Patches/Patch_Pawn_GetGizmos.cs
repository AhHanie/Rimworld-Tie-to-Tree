using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (TreeBindingRegistry.Count == 0 || !TreeBindingRegistry.IsBound(__instance))
            {
                return;
            }

            __result = Append(__result, MakeUntieGizmo(__instance));
        }

        private static IEnumerable<Gizmo> Append(IEnumerable<Gizmo> original, Gizmo extra)
        {
            if (original != null)
            {
                foreach (Gizmo gizmo in original)
                {
                    yield return gizmo;
                }
            }

            yield return extra;
        }

        private static Gizmo MakeUntieGizmo(Pawn prisoner)
        {
            return new Command_Action
            {
                defaultLabel = "TietoTree.CommandUntieLabel".Translate(),
                defaultDesc = "TietoTree.CommandUntieDesc".Translate(),
                icon = TietoTreeTextures.Untie,
                action = delegate
                {
                    OpenUntieMenu(prisoner);
                }
            };
        }

        private static void OpenUntieMenu(Pawn prisoner)
        {
            if (!prisoner.Spawned)
            {
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (Pawn colonist in prisoner.Map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist.Downed || !colonist.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) || !colonist.CanReach(prisoner, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                Pawn actor = colonist;
                options.Add(new FloatMenuOption(actor.LabelShortCap, delegate
                {
                    Job job = JobMaker.MakeJob(TietoTreeDefOf.TietoTree_TreeUntie, prisoner);
                    actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }));
            }

            if (options.Count == 0)
            {
                Messages.Message("TietoTree.NoOneToUntie".Translate(), prisoner, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            Find.WindowStack.Add(new FloatMenu(options, "TietoTree.PickUntiePawnTitle".Translate()));
        }
    }
}
