using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Tie_to_Tree
{
    public class Building_TreeRestraint : Building_Bed
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            yield break;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            yield break;
        }

        public override string GetInspectString()
        {
            return string.Empty;
        }

        public override void DrawGUIOverlay()
        {
        }

        public override void DrawExtraSelectionOverlays()
        {
        }
    }
}
