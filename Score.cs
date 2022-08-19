using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OsuVideoUploader.API;
using System;

namespace OsuVideoUploader
{
    public class Score
    {
        [JsonProperty(@"score")]
        public long TotalScore { get; set; }

        [JsonProperty(@"max_combo")]
        public int MaxCombo { get; set; }

        [JsonProperty(@"perfect")]
        public bool Perfect { get; set; }

        [JsonProperty(@"created_at")]
        public DateTimeOffset Date { get; set; }

        internal float Accuracy
        {
            get
            {
                switch (PlayMode)
                {
                    default:
                    case PlayModes.Osu:
                        return TotalHits > 0 ? (float)(Count50 * 50 + Count100 * 100 + Count300 * 300) / (TotalHits * 300) : 1;

                    case PlayModes.Taiko:
                        return TotalHits > 0 ? (float)(Count100 * 150 + Count300 * 300) / (TotalHits * 300) : 0;

                    case PlayModes.CatchTheBeat:
                        if (TotalHits == 0) return 1;

                        return (float)TotalSuccessfulHits / TotalHits;

                    case PlayModes.OsuMania:
                        if (TotalHits == 0)
                            return 1;

                        return (float)(Count50 * 50 + Count100 * 100 + CountKatu * 200 + (Count300 + CountGeki) * 300) / (TotalHits * 300);
                }
            }
        }

        internal int TotalHits
        {
            get
            {
                switch (PlayMode)
                {
                    default:
                        return Count50 + Count100 + Count300 + CountMiss;

                    case PlayModes.CatchTheBeat:
                        return Count50 + Count100 + Count300 + CountMiss + CountKatu;

                    case PlayModes.OsuMania:
                        return Count50 + Count100 + Count300 + CountMiss + CountGeki + CountKatu;
                }
            }
        }

        internal int TotalSuccessfulHits
        {
            get 
            {
                switch (PlayMode)
                {
                    default:
                        return Count50 + Count100 + Count300;

                    case PlayModes.OsuMania:
                        return Count50 + Count100 + Count300 + CountGeki + CountKatu;
                }
            }
        }

        internal virtual ScoreRank Rank
        {
            get
            {
                float acc = Accuracy;
                switch (PlayMode)
                {
                    default:
                    case PlayModes.Osu:
                        float ratio300 = (float)Count300 / TotalHits;
                        float ratio50 = (float)Count50 / TotalHits;
                        if (ratio300 == 1)
                            return ModUtils.CheckActive(EnabledMods, Mods.Hidden) ||
                                   ModUtils.CheckActive(EnabledMods, Mods.Flashlight)
                                ? ScoreRank.XH
                                : ScoreRank.X;
                        if (ratio300 > 0.9 && ratio50 <= 0.01 && CountMiss == 0)
                            return ModUtils.CheckActive(EnabledMods, Mods.Hidden) ||
                                   ModUtils.CheckActive(EnabledMods, Mods.Flashlight)
                                ? ScoreRank.SH
                                : ScoreRank.S;
                        if ((ratio300 > 0.8 && CountMiss == 0) || (ratio300 > 0.9))
                            return ScoreRank.A;
                        if ((ratio300 > 0.7 && CountMiss == 0) || (ratio300 > 0.8))
                            return ScoreRank.B;
                        if (ratio300 > 0.6)
                            return ScoreRank.C;
                        return ScoreRank.D;

                    case PlayModes.CatchTheBeat:
                        if (acc == 1)
                            return ModUtils.CheckActive(EnabledMods, Mods.Hidden) ||
                                   ModUtils.CheckActive(EnabledMods, Mods.Flashlight)
                                ? ScoreRank.XH
                                : ScoreRank.X;
                        if (acc > 0.98)
                            return ModUtils.CheckActive(EnabledMods, Mods.Hidden) ||
                                   ModUtils.CheckActive(EnabledMods, Mods.Flashlight)
                                ? ScoreRank.SH
                                : ScoreRank.S;
                        if (acc > 0.94)
                            return ScoreRank.A;
                        if (acc > 0.9)
                            return ScoreRank.B;
                        return acc > 0.85 ? ScoreRank.C : ScoreRank.D;

                    case PlayModes.OsuMania:
                        if (acc == 1)
                            return ModUtils.CheckActive(EnabledMods, Mods.Hidden | Mods.Flashlight | Mods.FadeIn) ? ScoreRank.XH : ScoreRank.X;
                        if (acc > 0.95)
                            return ModUtils.CheckActive(EnabledMods, Mods.Hidden | Mods.Flashlight | Mods.FadeIn) ? ScoreRank.SH : ScoreRank.S;
                        if (acc > 0.9)
                            return ScoreRank.A;
                        if (acc > 0.8)
                            return ScoreRank.B;
                        if (acc > 0.7)
                            return ScoreRank.C;
                        return ScoreRank.D;
                }
            }
        }

        public Mods EnabledMods;
        public APIBeatmap Beatmap;

        public string PlayerName;
        public ushort Count300;
        public ushort Count100;
        public ushort Count50;

        public ushort CountGeki;
        public ushort CountKatu;
        public ushort CountMiss;
        public string BeatmapChecksum;
        public PlayModes PlayMode;

        public override string ToString()
        {
            string str = $"{Beatmap} {Accuracy:P} {Rank}";
            string mods = ModUtils.Format(EnabledMods, true, true, false);

            str += $" +{mods}";

            if (CountMiss == 1)
            {
                str += " 1miss";
            }
            else if (Count100 == 1)
            {
                str += " 1x100";
            }
            if (Perfect)
            {
                str += " FC";
            }
            return str;
        }

        public static Score ReadFromReplay(string file)
        {
            using var sr = new SerializationReader(File.OpenRead(file));
            var score = new Score();
            score.PlayMode = (PlayModes)sr.ReadByte();
            sr.ReadInt32(); // Version
            score.BeatmapChecksum = sr.ReadString();
            score.PlayerName = sr.ReadString();
            sr.ReadString(); // localScoreChecksum
            score.Count300 = sr.ReadUInt16();
            score.Count100 = sr.ReadUInt16();
            score.Count50 = sr.ReadUInt16();
            score.CountGeki = sr.ReadUInt16();
            score.CountKatu = sr.ReadUInt16();
            score.CountMiss = sr.ReadUInt16();
            score.TotalScore = sr.ReadInt32();
            score.MaxCombo = sr.ReadUInt16();
            score.Perfect = sr.ReadBoolean();
            score.EnabledMods = (Mods)sr.ReadInt32();
            sr.ReadString(); // HpGraphString
            score.Date = sr.ReadDateTime();
            return score;
        }
    }

    public enum ScoreRank
    {
        D,
        C,
        B,
        A,
        S,
        SH,
        X,
        XH,
    }

    public enum PlayModes
    {
        Osu = 0,
        Taiko = 1,
        CatchTheBeat = 2,
        OsuMania = 3
    }
}
