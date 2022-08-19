using Newtonsoft.Json;

namespace OsuVideoUploader.API {
    public class AccessTokenResponse {
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty]
        public DateTimeOffset Time;
    }
}
