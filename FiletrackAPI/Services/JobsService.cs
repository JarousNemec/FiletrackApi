using System.Text.Json;
using Aspose.Zip;
using FiletrackAPI.Entities;
using FiletrackApi.Models;
using FiletrackAPI.Models;
using FiletrackAPI.Services;
using FiletrackWebInterface.Entities;
using FiletrackWebInterface.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FiletrackWebInterface.Services;

public interface IJobsService
{
    Task<int> CreateJob(CreateJobRequestModel model);
    List<Dictionary<string, string>> GetJobsInState(JobState state);
    CompleteJobInfo GetCompleteJob(string modelJobId);
    Task<int> UpdateJob(UpdateJobRequestModel model);
    void DeleteJob(string jobId);
    Task<string> DownloadJob(string jobId);
    void MoveJobToProduction(string jobId);
    void RevertJob(JobRevertRequest model);
    string GetJobReport(string jobId);
}

public class JobsService : IJobsService
{
    private readonly IOptions<AppSettings> _appsettings;
    private readonly IDbService _dbService;
    private readonly IAzureBlobService _azureBlobService;
    private readonly ITempStorageService _tempStorageService;
    public JobsService(IOptions<AppSettings> appSettings, IDbService dbService, IAzureBlobService azureBlobService, ITempStorageService tempStorageService)
    {
        _dbService = dbService;
        _appsettings = appSettings;
        _azureBlobService = azureBlobService;
        _tempStorageService = tempStorageService;
    }

    public List<Dictionary<string, string>> GetJobsInState(JobState state)
    {
        List<Dictionary<string, string>> jobs = new List<Dictionary<string, string>>();

        List<Job> jobInfo = _dbService.GetJobsInState(state);
        foreach (var job in jobInfo)
        {
            Dictionary<string, string> basicJobData = new Dictionary<string, string>();

            var atrs = _dbService.GetJobAttributes(job.Id);
            basicJobData.Add("id", job.Id);
            basicJobData.Add("state", ((int)job.State).ToString());
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
        var jobFiles = await CreateJobFilesAndUploadToBlob(updateJobRequestModel.JobAddedFiles, blobPathMembers,
            attributes,
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
        DeleteJobFilesInBlob(toDelete);

        if (!attributesUpdated)
            return;

        foreach (var file in toUpdate)
        {
            string newFilename = _azureBlobService.GenerateBlobFileName(blobPathMembers, attributes, file.FileName);
            var updatedBlobUrl = await _azureBlobService.UpdateBlob(file.BlobPath, newFilename);
            file.BlobPath = newFilename;
            file.BlobUrl = updatedBlobUrl;
        }

        _dbService.UpdateJobFiles(toUpdate);
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
            var fileName = _azureBlobService.GenerateBlobFileName(path, attributes, file.FileName);

            Stream fileStream = file.OpenReadStream();
            var blobUrl = await _azureBlobService.UploadBlob(fileName, fileStream);

            jobFiles.Add(new JobFile()
            {
                BlobPath = fileName,
                BlobUrl = blobUrl,
                FileName = file.FileName,
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                Type = file.ContentType
            });
        }

        return jobFiles;
    }

    private async void DeleteJobFilesInBlob(List<JobFile> files)
    {
        foreach (var file in files)
        {
            await _azureBlobService.DeleteBlob(file.BlobPath);
        }
    }


    public CompleteJobInfo GetCompleteJob(string jobId)
    {
        CompleteJobInfo info = new CompleteJobInfo();
        info.Id = jobId;
        info.Description = _dbService.GetJob(jobId).Description;
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
        DeleteJobFilesInBlob(jobFiles);
        _dbService.DeleteJobFiles(jobFiles);
        _dbService.DeleteJobAttributes(jobId);
        _dbService.DeleteJobReport(jobId);
        _dbService.DeleteJob(jobId);
    }

    public async Task<string> DownloadJob(string jobId)
    {
        string tempDir = Path.Combine(Directory.GetCurrentDirectory(), _appsettings.Value.TempDirName);
        string jobDir = Path.Combine(tempDir, jobId);
        _tempStorageService.PrepareJobDir(jobDir);
        var jobFiles = _dbService.GetJobFiles(jobId);

        foreach (var jobFile in jobFiles)
        {
            string path = Path.Combine(jobDir, jobFile.FileName);
            var fileStream = await _azureBlobService.DownloadBlob(jobFile.BlobPath);
            using (Stream stream = new FileStream(path, FileMode.Create))
            {
                await fileStream.CopyToAsync(stream);
            }

            fileStream.Close();
        }

        var zipPath = Path.Combine(tempDir, $"{jobId}.zip");
        _tempStorageService.ArchiveDirectory(zipPath, jobDir);
        return zipPath;
    }

    public void MoveJobToProduction(string jobId)
    {
        var job = _dbService.GetJob(jobId);
        job.State = JobState.InProduction;
        _dbService.UpdateJob(job);
    }
    
    public string GetJobReport(string jobId)
    {
        var report = _dbService.GetJobReport(jobId);
        return report.Report;
    }

    public void RevertJob(JobRevertRequest model)
    {
        var job = _dbService.GetJob(model.JobId);
        job.State = JobState.Reported;
        _dbService.UpdateJob(job);
        _dbService.SetJobReport(model.JobId, model.JobReport);
    }
}