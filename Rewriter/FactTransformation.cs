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


        protected override void InjectLambdaIntoTemplate(MethodDefinition template, MethodDefinition method)
        {
            var funcTaskConstructor = method.Module.ImportReference(FuncConstructorGenerator.GetConstructorInfo(null));

            var ilProcessor = template.Body.GetILProcessor();

            ilProcessor.RemoveAt(0); // nop
            ilProcessor.RemoveAt(0); // ldnull
            ilProcessor.RemoveAt(0); // stloc.0

            var firstInstruction = ilProcessor.Body.Instructions.First();

            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Nop));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldnull));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldftn, method));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Newobj, funcTaskConstructor));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Stloc_0));
        }
    }
}
