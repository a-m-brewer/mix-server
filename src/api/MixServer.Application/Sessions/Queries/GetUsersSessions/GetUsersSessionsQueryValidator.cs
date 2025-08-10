using FluentValidation;

namespace MixServer.Application.Sessions.Queries.GetUsersSessions;

public class GetUsersSessionsQueryValidator : AbstractValidator<GetUsersSessionsQuery>
{
    public GetUsersSessionsQueryValidator()
    {
        RuleFor(r => r.PageIndex)
            .GreaterThanOrEqualTo(0);

        RuleFor(r => r.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
    }
}