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
        foreach(var file in Directory.GetFiles(pathFrom))
        {
            // Copy the current file to the new path. 
            File.Copy(file, Path.Combine(pathTo, Path.GetFileName(file)), true);
        }

        // Get all the directories in the current path. 
        foreach (var directory in Directory.GetDirectories(pathFrom))
        { 
            // Create a new path for the current directory in the new location.                      
            var newDirectory = Path.Combine(pathTo, new DirectoryInfo(directory).Name);

            // Copy the directory over to the new path location if it does not already exist. 
            if (!Directory.Exists(newDirectory))
            {
                Directory.CreateDirectory(newDirectory);
            }
            // Call this routine again with the new path. 
            CopyFiles(directory, newDirectory);
        }
    }
}