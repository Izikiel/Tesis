using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateLambdaRoslyn
{
    public class LambdaGenerator : CodeGenerator
    {
        public LambdaGenerator(string methodName, Type returnType, params object[] args)
            : this(methodName, returnType, args?.Select(a => a.GetType()).ToArray() ?? Array.Empty<Type>())
        { }

        public LambdaGenerator(string methodName, Type returnType, params Type[] argsTypes)
        {
            if (returnType != typeof(Task))
            {
                throw new ArgumentException("Return type should be Task");
            }

            if (argsTypes.Length > 16)
            {
                throw new ArgumentException("Can't take more than 16 arguments");
            }

            this.MethodName = methodName;
            this.ArgumentTypes = argsTypes ?? Array.Empty<Type>();
            this.GeneratedClassName = $"GeneratedLambda_{this.MethodName}";
        }
        
        protected override string GenerateCode()
        {
            var argsBuilder = new StringBuilder();

            var numbers = Enumerable.Range(0, this.ArgumentTypes.Length).Select(n => $"({this.ArgumentTypes[n].Name})args[{n}]");

            argsBuilder.AppendJoin(",", numbers);

            var methodType = this.ToFuncTaskTn();

            string templateCode = @$"
                using System;
                using System.Threading.Tasks;
                namespace GeneratedLambdas{{
                    public class {this.GeneratedClassName}
                    {{ 
                        private object[] args;
                        private {methodType} method; 

                        public {this.GeneratedClassName}({methodType} method, params object[] args)
                        {{ 
                            this.args = args;
                            this.method = method;
                        }}

                        public Task ToFuncTask() => this.method({argsBuilder});
                    }}
                }}";

            return templateCode;
        }

        private string ToFuncTaskTn()
        {
            if (this.ArgumentTypes.Length == 0)
            {
                return "Func<Task>";
            }

            var funcBuilder = new StringBuilder();
            funcBuilder.Append("Func<");

            funcBuilder.AppendJoin(",", this.ArgumentTypes.Select(t => LambdaGenerator.TypeToString(t)));

            funcBuilder.Append(", Task>");

            return funcBuilder.ToString();
        }

        public static string TypeToString(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName;
            }
            const char genericSeparator = '`';

            var genName = type.FullName[0..type.FullName.IndexOf(genericSeparator)];

            var typeBuilder = new StringBuilder();

            typeBuilder.Append(genName);
            typeBuilder.Append("<");

            typeBuilder.AppendJoin(", ", type.GenericTypeArguments.Select(t => LambdaGenerator.TypeToString(t)));

            typeBuilder.Append(">");

            return typeBuilder.ToString();
        }
    }
}
