using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rewriter
{
    public abstract class XunitTransformation
    {
        protected virtual string TemplateMethodName { get; }

        protected virtual string TemplateModuleName { get; }

        protected virtual string TemplateTypeName { get; }

        protected virtual string TestAttributeName { get; }

        protected AssemblyDefinition TemplatesAssembly { get; }

        protected HashSet<string> Transformed { get; set; } = new HashSet<string>();

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

        protected bool DoesApply(MethodDefinition method)
        {
            if (this.Transformed.Contains(method.Name) || !method.HasCustomAttributes)
            {
                return false;
            }

            return method.CustomAttributes.Any(a => a.AttributeType.FullName == this.TestAttributeName);
        }

        public abstract void Apply(MethodDefinition method);

        protected object Transform(GenericInstanceMethod genericInstanceMethod, ModuleDefinition moduleDefinition)
        {
            var returnType = moduleDefinition.ImportReference(typeof(string[]));
            var declaringType = moduleDefinition.ImportReference(genericInstanceMethod.DeclaringType);
            return new object();
        }

        protected object Transform(MethodReference methodReference, ModuleDefinition moduleDefinition)
        {
            //if (methodReference is MethodSpecification specification)
            //{
            //    return Transform(specification, moduleDefinition);
            //}

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
