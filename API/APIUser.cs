// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace OsuVideoUploader.API
{
    [JsonObject(MemberSerialization.OptIn)]
    public class APIUser
    {
        [JsonProperty(@"id")]
        public int Id { get; set; }

        [JsonProperty(@"join_date")]
        public DateTimeOffset JoinDate;

        [JsonProperty(@"username")]
        public string Username { get; set; }

        [JsonProperty(@"previous_usernames")]
        public string[] PreviousUsernames;

        [JsonProperty(@"title")]
        public string Title;

        [JsonProperty(@"country")]
        public Country Country;

        [JsonProperty(@"location")]
        public string Location;

        [JsonProperty(@"avatar_url")]
        public string AvatarUrl;

        private UserStatistics statistics;

        /// <summary>
        /// User statistics for the requested ruleset (in the case of a <see cref="GetUserRequest"/> or <see cref="GetFriendsRequest"/> response).
        /// Otherwise empty.
        /// </summary>
        [JsonProperty(@"statistics")]
        public UserStatistics Statistics
        {
            get => statistics ??= new UserStatistics();
            set
            {
                if (statistics != null)
                    statistics = value;
            }
        }

        [JsonProperty(@"playmode")]
        public string PlayMode;

        [JsonProperty(@"profile_order")]
        public string[] ProfileOrder;

        public override string ToString() => $"{Username}[{PlayMode}] （{Statistics.PP}pp, Global #{Statistics.GlobalRank}, {Country.FullName} #{Statistics.CountryRank}）";
    }

    public class Country : IEquatable<Country>
    {
        /// <summary>
        /// The name of this country.
        /// </summary>
        [JsonProperty(@"name")]
        public string FullName;

        /// <summary>
        /// Two-letter flag acronym (ISO 3166 standard)
        /// </summary>
        [JsonProperty(@"code")]
        public string FlagName;

        public bool Equals(Country other) => FlagName == other?.FlagName;
    }

    public class UserStatistics
    {
        [JsonProperty]
        public APIUser User;

        [JsonProperty(@"level")]
        public LevelInfo Level;

        public struct LevelInfo
        {
            [JsonProperty(@"current")]
            public int Current;

            [JsonProperty(@"progress")]
            public int Progress;
        }

        [JsonProperty(@"is_ranked")]
        public bool IsRanked;

        [JsonProperty(@"global_rank")]
        public int? GlobalRank;

        [JsonProperty(@"country_rank")]
        public int? CountryRank;

        [JsonProperty(@"pp")]
        public decimal? PP;

        [JsonProperty(@"ranked_score")]
        public long RankedScore;

        [JsonProperty(@"hit_accuracy")]
        public double Accuracy;

        [JsonProperty(@"play_count")]
        public int PlayCount;

        [JsonProperty(@"play_time")]
        public int? PlayTime;

        [JsonProperty(@"total_score")]
        public long TotalScore;

        [JsonProperty(@"total_hits")]
        public int TotalHits;

        [JsonProperty(@"maximum_combo")]
        public int MaxCombo;

        [JsonProperty(@"replays_watched_by_others")]
        public int ReplaysWatched;

        [JsonProperty(@"grade_counts")]
        public Grades GradesCount;

        public struct Grades
        {
            [JsonProperty(@"ssh")]
            public int? SSPlus;

            [JsonProperty(@"ss")]
            public int SS;

            [JsonProperty(@"sh")]
            public int? SPlus;

            [JsonProperty(@"s")]
            public int S;

            [JsonProperty(@"a")]
            public int A;

            public int this[ScoreRank rank]
            {
                get
                {
                    switch (rank)
                    {
                        case ScoreRank.XH:
                            return SSPlus ?? 0;

                        case ScoreRank.X:
                            return SS;

                        case ScoreRank.SH:
                            return SPlus ?? 0;

                        case ScoreRank.S:
                            return S;

                        case ScoreRank.A:
                            return A;

                        default:
                            throw new ArgumentException($"API does not return {rank.ToString()}");
                    }
                }
            }
        }
    }
}
