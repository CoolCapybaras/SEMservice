using Domain.DTO;
using Microsoft.AspNetCore.Http;

namespace Domain.Interfaces;

public interface IEventAttachmentService
{
    Task<ServiceResult<EventAttachmentResponse>> UploadFileAsync(Guid eventId, Guid userId, IFormFile file, string? title);
    Task<ServiceResult<EventAttachmentResponse>> AddLinkAsync(Guid eventId, Guid userId, EventAttachmentLinkRequest request);
    Task<ServiceResult<List<EventAttachmentResponse>>> GetByEventAsync(Guid eventId, Guid userId, EventAttachmentListQuery? query = null);
    Task<ServiceResult<EventAttachmentFacetsResponse>> GetFacetsAsync(Guid eventId, Guid userId);
    Task<ServiceResult<EventAttachmentResponse>> GetByIdAsync(Guid eventId, Guid attachmentId, Guid userId);
    Task<ServiceResult<bool>> DeleteAsync(Guid eventId, Guid attachmentId, Guid userId);
}