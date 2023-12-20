using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using OsuVideoUploader.API;
using static System.Console;

namespace OsuVideoUploader
{
    internal class Program
    {
        public static Config Config;
        public const string CONFIG_FILE = "config.json";
        public const string TOKEN_FILE = "token.json";
        public static AccessTokenResponse AccessToken;
        public static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
        public static string TokenPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TOKEN_FILE);

        private static ApiV2Client client;

        private static readonly Dictionary<PlayModes, MapDifficultyRange> approachRateRanges = new()
        {
            { PlayModes.Osu, new MapDifficultyRange(1800, 1200, 450) }
        };

        private static readonly Dictionary<PlayModes, MapDifficultyRange> overallDifficultyRanges = new()
        {
            { PlayModes.Osu, new MapDifficultyRange(80, 50, 20) },
            { PlayModes.Taiko, new MapDifficultyRange(50, 35, 20) }
        };

        public static void Main(string[] args)
        {
            if (!initConfig())
            {
                WriteError("配置文件不存在，已经创建默认配置");
                pause();
            }

            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
            string file = string.Empty;

            bool noUpload = false;
            bool noRecord = false;
            bool hasFile = false;

            foreach (string arg in args)
            {
                if (arg.StartsWith("--"))
                {
                    switch (arg.Substring(2))
                    {
                        case "test":
                            noUpload = true;
                            noRecord = true;
                            break;
                        case "no-upload":
                            noUpload = true;
                            break;
                    }
                }
                else
                {
                    hasFile = true;
                    file = arg;
                }
            }

            if (!hasFile)
            {
                Write("输入回放文件路径：");
                file = ReadLine()?.Trim('"');
                WriteLine();
            }

            file = file.TrimStart();

            WriteLine($"回放文件： {file}");
            WriteLine();

            if (!File.Exists(file))
            {
                WriteError("回放文件不存在");
                pause();
            }

            try
            {
                client = new ApiV2Client(AccessToken);
            }
            catch (Exception e)
            {
                WriteError(e);
                pause();
            }

            var score = Score.ReadFromReplay(file);
            var beatmap = client.GetBeatmap(score.BeatmapChecksum);
            var mode = score.PlayMode;
            bool doubleTime = score.EnabledMods.CheckActive(Mods.DoubleTime);
            bool halfTime = score.EnabledMods.CheckActive(Mods.HalfTime);

            if (beatmap == null)
            {
                WriteError("获取谱面信息失败");
            }
            else
            {
                if (score.EnabledMods.CheckActive(Mods.HardRock))
                {
                    beatmap.CircleSize = Math.Min(beatmap.CircleSize * 1.3f, 10);
                    beatmap.ApproachRate = Math.Min(beatmap.ApproachRate * 1.4f, 10);
                    beatmap.OverallDifficulty = Math.Min(beatmap.OverallDifficulty * 1.4f, 10);
                    beatmap.DrainRate = Math.Min(beatmap.DrainRate * 1.4f, 10);
                }

                if (score.EnabledMods.CheckActive(Mods.Easy))
                {
                    beatmap.CircleSize /= 2;
                    beatmap.ApproachRate /= 2;
                    beatmap.OverallDifficulty /= 2;
                    beatmap.DrainRate /= 2;
                }

                MapDifficultyRange difficultyRange;

                if (doubleTime)
                {
                    if (approachRateRanges.TryGetValue(mode, out difficultyRange))
                    {
                        beatmap.ApproachRate = MathF.Round(difficultyRange.DifficultyFor((int)difficultyRange.ValueFor(beatmap.ApproachRate) / 1.5f), 2);
                    }

                    if (overallDifficultyRanges.TryGetValue(mode, out difficultyRange))
                    {
                        beatmap.OverallDifficulty = MathF.Round(difficultyRange.DifficultyFor((int)difficultyRange.ValueFor(beatmap.OverallDifficulty) / 1.5f), 2);
                    }

                    beatmap.BPM *= 1.5;
                    beatmap.Length /= 1.5;
                }

                if (halfTime)
                {
                    if (approachRateRanges.TryGetValue(mode, out difficultyRange))
                    {
                        beatmap.ApproachRate = MathF.Round(difficultyRange.DifficultyFor((int)difficultyRange.ValueFor(beatmap.ApproachRate) / 0.75f), 2);
                    }

                    if (overallDifficultyRanges.TryGetValue(mode, out difficultyRange))
                    {
                        beatmap.OverallDifficulty = MathF.Round(difficultyRange.DifficultyFor((int)difficultyRange.ValueFor(beatmap.OverallDifficulty) / 0.75f), 2);
                    }

                    beatmap.BPM *= 0.75;
                    beatmap.Length /= 0.75;
                }

                score.Beatmap = beatmap;
            }

            WriteLine();
            WriteLine($"读取分数: {score}");
            WriteLine();
            string apiMode = toApiMode(mode);
            var user = client.GetUser(score.PlayerName, apiMode);
            if (user == null)
            {
                WriteError("获取用户信息失败");
            }

            string outFileName = Path.GetRandomFileName();

            try
            {
                string videoPath = string.Empty;
                string coverPath = string.Empty;
                string[] danserLog = { string.Empty };
                Process p;

                bool runDanser = mode == PlayModes.Osu;
                if (!noRecord)
                {
                    if (runDanser)
                    {
                        string danserCommand = $"-r=\"{file}\" -out={outFileName} {Config.DanserArgs}";
                        string danserPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.DanserPath);
                        string danserLogPath = Path.Combine(Path.GetDirectoryName(danserPath), "danser.log");

                        WriteLine($"运行danser： {danserCommand}");
                        WriteLine();

                        p = Process.Start(new ProcessStartInfo
                        {
                            FileName = danserPath,
                            Arguments = danserCommand
                        });
                        p.WaitForExit();
                        if (File.Exists(danserLogPath))
                        {
                            danserLog = File.ReadAllLines(danserLogPath);
                        }

                        string danserDir = Path.GetFullPath(Config.DanserPath).Replace(Path.GetFileName(Config.DanserPath), string.Empty);
                        videoPath = Path.Combine(danserDir, "videos", outFileName + ".mp4");

                        danserCommand = $"-r=\"{file}\" -ss={beatmap?.Length + 5} -out={outFileName} {Config.DanserScreenshotArgs}";
                        WriteLine($"运行danser： {danserCommand}");
                        WriteLine();
                        p = Process.Start(new ProcessStartInfo
                        {
                            FileName = danserPath,
                            Arguments = danserCommand
                        });
                        p?.WaitForExit();

                        coverPath = Path.Combine(danserDir, "screenshots", outFileName + ".png");

                        if (!File.Exists(videoPath))
                        {
                            WriteError("录制视频文件不存在");
                            pause();
                        }

                        if (!File.Exists(coverPath))
                        {
                            WriteError("截图文件不存在");
                            WriteLine();
                            coverPath = string.Empty;
                        }
                    }
                    else
                    {
                        WriteError($"danser不支持该回放文件的模式 {mode}，请手动选择上传视频");
                        Write("输入视频文件路径：");
                        videoPath = ReadLine()?.Trim('"');
                        Write("输入视频封面路径（留空为不传）：");
                        coverPath = ReadLine()?.Trim('"');
                    }
                }

                string title = score.ToStringDetails();
                string mods = ModUtils.Format(score.EnabledMods, showEmpty: true);

                if (title.Length > 80)
                {
                    if (beatmap != null)
                    {
                        string diffNameMods = $"[{beatmap.DifficultyName}] {ModUtils.Format(score.EnabledMods)}";
                        string truncateTitle = truncate(beatmap.BeatmapSet.Title, 79 - diffNameMods.Length);
                        title = $"{truncateTitle} {diffNameMods}";
                    }
                }

                string desc = @"// Player info:
";
                if (user != null)
                {
                    var stats = user.Statistics;
                    var playTime = TimeSpan.FromSeconds(stats.PlayTime ?? 0);
                    string playTimeText = $"{playTime.Days:N0}d {playTime.Hours}h {playTime.Minutes}m";

                    desc += $@"{user}
Profile: https://osu.ppy.sh/u/{user.Id}
游戏时间: {playTimeText}
准确率: {stats.Accuracy:F2}%
游戏次数: {stats.PlayCount:N0}

";
                }
                else
                {
                    desc += $"Unknown player: {score.PlayerName}\n";
                }

                desc += "// Beatmap info:\n";

                const string date_pattern = @"(^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2})\s+";
                const string common_pattern = date_pattern + "(.+)";
                const string result_pattern = @"\|\s+(\d{1,2})\s+\|\s+?([A-Za-z0-9-\[\]_ ]+)\W+\|\s+([0-9,]+)\s+\|\s+(\d+\.\d+)\s+\|\s+([A-Z]{1,2})\s+\|\s+([0-9,]+)\s+\|\s+([0-9,]+)\s+\|\s+([0-9,]+)\s+\|\s+([0-9,]+)\s+\|\s+([0-9,]+)\s+\|\s+([0-9,]+)\s+\|\s+((?:[A-Z]+)?)\s+\|\s+(\d+\.\d+)\s+\|";

                string beatmapName = beatmap?.ToString();
                string pp = string.Empty;
                int? maxCombo = null;
                string maxComboText = string.Empty;
                double star = -1;

                foreach (string line in danserLog)
                {
                    var matches = Regex.Matches(line, common_pattern);
                    if (matches.Count > 0)
                    {
                        string log = matches[0].Groups[2].Value;
                        if (log.StartsWith("Playing"))
                        {
                            beatmapName = log.Replace("Playing: ", string.Empty);
                        }
                        else if (log.StartsWith("\tTotal") && star < 0)
                        {
                            string sr = log.Replace("\tTotal: ", string.Empty);
                            double.TryParse(sr, out star);
                        }
                        else
                        {
                            matches = Regex.Matches(log, result_pattern);
                            if (matches.Count > 0)
                            {
                                pp = matches[0].Groups[13].Value;
                            }
                        }
                    }
                }

                if (beatmap == null)
                {
                    desc += "Unknown beatmap";

                    if (!string.IsNullOrEmpty(beatmapName))
                    {
                        desc += ": " + beatmapName;
                    }

                    desc += "\n";
                    if (star > 0)
                    {
                        desc += $"Star: {star:0.##}\n";
                    }
                }
                else
                {
                    var difficultyAttributes = client.GetBeatmapAttributes(beatmap.OnlineID, apiMode, (int)score.EnabledMods);
                    star = difficultyAttributes?.StarRating ?? beatmap.StarRating;
                    maxCombo = beatmap.MaxCombo;

                    if (difficultyAttributes != null && difficultyAttributes.MaxCombo > 0)
                        maxCombo = difficultyAttributes.MaxCombo;

                    if (maxCombo.HasValue)
                        maxComboText = $" / {maxCombo}x";

                    desc += $@"{beatmapName}
Link: https://osu.ppy.sh/b/{beatmap.OnlineID}{(beatmap.RulesetID == (int)mode ? string.Empty : $"?mode={apiMode}")}
Star: {star:0.##} {(score.EnabledMods.CheckActive(Mods.DifficultyAdjustMods) ? $"({mods})" : string.Empty)}
Length: {TimeSpanUtil.FormatTime(TimeSpan.FromSeconds(beatmap.Length))}
BPM: {beatmap.BPM:0.##}
AR: {beatmap.ApproachRate:0.##} CS: {beatmap.CircleSize:0.##} OD: {beatmap.OverallDifficulty:0.##} HP: {beatmap.DrainRate:0.##}

";
                }
                desc += $@"// Score info:
Played by {score.PlayerName} on {score.Date.ToLocalTime()}{(string.IsNullOrEmpty(pp) ? string.Empty : $"\nPP: {pp}")}
Mods: {mods}
Accuracy: {score.Accuracy:P2}
300: {score.Count300}, 100: {score.Count100}, 50: {score.Count50}, Miss: {score.CountMiss}
Combo: {score.MaxCombo}x{maxComboText}";

                WriteLine();
                WriteLine("运行biliup");
                WriteLine($@"
投稿信息：
文件: {videoPath}
标题: {title}
简介: {desc}
Tag: {Config.VideoTags}
");

                if (!noUpload)
                {
                    p = Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.BiliupPath),
                        ArgumentList = { "upload", "--tid=136", "--title", title, "--tag", Config.VideoTags, "--desc", desc, "--cover", coverPath, videoPath },
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                        StandardErrorEncoding = Encoding.UTF8,
                        StandardOutputEncoding = Encoding.UTF8
                    });
                    p.WaitForExit();

                    WriteLine(p.StandardOutput.ReadToEnd());
                    WriteError(p.StandardError.ReadToEnd());
                }

                if (runDanser)
                {
                    if (Config.RemoveCover)
                    {
                        TryDelete(coverPath);
                    }

                    if (Config.RemoveVideo)
                    {
                        TryDelete(videoPath);
                    }
                }

                pause();
            }
            catch (Exception e)
            {
                WriteError(e);
                pause();
            }

            void pause()
            {
                WriteLine("按任意键退出");
                ReadKey();
                Environment.Exit(0);
            }
        }

