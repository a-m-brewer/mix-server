using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Converters;

public interface INodePathDtoConverter :
    IConverter<NodePath, NodePathHeaderDto>,
    IConverter<NodePathRequestDto, NodePath>
{
    NodePathDto ConvertToResponse(NodePath value);
}

public class NodePathDtoConverter : INodePathDtoConverter
{
    public NodePathHeaderDto Convert(NodePath value)
    {
        return new NodePathHeaderDto
        {
            RootPath = value.RootPath,
            RelativePath = value.RelativePath,
            AbsolutePath = value.AbsolutePath
        };
    }

    public NodePath Convert(NodePathRequestDto value)
    {
        if (string.IsNullOrEmpty(value.RootPath) && string.IsNullOrEmpty(value.RelativePath))
            return new NodePath(string.Empty, string.Empty);

        return new NodePath(value.RootPath ?? string.Empty, value.RelativePath ?? string.Empty);
    }

    public NodePathDto ConvertToResponse(NodePath value)
    {
        return new NodePathDto
        {
            RootPath = value.RootPath,
            RelativePath = value.RelativePath,
            FileName = value.FileName,
            AbsolutePath = value.AbsolutePath,
            Extension = value.Extension,
            Parent = Convert(value.Parent),
            IsRoot = value.IsRoot,
            IsRootChild = value.IsRootChild
        };
    }
}