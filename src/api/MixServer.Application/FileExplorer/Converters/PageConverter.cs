using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Converters;

public interface IPageConverter
    : IConverter<PageDto, Page>;

public class PageConverter : IPageConverter
{
    public Page Convert(PageDto value)
    {
        return new Page
        {
            PageIndex = value.PageIndex,
            PageSize = value.PageSize
        };
    }
}