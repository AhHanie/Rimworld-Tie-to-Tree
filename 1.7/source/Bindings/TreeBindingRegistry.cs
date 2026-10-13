using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Tie_to_Tree
{
    public static class TreeBindingRegistry
    {
        private static readonly Dictionary<Pawn, TreeBinding> byPawn = new Dictionary<Pawn, TreeBinding>();

        private static readonly Dictionary<Plant, TreeBinding> byTree = new Dictionary<Plant, TreeBinding>();

        private static readonly List<TreeBinding> tmpStale = new List<TreeBinding>();

        public static int Count => byPawn.Count;

        public static bool TryGet(Pawn pawn, out TreeBinding binding)
        {
            binding = null;
            return pawn != null && byPawn.Count != 0 && byPawn.TryGetValue(pawn, out binding);
        }

        public static bool TryGetByTree(Plant tree, out TreeBinding binding)
        {
            binding = null;
            return tree != null && byTree.Count != 0 && byTree.TryGetValue(tree, out binding);
        }

        public static bool IsBound(Pawn pawn)
        {
            return pawn != null && byPawn.Count != 0 && byPawn.ContainsKey(pawn);
        }

        public static bool Add(TreeBinding binding)
        {
            if (binding == null || !binding.HasAllReferences)
            {
                return false;
            }

            if (byPawn.ContainsKey(binding.prisoner) || byTree.ContainsKey(binding.tree))
            {
                return false;
            }

            byPawn.Add(binding.prisoner, binding);
            byTree.Add(binding.tree, binding);
            return true;
        }

        public static void Remove(TreeBinding binding)
        {
            if (binding == null)
            {
                return;
            }

            if (binding.prisoner != null && byPawn.TryGetValue(binding.prisoner, out TreeBinding pawnEntry) && pawnEntry == binding)
            {
                byPawn.Remove(binding.prisoner);
            }

            if (binding.tree != null && byTree.TryGetValue(binding.tree, out TreeBinding treeEntry) && treeEntry == binding)
            {
                byTree.Remove(binding.tree);
            }
        }

        public static void PurgeStale()
        {
            if (byPawn.Count == 0)
            {
                return;
            }

            tmpStale.Clear();
            List<Map> maps = Current.Game?.Maps;
            foreach (TreeBinding binding in byPawn.Values)
            {
                if (maps == null || binding.map == null || !maps.Contains(binding.map))
                {
                    tmpStale.Add(binding);
                }
            }

            for (int i = 0; i < tmpStale.Count; i++)
            {
                tmpStale[i].removed = true;
                Remove(tmpStale[i]);
            }

            tmpStale.Clear();
        }

        public static bool CanUseAnchor(Building_TreeRestraint anchor, Pawn sleeper)
        {
            if (anchor == null || sleeper == null || !anchor.Spawned)
            {
                return false;
            }

            if (anchor.Map != sleeper.MapHeld || anchor.IsBurning())
            {
                return false;
            }

            if (!TryGet(sleeper, out TreeBinding binding) || binding.anchor != anchor)
            {
                return false;
            }

            if (sleeper.HarmedByVacuum && anchor.Position.GetVacuum(anchor.Map) >= 0.5f)
            {
                return false;
            }

            if (!RestUtility.CanUseBedEver(sleeper, anchor.def))
            {
                return false;
            }

            Pawn occupant = anchor.GetCurOccupant(0);
            return occupant == null || occupant == sleeper;
        }
    }
}
