using Mono.Cecil;

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
    }
}
