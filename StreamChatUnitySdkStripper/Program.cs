using System.Diagnostics;
using System.Reflection;
using StreamChatUnitySdkStripper;

const string GhTokenFilename = "private_gh_token.txt";

// TODO: move to config json file
var config = new UnitySdkUnityEngineStripper.Config
{
    RootDirName = "StreamChat",
    LibsDirName = "Libs",
    RootEssentialDirs = new[] { "Core", "Libs" },
    RootEssentialFiles = new[] { "changelog", "readme" },
    LibsMode = LibsMode.Console,
    LibsModeToSourcePathMapping = new Dictionary<LibsMode, string>
    {
        { LibsMode.Console, "console/Libs" }
    },
    VersionRegexPattern = @"SDKVersion\s+=\s+new\s+Version\(\s*([0-9]+),\s*([0-9]+),\s*([0-9]+)\)\s*;",
    PackageNameTemplate = "stream-chat-dotnet-sdk-{0}.{1}.{2}",
    VersionFilename = "StreamChatLowLevelClient.cs",

    GitHubLibsRepoOwner = "sierpinskid",
    GitHubLibsRepoName = "stream-chat-sdk-dotnet-dependencies-library",

    GitHubStreamUnitySdkRepoOwner = "GetStream",
    GitHubStreamUnitySdkRepoName = "stream-chat-unity",
    GitHubStreamUnitySdkRootDir = "Assets/Plugins/StreamChat/",
    GitHubStreamUnitySdkRepoDefaultBranchName = "develop"
};

Console.WriteLine("This tool will strip all of the Unity Engine dependencies from the Stream Unity SDK.");
Console.WriteLine("Please provide the following arguments.");

var ghTokenFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), GhTokenFilename);

Logger.Info($"Try load GH token from `{ghTokenFilePath}`");

var ghTokenFile = File.ReadAllLines(ghTokenFilePath).Select(l => l.Trim())
    .FirstOrDefault(l => !l.StartsWith("//") && l.Length > 0);
if (string.IsNullOrEmpty(ghTokenFile))
{
    Logger.Error(
        $"Failed to find `{GhTokenFilename}` file. Please create this file with the GitHub access token. Keep this file on local machine only (add it to .gitignore).");
}

config.GitHubToken = ghTokenFile;

var stopwatch = new Stopwatch();
stopwatch.Start();

var targetPath = ReadTargetPathArg();

var stripper = new UnitySdkUnityEngineStripper();
await stripper.ExecuteAsync(targetPath, config);

stopwatch.Stop();
var ts = stopwatch.Elapsed;
Console.WriteLine($"{Math.Floor(ts.TotalMinutes)}:{ts.ToString("ss\\.ff")}");

string ReadTargetPathArg()
{
    while (true)
    {
        Console.WriteLine("Provide target path where the stripped SDK will be saved:");
        var pathArg = Console.ReadLine();

        try
        {
            var path = Path.GetFullPath(pathArg);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
        catch (Exception e)
        {
            Logger.Error($"Provided path `{pathArg}` is invalid and failed with an exception: {e.Message}");
        }
    }
}