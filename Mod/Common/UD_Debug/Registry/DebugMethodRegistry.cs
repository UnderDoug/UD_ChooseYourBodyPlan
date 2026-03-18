using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using XRL;

using static UD_ChooseYourBodyPlan.Mod.Utils;
using static UD_ChooseYourBodyPlan.Mod.Const;
using static UD_ChooseYourBodyPlan.Mod.Options;
using static UD_ChooseYourBodyPlan.Mod.Logging.Debug;
using static UD_ChooseYourBodyPlan.Mod.Logging.Indent;
using System.Linq;
using XRL.World;

namespace UD_ChooseYourBodyPlan.Mod.Logging
{
    [HasModSensitiveStaticCache]
    public class DebugMethodRegistry : HashSet<MethodRegistryEntry>
    {
        [ModSensitiveStaticCache(CreateEmptyInstance = false)]
        private static DebugMethodRegistry _Instance;
        public static DebugMethodRegistry Instance => GetRegistry(ref _Instance);

        private static bool _GotRegistry = false;

        public DebugMethodRegistry()
        {
        }

        public bool this[MethodBase MethodBase]
            => !TryGetValue(MethodBase, out bool value)
            || value;

        public bool this[MethodInfo MethodInfo]
            => !TryGetValue(MethodInfo, out bool value)
            || value;

        public static DebugMethodRegistry GetRegistry(ref DebugMethodRegistry Registry)
        {
            Registry ??= new();

            if (_GotRegistry)
                return Registry;

            if (ModManager.GetMethodsWithAttribute(typeof(UD_DebugRegistryAttribute))
                    //.Where(m => !m.ContainsGenericParameters).ToList() 
                    is not List<MethodInfo> debugRegistryMethods
                || debugRegistryMethods.IsNullOrEmpty())
            {
                MetricsManager.LogModError(
                    mod: ThisMod,
                    Message: CallChain(nameof(DebugMethodRegistry), nameof(GetRegistry)) + " failed to retrieve any " +
                        nameof(UD_DebugRegistryAttribute) + " decorated methods");

                return Registry;
            }
            foreach (MethodInfo debugRegistryMethod in debugRegistryMethods)
            {
                try
                {
                    MethodInfo methodToInvoke = debugRegistryMethod;
                    if (methodToInvoke.ContainsGenericParameters)
                    {
                        if (debugRegistryMethod.GetGenericArguments() is not Type[] genericArgs
                            || debugRegistryMethod.MakeGenericMethod(genericArgs) is not MethodInfo madeMethod)
                        {
                            string fullMethod = CallChain(
                                debugRegistryMethod.DeclaringType.ToStringWithGenerics(),
                                debugRegistryMethod.MethodSignature(true));
                            MetricsManager.LogModWarning(
                                    mod: ModManager.GetMod(debugRegistryMethod.DeclaringType.Assembly),
                                    Message: "Generic method " + fullMethod + " found during " +
                                        CallChain(nameof(DebugMethodRegistry), nameof(GetRegistry)));
                            continue;
                        }
                        methodToInvoke = madeMethod;
                    }
                    methodToInvoke.Invoke(null, new object[] { Registry });
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(CallChain(nameof(DebugMethodRegistry), nameof(GetRegistry)), x, GAME_MOD_EXCEPTION);
                }
            }
            return Registry;
        }

        [ModSensitiveCacheInit]
        public static void CacheDebugRegistry()
        {
            _GotRegistry = false;
            LogRegistry();
            _GotRegistry = true;
        }

        public static bool GetDoDebug(string CallingMethod = null)
        {
            if (!DoDebugSetting)
                return false;

            if (CallingMethod.IsNullOrEmpty()
                || Instance.IsNullOrEmpty()
                || Instance.None(m => m.MethodName == CallingMethod))
                return DoDebugSetting;

            try
            {
                if (TryGetCallingTypeAndMethod(out _, out MethodBase callingMethodBase)
                    && Instance.TryGetValue(callingMethodBase, out bool registryMethodValue)
                    && !registryMethodValue)
                    return false;
            }
            catch (Exception x)
            {
                MetricsManager.LogException(CallChain(nameof(DebugMethodRegistry), nameof(GetDoDebug)), x, GAME_MOD_EXCEPTION);
            }
            return DoDebugSetting;
        }

