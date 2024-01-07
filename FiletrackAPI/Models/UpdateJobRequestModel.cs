using System.ComponentModel.DataAnnotations;

namespace FiletrackAPI.Models;

public class UpdateJobRequestModel
{
    public string JobInfo { get; set; }
    public List<string> JobAttributes { get; set; } = new List<string>();
    public List<IFormFile> JobAddedFiles { get; set; } = new List<IFormFile>();
    public List<string> JobCurrentFiles { get; set; } = new List<string>();
}