using FiletrackAPI.Entities;

namespace FiletrackAPI.Models;

public class TagsUpdateRequest
{
    public Tag[] ListToUpdate { get; set; }
}