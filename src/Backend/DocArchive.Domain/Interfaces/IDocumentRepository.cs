using DocArchive.Domain.Entities;

namespace DocArchive.Domain.Interfaces;

public interface IDocumentRepository : IRepository<Document>
{
    Task<Document?> GetByNumberAsync(string number);
    Task<Document?> GetWithVersionsAsync(Guid id);
    Task<Document?> GetWithCommentsAsync(Guid id);
    Task<(IEnumerable<Document> Items, int TotalCount)> GetPagedAsync(
        string? search, string? sortBy, bool descending, int page, int pageSize);
}
