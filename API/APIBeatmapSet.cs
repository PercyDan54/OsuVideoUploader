// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using Newtonsoft.Json;

namespace OsuVideoUploader.API
{
    public class APIBeatmapSet
    {

        [JsonProperty(@"id")]
        public int OnlineID { get; set; }

        [JsonProperty(@"preview_url")]
        public string Preview { get; set; } = string.Empty;

        [JsonProperty(@"has_favourited")]
        public bool HasFavourited { get; set; }

        [JsonProperty(@"play_count")]
        public int PlayCount { get; set; }

        [JsonProperty(@"favourite_count")]
        public int FavouriteCount { get; set; }

        [JsonProperty(@"bpm")]
        public double BPM { get; set; }

        [JsonProperty(@"nsfw")]
        public bool HasExplicitContent { get; set; }

        [JsonProperty(@"video")]
        public bool HasVideo { get; set; }

        [JsonProperty(@"storyboard")]
        public bool HasStoryboard { get; set; }

        [JsonProperty(@"submitted_date")]
        public DateTimeOffset Submitted { get; set; }

        [JsonProperty(@"ranked_date")]
        public DateTimeOffset? Ranked { get; set; }

        [JsonProperty(@"last_updated")]
        public DateTimeOffset? LastUpdated { get; set; }

        [JsonProperty("ratings")]
        public int[] Ratings { get; set; } = Array.Empty<int>();

        [JsonProperty(@"track_id")]
        public int? TrackId { get; set; }

        public string Title { get; set; } = string.Empty;

        [JsonProperty("title_unicode")]
        public string TitleUnicode { get; set; } = string.Empty;

        public string Artist { get; set; } = string.Empty;

        [JsonProperty("artist_unicode")]
        public string ArtistUnicode { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        [JsonProperty(@"tags")]
        public string Tags { get; set; } = string.Empty;

        [JsonProperty(@"beatmaps")]
        public APIBeatmap[] Beatmaps { get; set; } = Array.Empty<APIBeatmap>();

        public override string ToString()
        {
            return $"{Artist} - {Title}";
        }
    }
}
