using System;
using System.Threading.Tasks;
using Xunit;

namespace GenerateLambdaRoslyn
{
    public class TestWrapper
    {
        private readonly Type objectType;
        private readonly Type constructorArgsType;
        private readonly string methodName;
        private readonly object[] methodArgs;
        private readonly Type[] methodArgsTypes;

        public TestWrapper(Type objectType, string methodName, params object[] methodArgs)
        {
            this.objectType = objectType; // TODO: Check what to do regarding ICollectionFixture.
            this.constructorArgsType = this.GetConstructorArgsType(objectType);
            this.methodName = methodName;
            this.methodArgs = methodArgs ?? new object[] { };

            var argsLength = this.methodArgs.Length;

            this.methodArgsTypes = new Type[argsLength];

            for (var i = 0; i < argsLength; i++)
            {
                methodArgsTypes[i] = this.methodArgs[i].GetType();
            }
        }

        private Type GetConstructorArgsType(Type objectType)
        {
            var iClassFixtureInstance = Array.Find(objectType.GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IClassFixture<>));

            return iClassFixtureInstance?.GetGenericArguments()[0];
        }

        private object[] CreateConstructorArgs() => this.constructorArgsType is null ? null : new object[] { Activator.CreateInstance(this.constructorArgsType, Type.EmptyTypes) };

        public async Task Invoke()
        {
            object instance = null;
            object[] constructorArgs = null;
            try
            {
                var methodToInvoke = this.objectType.GetMethod(this.methodName, this.methodArgsTypes);

                if (!methodToInvoke.IsStatic)
                {
                    constructorArgs = this.CreateConstructorArgs();
                    instance = Activator.CreateInstance(this.objectType, constructorArgs);
                }

                await (Task)methodToInvoke.Invoke(instance, this.methodArgs);
            }
            finally
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                else if (instance is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }

                if (constructorArgs?.Length > 0)
                {
                    if (constructorArgs[0] is IDisposable disposableArg)
                    {
                        disposableArg.Dispose();
                    }
                    else if (constructorArgs[0] is IAsyncDisposable asyncDisposableArg)
                    {
                        await asyncDisposableArg.DisposeAsync();
                    }
                }
            }
        }
    }
}
