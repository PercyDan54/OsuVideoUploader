using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using OsuVideoUploader.API;
using static System.Console;

namespace OsuVideoUploader;

internal class Program
{
    public static Config Config;
    public static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
    public const string CONFIG_FILE = "config.json";

    private static ApiV2Client client;
    private static Dictionary<PlayModes, MapDifficultyRange> approachRateRanges = new();
    private static Dictionary<PlayModes, MapDifficultyRange> overallDifficultyRanges = new();

    public static void Main(string[] args)
    {
        if (!InitConfig())
        {
            WriteError("配置文件不存在，已经创建默认配置");
            pause();
            return;
        }

        string file = string.Empty;
        if (args.Length == 0)
        {
            Write("输入rep路径：");
            file = ReadLine()?.Trim('"');
            WriteLine();
        }
        else
        {
            foreach (var str in args)
            {
                file += " " + str;
            }

            file = file.TrimStart();
        }

        WriteLine($"回放文件： {file}");
        WriteLine();

        if (!File.Exists(file))
        {
            WriteError("回放文件不存在");
            pause();
            return;
        }

        approachRateRanges.Add(PlayModes.Osu, new MapDifficultyRange(1800, 1200, 450));
        overallDifficultyRanges.Add(PlayModes.Osu, new MapDifficultyRange(80, 50, 20));
        overallDifficultyRanges.Add(PlayModes.Taiko, new MapDifficultyRange(50, 35, 20));

        WriteLine("正在获取 osu! API Access Token");
        client = new ApiV2Client();
        var score = Score.ReadFromReplay(file);
        var beatmap = client.GetBeatmap(score.BeatmapChecksum);

        if (beatmap == null)
        {
            WriteError("获取谱面信息失败");
        }
        else
        {
            if (ModUtils.CheckActive(score.EnabledMods, Mods.HardRock))
            {
                beatmap.CircleSize = Math.Min(beatmap.CircleSize * 1.3f, 10);
                beatmap.ApproachRate = Math.Min(beatmap.ApproachRate * 1.4f, 10);
                beatmap.OverallDifficulty = Math.Min(beatmap.OverallDifficulty * 1.4f, 10);
                beatmap.DrainRate = Math.Min(beatmap.DrainRate * 1.4f, 10);
            }
            if (ModUtils.CheckActive(score.EnabledMods, Mods.Easy))
            {
                beatmap.CircleSize /= 2;
                beatmap.ApproachRate /= 2;
                beatmap.OverallDifficulty /= 2;
                beatmap.DrainRate /= 2;
            }

            PlayModes mode = score.PlayMode;
            MapDifficultyRange difficultyRange;

            if (ModUtils.CheckActive(score.EnabledMods, Mods.DoubleTime))
            {
                if (approachRateRanges.TryGetValue(mode, out difficultyRange))
                {
                    beatmap.ApproachRate = MathF.Round(difficultyRange.DifficultyFor((int)difficultyRange.ValueFor(beatmap.ApproachRate) / 0.75f), 2);
                }

                if (overallDifficultyRanges.TryGetValue(mode, out difficultyRange))
                {
                    beatmap.OverallDifficulty = MathF.Round(difficultyRange.DifficultyFor((int)difficultyRange.ValueFor(beatmap.OverallDifficulty) / 0.75f), 2);
                }
            }
            if (ModUtils.CheckActive(score.EnabledMods, Mods.HalfTime))
            {
                if (approachRateRanges.TryGetValue(mode, out difficultyRange))
                {
                    beatmap.ApproachRate = MathF.Round(difficultyRange.DifficultyFor((int)difficultyRange.ValueFor(beatmap.ApproachRate) / 0.75f), 2);
                }

                if (overallDifficultyRanges.TryGetValue(mode, out difficultyRange))
                {
                    beatmap.OverallDifficulty = MathF.Round(difficultyRange.DifficultyFor((int)difficultyRange.ValueFor(beatmap.OverallDifficulty) / 0.75f), 2);
                }
            }

            score.Beatmap = beatmap;
        }

        WriteLine();
        WriteLine($"读取分数: {score}");
        WriteLine();
        var user = client.GetUser(score.PlayerName);

        string outFileName = Path.GetRandomFileName();

        try
        {
            string danserCommand = $"-r=\"{file}\" -out={outFileName} {Config.DanserArgs}";
            string danserPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.DanserPath);

            WriteLine($"运行danser： {danserCommand}");
            WriteLine();

            var p = Process.Start(new ProcessStartInfo
            {
                FileName = danserPath,
                Arguments = danserCommand,
            });
            p.WaitForExit();

            string danserDir = Path.GetFullPath(Config.DanserPath).Replace(Path.GetFileName(Config.DanserPath), string.Empty);
            string videoPath = Path.Combine(danserDir, "videos", outFileName + ".mp4");

            danserCommand = $"-r=\"{file}\" -ss={beatmap?.Length + 5} -out={outFileName} {Config.DanserArgs}";
            WriteLine($"运行danser： {danserCommand}");
            WriteLine();
            p = Process.Start(new ProcessStartInfo
            {
                FileName = danserPath,
                Arguments = danserCommand,
            });
            p.WaitForExit();

            string coverPath = Path.Combine(danserDir, "screenshots", outFileName + ".png");

            if (!File.Exists(videoPath))
            {
                WriteError("录制视频文件不存在");
                pause();
                return;
            }
            if (!File.Exists(coverPath))
            {
                WriteError("截图文件不存在");
                WriteLine();
                coverPath = string.Empty;
            }

            var stats = user.Statistics;
            var playTime = TimeSpan.FromSeconds(stats.PlayTime ?? 0);
            string playTimeText = $"{playTime.Days:N0}d {playTime.Hours}h {playTime.Minutes}m";
            int prevNameCount = user.PreviousUsernames.Length;
            string previousUsernames = prevNameCount > 0 ? "曾用名: " : string.Empty;
            for (int i = 0; i < prevNameCount; i++)
            {
                previousUsernames += user.PreviousUsernames[i] + (i == prevNameCount - 1 ? Environment.NewLine : ", ");
            }

            string title = score.ToStringDetails();

            if (title.Length > 80)
            {
                if (beatmap != null)
                {
                    string diffNameMods = $"[{beatmap.DifficultyName}] {ModUtils.Format(score.EnabledMods)}";
                    string truncateTitle = truncate(beatmap.BeatmapSet.TitleUnicode, 79 - diffNameMods.Length);
                    title = $"{truncateTitle} {diffNameMods}";
                }
            }

            string desc = $@"//Player info:
Player: {user}
Profile: https://osu.ppy.sh/users/{user.Id}
{previousUsernames}游戏时间: {playTimeText}
准确率: {stats.Accuracy:F2}%
游戏次数: {stats.PlayCount:N0}

// Beatmap info: ";
            if (beatmap == null)
            {
                desc += "Unknown beatmap";
            }
            else
            {
                var difficulty = client.GetBeatmapAttributes(beatmap.OnlineID, "osu", (int)score.EnabledMods);
                double star = difficulty?.StarRating ?? beatmap.StarRating;

                desc += $@"{beatmap}
Beatmap link: https://osu.ppy.sh/b/{beatmap.OnlineID}
Star: {star:##.##}
AR: {beatmap.ApproachRate} CS: {beatmap.CircleSize} OD: {beatmap.OverallDifficulty} HP: {beatmap.DrainRate}";
            }

            WriteLine();
            WriteLine("运行biliup");
            WriteLine($@"
投稿信息：
文件: {videoPath}
标题: {title}
简介: {desc}
Tag: {Config.VideoTags}
");
            p = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.BiliupPath),
                ArgumentList = { "upload" , "--tid=136" , "--title", title, "--tag", Config.VideoTags, "--desc", desc, "--cover", coverPath, videoPath },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
            });
            p.WaitForExit();

