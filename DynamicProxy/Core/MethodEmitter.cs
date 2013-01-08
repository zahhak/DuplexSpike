using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Telerik.DynamicProxy.Abstraction;
using System.Linq.Expressions;

namespace Telerik.DynamicProxy
{
    internal class MethodEmitter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodEmitter"/> class.
        /// </summary>
        /// <param name="emitter">Contains the Type detail udner which methods should be emitted.</param>
        public MethodEmitter(TypeEmitter emitter)
        {
            this.typeEmitter = emitter;
        }

        internal void Emit(MethodInfo methodInfo)
        {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            Type[] parameterTypes = Utility.ToTypeArray(parameterInfos);
            TypeBuilder typeBuilder = typeEmitter.Builder;
            MethodAttributes arrtributes = Utility.GetTargetAttributes(methodInfo);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, arrtributes);

            DefineGenericArguments(methodInfo, methodBuilder);
            DefineParameters(methodInfo, methodBuilder);

            // set the return type.
            methodBuilder.SetReturnType(methodInfo.ReturnType);
            methodBuilder.SetSignature(methodInfo.ReturnType, Type.EmptyTypes, Type.EmptyTypes, parameterTypes, null, null);

            MethodBuilder methodInterceptor = DefineExecutorMethod(methodInfo, parameterTypes);

            // Make the method generic from custom constructed generic arguments, this will make sure 
            //that method body taking the correct varient in case of constraint generic arguments.
            if (methodBuilder.IsGenericMethod)
            {
                methodInfo = methodBuilder.MakeGenericMethod(methodInfo.GetGenericArguments());
            }

            ILGenerator ilGenerator = methodBuilder.GetILGenerator();
            ILEmitter ilEmitter = new ILEmitter(ilGenerator);

            LocalBuilder locRuntimeMethod = ilEmitter.DeclareRuntimeMethod(methodInfo.DeclaringType, methodInfo);

            Label lblNotGenericMethod = ilGenerator.DefineLabel();

            string propIsGenericMethodDefination = GetName<MethodInfo, bool>(x => x.IsGenericMethodDefinition);

