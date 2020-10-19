using Mono.Cecil;

namespace Rewriter
{
    class TheoryTransformation : XUnitTransformation
    {
        protected override string TestAttributeName => "Xunit.TheoryAttribute";

        public TheoryTransformation(AssemblyDefinition assemblyDefinition)
            : base(assemblyDefinition)
        { }
    }
}
