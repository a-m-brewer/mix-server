using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Converters;

public interface INodePathDtoConverter : IConverter<NodePath, NodePathDto>, IConverter<NodePathDto, NodePath>;

public class NodePathDtoConverter : INodePathDtoConverter
{
    public NodePathDto Convert(NodePath value)
    {
        return new NodePathDto
        {
            RootPath = value.RootPath,
            RelativePath = value.RelativePath
        };
    }

    public NodePath Convert(NodePathDto value)
    {
        return new NodePath(value.RootPath, value.RelativePath);
    }
}