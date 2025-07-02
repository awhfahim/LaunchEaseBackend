using Minio;

namespace Common.Application.Services;

public interface IExternalMinioService
{
    IMinioClient MinioClient();
}