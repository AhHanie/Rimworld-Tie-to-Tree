using RimWorld;
using UnityEngine;
using Verse;

namespace Tie_to_Tree
{
    [StaticConstructorOnStartup]
    public static class TreeRopeDrawer
    {
        private static readonly Material BodyRopeMat = MaterialPool.MatFrom("TieToTree/BodyRope", ShaderDatabase.Transparent);

        private static readonly Vector3 BodyRopeScale = new Vector3(1f, 1f, 1f);

        public static void Draw(Pawn pawn, TreeBinding binding, Vector3 drawLoc)
        {
            if (!pawn.Spawned || binding.tree == null || !binding.tree.Spawned || pawn.Position.Fogged(pawn.Map))
            {
                return;
            }

            Vector3 bodyCenter = drawLoc;
            if (pawn.story != null && pawn.CurrentBed() == binding.anchor)
            {
                float lift = pawn.Drawer.renderer.BaseHeadOffsetAt(Rot4.South).z + pawn.story.bodyType.bedOffset;
                bodyCenter.z -= lift;
            }

            bodyCenter.y = AltitudeLayer.PawnRope.AltitudeFor() + 0.01f;
            Matrix4x4 matrix = Matrix4x4.TRS(bodyCenter, Quaternion.identity, BodyRopeScale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, BodyRopeMat, 0);
        }
    }
}
