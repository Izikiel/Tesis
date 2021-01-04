using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace TestWrappers.XUnit
{
    public class XUnitTestWrapper : IDisposable
    {
        private static readonly Type VoidType = typeof(void);
        private static readonly Type TaskType = typeof(Task);

        private readonly Type objectType;
        private readonly Type[] constructorArgsTypes;
        private readonly object[] constructorArgs;
        private readonly object[] methodArgs;
        private readonly MethodInfo methodToInvoke;

        public XUnitTestWrapper(bool _, object instance, string methodName, params object[] methodArgs)
        {
            this.objectType = instance.GetType();

            this.constructorArgsTypes = this.GetConstructorArgsTypesFixture();

            var constructorArgs = this.GetConstructorArgs(instance);

            var generatedConstructorArgs = this.CreateConstructorArgs();

            this.constructorArgs = new object[(generatedConstructorArgs?.Length ?? 0) + (constructorArgs?.Length ?? 0)];

            Array.Copy(constructorArgs, 0, this.constructorArgs, 0, constructorArgs?.Length ?? 0);
            Array.Copy(generatedConstructorArgs, 0, this.constructorArgs, constructorArgs?.Length ?? 0, generatedConstructorArgs?.Length ?? 0);

            this.methodArgs = methodArgs;
            var methodArgsTypes = Type.EmptyTypes;

            var argsLength = this.methodArgs?.Length ?? 0;
            if (argsLength > 0)
            {
                methodArgsTypes = new Type[argsLength];

                for (var i = 0; i < argsLength; i++)
                {
                    methodArgsTypes[i] = this.methodArgs[i].GetType();
                }
            }

            this.methodToInvoke = this.objectType.GetMethod(methodName, methodArgsTypes);
        }

        public XUnitTestWrapper(Type objectType, string methodName, params object[] methodArgs)
        {
            this.objectType = objectType; // TODO: Check what to do regarding ICollectionFixture.
            this.constructorArgsTypes = this.GetConstructorArgsTypesFixture();
            this.constructorArgs = this.CreateConstructorArgs();
            this.methodArgs = methodArgs;
            var methodArgsTypes = Type.EmptyTypes;

            var argsLength = this.methodArgs?.Length ?? 0;
            if (argsLength > 0)
            {
                methodArgsTypes = new Type[argsLength];

                for (var i = 0; i < argsLength; i++)
                {
                    methodArgsTypes[i] = this.methodArgs[i].GetType();
                }
            }

            this.methodToInvoke = this.objectType.GetMethod(methodName, methodArgsTypes);
        }

        private object[] GetConstructorArgs(object instance)
        {
            var fields = instance.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            var constructors = instance.GetType().GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            var constructor = constructors.Length > 0 ? constructors[0] : null;

            var constructorParameters = constructor?.GetParameters();

            var constructorParametersValues = new List<object>();

            foreach (var param in constructorParameters)
            {
                var paramFullName = param.ParameterType.FullName;

                foreach (var field in fields)
                {
                    if (field.FieldType.FullName == paramFullName)
                    {
                        constructorParametersValues.Add(field.GetValue(instance));
                    }
                }
            }

            return constructorParametersValues.ToArray();
        }

        private Type[] GetConstructorArgsTypesFixture()
        {
            var iClassFixtureInstances = Array.FindAll(this.objectType.GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IClassFixture<>));

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
                if (!this.methodToInvoke.IsStatic)
                {
                    instance = Activator.CreateInstance(this.objectType, this.constructorArgs);
                }

                if (this.methodToInvoke.ReturnType == XUnitTestWrapper.VoidType)
                {
                    this.methodToInvoke.Invoke(instance, this.methodArgs);
                }

                else if (this.methodToInvoke.ReturnType == XUnitTestWrapper.TaskType)
                {
                    await ((Task)this.methodToInvoke.Invoke(instance, this.methodArgs)).ConfigureAwait(false);
                }
            }
            finally
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else if (instance is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            this.Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

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
