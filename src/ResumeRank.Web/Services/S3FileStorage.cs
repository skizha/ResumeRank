using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace ResumeRank.Web.Services;

/// <summary>
/// Configuration options for S3 file storage.
/// </summary>
public class S3StorageOptions
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
}

/// <summary>
/// Interface for file storage operations.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Uploads a file and returns the storage path.
    /// </summary>
    Task<string> UploadAsync(string jobId, string fileName, Stream content, string contentType);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    Task DeleteAsync(string filePath);

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    Task<bool> ExistsAsync(string filePath);
}

/// <summary>
/// S3-based file storage for resume uploads.
/// </summary>
public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3FileStorage> _logger;

    public S3FileStorage(
        IAmazonS3 s3Client,
        IOptions<S3StorageOptions> options,
        ILogger<S3FileStorage> logger)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(string jobId, string fileName, Stream content, string contentType)
    {
        var key = $"resumes/{jobId}/{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

            await _s3Client.PutObjectAsync(request);
            _logger.LogInformation("Uploaded file to S3: {Key}", key);

            // Return the S3 key (not full URI) - Lambda will use the configured bucket
            return key;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to S3: {Key}", key);
            throw new InvalidOperationException($"Failed to upload file to S3: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(string filePath)
    {
        // Handle both S3 URI and plain key formats
        var key = filePath.StartsWith("s3://")
            ? ParseS3Uri(filePath).Key
            : filePath;

        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);
            _logger.LogInformation("Deleted file from S3: {Key}", key);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from S3: {Key}", key);
            throw new InvalidOperationException($"Failed to delete file from S3: {ex.Message}", ex);
        }
    }

    public async Task<bool> ExistsAsync(string filePath)
    {
        var key = filePath.StartsWith("s3://")
            ? ParseS3Uri(filePath).Key
            : filePath;

        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to check file existence in S3: {Key}", key);
            throw;
        }
    }

    private static (string Bucket, string Key) ParseS3Uri(string s3Uri)
    {
        if (!s3Uri.StartsWith("s3://"))
            throw new ArgumentException("Invalid S3 URI format", nameof(s3Uri));

        var path = s3Uri.Substring(5);
        var separatorIndex = path.IndexOf('/');
        if (separatorIndex < 0)
            throw new ArgumentException("Invalid S3 URI format", nameof(s3Uri));

        return (path.Substring(0, separatorIndex), path.Substring(separatorIndex + 1));
    }
}

/// <summary>
/// Local file system storage (default for local development).
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _uploadPath;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IConfiguration configuration, ILogger<LocalFileStorage> logger)
    {
        _uploadPath = configuration["FileStorage:UploadPath"] ?? "uploads";
        _logger = logger;
    }

    public async Task<string> UploadAsync(string jobId, string fileName, Stream content, string contentType)
    {
        var directory = Path.Combine(_uploadPath, jobId);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{Guid.NewGuid()}{Path.GetExtension(fileName)}");

        using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream);

        _logger.LogInformation("Uploaded file locally: {FilePath}", filePath);
        return filePath;
    }

    public Task DeleteAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted local file: {FilePath}", filePath);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath));
    }
}
