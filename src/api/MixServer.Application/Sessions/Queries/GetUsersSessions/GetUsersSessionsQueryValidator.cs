using FluentValidation;

namespace MixServer.Application.Sessions.Queries.GetUsersSessions;

public class GetUsersSessionsQueryValidator : AbstractValidator<GetUsersSessionsQuery>
{
    public GetUsersSessionsQueryValidator()
    {
        RuleFor(r => r.StartIndex)
            .GreaterThanOrEqualTo(0);

        RuleFor(r => r.EndIndex)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000);
        
        RuleFor(r => r)
            .Must(r => r.EndIndex > r.StartIndex)
            .WithMessage("EndIndex must be greater than StartIndex");
    }
}