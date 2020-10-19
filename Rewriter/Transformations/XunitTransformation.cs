using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using TestWrappers.XUnit;

namespace Rewriter
{
    public class XUnitTransformation
    {
        private const string TheoryAttributeName = "Xunit.TheoryAttribute";

        private const string FactAttributeName = "Xunit.FactAttribute";

        private static HashSet<string> Transformed { get; } = new HashSet<string>();

        public MethodDefinition WrapTestMethod(string wrapperName, MethodDefinition method)
        {
            var module = method.Module;

            var wrapper = new MethodDefinition(
                wrapperName,
                MethodAttributes.Static | MethodAttributes.Public,
                module.ImportReference(typeof(void)));

            var funcTaskConstructor = module.ImportReference(FuncConstructorGenerator.GetConstructorInfo(null));

            var testWrapperConstructor = module.ImportReference(typeof(XUnitTestWrapper).GetConstructors()[0]);

            var testWrapperInvokeReference = module.ImportReference(typeof(XUnitTestWrapper).GetMethod("Invoke"));

            var typeofReference = module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle"));

            var runInCoyoteReference = module.ImportReference(typeof(XUnitTestTemplates).GetMethod("RunTestInCoyote"));

            var disposeReference = module.ImportReference(typeof(IDisposable).GetMethod("Dispose"));

            var ilProcessor = wrapper.Body.GetILProcessor();

            ilProcessor.Body.Instructions.Clear();
            wrapper.Body.Variables.Clear();

            var localVariable = new VariableDefinition(module.ImportReference(typeof(XUnitTestWrapper)));

            wrapper.Body.Variables.Add(localVariable);
            wrapper.Body.InitLocals = true;

            var argsLoadInstruction = ilProcessor.Create(OpCodes.Ldnull);

            if (method.HasParameters)
            {
                argsLoadInstruction = ilProcessor.Create(OpCodes.Ldarg_0);

                var argsParameter = new ParameterDefinition("args", Mono.Cecil.ParameterAttributes.None, method.Module.ImportReference(typeof(object[])));

                var paramsAttributeCtor = module.ImportReference(typeof(ParamArrayAttribute).GetConstructor(Type.EmptyTypes));
                argsParameter.CustomAttributes.Add(new CustomAttribute(paramsAttributeCtor));
                wrapper.Parameters.Add(argsParameter);
            }

            // We are going to write the following:
            /* using XUnitTestWrapper xunitTestWrapper = new XUnitTestWrapper(typeof(method.DeclaringType), method.Name, method.HasParameters ? args : null)
             * {
             *     XUnitTestTemplates.RunTestInCoyote(new Func<Task>(xunitTestWrapper.Invoke);
             * }
             */

            var lastRet = ilProcessor.Create(OpCodes.Ret);

            var leaveS = ilProcessor.Create(OpCodes.Leave_S, lastRet);

            var endFinally = ilProcessor.Create(OpCodes.Endfinally);

            var brfalse_s = ilProcessor.Create(OpCodes.Brfalse_S, endFinally);

            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldtoken, method.DeclaringType));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Call, typeofReference));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, method.Name));
            ilProcessor.Append(argsLoadInstruction);

            ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, testWrapperConstructor));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc_0));

            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc_0));

            var tryStart = ilProcessor.Body.Instructions.Last();

            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldftn, testWrapperInvokeReference));

            ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, funcTaskConstructor));

            ilProcessor.Append(ilProcessor.Create(OpCodes.Call, runInCoyoteReference));


            ilProcessor.Append(leaveS);

            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc_0));
            var tryEnd = ilProcessor.Body.Instructions.Last();

            ilProcessor.Append(brfalse_s);
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc_0));

            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, disposeReference));


            ilProcessor.Append(endFinally);

            ilProcessor.Append(lastRet);

            var handlerEnd = ilProcessor.Body.Instructions.Last();

            var exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = tryStart,
                TryEnd = tryEnd,
                HandlerStart = tryEnd,
                HandlerEnd = handlerEnd
            };

            wrapper.Body.ExceptionHandlers.Clear();
            wrapper.Body.ExceptionHandlers.Add(exceptionHandler);

            return wrapper;
        }

        protected bool DoesApply(MethodDefinition method)
        {
            if (XUnitTransformation.Transformed.Contains(method.Name) || !method.HasCustomAttributes)
            {
                return false;
            }

            return method.CustomAttributes.Any(a => a.AttributeType.FullName == XUnitTransformation.FactAttributeName || a.AttributeType.FullName == XUnitTransformation.TheoryAttributeName);
        }

        public virtual void Apply(MethodDefinition method)
        {
            if (!this.DoesApply(method))
            {
                return;
            }

            var wrapperName = method.Name;
            method.Name += "__inner";
            var wrapper = this.WrapTestMethod(wrapperName, method);

            foreach (var attr in method.CustomAttributes)
            {
                if (attr.AttributeType.FullName.Contains("DebuggerStepThrough") || attr.AttributeType.FullName.Contains("AsyncStateMachine"))
                {
                    continue;
                }

                wrapper.CustomAttributes.Add(attr);
            }

            foreach (var attr in wrapper.CustomAttributes)
            {
                method.CustomAttributes.Remove(attr);
            }

            method.DeclaringType.Methods.Add(wrapper);

            Transformed.Add(wrapper.Name);
        }
    }
}
