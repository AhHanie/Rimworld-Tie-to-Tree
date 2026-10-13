using UnityEngine;
using Verse;

namespace Tie_to_Tree
{
    [StaticConstructorOnStartup]
    public static class TietoTreeTextures
    {
        public static readonly Texture2D Untie = ContentFinder<Texture2D>.Get("TieToTree/UntieFromTree");
    }
}
