using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Octokit;

namespace StreamChatUnitySdkStripper;

/// <summary>
/// Creates a copy of Unity Stream Chat SDK and strips all of the Unity Engine dependencies.
/// </summary>
public class UnitySdkUnityEngineStripper
{
    public class Config
    {
        public string RootDirName { get; init; }

        public string LibsDirName { get; set; }
        public IEnumerable<string> RootEssentialDirs { get; init; }
        public IEnumerable<string> RootEssentialFiles { get; init; }
        public LibsMode LibsMode { get; set; }
        public Dictionary<LibsMode, string> LibsModeToSourcePathMapping { get; set; }
        public string VersionRegexPattern { get; init; }
        public string PackageNameTemplate { get; init; }
        public string VersionFilename { get; init; }

        /// <summary>
        /// Never keep this stored in a source control. Load it from a hidden resource
        /// </summary>
        public string GitHubToken { get; set; }

        public string GitHubLibsRepoOwner { get; set; }
        public string GitHubLibsRepoName { get; set; }

        public string GitHubStreamUnitySdkRepoOwner { get; set; }
        public string GitHubStreamUnitySdkRepoName { get; set; }
        public string GitHubStreamUnitySdkRepoDefaultBranchName { get; set; }
        public string GitHubStreamUnitySdkRootDir { get; set; }
    }

    public async Task ExecuteAsync(string targetParentPath, Config config)
    {
        _config = config;

        Logger.Info($"Generate stripped .NET SDK to `{targetParentPath}`");

        var tempDownloadDir = CreateTempDownloadDir(targetParentPath);

        var archiveFilePath = Path.Combine(tempDownloadDir, $"{_config.GitHubStreamUnitySdkRepoDefaultBranchName}.zip");

        await DownloadUnitySdkZippedRepositoryAsync(archiveFilePath);
        UnpackZippedRepository(archiveFilePath, tempDownloadDir);

        var version = GetCurrentVersion(tempDownloadDir);

        CreateNewPackageDir(targetParentPath, version, out var newPackagePath);

        MoveSdkFilesToPackageDir(tempDownloadDir, newPackagePath);

        RemoveAllMetaFiles(newPackagePath);

        RemoveNonEssentialRootDirectories(newPackagePath);
        RemoveNonEssentialRootFiles(newPackagePath);

        var tempLibsPath = CreateTempLibsDir(newPackagePath);
        await DownloadLibsDirectoryAsync(tempLibsPath);

        ReplaceLibsDirectory(newPackagePath, tempLibsPath);

        RemoveTempLibsDir(tempLibsPath);

        Logger.Info("Finished Successfully");
    }

    private void RemoveTempLibsDir(string path)
    {
        Directory.Delete(path, recursive: true);
        Logger.Info($"Removed temp libs dir `{path}`");
    }

    private const string TempLibsDirName = "_TEMP_LIBS";
    private const string TempDownloadDirName = "__TEMP_DOWNLOAD";
    private const string MetaExtension = ".meta";

    private Config _config;

    /// <summary>
    /// This method traverses the whole repository tree and tends to be very slow.
    /// In case of big repositories might be better to download zipped archive
    /// </summary>
    private async Task DownloadUnitySdkRepositoryAsync(string targetPath)
    {
        var downloader = new GitHubDownloader(_config.GitHubToken);
        await downloader.DownloadRepositoryAsync(_config.GitHubStreamUnitySdkRepoOwner,
            _config.GitHubStreamUnitySdkRepoName, targetPath, ShouldDownloadFile);
    }

    private async Task DownloadUnitySdkZippedRepositoryAsync(string targetPath)
    {
        var defaultBranch = _config.GitHubStreamUnitySdkRepoDefaultBranchName;
        var path = Path.Combine("https://github.com/", _config.GitHubStreamUnitySdkRepoOwner,
            _config.GitHubStreamUnitySdkRepoName);

        Logger.Info($"Start downloading zipped repository from: {path}");
        await GitHubDownloader.DownloadZippedRepositoryAsync(path, defaultBranch, targetPath);
        Logger.Info($"Download zipped repository from: {path}");
    }

