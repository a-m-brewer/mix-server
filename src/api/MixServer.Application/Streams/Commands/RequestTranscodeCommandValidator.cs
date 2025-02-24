using FluentValidation;

namespace MixServer.Application.Streams.Commands;

public class RequestTranscodeCommandValidator : AbstractValidator<RequestTranscodeCommand>
{
    public RequestTranscodeCommandValidator()
    {
        RuleFor(x => x.AbsoluteFilePath).NotEmpty();
    }
}