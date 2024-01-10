using System.ComponentModel.DataAnnotations;

namespace FiletrackAPI.Models;

public class CreateJobRequestModel
{
    public string JobInfo { get; set; }
    
    [Required(ErrorMessage = "Please set attributes")]
    public List<string> JobAttributes { get; set; } = new List<string>();

    [Required(ErrorMessage = "Please select files")]
    public List<IFormFile> Jobfiles { get; set; } = new List<IFormFile>();
}