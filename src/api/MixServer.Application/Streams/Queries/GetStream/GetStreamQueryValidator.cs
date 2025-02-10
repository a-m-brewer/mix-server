using FluentValidation;

namespace MixServer.Application.Streams.Queries.GetStream;

public class GetStreamQueryValidator : AbstractValidator<GetStreamQuery>
{
    public GetStreamQueryValidator()
    {
        RuleFor(r => r.PlaybackSessionId)
            .NotEmpty();

        RuleFor(r => r.AccessToken)
            .NotEmpty();
    }
}