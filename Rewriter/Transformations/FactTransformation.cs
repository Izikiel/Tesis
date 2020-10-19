using Mono.Cecil;

namespace Rewriter
{
    class FactTransformation : XUnitTransformation
    {
        protected override string TestAttributeName => "Xunit.FactAttribute";

        public FactTransformation(AssemblyDefinition assemblyDefinition)
            : base(assemblyDefinition)
        { }
    }
}
