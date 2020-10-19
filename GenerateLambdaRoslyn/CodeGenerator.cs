using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GenerateLambdaRoslyn
{
    public abstract class CodeGenerator
    {
        protected string MethodName { get; set; }

        protected Type[] ArgumentTypes { get; set; }

        protected string GeneratedClassName { get; set; }

        protected abstract string GenerateCode();

        public Type CompileClass(string destinationPath = ".")
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(this.GenerateCode(), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8));

            var assemblyName = $"{this.GeneratedClassName}";
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            using var pdbMs = new MemoryStream();
            var result = compilation.Emit(ms, pdbMs);

            if (!result.Success)
            {
                var failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (var diagnostic in failures)
                {
                    Console.Error.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                }
                return default;
            }

            ms.Seek(0, SeekOrigin.Begin);

            pdbMs.Seek(0, SeekOrigin.Begin);

            destinationPath += (destinationPath[^1] == Path.DirectorySeparatorChar || destinationPath[^1] == Path.AltDirectorySeparatorChar) ? "" : Path.DirectorySeparatorChar.ToString();

            var dllPath = destinationPath + $"{assemblyName}.dll";
            var pdbPath = destinationPath + $"{assemblyName}.pdb";

            File.WriteAllBytes(dllPath, ms.ToArray());

            File.WriteAllBytes(pdbPath, pdbMs.ToArray());

            var assembly = Assembly.LoadFrom(dllPath);

            return assembly.GetTypes().First(t => t.Name == this.GeneratedClassName);
        }
    }
}