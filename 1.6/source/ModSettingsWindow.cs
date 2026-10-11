using UnityEngine;
using Verse;

namespace Tie_to_Tree
{
    public static class ModSettingsWindow
    {
        public static void Draw(Rect parent)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(parent);

            string percent = (ModSettings.EscapeChancePerDay * 100f).ToString("0.#") + "%";
            listing.Label("TietoTree.SettingsEscapeChance".Translate(percent), -1f, "TietoTree.SettingsEscapeChanceDesc".Translate());
            float chance = listing.Slider(ModSettings.EscapeChancePerDay, 0f, 1f);
            ModSettings.EscapeChancePerDay = Mathf.Clamp01(Mathf.Round(chance * 200f) / 200f);
            listing.Gap(6f);
            listing.Label("TietoTree.SettingsEscapeChanceExplain".Translate());
            listing.Gap(12f);

            listing.CheckboxLabeled("TietoTree.SettingsDebugLogging".Translate(), ref ModSettings.EnableDebugLogging, "TietoTree.SettingsDebugLoggingDesc".Translate());
            listing.Gap(12f);

            if (listing.ButtonText("TietoTree.SettingsReset".Translate()))
            {
                ModSettings.EscapeChancePerDay = ModSettings.DefaultEscapeChancePerDay;
                ModSettings.EnableDebugLogging = false;
            }

            listing.End();
        }
    }
}
