using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Tie_to_Tree
{
    [HarmonyPatch]
    public static class Patch_PawnRenderer_GetBodyPos
    {
        private static readonly Type[] TargetArgs = { typeof(Vector3), typeof(PawnPosture), typeof(bool).MakeByRefType() };

        public static bool Prepare()
        {
            if (TargetMethod() != null)
            {
                return true;
            }

            Log.Warning("[Tie to Tree] PawnRenderer.GetBodyPos(Vector3, PawnPosture, out bool) was not found; tied pawns may be hidden behind trees drawn over pawns.");
            return false;
        }

        public static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(PawnRenderer), "GetBodyPos", TargetArgs);
        }

        public static void Postfix(Pawn ___pawn, PawnPosture posture, ref Vector3 __result)
        {
            if (posture == PawnPosture.Standing || !TiedTreeDrawAltitude.TryGetLift(___pawn, out float lift))
            {
                return;
            }

            __result.y += lift;
        }
    }
}
