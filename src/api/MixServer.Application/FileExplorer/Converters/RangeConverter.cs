using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.Interfaces;
using Range = MixServer.Domain.FileExplorer.Models.Range;

namespace MixServer.Application.FileExplorer.Converters;

public interface IRangeConverter
    : IConverter<RangeDto, Range>;

public class RangeConverter : IRangeConverter
{
    public Range Convert(RangeDto value)
    {
        return new Range
        {
            Start = value.Start,
            End = value.End
        };
    }
}