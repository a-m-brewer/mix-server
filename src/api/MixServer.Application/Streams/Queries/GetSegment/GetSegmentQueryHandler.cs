using FluentValidation;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Streams.Services;

namespace MixServer.Application.Streams.Queries.GetSegment;

public class GetSegmentQueryHandler(
    ITranscodeService transcodeService,
    IValidator<GetSegmentQuery> validator) : IQueryHandler<GetSegmentQuery, SegmentFileInfo>
{
    public async Task<SegmentFileInfo> HandleAsync(GetSegmentQuery request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        return await transcodeService.GetSegmentAsync(request.Segment);
    }
}