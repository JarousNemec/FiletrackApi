using FiletrackWebInterface.Entities;

namespace FiletrackAPI.Models;

public class PathUpdateRequest
{
    public PathMember[] UpdatedMembers { get; set; }
}