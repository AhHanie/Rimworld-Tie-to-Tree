using HarmonyLib;
using Verse;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectString))]
    public static class Patch_Pawn_GetInspectString
    {
        public static void Postfix(Pawn __instance, ref string __result)
        {
            if (TreeBindingRegistry.Count == 0 || !TreeBindingRegistry.TryGet(__instance, out TreeBinding binding))
            {
                return;
            }

            string line = "TietoTree.TiedToInspect".Translate(binding.tree.LabelNoCount);
            __result = __result.NullOrEmpty() ? line : __result + "\n" + line;
        }
    }
}
