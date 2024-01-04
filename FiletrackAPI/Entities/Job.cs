namespace FiletrackWebInterface.Entities;

public class Job
{
    public string Id { get; set; }
    public JobState State { get; set; }
    public string AuthorId { get; set; }
    public string  Description { get; set; }
}