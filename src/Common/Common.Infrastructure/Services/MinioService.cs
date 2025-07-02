using System.Net;
using Common.Application.Services;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Common.Infrastructure.Services;

public class MinioService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioService> _logger;
    private readonly IExternalMinioService _externalMinioService;

    public MinioService(IMinioClient minioClient, ILogger<MinioService> logger,
        IExternalMinioService externalMinioService)
    {
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _externalMinioService = externalMinioService;
    }

    public async Task<bool> UploadFileAsync(string bucketName, string objectName, Stream stream,
        string contentType, CancellationToken ct = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(bucketName);
            ArgumentException.ThrowIfNullOrEmpty(objectName);
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentException.ThrowIfNullOrEmpty(contentType);

            await EnsureBucketExistsAsync(bucketName, ct);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);

            var response = await _minioClient.PutObjectAsync(putObjectArgs, ct);

            if (response.ResponseStatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Failed to upload object {ObjectName} to bucket {BucketName}, {ResponseContent}",
                    objectName, bucketName, response.ResponseContent);
                return false;
            }

            _logger.LogInformation("Successfully uploaded object {ObjectName} to bucket {BucketName}",
                objectName, bucketName);

            return true;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to upload object {ObjectName} to bucket {BucketName}",
                objectName, bucketName);
            throw new MinioServiceException($"Failed to upload file {objectName}", ex);
        }
    }

    public async Task<Stream> GetFileAsync(string bucketName, string objectName,
        CancellationToken ct = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(bucketName);
            ArgumentException.ThrowIfNullOrEmpty(objectName);

            if (!await DoesObjectExistAsync(bucketName, objectName, ct))
            {
                throw new FileNotFoundException($"Object {objectName} not found in bucket {bucketName}");
            }

            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                });

            await _minioClient.GetObjectAsync(getObjectArgs, ct);

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to get object {ObjectName} from bucket {BucketName}",
                objectName, bucketName);
            throw new MinioServiceException($"Failed to get file {objectName}", ex);
        }
    }

    public async Task<bool> DeleteFileAsync(string bucketName, string objectName,
        CancellationToken ct = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(bucketName);
            ArgumentException.ThrowIfNullOrEmpty(objectName);

            if (!await DoesObjectExistAsync(bucketName, objectName, ct))
            {
                return false;
            }

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, ct);
            _logger.LogInformation("Successfully deleted object {ObjectName} from bucket {BucketName}",
                objectName, bucketName);

            return true;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to delete object {ObjectName} from bucket {BucketName}",
                objectName, bucketName);
            throw new MinioServiceException($"Failed to delete file {objectName}", ex);
        }
    }

    public async Task<bool> UpdateFileAsync(string bucketName, string objectName, Stream stream,
        string contentType, CancellationToken ct = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(bucketName);
            ArgumentException.ThrowIfNullOrEmpty(objectName);
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentException.ThrowIfNullOrEmpty(contentType);

            if (!await DoesObjectExistAsync(bucketName, objectName, ct))
            {
                throw new FileNotFoundException($"Object {objectName} not found in bucket {bucketName}");
            }

            // MinIO doesn't have a direct update operation, so we'll delete and re-upload
            await DeleteFileAsync(bucketName, objectName, ct);
            return await UploadFileAsync(bucketName, objectName, stream, contentType, ct);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to update object {ObjectName} in bucket {BucketName}",
                objectName, bucketName);
            throw new MinioServiceException($"Failed to update file {objectName}", ex);
        }
    }

    public async Task<bool> DoesBucketExistAsync(string bucketName, CancellationToken ct = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(bucketName);

            var beArgs = new BucketExistsArgs()
                .WithBucket(bucketName);

            return await _minioClient.BucketExistsAsync(beArgs, ct);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to check existence of bucket {BucketName}", bucketName);
            throw new MinioServiceException($"Failed to check bucket existence {bucketName}", ex);
        }
    }

    public async Task<bool> DoesObjectExistAsync(string bucketName, string objectName,
        CancellationToken ct = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(bucketName);
            ArgumentException.ThrowIfNullOrEmpty(objectName);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statObjectArgs, ct);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to check existence of object {ObjectName} in bucket {BucketName}",
                objectName, bucketName);
            throw new MinioServiceException($"Failed to check object existence {objectName}", ex);
        }
    }

    public async Task<string> GetPreSignedUrlAsync(string bucketName, string objectName,
        int expiryMinutes = 60)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(bucketName);
            ArgumentException.ThrowIfNullOrEmpty(objectName);

            var presignedArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expiryMinutes * 60);

            return await _externalMinioService
                .MinioClient()
                .PresignedGetObjectAsync(presignedArgs);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for object {ObjectName} in bucket {BucketName}",
                objectName, bucketName);
            throw new MinioServiceException($"Failed to generate presigned URL for {objectName}", ex);
        }
    }

    public async Task<IReadOnlyList<string>> ListObjectsAsync(string bucketName, string? prefix = null,
        bool recursive = true, CancellationToken ct = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(bucketName);

            var listArgs = new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithPrefix(prefix)
                .WithRecursive(recursive);

            var objects = new List<string>();
            var items = _minioClient.ListObjectsEnumAsync(listArgs, ct);
            await foreach (var item in items)
            {
                objects.Add(item.Key);
            }

            return objects.AsReadOnly();
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "Failed to list objects in bucket {BucketName}", bucketName);
            throw new MinioServiceException($"Failed to list objects in bucket {bucketName}", ex);
        }
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct)
    {
        if (!await DoesBucketExistAsync(bucketName, ct))
        {
            var mbArgs = new MakeBucketArgs()
                .WithBucket(bucketName);
            await _minioClient.MakeBucketAsync(mbArgs, ct);
            _logger.LogInformation("Created new bucket {BucketName}", bucketName);
        }
    }
}

public class MinioServiceException : Exception
{
    public MinioServiceException(string message) : base(message)
    {
    }

    public MinioServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}