            WriteLine(p.StandardOutput.ReadToEnd());
            WriteError(p.StandardError.ReadToEnd());
            pause();
        }
        catch (Exception e)
        {
            WriteError(e.ToString());
            pause();
        }

        void pause()
        {
            WriteLine("按任意键退出");
            ReadKey();
        }
    }

    private static void WriteError(string msg)
    {
        ConsoleColor color = ForegroundColor;
        ForegroundColor = ConsoleColor.Red;
        WriteLine(msg);
        ForegroundColor = color;
    }

    private static bool InitConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            WriteDefaultConfig();
            return false;
        }

        try
        {
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath));
            return true;
        }
        catch (Exception e)
        {
            WriteLine(e);
            WriteDefaultConfig();
            return false;
        }

        void WriteDefaultConfig()
        {
            Config = new Config();
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
        }
    }

    private static string truncate(string str, int length)
    {
        if (str.Length <= length)
            return str;

        ReadOnlyMemory<char> strMem = str.AsMemory();

        do
        {
            strMem = strMem[..^1];
        } while (Encoding.UTF8.GetByteCount(strMem.Span) + 1 > 128);

        return string.Create(strMem.Length + 1, strMem, (span, mem) =>
        {
            mem.Span.CopyTo(span);
            span[^1] = '…';
        });
    }
}

[JsonObject]
class Config
{
    public string BiliupPath = "biliup";
    public string VideoTags = "osu, 萌新";
    public string DanserPath = string.Empty;
    public string DanserArgs = string.Empty;
}