        public static void LogRegistry()
        {
            if (!Instance.IsNullOrEmpty())
            {
                UnityEngine.Debug.Log("DEBUG [" + ThisMod.DisplayTitleStripped + "] - Start of " + CallChain(nameof(DebugMethodRegistry), nameof(LogRegistry)));

                foreach (MethodRegistryEntry methodEntry in Instance)
                    UnityEngine.Debug.Log(" ".ThisManyTimes(6) + methodEntry.ToString(IncludeSourceMod: true, FullSignature: true));

                UnityEngine.Debug.Log("DEBUG [" + ThisMod.DisplayTitleStripped + "] - End of " + CallChain(nameof(DebugMethodRegistry), nameof(LogRegistry)));
            }
            else
                UnityEngine.Debug.Log("DEBUG [" + ThisMod.DisplayTitleStripped + "] - " + CallChain(nameof(DebugMethodRegistry), nameof(LogRegistry)) + 
                    " called but " + nameof(Instance) + " not ready");

        }

        public DebugMethodRegistry Register(MethodRegistryEntry RegisterEntry)
        {
            MethodBase methodBase = RegisterEntry.GetMethod();
            string thisMethodName = CallChain(GetType().ToStringWithGenerics(), nameof(Register));
            string declaringType = methodBase?.DeclaringType?.Name;
            bool value = RegisterEntry.GetValue();

            if (methodBase == null)
                MetricsManager.LogModWarning(
                    mod: ThisMod,
                    Message: thisMethodName + " passed null " + nameof(MethodBase));

            Add(RegisterEntry);
            return this;
        }

        public DebugMethodRegistry Register<T>(T MethodBase, bool Value)
            where T : MethodBase
            => Register(new MethodRegistryEntry(MethodBase, Value));

        public DebugMethodRegistry Register(
            Type Class,
            string MethodName,
            bool Value)
            => Register(Class?.GetMethod(MethodName), Value);

        public DebugMethodRegistry Register(string MethodName, bool Value)
        {
            string thisMethodName = nameof(Debug) + "." + nameof(Register);
            if (TryGetCallingTypeAndMethod(out Type callingType, out MethodBase callingMethod))
            {
                bool any = false;
                foreach (MethodBase methodBase in callingType.GetMethods() ?? new MethodInfo[0])
                    if (methodBase.Name == MethodName)
                    {
                        any = true;
                        Register(methodBase, Value);
                    }
                if (!any)
                    MetricsManager.LogModWarning(
                        mod: ModManager.GetMod(callingType.Assembly),
                        Message: CallerSignatureString(callingType, callingMethod) +
                            " failed to register any methods called " + MethodName + " with " + thisMethodName);
            }
            else
                MetricsManager.LogModWarning(ThisMod, thisMethodName + " couldn't get " + nameof(callingType));

            return this;
        }

        public DebugMethodRegistry RegisterEachValue(
            Type Type,
            bool Value,
            params string[] Methods)
        {
            if (Methods.IsNullOrEmpty())
            {
                MetricsManager.LogModWarning(
                    mod: ModManager.GetMod(Type.Assembly),
                    Message: CallingTypeAndMethodNames(ConvertGenerics: true) + " passed empty " + nameof(Methods) + " to " +
                    CallChain(GetType().ToStringWithGenerics(), nameof(RegisterEachValue)));
                return this;
            }
            try
            {
                foreach (MethodBase typeMethod in Type.GetMethods())
                    if (Methods.Contains(typeMethod.Name))
                    {
                        MethodBase methodToRegister = typeMethod;
                        if (typeMethod.ContainsGenericParameters)
                        {
                            if (typeMethod is not MethodInfo typeMethodInfo
                                || typeMethodInfo.GetGenericArguments() is not Type[] genericArgs
                                || typeMethodInfo.MakeGenericMethod(genericArgs) is not MethodInfo madeMethod)
                            {
                                MetricsManager.LogModWarning(
                                    mod: ModManager.GetMod(Type.Assembly),
                                    Message: CallingTypeAndMethodNames(ConvertGenerics: true) + " failed to make non-generic " + typeMethod.MethodSignature(true) + " for " +
                                        CallChain(GetType().ToStringWithGenerics(), nameof(RegisterEachValue)));
                                continue;
                            }
                            methodToRegister = madeMethod;
                        }
                        Register(methodToRegister, Value);
                    }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(CallChain(GetType().ToStringWithGenerics(), nameof(RegisterEachValue)), x, GAME_MOD_EXCEPTION);
            }

            return this;
        }

