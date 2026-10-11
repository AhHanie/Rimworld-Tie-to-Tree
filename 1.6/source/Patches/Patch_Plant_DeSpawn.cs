using HarmonyLib;
using RimWorld;
using Verse;

namespace Tie_to_Tree
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.DeSpawn))]
    public static class Patch_Plant_DeSpawn
    {
        public static void Prefix(Plant __instance)
        {
            if (TreeBindingRegistry.Count == 0 || !TreeBindingRegistry.TryGetByTree(__instance, out TreeBinding binding))
            {
                return;
            }

            MapComponent_TreeBindings.Untie(binding, UnbindReason.TreeLost, true);
        }
    }
}
