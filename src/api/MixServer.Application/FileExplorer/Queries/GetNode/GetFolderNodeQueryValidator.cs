using FluentValidation;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class GetFolderNodeQueryValidator : AbstractValidator<GetFolderNodeQuery>
{
    public GetFolderNodeQueryValidator()
    {
        RuleFor(r => r.StartIndex)
            .GreaterThanOrEqualTo(0)
            .When(r => r.StartIndex.HasValue);

        RuleFor(r => r.EndIndex)
            .GreaterThan(0)
            .When(r => r.EndIndex.HasValue);

        RuleFor(r => r)
            .Must(r => r.StartIndex.HasValue == r.EndIndex.HasValue)
            .WithMessage("StartIndex and EndIndex must both be provided or both omitted");

        RuleFor(r => r)
            .Must(r => !r.StartIndex.HasValue || !r.EndIndex.HasValue || r.EndIndex > r.StartIndex)
            .WithMessage("EndIndex must be greater than StartIndex");

        RuleFor(r => r)
            .Must(r => !r.StartIndex.HasValue || !r.EndIndex.HasValue || r.EndIndex - r.StartIndex <= 1000)
            .WithMessage("Range size (EndIndex - StartIndex) cannot exceed 1000");
    }
}
