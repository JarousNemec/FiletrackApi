using System.Data;
using FiletrackAPI.Entities;
using FiletrackWebInterface.Entities;
using FiletrackWebInterface.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FiletrackAPI.Services;

public interface IDbService
{
    List<Tag> GetAllTags();
    List<PathMember> GetPath();
    void UpdateTag(Tag tag);
    void RemoveTag(string id);
    void AddTag(Tag tag);
    void AddPathMember(PathMember member);
    void RemovePathMembers();
    void AddJobFiles(List<JobFile> files);
    void AddJobAttributes(List<JobAttribute> attributes, string jobId);
    void AddJob(Job job);
    List<string> GetJobsInState(JobState state);
    List<JobAttribute> GetJobAttributes(string jobId);
    void DeleteJobAttributes(string jobId);
    Job GetBasicJobInfo(string jobId);
    List<JobFile> GetJobFiles(string jobId);
    void UpdateJob(Job job);
    void UpdateJobAttributes(List<JobAttribute> attributes, string jobId);
    void DeleteJobFiles(List<JobFile> toDelete);
    void UpdateJobFiles(List<JobFile> toUpdate);
    void DeleteJob(string jobId);
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

    public List<PathMember> GetPath()
    {
        using var connection = new SqlConnection(_connString);

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM Filespath";
        connection.Open();
        using var reader = command.ExecuteReader();
        var result = new List<PathMember>();
        while (reader.Read())
        {
            var item = new PathMember();
            item.Id = reader.GetString(0);
            item.Order = reader.GetInt16(1);
            result.Add(item);
        }

        return result;
    }

