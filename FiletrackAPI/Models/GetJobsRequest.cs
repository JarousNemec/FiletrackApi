using FiletrackWebInterface.Entities;

namespace FiletrackAPI.Models;

public class GetJobsRequest
{
    public JobState State { get; set; }
}