using FluentValidation;

namespace MixServer.Application.Sessions.Commands.SetCurrentSession;

public class SetCurrentSessionCommandValidator : AbstractValidator<SetCurrentSessionCommand>
{
    public SetCurrentSessionCommandValidator()
    {
        RuleFor(r => r.AbsoluteFolderPath)
            .NotEmpty()
            .Must(Directory.Exists);

        RuleFor(r => r.FileName)
            .NotEmpty();
        
        RuleFor(r => r)
            .Must(m => File.Exists(Path.Join(m.AbsoluteFolderPath, m.FileName)));
    }
}