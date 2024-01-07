using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FiletrackAPI.Entities;
using FiletrackAPI.Models;
using FiletrackAPI.Services;
using FiletrackWebInterface.Entities;
using FiletrackWebInterface.Helpers;
using Microsoft.Extensions.Options;

namespace FiletrackWebInterface.Services;

public interface IJobsService
{
    Task<int> CreateJob(CreateJobRequestModel model);
    List<Dictionary<string, string>> GetJobsInState(JobState state);
    CompleteJobInfo GetCompleteJob(string modelJobId);
    Task<int> UpdateJob(UpdateJobRequestModel model);
    void DeleteJob(string jobId);
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

    public List<Dictionary<string, string>> GetJobsInState(JobState state)
    {
        List<Dictionary<string, string>> jobs = new List<Dictionary<string, string>>();

        List<string> jobsIds = _dbService.GetJobsInState(state);
        foreach (var id in jobsIds)
        {
            Dictionary<string, string> basicJobData = new Dictionary<string, string>();

            var atrs = _dbService.GetJobAttributes(id);
            basicJobData.Add("id", id);
            foreach (var atr in atrs)
            {
                basicJobData.Add(atr.id, atr.value);
            }

            jobs.Add(basicJobData);
        }

        return jobs;
    }

    public async Task<int> CreateJob(CreateJobRequestModel model)
    {
        Job? job = JsonSerializer.Deserialize<Job>(model.JobInfo);

        if (job == null)
            return StatusCodes.Status500InternalServerError;

        var attributes = ParseJobAttributes(model.JobAttributes);
        var jobFiles = await CreateJobFilesAndUploadToBlob(model.Jobfiles, _dbService.GetPath(), attributes,
            _appsettings.Value.BlobConnectionString, _appsettings.Value.BlobContainer, job);

        _dbService.AddJob(job);
        _dbService.AddJobAttributes(attributes, job.Id);
        _dbService.AddJobFiles(jobFiles);

        return StatusCodes.Status200OK;
    }

    public async Task<int> UpdateJob(UpdateJobRequestModel updateJobRequestModel)
    {
        Job? job = JsonSerializer.Deserialize<Job>(updateJobRequestModel.JobInfo);
        if (job == null)
            return StatusCodes.Status500InternalServerError;

        var attributes = ParseJobAttributes(updateJobRequestModel.JobAttributes);
        var blobPathMembers = _dbService.GetPath();
        
        //update currentFiles in Filetrack
        List<string> currentFiles = updateJobRequestModel.JobCurrentFiles;

        var attrsChanged = DidAttrsChanged(job, attributes);
        if (attrsChanged)
        {
            _dbService.UpdateJobAttributes(attributes, job.Id);
            await UpdateCurrentFiles(currentFiles, job.Id, attrsChanged, attributes, blobPathMembers);
        }
        else
        {
            await UpdateCurrentFiles(currentFiles, job.Id);
        }
        
        //add new files to Filetrack
        var jobFiles = await CreateJobFilesAndUploadToBlob(updateJobRequestModel.JobAddedFiles, blobPathMembers, attributes,
            _appsettings.Value.BlobConnectionString, _appsettings.Value.BlobContainer, job);
        _dbService.AddJobFiles(jobFiles);


        _dbService.UpdateJob(job);
        return StatusCodes.Status200OK;
    }

    private bool DidAttrsChanged(Job job, List<JobAttribute> attributes)
    {
        bool attrsChanged = false;
        var currentAttributes = _dbService.GetJobAttributes(job.Id);
        foreach (var attribute in attributes)
        {
            if (currentAttributes.FirstOrDefault(x => x.id == attribute.id).value != attribute.value)
            {
                attrsChanged = true;
                break;
            }
        }

        return attrsChanged;
    }

