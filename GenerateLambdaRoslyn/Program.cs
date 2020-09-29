using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace GenerateLambdaRoslyn
{
    class Program
    {
        static void Main(string[] args)
        {
            //var nestedGeneric = typeof(Dictionary<string, Dictionary<string, int>>);
            //Console.WriteLine(nestedGeneric.FullName);
            //Console.WriteLine(LambdaGenerator.TypeToString(nestedGeneric));

            //var a = 1;
            //var b = 2;

            //Console.WriteLine(Add(a, b));

            var generator = new LambdaGenerator(nameof(Add), typeof(Task), 1, 2, 3, 4, 5, 6);
            //Console.WriteLine(generator.GenerateCode());

            var generatedType = generator.CompileClass();

            var localThis = new Program();

            //Func<int, int, int, int, int, int, Task> funcAdd = Add;
            var argsForConstructor = new object[] { 1, 2, 3, 4, 5, 6 };

            var instantiatedFuncTypeConstructor = FuncConstructorGenerator.GetConstructorInfo(argsForConstructor);

            var funcDel = new Func<int, int, int, int, int, int, Task>(localThis.Add);


            var methodInfo = typeof(Program).GetMethod("Add", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic );

            //var funcAdd = Delegate.CreateDelegate(instantiatedFuncType, methodInfo);

            var generated = Activator.CreateInstance(generatedType, funcDel, argsForConstructor);

            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;

            generatedType.InvokeMember("ToFuncTask", flags, null, generated, null);
        }


        Task Add(int a1, int a2, int a3, int a4, int a5, int a6)
        {
            int acc = a1 + a2 + a3 + a4 + a5 + a6;
            Console.WriteLine(acc);
            return Task.CompletedTask;
        }
    }
}