        public DebugMethodRegistry RegisterEachValue<T>(
            Type Type,
            bool Value,
            params T[] Methods)
            where T : MethodBase
        {
            if (Methods.IsNullOrEmpty())
            {
                MetricsManager.LogModWarning(
                    mod: ModManager.GetMod(Type.Assembly),
                    Message: CallingTypeAndMethodNames(ConvertGenerics: true) + " passed empty " + nameof(Methods) + " to " +
                    CallChain(GetType().ToStringWithGenerics(), nameof(RegisterEachValue)));
                return this;
            }
            foreach (MethodBase typeMethod in Type.GetMethods() ?? new MethodBase[0])
                if (Methods.Contains(typeMethod))
                    Register(typeMethod, Value);

            return this;
        }

        public DebugMethodRegistry RegisterEach(
            Type Type,
            Dictionary<string, bool> MethodNameValues)
        {
            if (MethodNameValues.IsNullOrEmpty())
            {
                MetricsManager.LogModWarning(
                    mod: ModManager.GetMod(Type.Assembly),
                    Message: CallingTypeAndMethodNames(ConvertGenerics: true) + " passed empty " + nameof(MethodNameValues) + " to " +
                    CallChain(GetType().ToStringWithGenerics(), nameof(RegisterEach)));
                return this;
            }
            if (MethodNameValues.Values.Any(v => v))
                RegisterEachValue(Type, true, MethodNameValues.Where(e => e.Value).Select(e => e.Key).ToArray());

            if (MethodNameValues.Values.Any(v => !v))
                RegisterEachValue(Type, false, MethodNameValues.Where(e => !e.Value).Select(e => e.Key).ToArray());

            return this;
        }

        public DebugMethodRegistry RegisterEach<T>(
            Type Type,
            Dictionary<T, bool> MethodValues)
            where T : MethodBase
        {
            if (MethodValues.IsNullOrEmpty())
            {
                MetricsManager.LogModWarning(
                    mod: ModManager.GetMod(Type.Assembly),
                    Message: CallingTypeAndMethodNames(ConvertGenerics: true) + " passed empty " + nameof(MethodValues) + " to " +
                    CallChain(GetType().ToStringWithGenerics(), nameof(RegisterEach)));
                return this;
            }
            if (MethodValues.Values.Any(v => v))
                RegisterEachValue(Type, true, MethodValues.Where(e => e.Value).Select(e => e.Key).ToArray());

            if (MethodValues.Values.Any(v => !v))
                RegisterEachValue(Type, false, MethodValues.Where(e => !e.Value).Select(e => e.Key).ToArray());

            return this;
        }

        public DebugMethodRegistry RegisterEachFalse(
            Type Type,
            params string[] Methods)
        {
            if (Methods.IsNullOrEmpty())
            {
                MetricsManager.LogModWarning(
                    mod: ModManager.GetMod(Type.Assembly),
                    Message: CallingTypeAndMethodNames(ConvertGenerics: true) + " passed empty " + nameof(Methods) + " to " +
                    CallChain(GetType().ToStringWithGenerics(), nameof(RegisterEachFalse)));
                return this;
            }
            return RegisterEachValue(Type, false, Methods);
        }

