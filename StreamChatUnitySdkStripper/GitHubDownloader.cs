using System.Text;
using Octokit;
using StreamChatUnitySdkStripper;
using FileMode = System.IO.FileMode;

public class GitHubDownloader
{
    public delegate bool ShouldDownloadFile(string localPath, string repoFilePath);

    private readonly GitHubClient _gitHubClient;

    public GitHubDownloader(string accessToken)
    {
        _gitHubClient = new GitHubClient(new ProductHeaderValue("MyApp"))
        {
            Credentials = new Credentials(accessToken)
        };
    }

    public static async Task DownloadZippedRepositoryAsync(string repositoryUrl, string branchName, string savePath)
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync($"{repositoryUrl}/archive/{branchName}.zip");

            if (response.IsSuccessStatusCode)
            {
                using (var fileStream = new FileStream(savePath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
            else
            {
                var message = await response.Content.ReadAsStringAsync();
                throw new Exception(message);
            }
        }
    }

    public async Task DownloadRepositoryAsync(string owner, string repoName, string localPath,
        ShouldDownloadFile shouldDownloadFile = null)
    {
        Logger.Info($"GH Downloader - Get repository - owner: {owner}, repo: {repoName}");
        var repository = await _gitHubClient.Repository.Get(owner, repoName);

        Logger.Info($"GH Downloader - Get branch: {repository.DefaultBranch}");
        var branch = await _gitHubClient.Repository.Branch.Get(owner, repoName, repository.DefaultBranch);

        Logger.Info($"GH Downloader - Get files tree");
        var tree = await _gitHubClient.Git.Tree.GetRecursive(owner, repoName, branch.Commit.Sha);

        var sb = new StringBuilder();
        var success = 0;
        var failed = 0;
        var skipped = 0;
        foreach (var item in tree.Tree)
        {
            if (item.Type != TreeType.Blob)
            {
                continue;
            }

            var filePath = string.Empty;
            try
            {
                var blob = await _gitHubClient.Git.Blob.Get(owner, repoName, item.Sha);
                filePath = Path.Combine(localPath, item.Path);

                if (!shouldDownloadFile?.Invoke(filePath, item.Path) ?? false)
                {
                    skipped++;
                    sb.AppendLine("SKIPPED file: " + filePath);
                    //Logger.Warning("SKIPPED file: " + filePath);
                    continue;
                }

                new FileInfo(filePath).Directory?.Create();

                await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(blob.Content));
                success++;
                sb.AppendLine("DOWNLOADED file: " + filePath);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to download file `{filePath}` with exception: {e.Message}");
                failed++;
            }
        }

        Logger.Info(sb.ToString());

        Logger.Info(
            $"GH Downloader - Download completed. Files written successfully: {success}, skipped: {skipped}, failed: {failed}");
    }
}