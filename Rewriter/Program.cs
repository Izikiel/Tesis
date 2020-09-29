using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace Rewriter
{
    class Program
    {
        static void Main(string[] args)
        {

            var filePath = "D:/tesis/rewriteTest/test/bin/Debug/netcoreapp3.1/test.dll";

            using var fileStream = File.Open(filePath, FileMode.Open);

            var testsAssembly = AssemblyDefinition.ReadAssembly(fileStream, new ReaderParameters(ReadingMode.Immediate));

            var templatesFilePath = "D:/tesis/rewriteTest/CoyoteTemplates/bin/Debug/netcoreapp3.1/CoyoteTemplates.dll";

            using var templateStream = File.Open(templatesFilePath, FileMode.Open);

            var templateAssembly = AssemblyDefinition.ReadAssembly(templateStream, new ReaderParameters(ReadingMode.Immediate));

            Console.WriteLine($"{testsAssembly}");

            var factTransform = new FactTransformation(templateAssembly);
            var theoryTransform = new TheoryTransformation(templateAssembly);

            foreach (var module in testsAssembly.Modules)
            {
                foreach (var type in module.Types)
                {
                    for (var i = 0; i < type.Methods.Count; i++)
                    {
                        var method = type.Methods[i];
                        Console.WriteLine(method);
                        theoryTransform.Apply(method);
                    }
                }
            }


            fileStream.Seek(0, SeekOrigin.Begin);

            
            testsAssembly.Write(fileStream);

            //foreach (var module in testsAssembly.Modules)
            //{
            //    foreach (var type in module.Types)
            //    {
            //        if (type.Name.Contains("<Module>"))
            //        {
            //            continue;
            //        }
            //        Console.WriteLine(type.FullName);

            //        var printer = type.Methods.First(m => m.Name.Contains("Printer"));
            //        var hello = type.Methods.First(m => m.Name.Contains("Hello"));

            //        // Renombrar el metodo original a _methodName
            //        // Nombrar mi metodo con el nombre del original
            //        // Crear el cuerpo de mi metodo para q llame al original.
            //        var originalMethodName = printer.Name;
            //        var internalMethodName = $"__{printer.Name}";
            //        printer.Name = internalMethodName;

            //        hello.Name = originalMethodName;

            //        var actionType = typeof(Action);
            //        var actionTypeReference = module.ImportReference(actionType);
            //        var delegateToInsert = new VariableDefinition(actionTypeReference);

            //        for (int i = 0; i < hello.Body.Instructions.Count; i++)
            //        {
            //            var instruction = hello.Body.Instructions[i];
            //            if (instruction.OpCode == OpCodes.Ldftn)
            //            {
            //                instruction.Operand = printer;
            //            }
            //        }

            //        foreach (var m in type.Methods)
            //        {
            //            if (m.Name.Contains(originalMethodName))
            //                continue;
            //            if (m.Name.Contains(internalMethodName))
            //                continue;

            //            var body = m.Body;

            //            for (int i = 0; i < body.Instructions.Count; i++)
            //            {
            //                var operand = (MethodReference)body.Instructions[i].Operand;
            //                if (operand?.Name == printer.Name)
            //                {
            //                    body.Instructions[i].Operand = hello;
            //                }
            //            }
            //        }
            //    }
            //    break;
            //}

            //fileStream.Seek(0, SeekOrigin.Begin);

            //testsAssembly.Write(fileStream);
        }
    }
}
