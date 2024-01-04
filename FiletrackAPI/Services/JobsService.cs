using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Storage.Blobs;
using FiletrackAPI.Entities;
using FiletrackAPI.Models;
using FiletrackAPI.Services;
using FiletrackWebInterface.Entities;
using FiletrackWebInterface.Helpers;
using Microsoft.Extensions.Options;

namespace FiletrackWebInterface.Services;

public interface IJobsService
{
    public Task<int> CreateJob(CreateJobRequestModel model);
}

public class JobsService : IJobsService
{
    private readonly IOptions<AppSettings> _appsettings;
    private readonly IDbService _dbService;

    public JobsService(IOptions<AppSettings> appSettings, IDbService dbService)
    {
        _dbService = dbService;
        _appsettings = appSettings;
    }

    public async Task<int> CreateJob(CreateJobRequestModel model)
    {
        Job? job = JsonSerializer.Deserialize<Job>(model.JobInfo);
        
        if (job == null)
            return StatusCodes.Status500InternalServerError;
        
        var attributes = ParseJobAttributes(model.JobAttributes);
        var jobFiles = await CreateJobFilesAndUploadToBlob(model, _dbService.GetPath(), attributes, _appsettings.Value.BlobConnectionString, _appsettings.Value.BlobContainer, job);
        
        _dbService.AddJob(job);
        _dbService.AddJobAttributes(attributes, job.Id);
        _dbService.AddJobFiles(jobFiles);
        
        return StatusCodes.Status200OK;
    }

    private List<JobAttribute> ParseJobAttributes(List<string> attrs)
    {
        List<JobAttribute> attributes = new List<JobAttribute>();
        foreach (var jobAttribute in attrs)
        {
            var atr = JsonSerializer.Deserialize<JobAttribute>(jobAttribute);
            if (atr != null)
                attributes.Add(atr);
        }

        return attributes;
    }

    private async Task<List<JobFile>> CreateJobFilesAndUploadToBlob(CreateJobRequestModel model, List<PathMember> path, List<JobAttribute> attributes,
        string Connection, string containerName, Job job)
    {
        List<JobFile> jobFiles = new List<JobFile>();
        foreach (var file in model.Jobfiles)
        {
            string fileName = "";
            for (int i = 0; i < path.Count; i++)
            {
                var member = path.FirstOrDefault(x => x.Order == i);
                var value = attributes.FirstOrDefault(x => x.id == member?.Id);
                fileName += value.value;
                fileName += "/";
            }

            fileName += file.FileName;

            Stream myBlob = file.OpenReadStream();
            var blobClient = new BlobContainerClient(Connection, containerName);
            var blob = blobClient.GetBlobClient(fileName);
            if (!blob.Exists())
                await blob.UploadAsync(myBlob);

            jobFiles.Add(new JobFile()
            {
                BlobPath = fileName,
                BlobUrl = blob.Uri.AbsoluteUri,
                FileName = file.FileName,
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                Type = file.ContentType
            });
        }

        return jobFiles;
    }
}