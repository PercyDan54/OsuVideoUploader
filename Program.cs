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

    public static void Main(string[] args)
    {
        if (!InitConfig())
        {
            WriteError("配置文件不存在，已经创建默认配置，按任意键退出");
            pause();
            return;
        }

        if (args.Length == 0)
        {
            WriteError("未指定回放文件");
            pause();
            return;
        }

        string file = string.Empty;

        foreach (var str in args)
        {
            file += " " + str;
        }

        file = file.TrimStart();

        WriteLine($"回放文件： {file}");
        WriteLine();

        if (!File.Exists(file))
        {
            WriteError("回放文件不存在");
            pause();
            return;
        }

        WriteLine("正在获取 Access Token");
        client = new ApiV2Client();
        var score = Score.ReadFromReplay(file);
        var beatmap = client.GetBeatmap(score.BeatmapChecksum);

        score.Beatmap = beatmap;
        if (beatmap == null)
        {
            WriteError("获取谱面信息失败");
        }
        else
        {
            if (ModUtils.CheckActive(score.EnabledMods, Mods.HardRock))
            {
                score.Beatmap.CircleSize = Math.Min(score.Beatmap.CircleSize * 1.3f, 10);
                score.Beatmap.ApproachRate = Math.Min(score.Beatmap.ApproachRate * 1.4f, 10);
                score.Beatmap.OverallDifficulty = Math.Min(score.Beatmap.OverallDifficulty * 1.4f, 10);
                score.Beatmap.DrainRate = Math.Min(score.Beatmap.DrainRate * 1.4f, 10);

            }
            if (ModUtils.CheckActive(score.EnabledMods, Mods.Easy))
            {
                score.Beatmap.CircleSize /= 2;
                score.Beatmap.ApproachRate /= 2;
                score.Beatmap.OverallDifficulty /= 2;
                score.Beatmap.DrainRate /= 2;
            }
        }

        var user = client.GetUser(score.PlayerName);
        WriteLine();
        WriteLine($"读取分数: {score}");
        WriteLine();

        string outFileName = Path.GetRandomFileName();

        try
        {
            string danserCommand = $"-r=\"{file}\" -record -out={outFileName} {Config.DanserArgs}";
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

            danserCommand = $"-r=\"{file}\" -ss={score.Beatmap?.Length + 5} -out={outFileName} {Config.DanserArgs}";
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

            string desc = $@"//Player info:
Player: {user}
Profile: https://osu.ppy.sh/users/{user.Id}
{previousUsernames}游戏时间: {playTimeText}
准确率: {stats.Accuracy:F2}%
Ranked 谱面总分: {stats.RankedScore:N0}
总分: {stats.TotalScore:N0}
游戏次数: {stats.PlayCount:N0}
Total Hits: {stats.TotalHits:N0}
pc/tth: {stats.TotalHits / (double)stats.PlayCount:F2}
最大连击: {stats.MaxCombo:N0}
回放被观看次数: {stats.ReplaysWatched}

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
标题: {score}
简介: {desc}
Tag: {Config.VideoTags}
");
            p = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.BiliupPath),
                ArgumentList = { "upload" , "--tid=136" , "--title", score.ToString(), "--tag", Config.VideoTags, "--desc", desc, "--cover", coverPath, videoPath },
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
}

[JsonObject]
class Config
{
    public string BiliupPath = "biliup";
    public string VideoTags = "osu, 萌新";
    public string DanserPath = string.Empty;
    public string DanserArgs = string.Empty;
}
