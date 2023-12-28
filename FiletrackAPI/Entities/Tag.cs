using System.Text.Json.Serialization;

namespace FiletrackWebInterface.Entities;

public class Tag
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool Mandatory { get; set; }
}