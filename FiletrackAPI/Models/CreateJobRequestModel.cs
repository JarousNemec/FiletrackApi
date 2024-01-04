using System.ComponentModel.DataAnnotations;

namespace FiletrackAPI.Models;

public class CreateJobRequestModel
{
    public string JobInfo { get; set; }
    
    public List<string> JobAttributes { get; set; }
    
    [Required(ErrorMessage = "Please select files")]
    public List<IFormFile> Jobfiles { get; set; }
}