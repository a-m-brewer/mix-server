using FluentValidation;

namespace MixServer.Application.Sessions.Commands.SetCurrentSession;

public class SetCurrentSessionCommandValidator : AbstractValidator<SetCurrentSessionCommand>
{
    public SetCurrentSessionCommandValidator()
    {
        RuleFor(r => r.NodePath.RelativePath)
            .NotEmpty();

        RuleFor(r => r.NodePath.RootPath)
            .NotEmpty();
        
        RuleFor(r => r)
            .Must(m => File.Exists(Path.Join(m.NodePath.RootPath, m.NodePath.RelativePath)));
    }
}