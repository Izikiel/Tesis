using System;
using System.Threading.Tasks;

namespace GenerateLambdaRoslyn
{
    public class TestWrapper
    {
        private readonly Type objectType;
        private readonly object[] constructorArgs;
        private readonly string methodName;
        private readonly object[] methodArgs;
        private readonly Type[] methodArgsTypes;
        private object instance;

        public TestWrapper(Type objectType, object[] constructorArgs, string methodName, params object[] methodArgs)
        {
            this.objectType = objectType; // TODO: Check if it's IClassFixture<T> type. If so, try to get T, construct it, and pass it to the obj constructor.
            this.constructorArgs = constructorArgs ?? new object[] { }; 
            this.methodName = methodName;
            this.methodArgs = methodArgs ?? new object[] { };

            var argsLength = this.methodArgs.Length;

            this.methodArgsTypes = new Type[argsLength];

            for (var i = 0; i < argsLength; i++)
            {
                methodArgsTypes[i] = this.methodArgs[i].GetType();
            }
        }

        public Task Invoke()
        {
            var methodToInvoke = this.objectType.GetMethod(this.methodName, this.methodArgsTypes);

            if (methodToInvoke.ReturnType == typeof(Task))
            {
                this.instance = methodToInvoke.IsStatic ? null : Activator.CreateInstance(this.objectType, this.constructorArgs);

                var taskResult = (Task)methodToInvoke.Invoke(this.instance, this.methodArgs);

                return taskResult.ContinueWith(HandleDispose);
            }

            throw new ArgumentException($"Expected return type of the method is '{typeof(Task).FullName}', got {methodToInvoke.ReturnType.FullName}");
        }

        public void HandleDispose(Task t)
        {
            if (this.instance is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (instance is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().Wait();
            }

            // We do this to ensure that the object is properly disposed and the
            // original stack trace is not lost.
            if (t.IsFaulted)
            {
                t.Wait();
            }
        }
    }
}
