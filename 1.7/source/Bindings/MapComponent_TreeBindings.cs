using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    public enum UnbindReason
    {
        Manual,
        TreeLost,
        PawnLeft,
        StatusChanged,
        Released,
        Escaped,
        Invalid,
        Replaced
    }

    public class MapComponent_TreeBindings : MapComponent
    {
        public const int TicksPerDay = 60000;

        private const int IntegrityInterval = 120;

        private struct ScheduleEntry
        {
            public int tick;

            public TreeBinding binding;
        }

        private List<TreeBinding> bindings = new List<TreeBinding>();

        private readonly List<ScheduleEntry> heap = new List<ScheduleEntry>();

        public MapComponent_TreeBindings(Map map) : base(map)
        {
        }

        public static MapComponent_TreeBindings For(Map map)
        {
            return map?.GetComponent<MapComponent_TreeBindings>();
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref bindings, "bindings", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && bindings == null)
            {
                bindings = new List<TreeBinding>();
            }
        }

        public override void FinalizeInit()
        {
            TreeBindingRegistry.PurgeStale();
            heap.Clear();
            if (bindings == null)
            {
                bindings = new List<TreeBinding>();
            }

            List<TreeBinding> loaded = new List<TreeBinding>(bindings);
            bindings.Clear();
            for (int i = 0; i < loaded.Count; i++)
            {
                TreeBinding binding = loaded[i];
                if (binding == null)
                {
                    continue;
                }

                binding.map = map;
                if (binding.IsIntact(map) && TreeBindingRegistry.Add(binding))
                {
                    bindings.Add(binding);
                    PushHeap(binding.nextEscapeCheckTick, binding);
                    Logger.Message("Restored tie of " + binding.prisoner.LabelShort + " to " + binding.tree.LabelShort);
                }
                else
                {
                    binding.removed = true;
                    ReleaseAnchor(binding.anchor, binding.prisoner);
                    Logger.Warning("Discarded an invalid tie record on load.");
                }
            }

            DestroyOrphanAnchors();
        }

        public override void MapRemoved()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                bindings[i].removed = true;
                TreeBindingRegistry.Remove(bindings[i]);
            }

            bindings.Clear();
            heap.Clear();
        }

        public override void MapComponentTick()
        {
            if (bindings.Count == 0)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (heap.Count > 0 && heap[0].tick <= now)
            {
                ProcessDue(now);
            }

            if (now % IntegrityInterval == 0)
            {
                CheckIntegrity();
            }
        }

        public TreeBinding Bind(Pawn prisoner, Plant tree, IntVec3 cell)
        {
            if (prisoner == null || tree == null || !prisoner.Spawned || !tree.Spawned || prisoner.Map != map || tree.Map != map)
            {
                return null;
            }

            if (TreeBindingRegistry.TryGetByTree(tree, out TreeBinding treeOwner) && treeOwner.prisoner != prisoner)
            {
                return null;
            }

            if (TreeBindingRegistry.TryGet(prisoner, out TreeBinding existing))
            {
                Unbind(existing, UnbindReason.Replaced, true);
            }

            Building_TreeRestraint anchor = ThingMaker.MakeThing(TietoTreeDefOf.TietoTree_TreeRestraint) as Building_TreeRestraint;
            if (anchor == null)
            {
                return null;
            }

            anchor.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(anchor, cell, map, Rot4.South);
            if (!anchor.Spawned)
            {
                return null;
            }

            anchor.ForOwnerType = BedOwnerType.Prisoner;

            TreeBinding binding = new TreeBinding
            {
                prisoner = prisoner,
                tree = tree,
                anchor = anchor,
                cell = cell,
                map = map
            };

            if (!TreeBindingRegistry.Add(binding))
            {
                ReleaseAnchor(anchor, prisoner);
                return null;
            }

            bindings.Add(binding);
            Schedule(binding, RandomTickInRemainingDay(Find.TickManager.TicksGame));
            Logger.Message("Tied " + prisoner.LabelShort + " to " + tree.LabelShort + " at " + cell);
            return binding;
        }

        public void Unbind(TreeBinding binding, UnbindReason reason, bool endRestJob)
        {
            if (binding == null || binding.removed)
            {
                return;
            }

            binding.removed = true;
            bindings.Remove(binding);

            if (endRestJob)
            {
                EndRestJob(binding);
            }

            Pawn prisoner = binding.prisoner;
            Building_TreeRestraint anchor = binding.anchor;
            TreeBindingRegistry.Remove(binding);
            ReleaseAnchor(anchor, prisoner);
            Logger.Message("Untied " + (prisoner != null ? prisoner.LabelShort : "null") + " (" + reason + ")");
        }

        public static bool Untie(Pawn pawn, UnbindReason reason, bool endRestJob = true)
        {
            if (!TreeBindingRegistry.TryGet(pawn, out TreeBinding binding))
            {
                return false;
            }

            Untie(binding, reason, endRestJob);
            return true;
        }

        public static void Untie(TreeBinding binding, UnbindReason reason, bool endRestJob = true)
        {
            MapComponent_TreeBindings component = For(binding.map);
            if (component != null)
            {
                component.Unbind(binding, reason, endRestJob);
                return;
            }

            if (binding.removed)
            {
                return;
            }

            binding.removed = true;
            if (endRestJob)
            {
                EndRestJob(binding);
            }

            TreeBindingRegistry.Remove(binding);
            ReleaseAnchor(binding.anchor, binding.prisoner);
        }

        public static void RestoreRest(TreeBinding binding)
        {
            Pawn prisoner = binding.prisoner;
            if (prisoner.CurrentBed() == binding.anchor)
            {
                return;
            }

            Job current = prisoner.CurJob;
            if (current != null && current.def != JobDefOf.Wait_Downed && !(current.def == JobDefOf.LayDown && current.targetA.Thing != binding.anchor))
            {
                return;
            }

            if (!TreeBindingRegistry.CanUseAnchor(binding.anchor, prisoner))
            {
                return;
            }

            prisoner.jobs.Notify_TuckedIntoBed(binding.anchor);
        }

        private static void EndRestJob(TreeBinding binding)
        {
            Pawn prisoner = binding.prisoner;
            if (prisoner == null || !prisoner.Spawned || prisoner.jobs == null)
            {
                return;
            }

            Job current = prisoner.CurJob;
            if (current != null && current.def == JobDefOf.LayDown && current.targetA.Thing == binding.anchor)
            {
                prisoner.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
            }
        }

        private static void ReleaseAnchor(Building_TreeRestraint anchor, Pawn prisoner)
        {
            if (anchor != null)
            {
                CompAssignableToPawn assignable = anchor.CompAssignableToPawn;
                if (assignable != null && assignable.AssignedPawnsForReading.Count > 0)
                {
                    List<Pawn> owners = new List<Pawn>(assignable.AssignedPawnsForReading);
                    for (int i = 0; i < owners.Count; i++)
                    {
                        owners[i].ownership?.UnclaimBed();
                    }
                }
            }

            if (prisoner != null && anchor != null && prisoner.ownership != null && prisoner.ownership.OwnedBed == anchor)
            {
                prisoner.ownership.UnclaimBed();
            }

            if (anchor != null && !anchor.Destroyed)
            {
                anchor.Destroy();
            }
        }

        private void DestroyOrphanAnchors()
        {
            List<Thing> placed = map.listerThings.ThingsOfDef(TietoTreeDefOf.TietoTree_TreeRestraint);
            if (placed.Count == 0)
            {
                return;
            }

            List<Thing> snapshot = new List<Thing>(placed);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Building_TreeRestraint anchor = snapshot[i] as Building_TreeRestraint;
                if (anchor == null || IsActiveAnchor(anchor))
                {
                    continue;
                }

                ReleaseAnchor(anchor, null);
                Logger.Warning("Removed an orphaned tree restraint at " + anchor.Position);
            }
        }

        private bool IsActiveAnchor(Building_TreeRestraint anchor)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].anchor == anchor)
                {
                    return true;
                }
            }

            return false;
        }

        private void CheckIntegrity()
        {
            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                TreeBinding binding = bindings[i];
                if (!binding.IsIntact(map))
                {
                    Unbind(binding, UnbindReason.Invalid, true);
                    continue;
                }

                RestoreRest(binding);
            }
        }

        private void ProcessDue(int now)
        {
            while (heap.Count > 0 && heap[0].tick <= now)
            {
                ScheduleEntry entry = PopHeap();
                TreeBinding binding = entry.binding;
                if (binding.removed || binding.nextEscapeCheckTick != entry.tick)
                {
                    continue;
                }

                if (!binding.IsIntact(map))
                {
                    Unbind(binding, UnbindReason.Invalid, true);
                    continue;
                }

                Schedule(binding, RandomTickInNextDay(now));
                RollEscape(binding);
            }
        }

        private void RollEscape(TreeBinding binding)
        {
            if (!PrisonBreakUtility.CanParticipateInPrisonBreak(binding.prisoner))
            {
                return;
            }

            if (!Rand.Chance(Mathf.Clamp01(ModSettings.EscapeChancePerDay)))
            {
                return;
            }

            Escape(binding);
        }

        private void Escape(TreeBinding binding)
        {
            Pawn prisoner = binding.prisoner;
            Unbind(binding, UnbindReason.Escaped, true);
            if (prisoner == null || !prisoner.Spawned)
            {
                return;
            }

            Messages.Message("TietoTree.MessageBrokeFree".Translate(prisoner.LabelShort, prisoner), prisoner, MessageTypeDefOf.ThreatSmall);
            Find.TickManager.slower.SignalForceNormalSpeed();

            if (RCellFinder.TryFindBestExitSpot(prisoner, out IntVec3 spot))
            {
                Job job = JobMaker.MakeJob(JobDefOf.Goto, spot);
                job.exitMapOnArrival = true;
                prisoner.jobs.StartJob(job, JobCondition.InterruptForced);
            }
            else
            {
                Logger.Message("No exit found for " + prisoner.LabelShort + " after breaking free.");
            }
        }

        private void Schedule(TreeBinding binding, int tick)
        {
            binding.nextEscapeCheckTick = tick;
            PushHeap(tick, binding);
        }

        public static int RandomTickInRemainingDay(int now)
        {
            int day = now / TicksPerDay;
            int start = now + 1;
            int dayEnd = (day + 1) * TicksPerDay;
            if (start >= dayEnd)
            {
                return RandomTickInDay(day + 1);
            }

            return Rand.RangeInclusive(start, dayEnd - 1);
        }

        public static int RandomTickInNextDay(int now)
        {
            return RandomTickInDay(now / TicksPerDay + 1);
        }

        private static int RandomTickInDay(int day)
        {
            int start = day * TicksPerDay;
            return Rand.RangeInclusive(start, start + TicksPerDay - 1);
        }

        private void PushHeap(int tick, TreeBinding binding)
        {
            heap.Add(new ScheduleEntry { tick = tick, binding = binding });
            int child = heap.Count - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (heap[parent].tick <= heap[child].tick)
                {
                    break;
                }

                ScheduleEntry swap = heap[parent];
                heap[parent] = heap[child];
                heap[child] = swap;
                child = parent;
            }
        }

        private ScheduleEntry PopHeap()
        {
            ScheduleEntry top = heap[0];
            int last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);
            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                int right = left + 1;
                int smallest = parent;
                if (left < heap.Count && heap[left].tick < heap[smallest].tick)
                {
                    smallest = left;
                }

                if (right < heap.Count && heap[right].tick < heap[smallest].tick)
                {
                    smallest = right;
                }

                if (smallest == parent)
                {
                    break;
                }

                ScheduleEntry swap = heap[parent];
                heap[parent] = heap[smallest];
                heap[smallest] = swap;
                parent = smallest;
            }

            return top;
        }
    }
}
