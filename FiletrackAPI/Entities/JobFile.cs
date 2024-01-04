namespace FiletrackAPI.Entities;

public class JobFile
{
    public string Id { get; set; }
    public string JobId { get; set; }
    public string FileName { get; set; }
    public string Type { get; set; }
    public string BlobUrl { get; set; }
    public string BlobPath { get; set; }
}