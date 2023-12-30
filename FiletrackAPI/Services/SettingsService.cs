using FiletrackAPI.Entities;
using FiletrackWebInterface.Entities;

namespace FiletrackAPI.Services;

public interface ISettingsService
{
    public List<Tag> GetAllTags();
    public List<PathMember> GetPath();
    public void UpdateTags(List<Tag> tags);
    public void UpdatePath(List<PathMember> members);
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

    public List<PathMember> GetPath()
    {
        return _dbService.GetPath();
    }

    public void UpdateTags(List<Tag> tags)
    {
        List<Tag> toAdd = new List<Tag>();
        List<Tag> toUpdate = new List<Tag>();
        List<string> toDelete = new List<string>();

        var actualTags = _dbService.GetAllTags();
        foreach (var tag in tags)
        {
            var tagIncluded = actualTags.FirstOrDefault(x => x.Id == tag.Id);
            if (tagIncluded == null)
            {
                toAdd.Add(tag);
            }
            else
            {
                if (tagIncluded.Name != tag.Name || tagIncluded.Mandatory != tag.Mandatory)
                    toUpdate.Add(tag);
            }
        }

        foreach (var actualTag in actualTags)
        {
            if (tags.All(x => x.Id != actualTag.Id))
            {
                toDelete.Add(actualTag.Id);
            }
        }

        foreach (var tag in toAdd)
        {
            _dbService.AddTag(tag);
        }

        foreach (var tag in toUpdate)
        {
            _dbService.UpdateTag(tag);
        }

        foreach (var tag in toDelete)
        {
            _dbService.RemoveTag(tag);
        }
    }

    public void UpdatePath(List<PathMember> members)
    {
        _dbService.RemovePathMembers();
        foreach (var member in members)
        {
            _dbService.AddPathMember(member);
        }
    }
}