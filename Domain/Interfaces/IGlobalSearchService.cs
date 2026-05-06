using Domain;
using Domain.DTO;

namespace SEM.Domain.Interfaces;

public interface IGlobalSearchService
{
    Task<ServiceResult<GlobalSearchResponse>> SearchAsync(GlobalSearchRequest request, Guid userId);
}