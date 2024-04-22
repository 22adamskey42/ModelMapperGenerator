using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ModelMapperGenerator.Attributes;
using System.Reflection;

namespace ModelMapperGenerator.TestInfrastructure
{
    public static class CompilationBuilder
    {
        public static Compilation CreateCompilation(string source)
        {
            SyntaxTree parsedSyntaxTree = CSharpSyntaxTree.ParseText(source);
            SyntaxTree[] syntaxTrees = [parsedSyntaxTree];
            Assembly[] currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            PortableExecutableReference binderReference = CreateReference(typeof(Binder));
            PortableExecutableReference attrReference = CreateReference(typeof(ModelGenerationTargetAttribute));
            PortableExecutableReference netstandard = CreateReference("netstandard", currentAssemblies);
            PortableExecutableReference runtime = CreateReference("System.Runtime", currentAssemblies);

            PortableExecutableReference[] references = [binderReference, attrReference, runtime, netstandard];
            CSharpCompilationOptions options = new(OutputKind.DynamicallyLinkedLibrary);

            Compilation compilation = CSharpCompilation.Create("testCompilation", syntaxTrees, references, options);
            return compilation;
        }

        private static PortableExecutableReference CreateReference(string assemblyName, Assembly[] assemblies)
        {
            Assembly? assembly = assemblies.FirstOrDefault(x => x.GetName().FullName.Contains(assemblyName));
            if (assembly is null)
            {
                throw new NullReferenceException(nameof(assembly));
            }

            PortableExecutableReference reference = MetadataReference.CreateFromFile(assembly.Location);
            return reference;
        }

        private static PortableExecutableReference CreateReference(Type type)
        {
            string path = type.GetTypeInfo().Assembly.Location;
            PortableExecutableReference reference = MetadataReference.CreateFromFile(path);
            return reference;
        }
    }
}