        public DebugMethodRegistry RegisterEachFalse<T>(
            Type Type,
            params T[] Methods)
            where T : MethodBase
        {
            if (Methods.IsNullOrEmpty())
            {
                MetricsManager.LogModWarning(
                    mod: ModManager.GetMod(Type.Assembly),
                    Message: CallingTypeAndMethodNames(ConvertGenerics: true) + " passed empty " + nameof(Methods) + " to " +
                    CallChain(GetType().ToStringWithGenerics(), nameof(RegisterEachFalse)));
                return this;
            }
            return RegisterEachValue(Type, false, Methods);
        }

        public DebugMethodRegistry RegisterEachTrue(
            Type Type,
            params string[] Methods)
        {
            if (Methods.IsNullOrEmpty())
            {
                MetricsManager.LogModWarning(
                    mod: ModManager.GetMod(Type.Assembly),
                    Message: CallingTypeAndMethodNames(ConvertGenerics: true) + " passed empty " + nameof(Methods) + " to " +
                    CallChain(GetType().ToStringWithGenerics(), nameof(RegisterEachTrue)));
                return this;
            }
            return RegisterEachValue(Type, true, Methods);
        }

        public DebugMethodRegistry RegisterEachTrue<T>(
            Type Type,
            params T[] Methods)
            where T : MethodBase
        {
            if (Methods.IsNullOrEmpty())
            {
                MetricsManager.LogModWarning(
                    mod: ModManager.GetMod(Type.Assembly),
                    Message: CallingTypeAndMethodNames(ConvertGenerics: true) + " passed empty " + nameof(Methods) + " to " +
                    CallChain(GetType().ToStringWithGenerics(), nameof(RegisterEachTrue)));
                return this;
            }
            return RegisterEachValue(Type, true, Methods);
        }

        public DebugMethodRegistry RegisterHandleEventVariants(
            Type Type,
            Dictionary<Type, bool> MinEventTypeValues)
        {
            if (MinEventTypeValues.IsNullOrEmpty())
            {
                MetricsManager.LogModWarning(
                    mod: ModManager.GetMod(Type.Assembly),
                    Message: CallingTypeAndMethodNames(ConvertGenerics: true) + " passed empty " + nameof(MinEventTypeValues) + " to " +
                    CallChain(GetType().ToStringWithGenerics(), nameof(RegisterHandleEventVariants)));
                return this;
            }
            return RegisterEach(
                Type: Type,
                MethodValues: Type.GetMethods()?.Aggregate(
                    seed: new Dictionary<MethodBase, bool>(),
                    func: delegate (Dictionary<MethodBase, bool> a, MethodInfo n)
                    {
                        if (n.Name == nameof(GameObject.HandleEvent)
                            && n.GetParameters() is ParameterInfo[] paramInfos
                            && paramInfos.Length == 1
                            && paramInfos[0].ParameterType is Type eventType
                            && MinEventTypeValues.ContainsKey(eventType))
                            a[n] = MinEventTypeValues[eventType];
                        return a;
                    }));
        }

        public bool Contains<T>(T MethodBase)
            where T : MethodBase
            => MethodBase is not null
            && this.Any(e => e.Equals(MethodBase as MethodBase));

        public bool Contains(Type DeclaringType, string MethodName)
            => !MethodName.IsNullOrEmpty()
            && DeclaringType
                ?.GetMethods()
                ?.Where(m => m.Name == MethodName)
                ?.Select(m => m as MethodBase) is MethodBase[] declaredTypeMethods
            && !declaredTypeMethods.IsNullOrEmpty()
            && declaredTypeMethods.Any(m => Contains(m));

        public bool Contains(string MethodName)
            => !MethodName.IsNullOrEmpty()
            && this.Any(e => e.GetMethod()?.Name == MethodName);

        public bool GetValue<T>(T MethodBase)
            where T : MethodBase
        {
            foreach (MethodRegistryEntry registryEntry in this)
                if (registryEntry.Equals(MethodBase as MethodBase))
                    return (bool)registryEntry;

            throw new ArgumentOutOfRangeException(nameof(MethodBase), "Not found.");
        }

        public bool TryGetValue<T>(
            T MethodBase,
            out bool Value)
            where T : MethodBase
        {
            Value = default;
            if (Contains(MethodBase))
            {
                Value = GetValue(MethodBase);
                return true;
            }
            return false;
        }
    }
}
