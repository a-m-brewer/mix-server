using FluentValidation;

namespace MixServer.Application.Sessions.Queries.GetUsersSessions;

public class GetUsersSessionsQueryValidator : AbstractValidator<GetUsersSessionsQuery>
{
    public GetUsersSessionsQueryValidator()
    {
        RuleFor(r => r.StartIndex)
            .GreaterThanOrEqualTo(0);

        RuleFor(r => r.EndIndex)
            .GreaterThan(0);
        
        RuleFor(r => r)
            .Must(r => r.EndIndex > r.StartIndex)
            .WithMessage("EndIndex must be greater than StartIndex");
        
        RuleFor(r => r)
            .Must(r => r.EndIndex - r.StartIndex <= 1000)
            .WithMessage("Range size (EndIndex - StartIndex) cannot exceed 1000");
    }
}