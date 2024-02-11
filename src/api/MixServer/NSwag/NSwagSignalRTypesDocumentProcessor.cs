using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace MixServer.NSwag;

/// <summary>
/// This class will scan an interface for Dto classes and ensure that
/// they are included in the generated Open-API Document. This is for SignalR types that
/// will not be automatically included in the document due to not being used by a controller.
/// </summary>
public class NSwagSignalRTypesDocumentProcessor<THub, TClient> : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        var signalRTypes = GetSignalRTypes();

        foreach (var type in signalRTypes)
        {
            if (!context.SchemaResolver.HasSchema(type, false))
            {
                context.SchemaGenerator.Generate(type, context.SchemaResolver);
            }
        }
    }

    private IEnumerable<Type> GetSignalRTypes()
    {
        var types = from m in typeof(TClient).GetMethods()
                .Concat(typeof(THub).GetMethods())
            from p in m.GetParameters()
            from t in GetConcreteTypesRecursively(p.ParameterType)
            where FullNameEndsWith(t, "Dto", "Command", "Query", "Response")
            select t;

        return types.Distinct();
    }

    /// <summary>
    /// For the supplied type, return:
    /// - the type itself, if concrete
    /// - if the type is generic, the concrete types of its arguments,
    ///   recursively where argument types are themselves generic
    /// </summary>
    private List<Type> GetConcreteTypesRecursively(Type type, List<Type>? list = null)
    {
        list ??= new List<Type>();

        if (type.IsGenericType)
        {
            var genericArguments = type.GetGenericArguments();
            list.AddRange(genericArguments.Where(a => !a.IsGenericType));

            foreach (var genericArgument in genericArguments.Where(a => a.IsGenericType))
            {
                GetConcreteTypesRecursively(genericArgument, list);
            }
        }
        else
        {
            list.Add(type);
        }

        return list;
    }

    private static bool FullNameEndsWith(Type type, params string[] suffixes)
    {
        return suffixes.Any(suffix => type.FullName?.EndsWith(suffix) ?? false);
    }
}