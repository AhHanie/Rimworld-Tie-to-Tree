using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Tie_to_Tree
{
    public struct TreeCandidate
    {
        public Plant tree;

        public IntVec3 cell;

        public float distance;
    }

    public static class TreeBindingUtility
    {
        public const float SearchRadius = 45f;

        public const float MinTreeGrowth = 0.8f;

        private const int PreselectionPool = 24;

        private const int CandidateCacheTicks = 250;

        private const int MaxCachedEntries = 64;

        private static readonly Dictionary<long, KeyValuePair<int, bool>> candidateCache = new Dictionary<long, KeyValuePair<int, bool>>();

        public static bool IsTreeUsable(Plant tree, Pawn captor, Pawn target)
        {
            if (tree == null || !tree.Spawned || tree.Destroyed || tree.def.plant == null || !tree.def.plant.IsTree)
            {
                return false;
            }

            if (tree.Growth < MinTreeGrowth)
            {
                return false;
            }

            if (tree.IsBurning() || tree.Dying || tree.Position.Fogged(tree.Map))
            {
                return false;
            }

            if (captor != null && tree.IsForbidden(captor))
            {
                return false;
            }

            if (TreeBindingRegistry.TryGetByTree(tree, out TreeBinding owner) && owner.prisoner != target)
            {
                return false;
            }

            return true;
        }

        public static bool IsCellUsable(IntVec3 cell, Map map, Pawn captor, Pawn target)
        {
            if (!cell.InBounds(map) || !cell.Walkable(map) || cell.Fogged(map))
            {
                return false;
            }

            if (cell.GetFirstBuilding(map) != null || cell.ContainsStaticFire(map))
            {
                return false;
            }

            if (captor != null)
            {
                if (cell.IsForbidden(captor) || cell.GetDangerFor(captor, map) == Danger.Deadly)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryFindTieCell(Plant tree, Pawn captor, Pawn target, out IntVec3 cell)
        {
            cell = tree.Position;
            if (!IsCellUsable(cell, tree.Map, captor, target))
            {
                return false;
            }

            return captor == null || captor.CanReserve(cell);
        }

        public static bool HasAnyCandidate(Pawn captor, Pawn target)
        {
            int now = Find.TickManager.TicksGame;
            long key = ((long)captor.thingIDNumber << 32) | (uint)target.thingIDNumber;
            if (candidateCache.TryGetValue(key, out KeyValuePair<int, bool> cached) && now - cached.Key < CandidateCacheTicks && now >= cached.Key)
            {
                return cached.Value;
            }

            if (candidateCache.Count >= MaxCachedEntries)
            {
                candidateCache.Clear();
            }

            bool result = FindCandidates(captor, target, 1).Count > 0;
            candidateCache[key] = new KeyValuePair<int, bool>(now, result);
            return result;
        }

        public static List<TreeCandidate> FindCandidates(Pawn captor, Pawn target, int max)
        {
            List<TreeCandidate> pool = new List<TreeCandidate>();
            Map map = captor.Map;
            if (map == null || !target.Spawned)
            {
                return pool;
            }

            TreeBindingRegistry.TryGet(target, out TreeBinding current);
            IntVec3 origin = target.Position;
            float maxDistanceSquared = SearchRadius * SearchRadius;
            List<Thing> plants = map.listerThings.ThingsInGroup(ThingRequestGroup.Plant);
            for (int i = 0; i < plants.Count; i++)
            {
                Plant tree = plants[i] as Plant;
                if (tree == null || tree.def.plant == null || !tree.def.plant.IsTree)
                {
                    continue;
                }

                float distance = (tree.Position - origin).LengthHorizontalSquared;
                if (distance > maxDistanceSquared)
                {
                    continue;
                }

                if (current != null && current.tree == tree)
                {
                    continue;
                }

                if (pool.Count >= PreselectionPool && distance >= pool[pool.Count - 1].distance)
                {
                    continue;
                }

                if (!IsTreeUsable(tree, captor, target) || !TryFindTieCell(tree, captor, target, out IntVec3 cell))
                {
                    continue;
                }

                InsertSorted(pool, new TreeCandidate { tree = tree, cell = cell, distance = distance });
                if (pool.Count > PreselectionPool)
                {
                    pool.RemoveAt(pool.Count - 1);
                }
            }

            List<TreeCandidate> reachable = new List<TreeCandidate>();
            for (int i = 0; i < pool.Count && reachable.Count < max; i++)
            {
                if (captor.CanReach(pool[i].cell, PathEndMode.OnCell, Danger.Deadly))
                {
                    reachable.Add(pool[i]);
                }
            }

            return reachable;
        }

        private static void InsertSorted(List<TreeCandidate> pool, TreeCandidate candidate)
        {
            int index = pool.Count;
            while (index > 0 && pool[index - 1].distance > candidate.distance)
            {
                index--;
            }

            pool.Insert(index, candidate);
        }

        public static bool NeedsAcceptanceRoll(Pawn target)
        {
            return !target.IsPrisonerOfColony;
        }

        public static bool RequiresDraftedCaptor(Pawn target)
        {
            return target.Faction == Faction.OfPlayer && !target.IsPrisonerOfColony;
        }

        public static string DescribeTree(Plant tree)
        {
            return tree.LabelNoCount;
        }
    }
}
