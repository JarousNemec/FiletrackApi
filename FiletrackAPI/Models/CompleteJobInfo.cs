using FiletrackAPI.Entities;

namespace FiletrackAPI.Models;

public class CompleteJobInfo
{
    public string Id { get; set; }
    public string Description { get; set; }
    public List<JobAttribute> JobAttributes { get; set; }
    public List<JobFile> JobFiles { get; set; }
}