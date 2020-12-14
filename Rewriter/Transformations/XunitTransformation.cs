using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;
using System.Reflection;
using TestWrappers;
using TestWrappers.XUnit;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace Rewriter
{
    public class XUnitTransformation
    {
        private const string TheoryAttributeName = "Xunit.TheoryAttribute";

        private const string FactAttributeName = "Xunit.FactAttribute";

        /// <summary>
        /// Returns a new method definition which wraps the given method so that it is run
        /// with Coyote. The method name will be modified.
        /// </summary>
        /// <param name="method">The method to wrap.</param>
        /// <returns>A new method definition which wraps the given method.</returns>
        public MethodDefinition WrapTestMethod(ref MethodDefinition method)
        {
            var module = method.Module;

            var wrapperName = method.Name;
            method.Name += "__inner";
            // TODO: Ver de conseguir el logger por reflection, hacer metodo de instancia si el 
            // metodo que se esta reescribiendo no es static. instancia => instancia, static => static.

            if ((method.Attributes & MethodAttributes.Static) != 0)
            {
                return WrapTestMethodStatic(method, module, wrapperName);
            }

            return WrapTestMethodInstance(method, module, wrapperName);
        }

        private static ConstructorInfo GetInstanceConstructor(ConstructorInfo[] constructors)
        {
            foreach (var constructor in constructors)
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    if (parameter.ParameterType.FullName == typeof(bool).FullName)
                    {
                        return constructor;
                    }
                }
            }

            throw new Exception("Missing instance constructor");
        }

        private static MethodDefinition WrapTestMethodInstance(MethodDefinition method, ModuleDefinition module, string wrapperName)
        {
            var wrapper = new MethodDefinition(
                wrapperName,
                MethodAttributes.Public,
                module.ImportReference(typeof(void)));

            var funcTaskConstructor = module.ImportReference(FuncConstructorGenerator.GetConstructorInfo(null));

            var testWrapperConstructor = module.ImportReference(XUnitTransformation.GetInstanceConstructor(typeof(XUnitTestWrapper).GetConstructors()));

            var testWrapperInvokeReference = module.ImportReference(typeof(XUnitTestWrapper).GetMethod("Invoke"));

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
                argsLoadInstruction = ilProcessor.Create(OpCodes.Ldarg_1);

                var argsParameter = new ParameterDefinition("args", ParameterAttributes.None, method.Module.ImportReference(typeof(object[])));

                var paramsAttributeCtor = module.ImportReference(typeof(ParamArrayAttribute).GetConstructor(Type.EmptyTypes));
                argsParameter.CustomAttributes.Add(new CustomAttribute(paramsAttributeCtor));
                wrapper.Parameters.Add(argsParameter);
            }

            // We are going to write the following:
            /* using XUnitTestWrapper xunitTestWrapper = new XUnitTestWrapper(true, this, method.Name, method.HasParameters ? args : null)
             * {
             *     XUnitTestTemplates.RunTestInCoyote(new Func<Task>(xunitTestWrapper.Invoke);
             * }
             */

            var lastRet = ilProcessor.Create(OpCodes.Ret);

            var leave_s = ilProcessor.Create(OpCodes.Leave_S, lastRet);

            var endFinally = ilProcessor.Create(OpCodes.Endfinally);

            var brfalse_s = ilProcessor.Create(OpCodes.Brfalse_S, endFinally);

            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4_1)); // true
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0)); // this
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, method.Name));
            ilProcessor.Append(argsLoadInstruction);

            ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, testWrapperConstructor));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc_0));

            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc_0));

            var tryStart = ilProcessor.Body.Instructions.Last();

            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldftn, testWrapperInvokeReference));

            ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, funcTaskConstructor));

            ilProcessor.Append(ilProcessor.Create(OpCodes.Call, runInCoyoteReference));

            ilProcessor.Append(leave_s);

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


        private static MethodDefinition WrapTestMethodStatic(MethodDefinition method, ModuleDefinition module, string wrapperName)
        {
            var wrapper = new MethodDefinition(
                wrapperName,
                MethodAttributes.Static | MethodAttributes.Public,
                module.ImportReference(typeof(void)));

            var funcTaskConstructor = module.ImportReference(FuncConstructorGenerator.GetConstructorInfo(null));

            var constructors = typeof(XUnitTestWrapper).GetConstructors();

            var instanceConstructor = XUnitTransformation.GetInstanceConstructor(constructors);

            var staticConstructor = constructors.First(c => !object.ReferenceEquals(c, instanceConstructor));

            var testWrapperConstructor = module.ImportReference(staticConstructor);

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

                var argsParameter = new ParameterDefinition("args", ParameterAttributes.None, method.Module.ImportReference(typeof(object[])));

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

            var leave_s = ilProcessor.Create(OpCodes.Leave_S, lastRet);

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

            ilProcessor.Append(leave_s);

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

        private bool DoesApply(MethodDefinition method)
        {
            var rewrittenAttributeTypeFullName = typeof(XunitCoyoteRewrittenAttribute).FullName;

            if (!method.HasCustomAttributes || method.CustomAttributes.Any(a => a.AttributeType.FullName == rewrittenAttributeTypeFullName))
            {
                return false;
            }

            return method.CustomAttributes.Any(a => a.AttributeType.FullName == XUnitTransformation.FactAttributeName || a.AttributeType.FullName == XUnitTransformation.TheoryAttributeName);
        }

        public bool Apply(MethodDefinition method)
        {
            if (!this.DoesApply(method))
            {
                return false;
            }

            var wrapper = this.WrapTestMethod(ref method);

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

            XUnitTransformation.MarkAsRewritten(method);
            XUnitTransformation.MarkAsRewritten(wrapper);

            return true;
        }

        public static void MarkAsRewritten(MethodDefinition method)
        {
            var attributeConstructor = method.Module.ImportReference(typeof(XunitCoyoteRewrittenAttribute).GetConstructor(Type.EmptyTypes));

            method.CustomAttributes.Add(new CustomAttribute(attributeConstructor));
        }
    }
}
