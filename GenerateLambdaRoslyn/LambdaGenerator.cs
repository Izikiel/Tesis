using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GenerateLambdaRoslyn
{
    public class LambdaGenerator
    {
        private string MethodName { get; }

        private Type[] ArgumentTypes { get; }

        private string GeneratedClassName { get; }

        private Type GeneratedType { get; set; }

        public LambdaGenerator(string methodName, Type returnType, params object[] args)
            : this(methodName, returnType, args?.Select(a => a.GetType()).ToArray() ?? Array.Empty<Type>())
        { }

        public LambdaGenerator(string methodName, Type returnType, params Type[] argsTypes)
        {
            if (returnType != typeof(Task))
            {
                throw new ArgumentException("Return type should be Task");
            }

            if (argsTypes.Length > 16)
            {
                throw new ArgumentException("Can't take more than 16 arguments");
            }

            this.MethodName = methodName;
            this.ArgumentTypes = argsTypes ?? Array.Empty<Type>();
            this.GeneratedClassName = $"GeneratedLambda_{this.MethodName}";
        }

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
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                }
                return default;
            }

            ms.Seek(0, SeekOrigin.Begin);

            pdbMs.Seek(0, SeekOrigin.Begin);

            destinationPath += destinationPath[^1] == Path.DirectorySeparatorChar || destinationPath[^1] == Path.AltDirectorySeparatorChar ? "" : Path.DirectorySeparatorChar.ToString();

            var dllPath = destinationPath + $"{assemblyName}.dll";
            var pdbPath = destinationPath + $"{assemblyName}.pdb";

            File.WriteAllBytes(dllPath, ms.ToArray());

            File.WriteAllBytes(pdbPath, pdbMs.ToArray());

            var assembly = Assembly.LoadFrom(dllPath);

            return assembly.GetTypes().First(t => t.Name == this.GeneratedClassName);
        }

        public string GenerateCode()
        {
            var argsBuilder = new StringBuilder();

            var numbers = Enumerable.Range(0, this.ArgumentTypes.Length).Select(n => $"({this.ArgumentTypes[n].Name})args[{n}]");

            argsBuilder.AppendJoin(",", numbers);

            var methodType = this.ToFuncTaskTn();

            string templateCode = @$"
                using System;
                using System.Threading.Tasks;
                namespace GeneratedLambdas{{
                    public class {this.GeneratedClassName}
                    {{ 
                        private object[] args;
                        private {methodType} method; 

                        public {this.GeneratedClassName}({methodType} method, params object[] args)
                        {{ 
                            this.args = args;
                            this.method = method;
                        }}

                        public Task ToFuncTask() => this.method({argsBuilder});
                    }}
                }}";

            return templateCode;
        }

        private string ToFuncTaskTn()
        {
            if (this.ArgumentTypes.Length == 0)
            {
                return "Func<Task>";
            }

            var funcBuilder = new StringBuilder();
            funcBuilder.Append("Func<");

            funcBuilder.AppendJoin(",", this.ArgumentTypes.Select(t => LambdaGenerator.TypeToString(t)));

            funcBuilder.Append(", Task>");

            return funcBuilder.ToString();
        }

        public static string TypeToString(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName;
            }
            const char genericSeparator = '`';

            var genName = type.FullName[0..type.FullName.IndexOf(genericSeparator)];

            var typeBuilder = new StringBuilder();

            typeBuilder.Append(genName);
            typeBuilder.Append("<");

            typeBuilder.AppendJoin(", ", type.GenericTypeArguments.Select(t => LambdaGenerator.TypeToString(t)));

            typeBuilder.Append(">");

            return typeBuilder.ToString();
        }
    }
}
