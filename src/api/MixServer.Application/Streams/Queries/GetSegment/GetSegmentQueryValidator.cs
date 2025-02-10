using FluentValidation;

namespace MixServer.Application.Streams.Queries.GetSegment;

public class GetSegmentQueryValidator : AbstractValidator<GetSegmentQuery>
{
    public GetSegmentQueryValidator()
    {
        RuleFor(r => r.Segment)
            .NotEmpty();
    }
}