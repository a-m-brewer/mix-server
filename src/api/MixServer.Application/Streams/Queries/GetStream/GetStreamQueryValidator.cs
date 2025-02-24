using FluentValidation;

namespace MixServer.Application.Streams.Queries.GetStream;

public class GetStreamQueryValidator : AbstractValidator<GetStreamQuery>
{
    public GetStreamQueryValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty();

        When(w => Guid.TryParse(w.Id, out _), () =>
        {
            RuleFor(r => r.AccessToken)
                .NotEmpty();
        });
    }
}