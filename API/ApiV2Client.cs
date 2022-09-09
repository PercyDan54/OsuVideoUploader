using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace OsuVideoUploader.API
{
    public class ApiV2Client
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string accessToken;

        public ApiV2Client(AccessTokenResponse token)
        {
            accessToken = token.AccessToken;
        }

        public static AccessTokenResponse GetAccessToken()
        {
            var data = new Dictionary<string, string>
            {
                // From https://github.com/ppy/osu/blob/master/osu.Game/Online/ProductionEndpointConfiguration.cs
                { "client_id", "5" },
                { "client_secret", "FGc9GAtyHzeQDshWP5Ah7dega8hJACAJpQtw6OXk" },
                { "grant_type", "client_credentials" },
                { "scope", "public" }
            };
            var req = new HttpRequestMessage(HttpMethod.Post, "https://osu.ppy.sh/oauth/token");
            req.Content = new FormUrlEncodedContent(data);
            Console.WriteLine("正在获取Access Token... 在这里卡超过一分钟建议重启本程序");
            var resp = client.Send(req);

            if (resp.IsSuccessStatusCode)
            {
                string str = resp.Content.ReadAsStringAsync().Result;
                var accessTokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(str);
                accessTokenResponse.Time = DateTimeOffset.UtcNow;
                return accessTokenResponse;
            }

            return null;
        }

        public APIBeatmap GetBeatmap(string md5)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/beatmaps/lookup?checksum={md5}");
            req.Headers.Add("Authorization", $"Bearer {accessToken}");
            req.Headers.Add("Accept", "application/json");
            var resp = client.Send(req);

            if (resp.IsSuccessStatusCode)
            {
                string str = resp.Content.ReadAsStringAsync().Result;
                var beatmap = JsonConvert.DeserializeObject<APIBeatmap>(str);
                return beatmap;
            }

            return null;
        }

        public APIBeatmapDifficultyAttributes GetBeatmapAttributes(int beatmap, string mode, int mods)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"https://osu.ppy.sh/api/v2/beatmaps/{beatmap}/attributes");

            req.Headers.Add("Authorization", $"Bearer {accessToken}");
            req.Headers.Add("Accept", "application/json");

            var data = new Dictionary<string, object>
            {
                { "mods", mods },
                { "ruleset", mode },
            };
            req.Content = new StringContent(JsonConvert.SerializeObject(data));
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var resp = client.Send(req);
            if (resp.IsSuccessStatusCode)
            {
                string str = resp.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<APIBeatmapDifficultyAttributesResponse>(str)?.Attributes;
            }

            return null;
        }

        public APIUser GetUser(string user, string mode = default)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/users/{user}/{mode}");

            req.Headers.Add("Authorization", $"Bearer {accessToken}");
            req.Headers.Add("Accept", "application/json");

            var resp = client.Send(req);
            if (resp.IsSuccessStatusCode)
            {
                string str = resp.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<APIUser>(str) ?? new APIUser();
            }

            return null;
        }
    }
}
