using RimWorld;
using Verse;

namespace Tie_to_Tree
{
    [DefOf]
    public static class TietoTreeDefOf
    {
        public static ThingDef TietoTree_TreeRestraint;

        public static JobDef TietoTree_TieToTree;

        public static JobDef TietoTree_TreeUntie;

        public static JobDef TietoTree_FeedTiedInfant;

        static TietoTreeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TietoTreeDefOf));
        }
    }
}
