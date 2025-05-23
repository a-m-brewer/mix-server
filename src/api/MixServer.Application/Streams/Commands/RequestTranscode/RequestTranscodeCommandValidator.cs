using FluentValidation;

namespace MixServer.Application.Streams.Commands.RequestTranscode;

public class RequestTranscodeCommandValidator : AbstractValidator<RequestTranscodeCommand>
{
    public RequestTranscodeCommandValidator()
    {
        RuleFor(x => x.NodePath).NotNull();
    }
}