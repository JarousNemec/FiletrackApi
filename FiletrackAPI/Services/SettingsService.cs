using FiletrackWebInterface.Entities;

namespace FiletrackAPI.Services;

public interface ISettingsService
{
    public List<Tag> GetAllTags();
    public List<FilesPathItem> GetPath();
    public void UpdateTags(List<Tag> tags);
    public void UpdatePath(string path);
}

public class SettingsService : ISettingsService
{
    private readonly IDbService _dbService;
    public SettingsService(IDbService dbService)
    {
        _dbService = dbService;
    }
    public List<Tag> GetAllTags()
    {
        return _dbService.GetAllTags();
    }

    public List<FilesPathItem> GetPath()
    {
        return _dbService.GetPath();
    }

    public void UpdateTags(List<Tag> tags)
    {
        throw new NotImplementedException();
    }

    public void UpdatePath(string path)
    {
        throw new NotImplementedException();
    }
}