    private void UnpackZippedRepository(string archivePath, string extractPath)
    {
        ZipFile.ExtractToDirectory(archivePath, extractPath);
        Logger.Info($"Unzipped `{archivePath}` to `{extractPath}`");
        File.Delete(archivePath);
        Logger.Info($"Deleted `{archivePath}`");
    }

    public void MoveSdkFilesToPackageDir(string sourcePath, string targetPath)
    {
        var sb = new StringBuilder();

        var sdkRootDir = Directory.EnumerateDirectories(sourcePath, _config.RootDirName, SearchOption.AllDirectories)
            .FirstOrDefault();
        if (sdkRootDir != null)
        {
            foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                if (!dirPath.Contains(sdkRootDir))
                {
                    sb.AppendLine($"Skipped directory: {dirPath}");
                    continue;
                }

                var dirTargetPath = dirPath.Replace(sdkRootDir, targetPath);
                Directory.CreateDirectory(dirTargetPath);
                sb.AppendLine($"Created directory: {dirTargetPath}");
            }

            foreach (var filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                if (!filePath.Contains(sdkRootDir))
                {
                    sb.AppendLine($"Skipped file: {filePath}");
                    continue;
                }
                var fileTargetPath = filePath.Replace(sdkRootDir, targetPath);

                if (Path.GetExtension(fileTargetPath) == MetaExtension)
                {
                    continue;
                }
                
                File.Move(filePath, fileTargetPath);
                sb.AppendLine($"Moved file from `{filePath}` to `{fileTargetPath}`");
            }

            Directory.Delete(sourcePath, true);
            sb.AppendLine($"Deleted `{sourcePath}` directory");
        }

