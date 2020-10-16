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

        public TestWrapper(Type objectType, string methodName, params object[] methodArgs)
        {
            this.objectType = objectType; // TODO: Check if it's IClassFixture<T> type. If so, try to get T, construct it, and pass it to the obj constructor.
            this.constructorArgs = null; // new object[] { };
            this.methodName = methodName;
            this.methodArgs = methodArgs ?? new object[] { };

            var argsLength = this.methodArgs.Length;

            this.methodArgsTypes = new Type[argsLength];

            for (var i = 0; i < argsLength; i++)
            {
                methodArgsTypes[i] = this.methodArgs[i].GetType();
            }
        }

        public async Task Invoke()
        {
            try
            {
                var methodToInvoke = this.objectType.GetMethod(this.methodName, this.methodArgsTypes);

                this.instance = methodToInvoke.IsStatic ? null : Activator.CreateInstance(this.objectType, this.constructorArgs);

                await (Task)methodToInvoke.Invoke(this.instance, this.methodArgs);
            }
            finally
            {
                if (this.instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                else if (instance is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
            }
        }
    }
}
