using Mono.Cecil;

namespace Rewriter
{
    class FactTransformation : XunitTransformation
    {
        protected override string TemplateMethodName => "FactTemplate";

        protected override string TestAttributeName => "Xunit.FactAttribute";

        public FactTransformation(AssemblyDefinition assemblyDefinition)
            : base(assemblyDefinition)
        { }
    }
}
