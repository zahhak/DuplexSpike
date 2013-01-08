using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using Telerik.DynamicProxy.Enhancers;

namespace Telerik.DynamicProxy
{
    internal class ILEmitter
    {
        public ILEmitter(ILGenerator ilGenerator)
        {
            this.ilGenerator = ilGenerator;
        }

        /// <summary>
        /// Decalares and initializes a variable with parameter values.
        /// </summary>
        /// <param name="parameterTypes"></param>
        /// <param name="offset">Starting index for the provided types.</param>
        /// <returns><see cref="LocalBuilder"/></returns>
        public LocalBuilder WrapArgsInObjArray(Type[] parameterTypes, int offset)
        {
            LocalBuilder args = ilGenerator.DeclareLocal(typeof(object[]));

            ilGenerator.Emit(OpCodes.Ldc_I4_S, parameterTypes.Length);
            ilGenerator.Emit(OpCodes.Newarr, typeof(object));
            ilGenerator.Emit(OpCodes.Stloc, args);

            if (parameterTypes.Length > 0)
            {
                ilGenerator.Emit(OpCodes.Ldloc, args);

                int parameterIndex = offset;

                for (int index = 0; index < parameterTypes.Length; index++)
                {
                    Type parameterType = parameterTypes[index].GetRealType();

                    ilGenerator.Emit(OpCodes.Ldc_I4_S, index);
                    ilGenerator.Emit(OpCodes.Ldarg, parameterIndex + 1);

                    if (parameterTypes[index].IsByRef)
                        EmitOutParameter(parameterType);

                    if (parameterType.IsValueType || parameterType.IsGenericParameter)
                        ilGenerator.Emit(OpCodes.Box, parameterType);

                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                    ilGenerator.Emit(OpCodes.Ldloc, args);

                    parameterIndex++;
                }

                ilGenerator.Emit(OpCodes.Stloc, args);
            }
            return args;
        }

        /// <summary>
        /// Puts the specific instruction on to MSIL stack
        /// </summary>
        public void Emit(OpCode code, int index)
        {
            ilGenerator.Emit(code, index);
        }

        /// <summary>
        /// Puts the specific instruction on to MSIL stack
        /// </summary>
        public void Emit(OpCode code, Type target)
        {
            ilGenerator.Emit(code, target);
        }

        /// <summary>
        /// Puts the specific instruction on to MSIL stack
        /// </summary>
        /// <param name="code"></param>
        /// <param name="method"></param>
        public void Emit(OpCode code, MethodInfo method)
        {
            ilGenerator.Emit(code, method);
        }

        /// <summary>
        /// Puts the specific instruction on to MSIL stack
        /// </summary>
        public void Emit(OpCode code, LocalBuilder locBuilder)
        {
            ilGenerator.Emit(code, locBuilder);
        }

        /// <summary>
        /// Puts the specific instruction on to MSIL stack
        /// </summary>
        public void Emit(OpCode code)
        {
            ilGenerator.Emit(code);
        }

        /// <summary>
        /// Declares and initializes a variable with the given type array.
        /// </summary>
        /// <param name="types"></param>
        public LocalBuilder CopyToLocalArray(Type[] types)
        {
            LocalBuilder locTypes = ilGenerator.DeclareLocal(typeof(Type[]));

            ilGenerator.Emit(OpCodes.Ldc_I4, types.Length);
            ilGenerator.Emit(OpCodes.Newarr, typeof(Type));

            ilGenerator.Emit(OpCodes.Stloc, locTypes);
            ilGenerator.Emit(OpCodes.Ldloc, locTypes);

            for (int index = 0; index < types.Length; index++)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4, index);

                Type paramType = types[index].GetRealType();

                ilGenerator.Emit(OpCodes.Ldtoken, paramType);
                ilGenerator.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"), null);

                if (types[index].IsByRef)
                {
                    ilGenerator.Emit(OpCodes.Callvirt, typeof(Type).GetMethod("MakeByRefType"));
                }

                ilGenerator.Emit(OpCodes.Stelem_Ref);
                ilGenerator.Emit(OpCodes.Ldloc, locTypes);
            }


            ilGenerator.Emit(OpCodes.Stloc, locTypes);

