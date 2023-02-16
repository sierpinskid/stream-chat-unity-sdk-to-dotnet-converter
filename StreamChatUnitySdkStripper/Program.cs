using System.Text;
using StreamChatUnitySdkStripper;

const string StreamChatUnitySdkRootDirName = "StreamChat";

var config = new UnitySdkUnityEngineStripper.Config
{
    RootDirName = "StreamChat",
    RootEssentialDirs = new[] { "Core", "Libs" },
    VersionRegexPattern = @"SDKVersion\s+=\s+new\s+Version\(\s*([0-9]+),\s*([0-9]+),\s*([0-9]+)\)\s*;",
    PackageNameTemplate = "DotNet-Chat-SDK-{0}.{1}.{2}",
    VersionFilename = "StreamChatLowLevelClient.cs",
};

Console.WriteLine("This tool will strip all of the Unity Engine dependencies from the Stream Unity SDK.");
Console.WriteLine("Please provide the following arguments.");

var streamChatSdkSourcePath = ReadStreamChatSourcePathArg();
var targetPath = ReadTargetPathArg();

var stripper = new UnitySdkUnityEngineStripper();
stripper.Execute(streamChatSdkSourcePath, targetPath, config);

//Todo: in next iteration we could just download it from GitHub 
string ReadStreamChatSourcePathArg()
{
    while (true)
    {
        Console.WriteLine("Provide source path of the Stream Chat Unity SDK:");
        var pathArg = Console.ReadLine();

        try
        {
            Logger.Info($"Searching for `{StreamChatUnitySdkRootDirName}`...");

            var path = Path.GetFullPath(pathArg);
            var files = Directory.GetDirectories(path, StreamChatUnitySdkRootDirName, SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                Logger.Error("Provided directory is empty");
                continue;
            }

            if (files.Length != 1)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Found multiple directories with `{StreamChatUnitySdkRootDirName}` keyword:");
                foreach (var file in files)
                {
                    sb.AppendLine(file);
                }

                sb.AppendLine("Please provide more specific path");
                Logger.Error(sb.ToString());
            }

            return files[0];
        }
        catch (Exception e)
        {
            Logger.Error($"Provided path `{pathArg}` is invalid and failed with an exception: {e.Message}");
        }
    }
}

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

            var files = Directory.GetFiles(path);
            if (files.Length == 0)
            {
                return path;
            }

            Logger.Error(
                $"Provided path `{pathArg}` is not empty. Please provide a valid empty path or remove all files and try again.");
        }
        catch (Exception e)
        {
            Logger.Error($"Provided path `{pathArg}` is invalid and failed with an exception: {e.Message}");
        }
    }
}