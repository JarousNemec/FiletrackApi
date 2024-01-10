using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FiletrackAPI.Entities;
using FiletrackWebInterface.Entities;
using FiletrackWebInterface.Helpers;
using Microsoft.Extensions.Options;

namespace FiletrackAPI.Services;

public interface IAzureBlobService
{
    Task<string> UploadBlob(string fileName, Stream fileStream);
    Task DeleteBlob(string fileName);
    Task<string> UpdateBlob(string existingFileName, string newFileName);
    Task<Stream> DownloadBlob(string filename);
    string GenerateBlobFileName(List<PathMember> path, List<JobAttribute> attributes, string fileName);
}

public class AzureBlobService : IAzureBlobService
{
    private readonly string _connectionString;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobService> _logger;

    public AzureBlobService(IOptions<AppSettings> appSettings,ILogger<AzureBlobService> logger)
    {
        _connectionString = appSettings.Value.BlobConnectionString;
        _containerName = appSettings.Value.BlobContainer;
        _logger = logger;
    }

    public async Task<string> UploadBlob(string fileName,
        Stream fileStream)
    {
        var blobClient = new BlobContainerClient(_connectionString, _containerName);
        var blob = blobClient.GetBlobClient(fileName);
        if (!await blob.ExistsAsync())
            await blob.UploadAsync(fileStream);
        return blob.Uri.AbsoluteUri;
    }

    public async Task DeleteBlob(string fileName)
    {
        var container = new BlobContainerClient(_connectionString, _containerName);
        var blob = container.GetBlobClient(fileName);
        await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }

    public async Task<string> UpdateBlob(string existingFileName,
        string newFileName)
    {
        var blobContainerClient = new BlobContainerClient(_connectionString, _containerName);
        var existingBlobClient = blobContainerClient.GetBlobClient(existingFileName);
        var newBlobClient = blobContainerClient.GetBlobClient(newFileName);
        if (await newBlobClient.ExistsAsync())
        {
            var poller = await newBlobClient.StartCopyFromUriAsync(existingBlobClient.Uri);
            await poller.WaitForCompletionAsync();
            await existingBlobClient.DeleteIfExistsAsync();

            return newBlobClient.Uri.AbsoluteUri;
        }

        return existingBlobClient.Uri.AbsoluteUri;
    }

    public async Task<Stream> DownloadBlob(string filename)
    {
        var blobContainerClient = new BlobContainerClient(_connectionString, _containerName);
        var blobClient = blobContainerClient.GetBlobClient(filename);
        MemoryStream output = new MemoryStream();
        if (await blobClient.ExistsAsync())
            await blobClient.DownloadToAsync(output);
        return output;
    }
    
    public string GenerateBlobFileName(List<PathMember> path, List<JobAttribute> attributes, string fileName)
    {
        string blobFileName = "";
        for (int i = 0; i < path.Count; i++)
        {
            var member = path.FirstOrDefault(x => x.Order == i);
            var value = attributes.FirstOrDefault(x => x.id == member?.Id);
            blobFileName += value?.value;
            blobFileName += "/";
        }

        blobFileName += fileName;
        return blobFileName;
    }
}