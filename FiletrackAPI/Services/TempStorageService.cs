using Aspose.Zip;
using FiletrackWebInterface.Helpers;
using Microsoft.Extensions.Options;

namespace FiletrackAPI.Services;

public interface ITempStorageService
{
    void ArchiveDirectory(string destination, string target);
    void PrepareJobDir(string path);
    void PrepareTempDir();
}

public class TempStorageService : ITempStorageService
{
    private static bool _tempCleared = false;
    private readonly IOptions<AppSettings> _appsettings;
    public TempStorageService(IOptions<AppSettings> appSettings)
    {
        _appsettings = appSettings;
        if (!_tempCleared)
            PrepareTempDir();
    }
    public void ArchiveDirectory(string destination, string target)
    {
        using (FileStream zipFile = File.Open(destination, FileMode.Create))
        {
            using (Archive archive = new Archive())
            {
                DirectoryInfo dirToArchive = new DirectoryInfo(target);
                archive.CreateEntries(dirToArchive);
                archive.Save(zipFile);
            }
        }
    }

    public void PrepareJobDir(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public void PrepareTempDir()
    {
        DirectoryInfo di =
            new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), _appsettings.Value.TempDirName));

        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }

        foreach (DirectoryInfo dir in di.GetDirectories())
        {
            dir.Delete(true);
        }

        _tempCleared = true;
    }
}