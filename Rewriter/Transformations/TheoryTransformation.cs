using Mono.Cecil;

namespace Rewriter
{
    class TheoryTransformation : XunitTransformation
    {
        protected override string TemplateMethodName => "TheoryTemplate";

        protected override string TestAttributeName => "Xunit.TheoryAttribute";

        public TheoryTransformation(AssemblyDefinition assemblyDefinition)
            : base(assemblyDefinition)
        { }
    }
}
