namespace StreamChatUnitySdkStripper;

public static class FileUtils
{
    /// <summary>
    /// Takes the files from the PathFrom and copies them to the PathTo. 
    /// </summary>
    /// <param name="pathFrom"></param>
    /// <param name="pathTo"></param>
    public static void CopyFiles(string pathFrom, string pathTo)
    {
        foreach (var file in Directory.GetFiles(pathFrom))
        {
            File.Copy(file, Path.Combine(pathTo, Path.GetFileName(file)), true);
        }

        foreach (var directory in Directory.GetDirectories(pathFrom))
        {
            var newDirectory = Path.Combine(pathTo, new DirectoryInfo(directory).Name);

            if (!Directory.Exists(newDirectory))
            {
                Directory.CreateDirectory(newDirectory);
            }

            CopyFiles(directory, newDirectory);
        }
    }
}