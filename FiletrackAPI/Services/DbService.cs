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
    List<Job> GetJobsInState(JobState state);
    List<JobAttribute> GetJobAttributes(string jobId);
    void DeleteJobAttributes(string jobId);
    Job GetJob(string jobId);
    List<JobFile> GetJobFiles(string jobId);
    void UpdateJob(Job job);
    void UpdateJobAttributes(List<JobAttribute> attributes, string jobId);
    void DeleteJobFiles(List<JobFile> toDelete);
    void UpdateJobFiles(List<JobFile> toUpdate);
    void DeleteJob(string jobId);
    void SetJobReport(string jobId, string jobReport);
    JobReport GetJobReport(string jobId);
    void DeleteJobReport(string jobId);
}

public class DbService : IDbService
{
    private readonly string _connString;
    private readonly ILogger<DbService> _logger;

    public DbService(IOptions<AppSettings> appSettings, ILogger<DbService> logger)
    {
        var builder = new SqlConnectionStringBuilder();
        builder.Password = appSettings.Value.Password;
        builder.DataSource = appSettings.Value.DataSource;
        builder.UserID = appSettings.Value.UserID;
        builder.InitialCatalog = appSettings.Value.InitialCatalog;
        builder.TrustServerCertificate = appSettings.Value.TrustServerCertificate;
        _connString = builder.ToString();
        _logger = logger;
    }

    public List<Tag> GetAllTags()
    {
        string query = "SELECT * FROM tag";
        using var reader = ExecuteQuery(query, new List<SqlParameter>(), out SqlConnection connection);
        if (reader == null)
        {
            return new List<Tag>();
        }

        var result = new List<Tag>();
        while (reader.Read())
        {
            var tag = new Tag();
            tag.Id = reader.GetString(0);
            tag.Name = reader.GetString(1);
            tag.Mandatory = reader.GetBoolean(2);
            result.Add(tag);
        }

        reader.Close();
        connection.Close();
        return result;
    }

    public List<PathMember> GetPath()
    {
        string query = $"SELECT * FROM filepath";
        using var reader = ExecuteQuery(query, new List<SqlParameter>(), out SqlConnection connection);
        if (reader == null)
        {
            return new List<PathMember>();
        }

        var result = new List<PathMember>();
        while (reader.Read())
        {
            var item = new PathMember();
            item.Id = reader.GetString(0);
            item.Order = reader.GetInt16(1);
            result.Add(item);
        }

        reader.Close();
        connection.Close();
        return result;
    }

    public void RemoveTag(string id)
    {
        string query = "DELETE FROM tag WHERE id = @Id";
        var parameters = new List<SqlParameter>
        {
            new("@Id", id)
        };
        ExecuteNonQuery(query, parameters);
    }

    public void AddTag(Tag tag)
    {
        string query = "INSERT INTO tag (id, name, mandatory) VALUES (@Id, @Name, @Mandatory)";
        var parameters = new List<SqlParameter>
        {
            new("@Id", tag.Id), new("@Name", tag.Name), new("@Mandatory", tag.Mandatory)
        };
        ExecuteNonQuery(query, parameters);
    }

    public void UpdateTag(Tag tag)
    {
        string query = "UPDATE tag SET id = @Id, name = @Name, mandatory = @Mandatory WHERE id = @Id";
        var parameters = new List<SqlParameter>
        {
            new("@Id", tag.Id), new("@Name", tag.Name), new("@Mandatory", tag.Mandatory)
        };
        ExecuteNonQuery(query, parameters);
    }

    public void AddPathMember(PathMember member)
    {
        string query = "INSERT INTO filepath (id, \"order\") VALUES (@Id, @Order)";
        var parameters = new List<SqlParameter>
        {
            new("@Id", member.Id), new("@Order", member.Order)
        };
        ExecuteNonQuery(query, parameters);
    }

    public void RemovePathMembers()
    {
        string query = $"DELETE FROM filepath";
        ExecuteNonQuery(query, new List<SqlParameter>());
    }

    public void AddJob(Job job)
    {
        string query =
            "Insert Into job (id, state, author_id, description) values (@Id, @State, @AuthorId, @Description)";
        var parameters = new List<SqlParameter>
        {
            new("@Id", job.Id), new("@State", job.State), new("@AuthorId", job.AuthorId),
            new("@Description", job.Description)
        };
        ExecuteNonQuery(query, parameters);
    }

