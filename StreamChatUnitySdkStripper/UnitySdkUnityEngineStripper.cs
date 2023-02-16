using System.Text.RegularExpressions;

namespace StreamChatUnitySdkStripper;

/// <summary>
/// Creates a copy of Unity Stream Chat SDK and strips all of the Unity Engine dependencies.
/// </summary>
public class UnitySdkUnityEngineStripper
{
    public class Config
    {
        public string RootDirName { get; init; }

        public IEnumerable<string> RootEssentialDirs { get; init; }
        public string VersionRegexPattern { get; init; }
        public string PackageNameTemplate { get; init; }
        public string VersionFilename { get; init; }
    }

    public void Execute(string sourcePath, string targetPath, Config config)
    {
        _config = config;

        Logger.Info($"Generate stripped .NET SDK from `{sourcePath}` to `{targetPath}`");

        var version = GetCurrentVersion(sourcePath);
        Logger.Info("Detected version: " + version);

        var newPackageDir = CreateNewPackageDir(targetPath, version);
        Logger.Info($"Created `{newPackageDir}` directory");

        CopyFiles(sourcePath, newPackageDir);
        Logger.Info($"Copied files from `{sourcePath}` to `{newPackageDir}`");

        RemoveNonEssentialDirectories(targetPath);
        // Remove all except Core and Libs
        // Replace libs
    }

    private Config _config;

    private static void CopyFiles(string sourcePath, string targetPath) => FileUtils.CopyFiles(sourcePath, targetPath);

    private string CreateNewPackageDir(string targetPath, Version version)
    {
        var newDirName = string.Format(_config.PackageNameTemplate, version.Major, version.Minor, version.Build);
        var fullPath = Path.Combine(targetPath, newDirName);

        Directory.CreateDirectory(fullPath);


        return fullPath;
    }

    private Version GetCurrentVersion(string sourcePath)
    {
        var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
        var streamChatClientFilePath = files.FirstOrDefault(f => Path.GetFileName(f) == _config.VersionFilename);

        if (string.IsNullOrEmpty(streamChatClientFilePath))
        {
            throw new ArgumentException(
                $"Failed to find version file `{_config.VersionFilename} in `{sourcePath}` files");
        }

        var content = File.ReadAllText(streamChatClientFilePath);

        var match = Regex.Match(content, _config.VersionRegexPattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Failed to find version line in `{streamChatClientFilePath}`");
        }

        int GetComponent(int index)
        {
            if (match.Groups.Count <= index)
            {
                throw new InvalidOperationException("Failed to find version component with index: " + index);
            }

            var value = match.Groups[index].Value;
            if (!int.TryParse(value, out var versionComponent))
            {
                throw new InvalidOperationException(
                    $"Version component with index `{index}` is not a valid integer. Received value: " + value);
            }

            return versionComponent;
        }

        return new Version(GetComponent(1), GetComponent(2), GetComponent(3));
    }

    private void RemoveNonEssentialDirectories(string path)
    {
        var directories = Directory.GetDirectories(path);

        foreach (var dir in directories)
        {
            var name = Path.GetDirectoryName(dir);

            if (!_config.RootEssentialDirs.Contains(name))
            {
                Console.WriteLine("Delete dir: " + dir);
            }
        }
    }
}