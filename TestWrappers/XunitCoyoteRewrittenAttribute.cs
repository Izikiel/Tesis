using System;

namespace TestWrappers
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class XunitCoyoteRewrittenAttribute : Attribute
    {
        public XunitCoyoteRewrittenAttribute()
        {
        }
    }
}
