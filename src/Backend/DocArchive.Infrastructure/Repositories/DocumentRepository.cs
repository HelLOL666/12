using DocArchive.Domain.Entities;
using DocArchive.Domain.Interfaces;
using DocArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocArchive.Infrastructure.Repositories;

public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    public DocumentRepository(AppDbContext context) : base(context) { }

    public async Task<Document?> GetByNumberAsync(string number)
        => await Context.Documents.Include(d => d.Author)
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Number == number);

    public async Task<Document?> GetWithVersionsAsync(Guid id)
        => await Context.Documents.Include(d => d.Author)
            .Include(d => d.Versions.OrderByDescending(v => v.VersionNumber))
                .ThenInclude(v => v.UploadedBy)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<Document?> GetWithCommentsAsync(Guid id)
        => await Context.Documents.Include(d => d.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<(IEnumerable<Document> Items, int TotalCount)> GetPagedAsync(
        string? search, string? sortBy, bool descending, int page, int pageSize)
    {
        var query = Context.Documents.Include(d => d.Author)
            .Include(d => d.Versions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(d =>
                d.Number.ToLower().Contains(s) ||
                d.Title.ToLower().Contains(s) ||
                d.Author.FullName.ToLower().Contains(s));
        }

        query = sortBy?.ToLower() switch
        {
            "number" => descending ? query.OrderByDescending(d => d.Number) : query.OrderBy(d => d.Number),
            "title" => descending ? query.OrderByDescending(d => d.Title) : query.OrderBy(d => d.Title),
            "author" => descending ? query.OrderByDescending(d => d.Author.FullName) : query.OrderBy(d => d.Author.FullName),
            "date" => descending ? query.OrderByDescending(d => d.UpdatedAt) : query.OrderBy(d => d.UpdatedAt),
            _ => query.OrderByDescending(d => d.UpdatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }
}
