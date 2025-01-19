using FluentValidation;

namespace MixServer.Application.FileExplorer.Commands.CopyNode;

public class CopyNodeCommandValidator : AbstractValidator<CopyNodeCommand>
{
    public CopyNodeCommandValidator()
    {
        RuleFor(r => r.SourceAbsolutePath).NotEmpty();
        
        RuleFor(r => r.DestinationFolder).NotEmpty();
        
        RuleFor(r => r.DestinationName).NotEmpty();
    }
}