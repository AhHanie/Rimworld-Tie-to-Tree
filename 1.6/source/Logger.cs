using System;
using Verse;

namespace Tie_to_Tree
{
    public static class Logger
    {
        private const string Prefix = "[Tie to Tree] ";

        public static bool Enabled => ModSettings.EnableDebugLogging;

        public static void Message(string message)
        {
            if (Enabled)
            {
                Log.Message(Prefix + message);
            }
        }

        public static void Warning(string message)
        {
            if (Enabled)
            {
                Log.Warning(Prefix + message);
            }
        }

        public static void Error(string message)
        {
            if (Enabled)
            {
                Log.Error(Prefix + message);
            }
        }

        public static void Exception(Exception exception, string context = null)
        {
            if (!Enabled || exception == null)
            {
                return;
            }

            string prefix = string.IsNullOrWhiteSpace(context) ? Prefix : Prefix + context + ": ";
            Log.Error(prefix + exception);
        }
    }
}
