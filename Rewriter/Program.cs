using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Mono.Cecil;

namespace Rewriter
{
    static class Program
    {
        public class Options
        {
            [Option("test-dll-to-rewrite", HelpText = "Path to the DLL to rewrite")]
            public string TestDllToRewrite { get; set; }
        }


        static void Main(string[] args)
        {
            const string templatesFilePath = "D:/tesis/rewriteTest/TestWrappers/bin/Debug/net5.0/TestWrappers.dll";

            var filePath = string.Empty;

            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed(o =>
                   {
                       if (!string.IsNullOrWhiteSpace(o.TestDllToRewrite))
                       {
                           filePath = o.TestDllToRewrite;
                       }
                   });

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Missing dll to rewrite");
            }

            using var fileStream = File.Open(filePath, FileMode.Open);

            Directory.SetCurrentDirectory(Path.GetDirectoryName(filePath));

            var testsAssembly = AssemblyDefinition.ReadAssembly(fileStream, new ReaderParameters(ReadingMode.Immediate));

            Console.WriteLine($"{testsAssembly}");

            var transforms = new List<XUnitTransformation>
            {
                new XUnitTransformation()
            };

            foreach (var module in testsAssembly.Modules)
            {
                foreach (var type in module.Types)
                {
                    for (var i = 0; i < type.Methods.Count; i++)
                    {
                        var method = type.Methods[i];

                        foreach (var transform in transforms)
                        {
                            if(transform.Apply(method))
                            {
                                Console.WriteLine($"Rewritten method: '{method}'");
                            }
                        }
                    }
                }
            }

            fileStream.Seek(0, SeekOrigin.Begin);

            testsAssembly.Write(fileStream);

            File.Copy(
                sourceFileName: templatesFilePath,
                destFileName: Path.GetDirectoryName(filePath) + Path.DirectorySeparatorChar + Path.GetFileName(templatesFilePath),
                overwrite: true);
        }
    }
}
