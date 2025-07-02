namespace Common.Application.Services;

public interface IFileStorageService
{
    Task<bool> UploadFileAsync(string bucketName, string objectName, Stream stream, string contentType,
        CancellationToken ct = default);

    Task<Stream> GetFileAsync(string bucketName, string objectName, CancellationToken ct = default);
    Task<bool> DeleteFileAsync(string bucketName, string objectName, CancellationToken ct = default);

    Task<bool> UpdateFileAsync(string bucketName, string objectName, Stream stream, string contentType,
        CancellationToken ct = default);

    Task<bool> DoesBucketExistAsync(string bucketName, CancellationToken ct = default);
    Task<bool> DoesObjectExistAsync(string bucketName, string objectName, CancellationToken ct = default);
    Task<string> GetPreSignedUrlAsync(string bucketName, string objectName, int expiryMinutes = 60);

    Task<IReadOnlyList<string>> ListObjectsAsync(string bucketName, string? prefix = null, bool recursive = true,
        CancellationToken ct = default);
}