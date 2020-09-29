using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GenerateLambdaRoslyn;

namespace Rewriter
{
    class FactTransformation : XunitTransformation
    {
        protected override string TemplateMethodName => "FactTemplate";

        protected override string TemplateModuleName => "CoyoteTemplates.dll";

        protected override string TemplateTypeName => "CoyoteTemplates";

        protected override string TestAttributeName => "Xunit.FactAttribute";

        public FactTransformation(AssemblyDefinition assemblyDefinition)
            : base(assemblyDefinition)
        { }

        public override void Apply(MethodDefinition method)
        {
            if (!this.DoesApply(method))
            {
                return;
            }
            //var parameters = method.Parameters.Select(pdefinition => pdefinition.)

            //var methodLambdaGenerator = new LambdaGenerator(method.Name, typeof(Task), )

            var template = this.GetTemplateMethod();
            CustomAttribute factAttr = default;

            foreach (var attr in method.CustomAttributes)
            {
                template.CustomAttributes.Add(attr);
                if (attr.AttributeType.FullName == this.TestAttributeName)
                {
                    factAttr ??= attr;
                }
            }

            if (factAttr != default)
            {
                method.CustomAttributes.Remove(factAttr);
            }

            var originalName = method.Name;
            method.Name += "__inner";

            template.Name = originalName;

            var lambdaNestedType = template.DeclaringType.NestedTypes.First(); // reemplazar por creacion de Func<Task> local.
            lambdaNestedType.Name = template.Name + "_nestedType";
            lambdaNestedType.DeclaringType = null;
            lambdaNestedType.Scope = method.DeclaringType.Scope;

            SetModuleValue(lambdaNestedType, method.Module);

            method.DeclaringType.NestedTypes.Add(lambdaNestedType);

            var body = template.Body;
            for (int i = 0; i < body.Variables.Count; i++)
            {
                body.Variables[i].VariableType = Transform(body.Variables[i].VariableType, method.Module);
            }

            for (int i = 0; i < body.Instructions.Count; i++)
            {
                var opCode = body.Instructions[i].OpCode;
                var operand = body.Instructions[i].Operand;

                if (operand is GenericInstanceMethod genericInstanceMethod)
                {
                    operand = Transform(genericInstanceMethod, method.Module);
                }
                else if (operand is MethodReference methodReference)
                {
                    operand = Transform(methodReference, method.Module);
                }

                if (operand is FieldDefinition field)
                {
                    Transform(field, method.Module);
                }

                if (opCode.Code == Code.Ldftn)
                {
                    body.Instructions[i].Operand = method;
                }
            }

            template.DeclaringType = null;

            method.DeclaringType.Methods.Add(template);

            var nestedRef = method.Module.ImportReference(lambdaNestedType);

            var bob = nestedRef.Resolve();

            var nona = template.Resolve();

            Transformed.Add(template.Name);
        }
    }
}