    private async Task UpdateCurrentFiles(List<string> files, string jobId, bool attributesUpdated = false,
        List<JobAttribute> attributes = null, List<PathMember> blobPathMembers = null)
    {
        var currentFilesInDb = _dbService.GetJobFiles(jobId);
        List<JobFile> toDelete = new List<JobFile>();
        List<JobFile> toUpdate = new List<JobFile>();

        foreach (var file in currentFilesInDb)
        {
            if (files.All(x => x != file.Id))
            {
                toDelete.Add(file);
            }
            else
            {
                toUpdate.Add(file);
            }
        }

        _dbService.DeleteJobFiles(toDelete);
        DeleteJobFilesInBlob(toDelete, _appsettings.Value.BlobConnectionString, _appsettings.Value.BlobContainer);
        
        if(!attributesUpdated)
            return;

        foreach (var file in toUpdate)
        {
            string newFilename = GenerateBlobFileName(blobPathMembers, attributes, file.FileName);
            var updatedBlobClient = await UpdateBlob(_appsettings.Value.BlobConnectionString, _appsettings.Value.BlobContainer,file.BlobPath,newFilename);
            file.BlobPath = newFilename;
            file.BlobUrl = updatedBlobClient.Uri.AbsoluteUri;
        }

        _dbService.UpdateJobFiles(toUpdate);
    }


    private async Task<BlobClient> UpdateBlob(string connectionString, string containerName, string existingFileName,
        string newFileName)
    {
        var blobContainerClient = new BlobContainerClient(connectionString, containerName);
        var existingBlobClient = blobContainerClient.GetBlobClient(existingFileName);
        var newBlobClient = blobContainerClient.GetBlobClient(newFileName);

        var poller = await newBlobClient.StartCopyFromUriAsync(existingBlobClient.Uri);
        await poller.WaitForCompletionAsync();

        /* test code to ensure that blob and its properties/metadata are copied over
        const prop1 = await blobClient.getProperties();
        const prop2 = await newBlobClient.getProperties();

        if (prop1.contentLength !== prop2.contentLength) {
          throw new Error("Expecting same size between copy source and destination");
        }

        if (prop1.contentEncoding !== prop2.contentEncoding) {
          throw new Error("Expecting same content encoding between copy source and destination");
        }

        if (prop1.metadata.keya !== prop2.metadata.keya) {
          throw new Error("Expecting same metadata between copy source and destination");
        }
        */

        await existingBlobClient.DeleteIfExistsAsync();

        return newBlobClient;
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

    private async Task<List<JobFile>> CreateJobFilesAndUploadToBlob(List<IFormFile> addedFiles, List<PathMember> path,
        List<JobAttribute> attributes,
        string connectionString, string containerName, Job job)
    {
        List<JobFile> jobFiles = new List<JobFile>();
        foreach (var file in addedFiles)
        {
            var fileName = GenerateBlobFileName(path, attributes, file.FileName);

            Stream myBlob = file.OpenReadStream();
            var blobClient = new BlobContainerClient(connectionString, containerName);
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

    private string GenerateBlobFileName(List<PathMember> path, List<JobAttribute> attributes, string FileName)
    {
        string blobFileName = "";
        for (int i = 0; i < path.Count; i++)
        {
            var member = path.FirstOrDefault(x => x.Order == i);
            var value = attributes.FirstOrDefault(x => x.id == member?.Id);
            blobFileName += value.value;
            blobFileName += "/";
        }

        blobFileName += FileName;
        return blobFileName;
    }

    private async void DeleteJobFilesInBlob(List<JobFile> files, string connectionString, string containerName)
    {
        foreach (var file in files)
        {
            var container = new BlobContainerClient(connectionString, containerName);
            var blob = container.GetBlobClient(file.BlobPath);
            await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }
    }

    public CompleteJobInfo GetCompleteJob(string jobId)
    {
        CompleteJobInfo info = new CompleteJobInfo();
        info.Id = jobId;
        info.Description = _dbService.GetBasicJobInfo(jobId).Description;
        var tags = _dbService.GetAllTags();
        var atrs = _dbService.GetJobAttributes(jobId);
        foreach (var atr in atrs)
        {
            atr.name = tags.FirstOrDefault(x => x.Id == atr.id)?.Name;
        }

        info.JobAttributes = atrs;
        info.JobFiles = _dbService.GetJobFiles(jobId);
        return info;
    }

    public void DeleteJob(string jobId)
    {
        var jobFiles = _dbService.GetJobFiles(jobId);
        DeleteJobFilesInBlob(jobFiles, _appsettings.Value.BlobConnectionString, _appsettings.Value.BlobContainer);
        _dbService.DeleteJobFiles(jobFiles);
        _dbService.DeleteJobAttributes(jobId);
        _dbService.DeleteJob(jobId);
    }
}