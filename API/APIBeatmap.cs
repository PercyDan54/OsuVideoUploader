// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using Newtonsoft.Json;

namespace OsuVideoUploader.API
{
    public class APIBeatmap : IEquatable<APIBeatmap>, IComparable<APIBeatmap>, IComparable
    {
        [JsonProperty(@"id")] public int OnlineID { get; set; }

        [JsonProperty(@"beatmapset_id")] public int OnlineBeatmapSetID { get; set; }

        [JsonProperty("checksum")] public string Checksum { get; set; } = string.Empty;

        [JsonProperty(@"user_id")] public int AuthorID { get; set; }

        [JsonProperty(@"beatmapset")] public APIBeatmapSet? BeatmapSet { get; set; }

        [JsonProperty(@"playcount")] public int PlayCount { get; set; }

        [JsonProperty(@"passcount")] public int PassCount { get; set; }

        [JsonProperty(@"mode_int")] public int RulesetID { get; set; }

        [JsonProperty(@"difficulty_rating")] public double StarRating { get; set; }

        [JsonProperty(@"drain")] public float DrainRate { get; set; }

        [JsonProperty(@"cs")] public float CircleSize { get; set; }

        [JsonProperty(@"ar")] public float ApproachRate { get; set; }

        [JsonProperty(@"accuracy")] public float OverallDifficulty { get; set; }

        [JsonProperty(@"total_length")]
        public double Length { get; set; }

        [JsonProperty(@"count_circles")] public int CircleCount { get; set; }

        [JsonProperty(@"count_sliders")] public int SliderCount { get; set; }

        [JsonProperty(@"version")] public string DifficultyName { get; set; } = string.Empty;

        [JsonProperty(@"max_combo")] public int? MaxCombo { get; set; }

        public double BPM { get; set; }

        public override string ToString() => $"{BeatmapSet} [{DifficultyName}]";

        public bool Equals(APIBeatmap? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return OnlineID == other.OnlineID;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((APIBeatmap)obj);
        }

        public override int GetHashCode() => OnlineID.GetHashCode();

        public int CompareTo(APIBeatmap? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return OnlineID.CompareTo(other.OnlineID);
        }

        public int CompareTo(object? obj)
        {
            if (ReferenceEquals(this, obj)) return 0;
            if (ReferenceEquals(null, obj)) return 1;

            return CompareTo((APIBeatmap)obj);
        }
    }
}