    public void UpdateJob(Job job)
    {
        using var connection = new SqlConnection(_connString);
        string query =
            "UPDATE job SET id = @Id, state = @State, author_id = @AuthorId, description = @Description WHERE id = @Id";
        var parameters = new List<SqlParameter>
        {
            new("@Id", job.Id), new("@State", job.State), new("@AuthorId", job.AuthorId),
            new("@Description", job.Description)
        };
        ExecuteNonQuery(query, parameters);
    }

    public Job GetJob(string jobId)
    {
        string query = "select id, state, author_id, description from job where id = @Id";
        var parameters = new List<SqlParameter>
        {
            new("@Id", jobId)
        };
        using var reader = ExecuteQuery(query, parameters, out SqlConnection connection);
        if (reader == null)
        {
            return new Job();
        }

        var result = new Job();
        while (reader.Read())
        {
            result.Id = reader.GetString(0);
            result.Description = reader.GetString(3);
            result.AuthorId = reader.GetString(2);
            result.State = (JobState)reader.GetInt16(1);
        }

        reader.Close();
        connection.Close();
        return result;
    }

    public List<Job> GetJobsInState(JobState state)
    {
        string query = $"select id, state from job where state = @State";
        var parameters = new List<SqlParameter>
        {
            new("@State", (int)state)
        };
        if (state == JobState.Saved)
        {
            query = $"select id, state from job where state = @State or state = @State2";
            parameters.Add(new("@State2", (int)JobState.Reported));
        }
        
        using var reader = ExecuteQuery(query, parameters, out SqlConnection connection);
        if (reader == null)
        {
            return new List<Job>();
        }

        var result = new List<Job>();
        while (reader.Read())
        {
            var newJob = new Job();
            newJob.Id = reader.GetString(0);
            newJob.State = (JobState)reader.GetInt16(1);
            result.Add(newJob);
        }

        reader.Close();
        connection.Close();
        return result;
    }

    public List<JobAttribute> GetJobAttributes(string jobId)
    {
        string query = "select attribute_id, value from job_attribute where job_id = @JobId";
        var parameters = new List<SqlParameter>
        {
            new("@JobId", jobId)
        };
        using var reader = ExecuteQuery(query, parameters, out SqlConnection connection);
        if (reader == null)
        {
            return new List<JobAttribute>();
        }

        var result = new List<JobAttribute>();
        while (reader.Read())
        {
            var jobAttribute = new JobAttribute();
            jobAttribute.id = reader.GetString(0);
            jobAttribute.value = reader.GetString(1);
            result.Add(jobAttribute);
        }

        reader.Close();
        connection.Close();
        return result;
    }

    public void AddJobAttributes(List<JobAttribute> attributes, string jobId)
    {
        string query =
            "Insert Into job_attribute (id, attribute_id, value, job_id) values (@Id, @AttributeId, @Value, @JobId)";
        foreach (var attribute in attributes)
        {
            var parameters = new List<SqlParameter>
            {
                new("@Id", Guid.NewGuid().ToString()), new("@AttributeId", attribute.id),
                new("@Value", attribute.value), new("@JobId", jobId)
            };
            ExecuteNonQuery(query, parameters);
        }
    }

    public void UpdateJobAttributes(List<JobAttribute> attributes, string jobId)
    {
        string query =
            $"UPDATE job_attribute SET value = @Value WHERE job_id = @JobId and attribute_id = @AttributeId";
        foreach (var attribute in attributes)
        {
            var parameters = new List<SqlParameter>
                { new("@AttributeId", attribute.id), new("@Value", attribute.value), new("@JobId", jobId) };
            ExecuteNonQuery(query, parameters);
        }
    }

    public void DeleteJobAttributes(string jobId)
    {
        string query =
            "DELETE FROM job_attribute WHERE job_id = @Id";
        var parameters = new List<SqlParameter> { new("@Id", jobId) };
        ExecuteNonQuery(query, parameters);
    }

