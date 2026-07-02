using AutoMapper;
using DocArchive.Application.DTOs;
using DocArchive.Application.Interfaces;
using DocArchive.Domain.Entities;
using DocArchive.Domain.Interfaces;

namespace DocArchive.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AuditLogService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AuditLogListResponse> GetAllAsync(int page, int pageSize)
    {
        var allLogs = await _unitOfWork.AuditLogs.GetAllAsync();
        var totalCount = allLogs.Count();
        var items = allLogs.OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        var dtos = _mapper.Map<IEnumerable<AuditLogDto>>(items);
        return new AuditLogListResponse(dtos, totalCount, page, pageSize);
    }
}
