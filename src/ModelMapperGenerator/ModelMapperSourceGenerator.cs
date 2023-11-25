using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using ModelMapperGenerator.CompilationElementBuilders;
using ModelMapperGenerator.Descriptors;
using System.Runtime.CompilerServices;
using ModelMapperGenerator.Constants;

[assembly: InternalsVisibleTo("ModelMapperGenerator.UnitTests")]

namespace ModelMapperGenerator
{
    [Generator]
    internal sealed class ModelMapperSourceGenerator : IIncrementalGenerator
    {
        public ModelMapperSourceGenerator()
        {
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        { 
            IncrementalValuesProvider<TargetAttributeDescriptor> targetAttributeProvider = context.SyntaxProvider.ForAttributeWithMetadataName<TargetAttributeDescriptor?>(
                fullyQualifiedMetadataName: AttributeNamesConstants.TargetAttributeFullyQualifiedName,
                predicate: static (_, _) => true,
                transform: static (context, cancellationToken) =>
                {
                    INamedTypeSymbol targetSymbol = (INamedTypeSymbol)context.TargetSymbol;
                    TargetAttributeDescriptor? descriptor = TargetAttributeDescriptor.Create(targetSymbol);

                    return descriptor;
                })
                .Where(x => x is not null)
                .WithTrackingName(TrackingStepsConstants.GetTargetTypesStep)!;

            IncrementalValueProvider<ImmutableArray<TargetAttributeDescriptor>> collectedTargetAttributeProvider = targetAttributeProvider.Collect().WithTrackingName(TrackingStepsConstants.CollectTargetDescriptors)!;

            context.RegisterSourceOutput(collectedTargetAttributeProvider, Execute);
        }

        private static void Execute(SourceProductionContext context, ImmutableArray<TargetAttributeDescriptor> targets)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (targets.Length == 0)
            {
                return;
            }

            bool targetsValid = CheckTargetsValid(ref targets);
            if (!targetsValid)
            {
                return;
            }

            foreach (var target in targets)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                foreach (var source in target.ContainedTypes)
                {
                    if (source.Symbol.TypeKind == TypeKind.Enum)
                    {
                        EnumCompilationBuilder.BuildCompilationElements(source, target, ref context);
                    }
                    else if (source.Symbol.TypeKind == TypeKind.Class)
                    {
                        source.MarkRelatedProperties(target);
                        ClassCompilationBuilder.BuildCompilationElements(source, target, ref context);
                    }
                }
            }
        }

        private static bool CheckTargetsValid(ref ImmutableArray<TargetAttributeDescriptor> targets)
        {
            int len = targets.Length;

            for (int i = 0; i < len; i++)
            {
                TargetAttributeDescriptor outer = targets[i];

                for (int j = 0; j < len; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    TargetAttributeDescriptor inner = targets[j];

                    if (outer.TargetNamespace == inner.TargetNamespace)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
