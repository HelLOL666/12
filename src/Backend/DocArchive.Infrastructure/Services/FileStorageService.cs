using DocArchive.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DocArchive.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _storageRoot;

    public FileStorageService(IConfiguration configuration)
    {
        _storageRoot = configuration["Storage:Path"] ?? "/app/storage";
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string subFolder)
    {
        var folder = Path.Combine(_storageRoot, subFolder);
        Directory.CreateDirectory(folder);

        var uniqueName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(folder, uniqueName);

        await using var outputStream = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream);

        return Path.Combine(subFolder, uniqueName);
    }

    public Task<Stream> GetFileAsync(string storagePath)
    {
        var fullPath = Path.Combine(_storageRoot, storagePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found", fullPath);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteFileAsync(string storagePath)
    {
        var fullPath = Path.Combine(_storageRoot, storagePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetStorageRoot() => _storageRoot;
}
