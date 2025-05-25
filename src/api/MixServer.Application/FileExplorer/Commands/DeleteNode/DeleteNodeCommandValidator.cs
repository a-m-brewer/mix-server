using FluentValidation;

namespace MixServer.Application.FileExplorer.Commands.DeleteNode;

public class DeleteNodeCommandValidator : AbstractValidator<DeleteNodeCommand>
{
    public DeleteNodeCommandValidator()
    {
        RuleFor(r => r.NodePath)
            .NotNull();
    }
}