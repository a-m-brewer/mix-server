using FluentValidation;

namespace MixServer.Application.FileExplorer.Commands.SetFolderSort;

public class SetFolderSortCommandValidator : AbstractValidator<SetFolderSortCommand>
{
    public SetFolderSortCommandValidator()
    {
        RuleFor(r => r.AbsoluteFolderPath)
            .NotEmpty();

        RuleFor(r => r.SortMode)
            .IsInEnum();
    }
}