            // only convert the method, if it is generic regardless of its class.
            ilGenerator.Emit(OpCodes.Ldloc, locRuntimeMethod);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(MethodInfo).GetMethod(propIsGenericMethodDefination));
            ilGenerator.Emit(OpCodes.Brfalse, lblNotGenericMethod);

            LocalBuilder userTypes = ilEmitter.DeclareTypeArrayLocal(methodInfo.GetGenericArguments());

            ilGenerator.Emit(OpCodes.Ldloc, locRuntimeMethod);
            ilGenerator.Emit(OpCodes.Ldloc, userTypes);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(MethodInfo).GetMethod("MakeGenericMethod"));
            ilGenerator.Emit(OpCodes.Stloc, locRuntimeMethod);
            ilGenerator.MarkLabel(lblNotGenericMethod);

            LocalBuilder locInvocation = DefineMethodInovcation(ilGenerator, locRuntimeMethod, parameterTypes);

            SetDeclaringType(ilGenerator, locInvocation, methodInfo.DeclaringType);

            // call internal method
            ilGenerator.Emit(OpCodes.Ldarg_0);

            for (int paramIndex = 0; paramIndex < parameterTypes.Length; paramIndex++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, paramIndex + 1);
            }

            ilGenerator.Emit(OpCodes.Ldloc, locInvocation);
            ilGenerator.Emit(OpCodes.Ldc_I4, 0);
            ilGenerator.Emit(OpCodes.Ldc_I4_S, 0);
            ilGenerator.Emit(OpCodes.Call, methodInterceptor);

            ilGenerator.Emit(OpCodes.Ret);

            // ovverride the method.
            if (methodInfo.DeclaringType.IsInterface)
            {
                typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
            }
        }

        internal string GetName<T, TReturn>(Expression<Func<T, TReturn>> expression)
        {
            if (expression.Body.NodeType == ExpressionType.MemberAccess)
            {
                var member = expression.Body as MemberExpression;
                if (member != null) return "get_" + member.Member.Name;
            }

            return string.Empty;
        }

        private MethodBuilder DefineExecutorMethod(MethodInfo methodInfo, Type[] parameters)
        {
            TypeBuilder typeBuilder = this.typeEmitter.Builder;
            MethodBuilder methodBuilder = typeBuilder.DefineMethod("_" + methodInfo.Name, MethodAttributes.Public | MethodAttributes.Final);

            DefineGenericArguments(methodInfo, methodBuilder);
            Type[] extendParams = DefineParameters(methodInfo, methodBuilder, new[] { typeof(MethodInvocation), typeof(int), typeof(bool) });

            methodBuilder.SetSignature(methodInfo.ReturnType, Type.EmptyTypes, Type.EmptyTypes, extendParams, null, null);
            methodBuilder.SetReturnType(methodInfo.ReturnType);

            int paramLen = parameters.Length;

            ILGenerator ilGenerator = methodBuilder.GetILGenerator();
            ILEmitter ilEmitter = new ILEmitter(ilGenerator);

            FieldInfo interceptor = typeEmitter.Interceptors;
            LocalBuilder locMethodInvocation = ilGenerator.DeclareLocal(typeof(MethodInvocation));

            ilGenerator.Emit(OpCodes.Ldarg, paramLen + 1);
            ilGenerator.Emit(OpCodes.Stloc, locMethodInvocation);

            LocalBuilder locIndex = ilGenerator.DeclareLocal(typeof(int));

            // load index from method arguments.
            ilGenerator.Emit(OpCodes.Ldarg, paramLen + 2);
            ilGenerator.Emit(OpCodes.Stloc, locIndex);

            Label interceptorLabel = ilGenerator.DefineLabel();
            Label invokeBaseLabel = ilGenerator.DefineLabel();
            Label exitLabel = ilGenerator.DefineLabel();
            Label terminateLabel = ilGenerator.DefineLabel();
            Label continueLabel = ilGenerator.DefineLabel();

            LocalBuilder locReturn = null;

            // call base if there is no interceptor defined yet.
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, interceptor);
            ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Brfalse, continueLabel);

            ilGenerator.Emit(OpCodes.Newobj, typeof(DefaultInterceptor).GetConstructor(Type.EmptyTypes));

            ilGenerator.Emit(OpCodes.Ldloc, locMethodInvocation);
            ilGenerator.Emit(OpCodes.Castclass, typeof(IInvocation));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(IInterceptor).GetMethod("Intercept", new[] { typeof(IInvocation) }));

            ilGenerator.Emit(OpCodes.Br, terminateLabel);

            ilGenerator.MarkLabel(continueLabel);

            // check if the passed index is greater than the items
            ilGenerator.Emit(OpCodes.Ldloc, locIndex);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, interceptor);
            ilGenerator.Emit(OpCodes.Ldlen);
            ilGenerator.Emit(OpCodes.Conv_I4);
            ilGenerator.Emit(OpCodes.Clt);
            ilGenerator.Emit(OpCodes.Brtrue, interceptorLabel);

            // go to invoke base section.
            ilGenerator.Emit(OpCodes.Br, invokeBaseLabel);

            ilGenerator.MarkLabel(interceptorLabel);

            InvokeInterceptor(ilGenerator, interceptor, locMethodInvocation, locIndex);

            //// Emit out parameters.
            SetOutArgsFromInterceptor(ilEmitter, parameters, locMethodInvocation);

            ilGenerator.MarkLabel(terminateLabel);

            if (methodInfo.ReturnType != typeof(void))
            {
                locReturn = ilGenerator.DeclareLocal(methodInfo.ReturnType);

                ilGenerator.Emit(OpCodes.Ldloc, locMethodInvocation);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(MethodInvocation).GetMethod("get_ReturnValue"));
                ilGenerator.Emit(OpCodes.Castclass, methodInfo.ReturnType);
                ilEmitter.UnboxIfReq(methodInfo);

                ilGenerator.Emit(OpCodes.Stloc, locReturn);
            }

            // exit.
            ilGenerator.Emit(OpCodes.Br, exitLabel);

            ilGenerator.MarkLabel(invokeBaseLabel);

            InvokeBaseIfRequired(ilGenerator, (paramLen + 3), delegate
            {
                InvokeBase(methodInfo, ilEmitter, parameters, locReturn);
            });

            ilGenerator.MarkLabel(exitLabel);

            if (methodInfo.ReturnType != typeof(void))
            {
                if (locReturn != null) ilGenerator.Emit(OpCodes.Ldloc, locReturn);
                ilGenerator.Emit(OpCodes.Ret);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ret);
            }
            return methodBuilder;
        }

        private void InvokeBase(MethodInfo methodInfo, ILEmitter ilEmitter, Type[] parameters, LocalBuilder locReturn)
        {
            if (!methodInfo.DeclaringType.IsInterface)
            {
                if (methodInfo.ReturnType != typeof(void))
                {
                    ilEmitter.InovkeBase(methodInfo, parameters);
                    ilEmitter.Emit(OpCodes.Stloc, locReturn);
                }
                else
                {
                    ilEmitter.InovkeBase(methodInfo, parameters);
                }
            }
        }

        private static void InvokeBaseIfRequired(ILGenerator ilGenerator, int flagArgIndex, Action action)
        {
            Label exitLabel = ilGenerator.DefineLabel();

            ilGenerator.Emit(OpCodes.Ldarg, flagArgIndex);
            ilGenerator.Emit(OpCodes.Brfalse, exitLabel);

            action();

            ilGenerator.MarkLabel(exitLabel);
        }

        private static LocalBuilder DefineMethodInovcation(ILGenerator ilGenerator, LocalBuilder locRuntimeMethod, Type[] parameterTypes)
        {
            ILEmitter ilEmitter = new ILEmitter(ilGenerator);

            LocalBuilder locMehodInvocation = GetMethodInvocation(ilGenerator, delegate
            {
                LocalBuilder locArguments = ilGenerator.DeclareLocal(typeof(Argument[]));
                LocalBuilder locUserArguments = ilEmitter.WrapArgsInObjArray(parameterTypes, 0);
                LocalBuilder locTypes = ilEmitter.CopyToLocalArray(parameterTypes);

                ilGenerator.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
                ilGenerator.Emit(OpCodes.Newarr, typeof(Argument));
                ilGenerator.Emit(OpCodes.Stloc, locArguments);

                for (int index = 0; index < parameterTypes.Length; index++)
                {
                    ilGenerator.Emit(OpCodes.Ldloc, locArguments);
                    ilGenerator.Emit(OpCodes.Ldc_I4, index);

                    ilGenerator.Emit(OpCodes.Ldloc, locUserArguments);
                    ilGenerator.Emit(OpCodes.Ldc_I4, index);
                    ilGenerator.Emit(OpCodes.Ldelem_Ref);

                    ilGenerator.Emit(OpCodes.Ldloc, locTypes);
                    ilGenerator.Emit(OpCodes.Ldc_I4, index);
                    ilGenerator.Emit(OpCodes.Ldelem_Ref);

                    ilGenerator.Emit(OpCodes.Ldc_I4, parameterTypes[index].IsByRef ? 1 : 0);

                    ilGenerator.Emit(OpCodes.Newobj, typeof(Argument).GetConstructor(new Type[] 
                    { 
                        typeof(object),
                        typeof(Type),
                        typeof(bool)
                    }));

                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                }

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldloc, locRuntimeMethod);
                ilGenerator.Emit(OpCodes.Ldloc, locArguments);
            });

            return locMehodInvocation;
        }

        private static void SetOutArgsFromInterceptor(ILEmitter ilEmitter, Type[] parameters, LocalBuilder locMehodInvocation)
        {
            for (int index = 0; index < parameters.Length; index++)
            {
                Type parameterType = parameters[index];
                Type underlyingType = parameterType.GetElementType();

                if (parameterType.IsByRef)
                {
                    ilEmitter.Emit(OpCodes.Ldarg, index + 1);
                    ilEmitter.Emit(OpCodes.Ldloc, locMehodInvocation);
                    ilEmitter.Emit(OpCodes.Call, typeof(MethodInvocation).GetMethod("get_Arguments"));
                    ilEmitter.Emit(OpCodes.Ldc_I4, index);
                    ilEmitter.Emit(OpCodes.Ldelem_Ref);
                    ilEmitter.Emit(OpCodes.Call, typeof(Argument).GetMethod("get_Value"));
                    ilEmitter.Emit(OpCodes.Castclass, underlyingType);

                    if (underlyingType.IsValueType || underlyingType.IsGenericParameter)
                    {
                        ilEmitter.Emit(OpCodes.Unbox_Any, underlyingType);
                    }
                    ilEmitter.EmitObjectRef(underlyingType);
                }
            }
        }

        private static LocalBuilder GetMethodInvocation(ILGenerator ilGenerator, Action preExecution)
        {
            LocalBuilder locMehodInvocation = ilGenerator.DeclareLocal(typeof(MethodInvocation));

            preExecution();
            ilGenerator.Emit(OpCodes.Newobj, typeof(MethodInvocation).GetConstructor(new[] 
            {    
                typeof(object), 
                typeof(MethodInfo), 
                typeof(Argument[]),
            }));
            ilGenerator.Emit(OpCodes.Stloc, locMehodInvocation);

            return locMehodInvocation;
        }

        private static void SetDeclaringType(ILGenerator ilGenerator, LocalBuilder locMehodInvocation, Type target)
        {
            ilGenerator.Emit(OpCodes.Ldloc, locMehodInvocation);
            ilGenerator.Emit(OpCodes.Ldtoken, target);
            ilGenerator.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"), null);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodInvocation).GetMethod("set_DeclaringType"));
        }

        private static Type[] DefineParameters(MethodInfo methodInfo, MethodBuilder methodBuilder, params Type[] extras)
        {
            IList<Type> list = methodInfo.GetParameters().Select(paraInfo => paraInfo.ParameterType).ToList();

            for (int index = 0; index < extras.Length; index++)
            {
                Type extra = extras[index];
                list.Add(extra);
            }

            var array = new Type[list.Count];

            list.CopyTo(array, 0);

            methodBuilder.SetParameters(array);

            return array;
        }

        private static GenericTypeParameterBuilder[] DefineGenericArguments(MethodInfo methodInfo, MethodBuilder methodBuilder)
        {
            GenericTypeParameterBuilder[] builders = null;

            if (methodInfo.IsGenericMethod)
            {
                Type[] genArguments = methodInfo.GetGenericArguments();
                var genArgNames = new string[genArguments.Length];

                for (int index = 0; index < genArgNames.Length; index++)
                {
                    genArgNames[index] = genArguments[index].Name;
                }

                builders = methodBuilder.DefineGenericParameters(genArgNames);
                DefineConstraints(builders, methodInfo);
            }
            return builders;
        }

        private static void DefineConstraints(GenericTypeParameterBuilder[] builders, MethodInfo methodInfo)
        {
            Type[] genericArguments = methodInfo.GetGenericArguments();

            for (int i = 0; i < builders.Length; ++i)
            {
                Type originalGenericArgument = genericArguments[i];
                builders[i].SetGenericParameterAttributes(originalGenericArgument.GenericParameterAttributes);

                Type[] paramConstraints = genericArguments[i].GetGenericParameterConstraints();

                List<Type> ifcConstraints = null;
                foreach (Type type in paramConstraints)
                {
                    Type constrainTarget = type;

                    if (constrainTarget.DeclaringType != null)
                    {
                        if (constrainTarget.DeclaringType.IsGenericType)
                        {
                            Type[] classTypes = constrainTarget.DeclaringType.GetGenericArguments();

                            int index = Array.IndexOf<Type>(classTypes, type);

                            if (index != -1)
                            {
                                constrainTarget = methodInfo.DeclaringType.GetGenericArguments()[index];
                            }
                        }
                    }

                    if (!constrainTarget.IsInterface)
                    {
                        builders[i].SetBaseTypeConstraint(constrainTarget);
                    }
                    else
                    {
                        if (ifcConstraints == null)
                        {
                            ifcConstraints = new List<Type>();
                        }
                        ifcConstraints.Add(constrainTarget);
                    }
                }
                if (ifcConstraints != null)
                {
                    builders[i].SetInterfaceConstraints(ifcConstraints.ToArray());
                }
            }
        }

        private static void InvokeInterceptor(ILGenerator ilGenerator, FieldInfo fldInterceptors, LocalBuilder locMethodInvocation, LocalBuilder locIndex)
        {
            ilGenerator.Emit(OpCodes.Ldloc, locMethodInvocation);

            // set the current index.
            ilGenerator.Emit(OpCodes.Ldloc, locIndex);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(MethodInvocation).GetMethod("set_Index"));

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, fldInterceptors);
            ilGenerator.Emit(OpCodes.Ldloc, locIndex);
            ilGenerator.Emit(OpCodes.Ldelem_Ref);

            ilGenerator.Emit(OpCodes.Ldloc, locMethodInvocation);
            ilGenerator.Emit(OpCodes.Castclass, typeof(IInvocation));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(IInterceptor).GetMethod("Intercept", new[] { typeof(IInvocation) }));
        }

        private readonly TypeEmitter typeEmitter;
    }
}
