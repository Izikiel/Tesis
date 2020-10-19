using System;
using System.Threading.Tasks;
using Xunit;

namespace TestWrappers.XUnit
{
    public class XUnitTestWrapper : IDisposable
    {
        private readonly Type objectType;
        private readonly Type[] constructorArgsTypes;
        private readonly object[] constructorArgs;
        private readonly string methodName;
        private readonly object[] methodArgs;
        private readonly Type[] methodArgsTypes;

        public XUnitTestWrapper(Type objectType, string methodName, params object[] methodArgs)
        {
            this.objectType = objectType; // TODO: Check what to do regarding ICollectionFixture.
            this.constructorArgsTypes = this.GetConstructorArgsTypes(objectType);
            this.constructorArgs = this.CreateConstructorArgs();
            this.methodName = methodName;
            this.methodArgs = methodArgs;
            this.methodArgsTypes = Type.EmptyTypes;

            var argsLength = this.methodArgs?.Length ?? 0;
            if (argsLength > 0)
            {
                this.methodArgsTypes = new Type[argsLength];

                for (var i = 0; i < argsLength; i++)
                {
                    this.methodArgsTypes[i] = this.methodArgs[i].GetType();
                }
            }
        }

        private Type[] GetConstructorArgsTypes(Type objectType)
        {
            var iClassFixtureInstances = Array.FindAll(objectType.GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IClassFixture<>));

            return Array.ConvertAll(iClassFixtureInstances, instance => instance.GetGenericArguments()[0]);
        }

        private object[] CreateConstructorArgs()
        {
            if (this.constructorArgsTypes is null)
            {
                return null;
            }
            return Array.ConvertAll(this.constructorArgsTypes, typeInstance => Activator.CreateInstance(typeInstance, Type.EmptyTypes));
        }

        public async Task Invoke()
        {
            object instance = null;
            try
            {
                var methodToInvoke = this.objectType.GetMethod(this.methodName, this.methodArgsTypes);

                if (!methodToInvoke.IsStatic)
                {
                    instance = Activator.CreateInstance(this.objectType, this.constructorArgs);
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
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < this.constructorArgs?.Length; i++)
            {
                if (this.constructorArgs[i] is IDisposable disposableArg)
                {
                    disposableArg.Dispose();
                }
            }
        }
    }
}
