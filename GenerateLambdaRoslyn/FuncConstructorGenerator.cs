using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GenerateLambdaRoslyn
{
    public class FuncConstructorGenerator
    {
        static Type[] FuncTypes { get; } = new Type[]
        {
            typeof(Func<>),
            typeof(Func<,>),
            typeof(Func<,,>),
            typeof(Func<,,,>),
            typeof(Func<,,,,>),
            typeof(Func<,,,,,>),
            typeof(Func<,,,,,,>),
            typeof(Func<,,,,,,,>),
            typeof(Func<,,,,,,,,>),
            typeof(Func<,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,,>),
        };

        public static Type InstatiatedFuncType(params object[] argsForConstructor)
        {
            var typeArguments = argsForConstructor?.Select(a => a.GetType()).ToArray() ?? Array.Empty<Type>();

            return FuncConstructorGenerator.InstatiatedFuncType(typeArguments);
        }

        public static ConstructorInfo GetConstructorInfo(params object[] argsForConstructor)
        {
            var typeArguments = argsForConstructor?.Select(a => a.GetType()).ToArray() ?? Array.Empty<Type>();

            return FuncConstructorGenerator.GetConstructorInfo(typeArguments);
        }

        public static Type InstatiatedFuncType(params Type[] argsForConstructor)
        {
            argsForConstructor ??= Array.Empty<Type>();

            var funcType = FuncTypes[argsForConstructor.Length];

            var typeArguments = argsForConstructor.Append(typeof(Task)).ToArray();

            return funcType.MakeGenericType(typeArguments);
        }

        public static ConstructorInfo GetConstructorInfo(params Type[] argsForConstructor)
        {
            argsForConstructor ??= Array.Empty<Type>();

            var funcType = FuncTypes[argsForConstructor.Length];

            var typeArguments = argsForConstructor.Append(typeof(Task)).ToArray();

            var instantiatedFuncType = funcType.MakeGenericType(typeArguments);

            return instantiatedFuncType.GetConstructors()[0];
        }
    }
}