    public void AddJobFiles(List<JobFile> files)
    {
        string query =
            "Insert Into job_file (id, job_id, filename, type, blob_url, blob_path) values (@Id, @JobId, @FileName, @Type, @BlobUrl, @BlobPath)";
        foreach (var file in files)
        {
            var parameters = new List<SqlParameter>()
            {
                new("@Id", file.Id), new("@JobId", file.JobId), new("@FileName", file.FileName),
                new("@Type", file.Type), new("@BlobUrl", file.BlobUrl), new("@BlobPath", file.BlobPath)
            };
            ExecuteNonQuery(query, parameters);
        }
    }

    public void UpdateJobFiles(List<JobFile> toUpdate)
    {
        string query =
            "UPDATE job_file SET blob_url = @BlobUrl, blob_path = @BlobPath WHERE id = @Id";
        foreach (var file in toUpdate)
        {
            var parameters = new List<SqlParameter>()
                { new("@BlobUrl", file.BlobUrl), new("@BlobPath", file.BlobPath), new("@Id", file.Id) };
            ExecuteNonQuery(query, parameters);
        }
    }

    public void DeleteJobFiles(List<JobFile> toDelete)
    {
        string query = "DELETE FROM job_file WHERE id = @Id";
        foreach (var file in toDelete)
        {
            var parameters = new List<SqlParameter>() { new("@Id", file.Id) };
            ExecuteNonQuery(query, parameters);
        }
    }

    public void DeleteJob(string jobId)
    {
        string query = "DELETE FROM job WHERE id = @Id";
        var parameters = new List<SqlParameter>() { new("@Id", jobId) };
        ExecuteNonQuery(query, parameters);
    }
    
    public void DeleteJobReport(string jobId)
    {
        string query = "DELETE FROM job_report WHERE job_id = @Id";
        var parameters = new List<SqlParameter>() { new("@Id", jobId) };
        ExecuteNonQuery(query, parameters);
    }

    public List<JobFile> GetJobFiles(string jobId)
    {
        string query = "select id, job_id, filename, type, blob_url, blob_path  from job_file where job_id = @JobId";
        var parameters = new List<SqlParameter>() { new("@JobId", jobId) };
        using var reader = ExecuteQuery(query, parameters, out SqlConnection connection);
        if (reader == null)
        {
            return new List<JobFile>();
        }

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

        reader.Close();
        connection.Close();
        return result;
    }

    public JobReport GetJobReport(string jobId)
    {
        string query = "select id, job_id, report from job_report where job_id = @JobId";
        var parameters = new List<SqlParameter>() { new("@JobId", jobId) };
        using var reader = ExecuteQuery(query, parameters, out SqlConnection connection);
        if (reader == null)
        {
            return new JobReport();
        }

        var result = new JobReport();
        if (reader.Read())
        {
            result.Id = reader.GetString(0);
            result.JobId = reader.GetString(1);
            result.Report = reader.GetString(2);
        }

        reader.Close();
        connection.Close();
        return result;
    }

    public void SetJobReport(string jobId, string jobReport)
    {
        var reportInDb = GetJobReport(jobId);
        string id = "";
        string query = "";
        if (reportInDb.Id == string.Empty)
        {
            id = Guid.NewGuid().ToString();
            query =
                "Insert Into job_report (id , job_id , report) values (@Id, @JobId, @Report)";
        }
        else
        {
            id = reportInDb.Id;
            query =
                "UPDATE job_report SET id = @Id, job_id = @JobId, report = @Report WHERE job_id = @JobId";
        }

        var parameters = new List<SqlParameter>() { new("@Id", id), new("@JobId", jobId), new("@Report", jobReport) };
        ExecuteNonQuery(query, parameters);
    }

    private SqlDataReader? ExecuteQuery(string query, List<SqlParameter> parameters, out SqlConnection livingConnection)
    {
        var connection = new SqlConnection(_connString);
        livingConnection = connection;
        try
        {
            connection.Open();
            SqlCommand command = new SqlCommand(query, connection);

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            return command.ExecuteReader();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }

        return null;
    }

    private void ExecuteNonQuery(string query, List<SqlParameter> parameters)
    {
        using var connection = new SqlConnection(_connString);
        try
        {
            connection.Open();
            using SqlCommand command = new SqlCommand(query, connection);

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            command.ExecuteNonQuery();

            connection.Close();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
    }
}