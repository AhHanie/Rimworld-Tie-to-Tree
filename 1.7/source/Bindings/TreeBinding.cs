using RimWorld;
using Verse;

namespace Tie_to_Tree
{
    public class TreeBinding : IExposable
    {
        public Pawn prisoner;

        public Plant tree;

        public Building_TreeRestraint anchor;

        public IntVec3 cell;

        public int nextEscapeCheckTick;

        public Map map;

        public bool removed;

        public bool HasAllReferences => prisoner != null && tree != null && anchor != null;

        public void ExposeData()
        {
            Scribe_References.Look(ref prisoner, "prisoner");
            Scribe_References.Look(ref tree, "tree");
            Scribe_References.Look(ref anchor, "anchor");
            Scribe_Values.Look(ref cell, "cell");
            Scribe_Values.Look(ref nextEscapeCheckTick, "nextEscapeCheckTick", 0);
        }

        public bool IsIntact(Map expectedMap)
        {
            if (!HasAllReferences)
            {
                return false;
            }

            if (prisoner.Destroyed || prisoner.Dead || !prisoner.Spawned || prisoner.Map != expectedMap)
            {
                return false;
            }

            if (!prisoner.IsPrisonerOfColony)
            {
                return false;
            }

            if (tree.Destroyed || !tree.Spawned || tree.Map != expectedMap || tree.IsBurning())
            {
                return false;
            }

            if (anchor.Destroyed || !anchor.Spawned || anchor.Map != expectedMap)
            {
                return false;
            }

            return prisoner.Position == cell && anchor.Position == cell;
        }
    }
}
