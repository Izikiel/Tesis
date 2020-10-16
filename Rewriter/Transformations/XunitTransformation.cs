using GenerateLambdaRoslyn;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rewriter
{
    public abstract class XunitTransformation
    {
        protected virtual string TemplateMethodName { get; }

        protected virtual string TemplateModuleName { get; }

        protected virtual string TemplateTypeName { get; }

        protected virtual string TestAttributeName { get; }

        protected AssemblyDefinition TemplatesAssembly { get; }

        protected static HashSet<string> Transformed { get; set; } = new HashSet<string>();

        public XunitTransformation(AssemblyDefinition assemblyDefinition)
        {
            this.TemplatesAssembly = assemblyDefinition;
        }

        protected MethodDefinition GetTemplateMethod()
        {
            return this.TemplatesAssembly.Modules
                .First(m => m.Name == this.TemplateModuleName)
                .Types
                .First(t => t.Name == this.TemplateTypeName)
                .Methods
                .First(m => m.Name == this.TemplateMethodName);
        }

        private void InjectLambdaIntoTemplate(MethodDefinition template, MethodDefinition method)
        {
            var module = method.Module;

            var funcTaskConstructor = module.ImportReference(FuncConstructorGenerator.GetConstructorInfo(null));

            var testWrapperConstructor = module.ImportReference(typeof(TestWrapper).GetConstructors()[0]);

            var testWrapperInvokeReference = module.ImportReference(typeof(TestWrapper).GetMethod("Invoke"));

            var typeofReference = module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle"));

            var ilProcessor = template.Body.GetILProcessor();

            ilProcessor.RemoveAt(0); // nop
            ilProcessor.RemoveAt(0); // ldnull
            ilProcessor.RemoveAt(0); // stloc.0

            var firstInstruction = ilProcessor.Body.Instructions[0];

            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Nop));

            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldtoken, method.DeclaringType));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, typeofReference));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldstr, method.Name));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(template.HasParameters ? OpCodes.Ldarg_0 : OpCodes.Ldnull));

            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Newobj, testWrapperConstructor));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldftn, testWrapperInvokeReference));

            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Newobj, funcTaskConstructor));

            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Stloc_0));
        }

        protected bool DoesApply(MethodDefinition method)
        {
            if (XunitTransformation.Transformed.Contains(method.Name) || !method.HasCustomAttributes)
            {
                return false;
            }

            return method.CustomAttributes.Any(a => a.AttributeType.FullName == this.TestAttributeName);
        }

        public virtual void Apply(MethodDefinition method)
        {
            if (!this.DoesApply(method))
            {
                return;
            }

            var template = this.GetTemplateMethod();

            var originalName = method.Name;
            method.Name += "__inner";

            template.Name = originalName;

            var module = method.Module;

            this.InjectLambdaIntoTemplate(template, method);

            for (int i = 0; i < template.Parameters.Count; i++)
            {
                var param = template.Parameters[i];
                param.ParameterType = Transform(param.ParameterType, method.Module);

                if (param.HasCustomAttributes)
                {
                    for (int j = 0; j < param.CustomAttributes.Count; j++)
                    {
                        var customAttr = param.CustomAttributes[j];

                        var ctorTypeReference = module.ImportReference(Type.GetType(customAttr.AttributeType.FullName).GetConstructor(Type.EmptyTypes));

                        param.CustomAttributes.RemoveAt(j);

                        param.CustomAttributes.Add(new CustomAttribute(ctorTypeReference));

                    }
                }
            }


            var body = template.Body;
            for (int i = 0; i < body.Variables.Count; i++)
            {
                body.Variables[i].VariableType = Transform(body.Variables[i].VariableType, method.Module);
            }

            for (int i = 0; i < body.Instructions.Count; i++)
            {
                var operand = body.Instructions[i].Operand;

                if (operand is null)
                {
                    continue;
                }

                if (operand is MethodReference copyToRef && copyToRef.Name.Contains("CopyTo"))
                {
                    var genericReference = module.ImportReference(copyToRef.Resolve());

                    body.Instructions[i].Operand = genericReference.MakeGeneric(module.ImportReference(typeof(string)));
                }
                else if (operand is MethodReference methodReference)
                {
                    body.Instructions[i].Operand = Transform(methodReference, method.Module);
                }

                if (operand is TypeReference typeReference)
                {
                    body.Instructions[i].Operand = Transform(typeReference, method.Module);
                }
            }

            foreach (var attr in method.CustomAttributes)
            {
                if (attr.AttributeType.FullName.Contains("DebuggerStepThrough") || attr.AttributeType.FullName.Contains("AsyncStateMachine"))
                {
                    continue;
                }

                template.CustomAttributes.Add(attr);
            }

            foreach (var attr in template.CustomAttributes)
            {
                method.CustomAttributes.Remove(attr);
            }

            template.DeclaringType = null;

            method.DeclaringType.Methods.Add(template);

            Transformed.Add(template.Name);
        }

        protected object Transform(MethodReference methodReference, ModuleDefinition moduleDefinition)
        {
            methodReference.DeclaringType = Transform(methodReference.DeclaringType, moduleDefinition);
            methodReference.ReturnType = Transform(methodReference.ReturnType, moduleDefinition);
            if (methodReference.HasParameters)
            {
                for (int i = 0; i < methodReference.Parameters.Count; i++)
                {
                    methodReference.Parameters[i].ParameterType = Transform(methodReference.Parameters[i].ParameterType, moduleDefinition);
                }
            }
            return moduleDefinition.ImportReference(methodReference);
        }

        protected MethodSpecification Transform(MethodSpecification methodSpecification, ModuleDefinition moduleDefinition)
        {
            return (MethodSpecification)moduleDefinition.ImportReference(methodSpecification);
        }

        protected TypeReference Transform(TypeReference typeReference, ModuleDefinition moduleDefinition)
        {
            var importedReference = moduleDefinition.ImportReference(typeReference);
            if (importedReference.HasGenericParameters)
            {
                for (int i = 0; i < importedReference.GenericParameters.Count; i++)
                {
                    importedReference.GenericParameters[i].DeclaringType = Transform(importedReference.GenericParameters[i].DeclaringType, moduleDefinition);
                }
            }

            if (importedReference is GenericInstanceType instanceType && instanceType.HasGenericArguments)
            {
                for (int i = 0; i < instanceType.GenericArguments.Count; i++)
                {
                    instanceType.GenericArguments[i] = Transform(instanceType.GenericArguments[i], moduleDefinition);
                }
            }

            return importedReference;
        }

        protected void Transform(FieldDefinition field, ModuleDefinition moduleDefinition)
        {
            var importedType = moduleDefinition.ImportReference(field.FieldType);
            field.FieldType = importedType;
        }

        protected static void SetModuleValue(MemberReference type, ModuleDefinition moduleDefinition)
        {
            var internalModule = type.GetType().GetField("module", BindingFlags.NonPublic | BindingFlags.Instance);

            internalModule.SetValue(type, moduleDefinition);
        }
    }
}