    public void RemoveTag(string id)
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM Tags WHERE Id = '{id}'";
        command.ExecuteNonQuery();
    }

    public void AddTag(Tag tag)
    {
        using var connection = new SqlConnection(_connString);
        string query = "INSERT INTO Tags (Id, Name, Mandatory) VALUES (@Id, @Name, @Mandatory)";
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Id", tag.Id);
            command.Parameters.AddWithValue("@Name", tag.Name);
            command.Parameters.AddWithValue("@Mandatory", tag.Mandatory);
            connection.Open();
            command.ExecuteNonQuery();
        }
    }

    public void UpdateTag(Tag tag)
    {
        using var connection = new SqlConnection(_connString);
        string query = $"UPDATE Tags SET Id = @Id, Name = @Name, Mandatory = @Mandatory WHERE Id = '{tag.Id}'";
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Id", tag.Id);
            command.Parameters.AddWithValue("@Name", tag.Name);
            command.Parameters.AddWithValue("@Mandatory", tag.Mandatory);
            connection.Open();
            command.ExecuteNonQuery();
        }
    }

    public void AddPathMember(PathMember member)
    {
        using var connection = new SqlConnection(_connString);
        string query = "INSERT INTO Filespath (Id, \"Order\") VALUES (@Id, @Order)";
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Id", member.Id);
            command.Parameters.AddWithValue("@Order", member.Order);
            connection.Open();
            command.ExecuteNonQuery();
        }
    }

    public void RemovePathMembers()
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM Filespath";
        command.ExecuteNonQuery();
    }

    public void AddJob(Job job)
    {
        using var connection = new SqlConnection(_connString);
        string query =
            $"Insert Into Jobs (Id, State, AuthorId, Description) values (@Id, @State, @AuthorId, @Description)";
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Id", job.Id);
            command.Parameters.AddWithValue("@State", job.State);
            command.Parameters.AddWithValue("@AuthorId", job.AuthorId);
            command.Parameters.AddWithValue("@Description", job.Description);
            connection.Open();
            command.ExecuteNonQuery();
        }
    }

    public void UpdateJob(Job job)
    {
        using var connection = new SqlConnection(_connString);
        string query =
            $"UPDATE Jobs SET Id = @Id, State = @State, AuthorId = @AuthorId, Description = @Description WHERE Id = '{job.Id}'";
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Id", job.Id);
            command.Parameters.AddWithValue("@State", job.State);
            command.Parameters.AddWithValue("@AuthorId", job.AuthorId);
            command.Parameters.AddWithValue("@Description", job.Description);
            connection.Open();
            command.ExecuteNonQuery();
        }
    }

    public Job GetBasicJobInfo(string jobId)
    {
        using var connection = new SqlConnection(_connString);

        using var command = connection.CreateCommand();
        command.CommandText = $"select Id, State, AuthorId, Description from Jobs where Id = '{jobId}'";
        connection.Open();
        using var reader = command.ExecuteReader();
        var result = new Job();
        while (reader.Read())
        {
            result.Id = reader.GetString(0);
            result.Description = reader.GetString(3);
            result.AuthorId = reader.GetString(2);
            result.State = (JobState)reader.GetInt16(1);
        }

        return result;
    }

    public List<string> GetJobsInState(JobState state)
    {
        using var connection = new SqlConnection(_connString);

        using var command = connection.CreateCommand();
        command.CommandText = $"select Id from Jobs where State = {(int)state}";
        connection.Open();
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    public List<JobAttribute> GetJobAttributes(string jobId)
    {
        using var connection = new SqlConnection(_connString);

        using var command = connection.CreateCommand();
        command.CommandText = $"select AttributeId, Value from JobAttributes where JobId = '{jobId}'";
        connection.Open();
        using var reader = command.ExecuteReader();
        var result = new List<JobAttribute>();
        while (reader.Read())
        {
            var jobAttribute = new JobAttribute();
            jobAttribute.id = reader.GetString(0);
            jobAttribute.value = reader.GetString(1);
            result.Add(jobAttribute);
        }

        return result;
    }

    public void AddJobAttributes(List<JobAttribute> attributes, string jobId)
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        string query =
            $"Insert Into JobAttributes (Id, AttributeId, Value, JobId) values (@Id, @AttributeId, @Value, @JobId)";
        foreach (var attribute in attributes)
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@AttributeId", attribute.id);
                command.Parameters.AddWithValue("@Value", attribute.value);
                command.Parameters.AddWithValue("@JobId", jobId);
                command.ExecuteNonQuery();
            }
        }
    }

    public void UpdateJobAttributes(List<JobAttribute> attributes, string jobId)
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        string query =
            $"UPDATE JobAttributes SET Value = @Value WHERE JobId = @JobId and AttributeId = @AttributeId";
        foreach (var attribute in attributes)
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@AttributeId", attribute.id);
                command.Parameters.AddWithValue("@Value", attribute.value);
                command.Parameters.AddWithValue("@JobId", jobId);
                command.ExecuteNonQuery();
            }
        }
    }

    public void DeleteJobAttributes(string jobId)
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        string query =
            $"DELETE FROM JobAttributes WHERE JobId = @Id";
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Id", jobId);
            command.ExecuteNonQuery();
        }
    }

    public void AddJobFiles(List<JobFile> files)
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        string query =
            $"Insert Into JobFiles (Id, JobId, FileName, Type, BlobUrl, BlobPath) values (@Id, @JobId, @FileName, @Type, @BlobUrl, @BlobPath)";
        foreach (var file in files)
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", file.Id);
                command.Parameters.AddWithValue("@JobId", file.JobId);
                command.Parameters.AddWithValue("@FileName", file.FileName);
                command.Parameters.AddWithValue("@Type", file.Type);
                command.Parameters.AddWithValue("@BlobUrl", file.BlobUrl);
                command.Parameters.AddWithValue("@BlobPath", file.BlobPath);
                command.ExecuteNonQuery();
            }
        }
    }

    public void UpdateJobFiles(List<JobFile> toUpdate)
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        string query =
            $"UPDATE JobFiles SET BlobUrl = @BlobUrl, BlobPath = @BlobPath WHERE Id = @Id";
        foreach (var file in toUpdate)
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@BlobUrl", file.BlobUrl);
                command.Parameters.AddWithValue("@BlobPath", file.BlobPath);
                command.Parameters.AddWithValue("@Id", file.Id);
                command.ExecuteNonQuery();
            }
        }
    }

    public void DeleteJobFiles(List<JobFile> toDelete)
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        string query =
            $"DELETE FROM JobFiles WHERE Id = @Id";
        foreach (var file in toDelete)
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", file.Id);
                command.ExecuteNonQuery();
            }
        }
    }

    public void DeleteJob(string jobId)
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        string query =
            $"DELETE FROM Jobs WHERE Id = @Id";
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Id", jobId);
            command.ExecuteNonQuery();
        }
    }

    public List<JobFile> GetJobFiles(string jobId)
    {
        using var connection = new SqlConnection(_connString);

        using var command = connection.CreateCommand();
        command.CommandText =
            $"select Id, JobId, FileName, Type, BlobUrl, BlobPath  from JobFiles where JobId = '{jobId}'";
        connection.Open();
        using var reader = command.ExecuteReader();
        var result = new List<JobFile>();
        while (reader.Read())
        {
            var jobFile = new JobFile();
            jobFile.Id = reader.GetString(0);
            jobFile.JobId = reader.GetString(1);
            jobFile.FileName = reader.GetString(2);
            jobFile.Type = reader.GetString(3);
            jobFile.BlobUrl = reader.GetString(4);
            jobFile.BlobPath = reader.GetString(5);
            result.Add(jobFile);
        }

        return result;
    }
}