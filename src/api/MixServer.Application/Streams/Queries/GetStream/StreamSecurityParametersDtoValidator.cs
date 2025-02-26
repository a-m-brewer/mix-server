using FluentValidation;

namespace MixServer.Application.Streams.Queries.GetStream;

public class StreamSecurityParametersDtoValidator : AbstractValidator<StreamSecurityParametersDto>
{
    public StreamSecurityParametersDtoValidator()
    {
        RuleFor(x => x.Key).NotEmpty();
        RuleFor(x => x.Expires).NotEmpty();
        RuleFor(r => r.DeviceId).NotEmpty();
    }
}