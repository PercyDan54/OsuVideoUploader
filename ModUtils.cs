namespace OsuVideoUploader
{
    public class ModUtils
    {
        private delegate void FormatAddString(string shortString, string longString = null);

        public static string Format(Mods mods, bool shortForm = true, bool showEmpty = false, bool addSpace = false)
        {
            if (mods == Mods.None)
                return showEmpty ? "None" : string.Empty;

            string r = string.Empty;

            FormatAddString add = delegate (string shortString, string longString)
            {
                r += (shortForm ? shortString : longString ?? shortString);
            };

            if (CheckActive(Mods.Cinema, mods))
                add("Cinema");
            else if (CheckActive(Mods.Autoplay, mods))
                add("Auto");
            if (CheckActive(Mods.Target, mods))
                add("TP", "TargetPractice");
            if (CheckActive(Mods.SpunOut, mods))
                add("SO", "SpunOut");
            if (CheckActive(Mods.Easy, mods))
                add("EZ", "Easy");
            if (CheckActive(Mods.NoFail, mods))
                add("NF", "NoFail");
            if (CheckActive(Mods.Hidden, mods))
                add("HD", "Hidden");
            else if (CheckActive(Mods.FadeIn, mods))
                add("FI", "FadeIn");
            if (CheckActive(Mods.Nightcore, mods))
                add("NC", "Nightcore");
            else if (CheckActive(Mods.DoubleTime, mods))
                add("DT", "DoubleTime");
            if (CheckActive(Mods.HalfTime, mods))
                add("HT", "HalfTime");
            if (CheckActive(Mods.HardRock, mods))
                add("HR", "HardRock");
            if (CheckActive(Mods.Relax, mods))
                add("Relax");
            if (CheckActive(Mods.Relax2, mods))
                add("AP", "AutoPilot");
            if (CheckActive(Mods.Perfect, mods))
                add("PF", "Perfect");
            else if (CheckActive(Mods.SuddenDeath, mods))
                add("SD", "SuddenDeath");
            if (CheckActive(Mods.Flashlight, mods))
                add("FL", "Flashlight");

            if (CheckActive(Mods.Key1, mods))
                add("1K");
            else if (CheckActive(Mods.Key2, mods))
                add("2K");
            else if (CheckActive(Mods.Key3, mods))
                add("3K");
            else if (CheckActive(Mods.Key4, mods))
                add("4K");
            else if (CheckActive(Mods.Key5, mods))
                add("5K");
            else if (CheckActive(Mods.Key6, mods))
                add("6K");
            else if (CheckActive(Mods.Key7, mods))
                add("7K");
            else if (CheckActive(Mods.Key8, mods))
                add("8K");
            else if (CheckActive(Mods.Key9, mods))
                add("9K");

            if (CheckActive(Mods.KeyCoop, mods))
                add("2P", "Co-op");
            if (CheckActive(Mods.Random, mods))
                add("RD", "Random");

            if (r.Length == 0)
                return "";

            if (addSpace)
                r += " ";
            return r;
        }

        public static bool CheckActive(Mods haystack, Mods needle)
        {
            return (haystack & needle) > 0;
        }
    }
}
