using FluentValidation;

namespace MixServer.Application.FileExplorer.Commands.CopyNode;

public class CopyNodeCommandValidator : AbstractValidator<CopyNodeCommand>
{
    public CopyNodeCommandValidator()
    {
        RuleFor(r => r.SourcePath)
            .NotNull();
        
        RuleFor(r => r.DestinationPath)
            .NotNull();
    }
}