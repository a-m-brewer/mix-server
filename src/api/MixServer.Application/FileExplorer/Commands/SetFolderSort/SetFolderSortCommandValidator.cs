using FluentValidation;

namespace MixServer.Application.FileExplorer.Commands.SetFolderSort;

public class SetFolderSortCommandValidator : AbstractValidator<SetFolderSortCommand>
{
    public SetFolderSortCommandValidator()
    {
        RuleFor(r => r.NodePath)
            .NotNull();

        RuleFor(r => r.SortMode)
            .IsInEnum();
    }
}