            return locTypes;
        }

        /// <summary>
        /// Dynamically declares array of types from the argument.
        /// </summary>
        /// <param name="parameters">Parameters type</param>
        public LocalBuilder DeclareTypeArrayLocal(Type[] parameters)
        {
            LocalBuilder userTypes = ilGenerator.DeclareLocal(typeof(Type[]));

            ilGenerator.Emit(OpCodes.Ldc_I4_S, parameters.Length);
            ilGenerator.Emit(OpCodes.Newarr, typeof(Type));
            ilGenerator.Emit(OpCodes.Stloc, userTypes);

            for (int index = 0; index < parameters.Length; index++)
            {
                ilGenerator.Emit(OpCodes.Ldloc, userTypes);
                ilGenerator.Emit(OpCodes.Ldc_I4, index);

                ilGenerator.Emit(OpCodes.Ldtoken, parameters[index]);
                ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) }));

                ilGenerator.Emit(OpCodes.Stelem_Ref);

            }

            return userTypes;
        }

        /// <summary>
        /// Un-boxes the return type of the specified method when required.
        /// </summary>
        public void UnboxIfReq(MethodInfo method)
        {
            if (!method.ReturnType.IsGenericParameter)
            {
                if (method.ReturnType.IsPrimitive || method.ReturnType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Unbox, method.ReturnType);
                    ilGenerator.Emit(OpCodes.Ldobj, method.ReturnType);
                }
                else if (method.ReturnType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Unbox, method.ReturnType);
                    EmitOutParameter(method.ReturnType);
                }
            }
            else
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, method.ReturnType);
            }
        }

        /// <summary>
        /// Emits out parameter
        /// </summary>
        /// <param name="target"></param>
        public void EmitOutParameter(Type target)
        {
            var codes = new Dictionary<Type, OpCode>();
            codes[typeof(sbyte)] = OpCodes.Ldind_I1;
            codes[typeof(short)] = OpCodes.Ldind_I2;
            codes[typeof(int)] = OpCodes.Ldind_I4;
            codes[typeof(long)] = OpCodes.Ldind_I8;
            codes[typeof(byte)] = OpCodes.Ldind_U1;
            codes[typeof(ushort)] = OpCodes.Ldind_U2;
            codes[typeof(uint)] = OpCodes.Ldind_U4;
            codes[typeof(ulong)] = OpCodes.Ldind_I8;
            codes[typeof(float)] = OpCodes.Ldind_R4;
            codes[typeof(double)] = OpCodes.Ldind_R8;
            codes[typeof(char)] = OpCodes.Ldind_U2;
            codes[typeof(bool)] = OpCodes.Ldind_I1;

            if (codes.ContainsKey(target))
            {
                ilGenerator.Emit(codes[target]);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldobj, target);
            }
        }

        /// <summary>
        /// Emits set for out parameter.
        /// </summary>
        /// <param name="target"></param>
        public void EmitObjectRef(Type target)
        {
            var codes = new Dictionary<Type, OpCode>();

            codes[typeof(sbyte)] = OpCodes.Stind_I1;
            codes[typeof(short)] = OpCodes.Stind_I2;
            codes[typeof(int)] = OpCodes.Stind_I4;
            codes[typeof(long)] = OpCodes.Stind_I8;
            codes[typeof(ulong)] = OpCodes.Stind_I8;
            codes[typeof(float)] = OpCodes.Stind_R4;
            codes[typeof(double)] = OpCodes.Stind_R8;
            codes[typeof(bool)] = OpCodes.Stind_I1;

            if (codes.ContainsKey(target))
            {
                ilGenerator.Emit(codes[target]);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Stobj, target);
            }
        }

        internal void InovkeBase(MethodInfo info, Type[] parameters)
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);

            for (int index = 0; index < parameters.Length; index++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, index + 1);
            }
            ilGenerator.Emit(OpCodes.Call, info);
        }


        /// <summary>
        /// Throws exception with for the given type.
        /// </summary>
        /// <param name="expectionTarget">Target type for the exception.</param>
        /// <param name="message">Message that will be thrown for the exception.</param>
        public void ThrowException(Type expectionTarget, string message)
        {
            ilGenerator.Emit(OpCodes.Ldstr, message);
            ilGenerator.Emit(OpCodes.Newobj, expectionTarget.GetConstructor(new[] { typeof(string) }));
            ilGenerator.Emit(OpCodes.Throw);
        }


        internal LocalBuilder DeclareRuntimeMethod(Type target, MethodBase method)
        {
            Type memberType = typeof(MethodInfo);

            LocalBuilder locRuntimeMethod = null;

            if (method.MemberType == MemberTypes.Method)
            {
                locRuntimeMethod = ilGenerator.DeclareLocal(typeof(MethodInfo));
                ilGenerator.Emit(OpCodes.Ldtoken, ((MethodInfo)method));
            }
            else
            {
                memberType = typeof(ConstructorInfo);
                locRuntimeMethod = ilGenerator.DeclareLocal(memberType);
                ilGenerator.Emit(OpCodes.Ldtoken, ((ConstructorInfo)method));
            }

            ilGenerator.Emit(OpCodes.Ldtoken, target);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new[]
            {
                typeof (RuntimeMethodHandle),
                typeof (RuntimeTypeHandle)
            }));

            ilGenerator.Emit(OpCodes.Castclass, memberType);
            ilGenerator.Emit(OpCodes.Stloc, locRuntimeMethod);

            return locRuntimeMethod;
        }


        private ILGenerator ilGenerator;
    }
}
