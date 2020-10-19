﻿using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace Rewriter
{
    static class Program
    {
        static void Main(string[] args)
        {
            const string filePath = "D:/tesis/rewriteTest/test/bin/Debug/netcoreapp3.1/test.dll";
            const string templatesFilePath = "D:/tesis/rewriteTest/TestWrappers/bin/Debug/netcoreapp3.1/TestWrappers.dll";

            using var fileStream = File.Open(filePath, FileMode.Open);

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
                        Console.WriteLine(method);

                        foreach (var transform in transforms)
                        {
                            transform.Apply(method);
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
