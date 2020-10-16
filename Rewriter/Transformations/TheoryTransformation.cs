using Mono.Cecil;

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
    }
}
