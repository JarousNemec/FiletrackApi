using FiletrackWebInterface.Entities;
using FiletrackWebInterface.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FiletrackAPI.Services;

public interface IDbService
{
    public List<Tag> GetAllTags();
    public List<FilesPathItem> GetPath();
}

public class DbService : IDbService
{
    private readonly string _connString;
    public DbService(IOptions<AppSettings> appSettings)
    {
        var builder = new SqlConnectionStringBuilder();
        builder.Password = appSettings.Value.Password;
        builder.DataSource = appSettings.Value.DataSource;
        builder.UserID = appSettings.Value.UserID;
        builder.InitialCatalog = appSettings.Value.InitialCatalog;
        builder.TrustServerCertificate = appSettings.Value.TrustServerCertificate;
        _connString = builder.ToString();
    }
    
    public List<Tag> GetAllTags()
    {
        using var connection = new SqlConnection(_connString);
        
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM Tags";
        connection.Open();
        using var reader = command.ExecuteReader();
        var result = new List<Tag>();
        while (reader.Read())
        {
            var tag = new Tag();
            tag.Id = reader.GetString(0);
            tag.Name = reader.GetString(1);
            tag.Mandatory = reader.GetBoolean(2);
            result.Add(tag);
        }
        return result;
    }
    public List<FilesPathItem> GetPath()
    {
        using var connection = new SqlConnection(_connString);
        
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM Filespath";
        connection.Open();
        using var reader = command.ExecuteReader();
        var result = new List<FilesPathItem>();
        while (reader.Read())
        {
            var item = new FilesPathItem();
            item.Id = reader.GetString(0);
            item.Order = reader.GetInt16(1);
            result.Add(item);
        }
        return result;
    }
}