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
        }

        if (args.Length == 0)
        {
            WriteError("未指定回放文件");
            pause();
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

        var outFileName = Path.GetRandomFileName();

        try
        {
            var danserCommand = $"-r=\"{file}\" -out={outFileName} {Config.DanserArgs}";

            WriteLine($"运行danser： {danserCommand}");
            WriteLine();
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = Config.DanserPath,
                Arguments = danserCommand,
            });
            p.WaitForExit();
            outFileName = Path.Combine(Path.GetFullPath(Config.DanserPath).Replace(Path.GetFileName(Config.DanserPath), ""), "videos", outFileName + ".mp4");
            
            if (!File.Exists(outFileName))
            {
                WriteError("录制视频文件不存在");
                pause();
                return;
            }

            string desc = $@"
//Player info:
Player: {user}
Profile: http://osu.ppy.sh/users/{user.Id}

// Beatmap info:
";
            if (beatmap == null)
            {
                desc += "Unknown beatmap";
            }
            else
            {
                var difficulty = client.GetBeatmapAttributes(beatmap.OnlineID, "osu", (int)score.EnabledMods);
                double star = difficulty?.StarRating ?? score.Beatmap.StarRating;

                desc += @$"{beatmap}
Beatmap link: http://osu.ppy.sh/b/{beatmap.OnlineID}
Star: {star:##.##}
AR: {beatmap.ApproachRate} CS: {beatmap.CircleSize} OD: {beatmap.OverallDifficulty} HP: {beatmap.DrainRate}";
            }

            WriteLine();
            WriteLine("运行biliup");
            WriteLine();
            p = Process.Start(new ProcessStartInfo
            {
                FileName = Config.BiliupPath,
                ArgumentList = { "upload" , "--tid=136" , "--title", score.ToString(), "--tag", Config.VideoTags, "--desc", desc, outFileName },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
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
