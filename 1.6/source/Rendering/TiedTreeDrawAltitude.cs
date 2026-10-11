using RimWorld;
using Verse;

namespace Tie_to_Tree
{
    public static class TiedTreeDrawAltitude
    {
        public static bool TryGetLift(Pawn pawn, out float lift)
        {
            lift = 0f;
            return TreeBindingRegistry.Count != 0
                && TreeBindingRegistry.TryGet(pawn, out TreeBinding binding)
                && TryGetLift(pawn, binding, out lift);
        }

        public static bool TryGetLift(Pawn pawn, TreeBinding binding, out float lift)
        {
            lift = 0f;
            Plant tree = binding.tree;
            if (binding.removed || tree == null || !tree.Spawned || !pawn.Spawned || tree.Map != pawn.Map)
            {
                return false;
            }

            if (pawn.GetPosture() == PawnPosture.Standing || pawn.CurrentBed() != binding.anchor)
            {
                return false;
            }

            // Plant.Print raises the top vertices by TopVerticesAltitudeBias; one extra step clears the snow overlay and pawn node offsets.
            float treeTop = tree.def.Altitude + Plant.TopVerticesAltitudeBias + Altitudes.AltInc;
            float needed = treeTop - AltitudeLayer.LayingPawn.AltitudeFor();
            if (needed <= 0f)
            {
                return false;
            }

            lift = needed;
            return true;
        }
    }
}