        public static void WriteError(object obj)
        {
            var color = ForegroundColor;
            ForegroundColor = ConsoleColor.Red;
            Error.WriteLine(obj);
            ForegroundColor = color;
        }

        public static void TryDelete(string file)
        {
            if (!File.Exists(file))
            {
                return;
            }

            try
            {
                File.Delete(file);
            }
            catch (Exception e)
            {
                WriteError(e);
            }
        }

        private static bool initConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                writeDefaultConfig();
                return false;
            }

            if (!File.Exists(TokenPath))
            {
                AccessToken = ApiV2Client.GetAccessToken();
                File.WriteAllText(TokenPath, JsonConvert.SerializeObject(AccessToken));
            }
            else
            {
                AccessToken = JsonConvert.DeserializeObject<AccessTokenResponse>(File.ReadAllText(TokenPath));
                if (AccessToken != null && AccessToken.Time.Add(TimeSpan.FromSeconds(AccessToken.ExpiresIn)) < DateTimeOffset.UtcNow)
                {
                    WriteError("Access Token 已失效");
                    AccessToken = ApiV2Client.GetAccessToken();
                    File.WriteAllText(TokenPath, JsonConvert.SerializeObject(AccessToken));
                }
            }

            try
            {
                Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath));
                return true;
            }
            catch (Exception e)
            {
                WriteLine(e);
                writeDefaultConfig();
                return false;
            }

            void writeDefaultConfig()
            {
                Config = new Config();
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
            }
        }

        private static string truncate(string str, int length)
        {
            if (str.Length <= length)
            {
                return str;
            }

            int prefixLength = length - 3 - str.Split(' ')[^1].Length;
            int suffixLength = length - 3 - prefixLength;

            string prefix = str.Substring(0, prefixLength);
            string suffix = str.Substring(str.Length - suffixLength);

            return prefix + "..." + suffix;
        }

        private static string toApiMode(PlayModes mode)
        {
            return mode switch
            {
                PlayModes.Osu => "osu",
                PlayModes.Taiko => "taiko",
                PlayModes.CatchTheBeat => "fruits",
                PlayModes.OsuMania => "mania",
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };
        }
    }

    [JsonObject]
    public class Config
    {
        public string BiliupPath = "biliup";
        public string VideoTags = "osu, 萌新";
        public string DanserPath = string.Empty;
        public string DanserArgs = string.Empty;
        public string DanserScreenshotArgs = string.Empty;
        public bool RemoveVideo;
        public bool RemoveCover;
    }
}