        Logger.Info(sb.ToString());
    }

    private bool ShouldDownloadFile(string localFilePath, string repoFilePath)
    {
        if (Path.GetExtension(localFilePath) == MetaExtension)
        {
            return false;
        }

        var isDir = Path.GetExtension(repoFilePath) == string.Empty;
        if (isDir)
        {
            if (_config.RootEssentialDirs.All(d
                    => !repoFilePath.StartsWith(Path.Combine(d, _config.GitHubStreamUnitySdkRootDir))))
            {
                return false;
            }
        }
        else
        {
            var dirPath = Path.GetDirectoryName(localFilePath);
            if (dirPath == _config.GitHubStreamUnitySdkRootDir)
            {
                if (_config.RootEssentialFiles.All(f => !repoFilePath.Contains(f)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void CreateNewPackageDir(string targetPath, Version version, out string newPackagePath)
    {
        var newDirName = string.Format(_config.PackageNameTemplate, version.Major, version.Minor, version.Build);
        var fullPath = Path.Combine(targetPath, newDirName);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        var directory = new DirectoryInfo(fullPath);
        var directories = directory.GetDirectories();
        var files = directory.GetFiles();
        if (directories.Any() || files.Any())
        {
            Logger.Warning("Folder is not empty. Contents:");
            foreach (var dir in directories)
            {
                Logger.Warning($"Directory: {dir.Name}");
            }

            foreach (var file in files)
            {
                Logger.Warning($"File: {file.Name}");
            }

            Logger.Error($"DELETE `{fullPath}` directory contents recursively? Type `k` to CONFIRM and DELETE");
            var confirm = Console.ReadLine();
            if (confirm?.ToLower().First() == 'k')
            {
                Directory.Delete(fullPath, true);
                Directory.CreateDirectory(fullPath);
            }
        }

        Logger.Info($"Created `{fullPath}` directory");

        newPackagePath = fullPath;
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

        var version = new Version(GetComponent(1), GetComponent(2), GetComponent(3));
        Logger.Info("Detected version: " + version);
        return version;
    }

    private void RemoveNonEssentialRootDirectories(string path)
    {
        var directory = new DirectoryInfo(path);
        var dirs = directory.GetDirectories();

        foreach (var dir in dirs)
        {
            var name = dir.Name;
            var toDelete = !_config.RootEssentialDirs.Contains(name);
            if (toDelete)
            {
                dir.Delete(recursive: true);
            }

            var prefix = toDelete ? "DELETED" : "SKIPPED";
            Console.WriteLine($"{prefix} directory `{dir}` ");
        }

        Logger.Info($"Removed non-essential directories from `{path}`");
    }

    private void RemoveNonEssentialRootFiles(string path)
    {
        var files = Directory.GetFiles(path);

        foreach (var file in files)
        {
            var name = new FileInfo(file).Name.ToLower();
            if (_config.RootEssentialFiles.Any(name.Contains))
            {
                continue;
            }

            File.Delete(file);
            Logger.Info($"DELETED file `{file}`");
        }
    }

    private string CreateTempLibsDir(string targetPath)
    {
        var tempLibsPath = Path.Combine(targetPath, TempLibsDirName);
        Directory.CreateDirectory(tempLibsPath);
        return tempLibsPath;
    }

    private string CreateTempDownloadDir(string targetPath)
    {
        var tempDownloadPath = Path.Combine(targetPath, TempDownloadDirName);

        if (Directory.Exists(tempDownloadPath))
        {
            Directory.Delete(tempDownloadPath, recursive: true);
        }

        Directory.CreateDirectory(tempDownloadPath);
        return tempDownloadPath;
    }

    private async Task DownloadLibsDirectoryAsync(string targetPath)
    {
        Logger.Info($"Download GH repository `{_config.GitHubLibsRepoName}` to: `{targetPath}`");

        var downloader = new GitHubDownloader(_config.GitHubToken);
        await downloader.DownloadRepositoryAsync(_config.GitHubLibsRepoOwner, _config.GitHubLibsRepoName, targetPath);
        Logger.Info($"Downloaded temp libs to {targetPath}");
    }

    private void ReplaceLibsDirectory(string targetPath, string tempLibsPath)
    {
        var targetLibsDirs = Directory.GetDirectories(targetPath, _config.LibsDirName, SearchOption.TopDirectoryOnly);
        if (targetLibsDirs.Length != 1)
        {
            Logger.Error($"Found {targetLibsDirs.Length} dirs with `{_config.LibsDirName}` keyword:");
            foreach (var dir in targetLibsDirs)
            {
                Logger.Info(dir);
            }

            throw new NotSupportedException(
                $"Expected to find exactly one `{_config.LibsDirName}` dir in target path `{targetPath}`");
        }

        var libsTargetPath = targetLibsDirs[0];

        var sourceLibsSearchPattern = _config.LibsModeToSourcePathMapping[_config.LibsMode];

        var sourceLibsDirs = Directory.GetDirectories(tempLibsPath, sourceLibsSearchPattern);
        if (sourceLibsDirs.Length != 1)
        {
            throw new NotSupportedException(
                $"Expected to find exactly one `{sourceLibsSearchPattern}` dir in target path `{tempLibsPath}`");
        }

        var libsSourcePath = sourceLibsDirs[0];

        Directory.Delete(libsTargetPath, recursive: true);
        Directory.CreateDirectory(libsTargetPath);
        FileUtils.CopyFiles(libsSourcePath, libsTargetPath);

        Logger.Info(
            $"Replaced Libs based on {_config.LibsMode}. Target path: `{libsTargetPath}`, source path: `{libsSourcePath}`");
    }

    private void RemoveAllMetaFiles(string targetPath)
    {
        Logger.Info($"Removing all `.meta` files in {targetPath}");
        var metaFiles = Directory.GetFiles(targetPath, "*.meta", SearchOption.AllDirectories);
        foreach (var file in metaFiles)
        {
            File.Delete(file);
            Logger.Info($"DELETED {file}");
        }

        Logger.Info($"Finished removing all `.meta` files in {targetPath}");
    }
}