using GenerateLambdaRoslyn;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rewriter
{
    class TheoryTransformation : XunitTransformation
    {
        protected override string TemplateMethodName => "TheoryTemplate";

        protected override string TemplateModuleName => "CoyoteTemplates.dll";

        protected override string TemplateTypeName => "CoyoteTemplates";

        protected override string TestAttributeName => "Xunit.TheoryAttribute";

        public TheoryTransformation(AssemblyDefinition assemblyDefinition)
            : base(assemblyDefinition)
        { }

        public override void Apply(MethodDefinition method)
        {
            if (!this.DoesApply(method))
            {
                return;
            }

            var module = method.Module;

            var parameters = method.Parameters.Select(def => Type.GetType(def.ParameterType.FullName)).ToArray();

            var lambdaMethodGenerator = new LambdaGenerator(method.Name, typeof(Task), parameters);

            var generatedLambda = lambdaMethodGenerator.CompileClass();

            var funcTaskConstructor = module.ImportReference(FuncConstructorGenerator.GetConstructorInfo(null));

            var funcConstructor = module.ImportReference(FuncConstructorGenerator.GetConstructorInfo(parameters));

            var generatedLambdaConstructor = module.ImportReference(generatedLambda.GetConstructors().First());

            var toFuncTaskMethod = module.ImportReference(generatedLambda.GetMethod("ToFuncTask"));

            var template = this.GetTemplateMethod();

            var ilProcessor = template.Body.GetILProcessor();

            ilProcessor.RemoveAt(0); // nop
            ilProcessor.RemoveAt(0); // ldnull
            ilProcessor.RemoveAt(0); // stloc.0

            var firstInstruction = ilProcessor.Body.Instructions.First();

            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Nop));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldnull));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldftn, method));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Newobj, funcConstructor));

            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg_0));

            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Newobj, generatedLambdaConstructor));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldftn, toFuncTaskMethod));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Newobj, funcTaskConstructor));

            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Stloc_0));

            
        }
    }
}
