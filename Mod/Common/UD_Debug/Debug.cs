using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;

using HarmonyLib;

using XRL;
using XRL.Wish;

using UD_ChooseYourBodyPlan.Mod;
using static UD_ChooseYourBodyPlan.Mod.Options;
using static UD_ChooseYourBodyPlan.Mod.Const;
using static UD_ChooseYourBodyPlan.Mod.Utils;

namespace UD_ChooseYourBodyPlan.Mod.Logging
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasWishCommand]
    public static class Debug
    {
        #region Debug Registration
        [UD_DebugRegistry]
        public static void doDebugRegistry(DebugMethodRegistry Registry)
            => Registry.RegisterEachTrue(
                Type: typeof(UD_ChooseYourBodyPlan.Mod.Logging.Debug),
                Methods: new string[]
                {
                    nameof(LogCritical),
                });
        #endregion

        public static bool SilenceLogging = false;

        public static void SetSilenceLogging(bool Value)
        {
            SilenceLogging = Value;
        }
        public static bool GetSilenceLogging()
            => SilenceLogging;

        public static void ToggleLogging()
        {
            SetSilenceLogging(!SilenceLogging);
        }

        public static DebugMethodRegistry DoDebugRegistry => DebugMethodRegistry.Instance;

        public static bool DoDebugSetting
            => (EnableLogging is not false)
            && !SilenceLogging
            ;

        public static List<Type> DebugTypes = new()
        {
            typeof(UD_ChooseYourBodyPlan.Mod.Logging.Debug),
            typeof(UD_ChooseYourBodyPlan.Mod.Logging.Debug.ArgPair),
            typeof(UD_ChooseYourBodyPlan.Mod.Logging.Indent),
            typeof(UD_ChooseYourBodyPlan.Mod.Logging.DebugMethodRegistry),
            typeof(UD_ChooseYourBodyPlan.Mod.Logging.MethodRegistryEntry),
        };

        public static string CallerString(Type CallingType, MethodBase CallingMethod)
            => CallingType.Name + "." + CallingMethod.Name;

        public static string CallerSignatureString(Type CallingType, MethodBase CallingMethod)
            => CallerString(CallingType, CallingMethod) +
            "(" +
            CallingMethod
                ?.GetParameters()
                ?.Aggregate("", delegate (string a, ParameterInfo n)
                {
                    string paramType = n?.ParameterType?.Name ?? "null??";
                    return !a.IsNullOrEmpty()
                        ? a + ", " + paramType
                        : a + paramType;
                }) +
            ")";

        [ModSensitiveStaticCache(CreateEmptyInstance = true)]
        private static Stack<Indent> Indents = new();

        public static Indent LastIndent
            => Indents.TryPeek(out Indent peek)
            ? peek
            : ResetIndent();

        public static Indent GetNewIndent(int Offset)
            => new(Offset);

        public static Indent GetNewIndent()
            => GetNewIndent(0);

        public static bool HaveIndents()
            => !Indents.IsNullOrEmpty();

        public static void PushToIndents(Indent Indent)
            => Indents.Push(Indent);

        [GameBasedCacheInit]
        [ModSensitiveCacheInit]
        public static Indent ResetIndent()
        {
            Indents ??= new();
            Indents.Clear();
            return GetNewIndent();
        }

        public static Indent DiscardIndent()
        {
            if (!Indents.TryPop(out _))
                ResetIndent();

            return LastIndent;
        }
        public static bool HasIndent(Indent Indent)
            => Indents.Contains(Indent);

        public static string CallingTypeAndMethodNames(bool AppendSpace = false, bool TrimModPrefix = true, bool ConvertGenerics = false)
        {
            if (TryGetCallingTypeAndMethod(out Type declaringType, out MethodBase methodBase))
            {
                string declaringTypeName = ConvertGenerics
                    ? declaringType.ToStringWithGenerics()
                    : declaringType.Name;

                if (TrimModPrefix)
                    declaringTypeName = declaringTypeName.Replace(ThisMod.ID + "_", "");

                return CallChain(declaringTypeName, methodBase.Name) + (AppendSpace ? " " : "");
            }
            return null;
        }
        public static string CallingMethodName(bool AppendSpace = false)
        {
            if (TryGetCallingTypeAndMethod(out _, out MethodBase methodBase))
            {
                return methodBase.Name + (AppendSpace ? " " : "");
            }
            return null;
        }

        private static bool IsDebugFrame(this StackFrame Frame)
            => DebugTypes?.Any(t => t == Frame?.GetMethod()?.DeclaringType) is true;

        private static bool IsNonDebugFrame(this StackFrame Frame)
            => !Frame.IsDebugFrame();

        private static bool TryGetCallingTypeAndMethod(
            this StackFrame Frame,
            out Type CallingType,
            out MethodBase CallingMethod)
            => (CallingType = (CallingMethod = Frame?.GetMethod())?.DeclaringType) != null;

        public static bool TryGetCallingTypeAndMethod(out Type CallingType, out MethodBase CallingMethod)
        {
            CallingType = null;
            CallingMethod = null;
            try
            {
                return new StackTrace()
                        ?.GetFrames()
                        ?.FirstOrDefault(IsNonDebugFrame)
                        ?.TryGetCallingTypeAndMethod(out CallingType, out CallingMethod)
                    ?? false;
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(TryGetCallingTypeAndMethod), x, GAME_MOD_EXCEPTION);
            }
            return false;
        }

        public static Indent LogCritical<T>(string Label, T Value, Indent Indent = null)
        {
            Indent ??= LastIndent;
            string output = Label;
            if (Value != null &&
                !Value.ToString().IsNullOrEmpty())
                output += ": " + Value;

            UnityEngine.Debug.Log(Indent.ToString() + output);
            return Indent;
        }
        public static Indent LogCritical(string Message, Indent Indent = null)
            => LogCritical(Message, (string)null, Indent);

        public static Indent Log<T>(
            string Label,
            T Value,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = ""
            )
        {
            if (!DebugMethodRegistry.GetDoDebug(CallingMethod))
                return Indent;

            return LogCritical(Label, Value, Indent);
        }

        public static Indent Log(
            string Message,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = ""
            )
            => Log(Message, (string)null, Indent, CallingMethod);

        public readonly struct ArgPair
        {
            public static ArgPair Empty = default;

            private readonly string Name;
            private readonly object Value;
            public ArgPair(string Name, object Value)
            {
                this.Name = Name;
                this.Value = Value;
            }

            public override readonly string ToString()
                => Name.IsNullOrEmpty() 
                ? Value?.ToString() 
                : Name + ": " + Value?.ToString();

            public Indent Log(Indent Indent, [CallerMemberName] string CallingMethod = "")
                => Debug.Log(Name, Value, Indent ?? LastIndent, CallingMethod);
            public Indent Log([CallerMemberName] string CallingMethod = "")
                => Log(null, CallingMethod);
            public Indent Log(int Offset, [CallerMemberName] string CallingMethod = "")
                => Log(LastIndent[Offset], CallingMethod);

            public override bool Equals(object obj)
                => (obj is ArgPair argPairObj
                    && Equals(argPairObj))
                || base.Equals(obj);

            public bool Equals(ArgPair Other)
            {
                if (Name != Other.Name)
                {
                    return false;
                }
                if ((Value != null) != (Other.Value != null))
                {
                    return false;
                }
                return Value == Other.Value;
            }

            public override int GetHashCode()
                => (Name?.GetHashCode() ?? 0) ^ (Value?.GetHashCode() ?? 0);

            public static bool operator ==(ArgPair Operand1, ArgPair Operand2)
                => Operand1.Equals(Operand2);
            public static bool operator !=(ArgPair Operand1, ArgPair Operand2)
                => !(Operand1 == Operand2);
        }

        public static ArgPair Arg(string Name, object Value)
            => new(Name, Value);

        public static ArgPair Arg(object Value)
            => Arg(null, Value);

        public static Indent LogCaller(
            string MessageAfter,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
        {
            if (!DebugMethodRegistry.GetDoDebug(CallingMethod))
                return Indent;

            string output = "";
            if (!ArgPairs.IsNullOrEmpty())
            {
                List<string> joinableArgs = ArgPairs.ToList()
                    ?.Where(ap => ap != ArgPair.Empty)
                    ?.ToList()
                    ?.ConvertAll(ap => ap.ToString())
                    ?.ToList();
                if (!joinableArgs.IsNullOrEmpty())
                {
                    output += "(" + joinableArgs?.SafeJoin() + ")";
                }
            }
            if (!MessageAfter.IsNullOrEmpty())
            {
                output += " " + MessageAfter;
            }
            return Log(CallingTypeAndMethodNames(ConvertGenerics: true) + output, Indent, CallingMethod);
        }
        public static Indent LogCaller(
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
            => LogCaller(null, Indent, CallingMethod, ArgPairs);

        public static Indent LogMethod(
            string MessageAfter,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
        {
            if (!DebugMethodRegistry.GetDoDebug(CallingMethod))
            {
                return Indent;
            }
            string output = "";
            if (!ArgPairs.IsNullOrEmpty())
            {
                List<string> joinableArgs = ArgPairs.ToList()
                    ?.Where(ap => ap != ArgPair.Empty)
                    ?.ToList()
                    ?.ConvertAll(ap => ap.ToString())
                    ?.ToList();
                if (!joinableArgs.IsNullOrEmpty())
                {
                    output += "(" + joinableArgs?.SafeJoin() + ")";
                }
            }
            if (!MessageAfter.IsNullOrEmpty())
            {
                output += " " + MessageAfter;
            }
            return Log(CallingMethod + output, Indent, CallingMethod);
        }
        public static Indent LogMethod(
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
            => LogMethod(null, Indent, CallingMethod, ArgPairs);

        public static Indent LogArgs(
            string MessageBefore,
            string MessageAfter,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
        {
            string output = "";
            if (!MessageBefore.IsNullOrEmpty())
            {
                output += MessageBefore;
            }
            if (!ArgPairs.IsNullOrEmpty())
            {
                List<string> joinableArgs = ArgPairs.ToList()
                    ?.Where(ap => ap != ArgPair.Empty)
                    ?.ToList()
                    ?.ConvertAll(ap => ap.ToString())
                    ?.ToList();
                if (!joinableArgs.IsNullOrEmpty())
                {
                    output += joinableArgs?.SafeJoin();
                }
            }
            if (!MessageAfter.IsNullOrEmpty())
            {
                output += MessageAfter;
            }
            return Log(output, Indent, CallingMethod);
        }

        public static Indent LogArgs(
            string MessageBefore,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
            => LogArgs(MessageBefore, null, Indent, CallingMethod, ArgPairs);

        public static Indent YehNah(
            string Message,
            object Value,
            bool? Good = null,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
        {
            string append;
            if (Good != null)
            {
                if (!Good.GetValueOrDefault())
                {
                    append = AppendCross("");
                }
                else
                {
                    append = AppendTick("");
                }
            }
            else
            {
                append = "[-] ";
            }
            return Log(append + Message, Value, Indent, CallingMethod);
        }
        public static Indent YehNah(
            string Message,
            bool?Good = null,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
            => YehNah(Message, null, Good, Indent, CallingMethod);

        public static Indent CheckYeh(
            string Message,
            object Value,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
            => YehNah(Message, Value, true, Indent, CallingMethod);

        public static Indent CheckYeh(
            string Message,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
            => YehNah(Message, null, true, Indent, CallingMethod);

        public static Indent CheckNah(
            string Message,
            object Value,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
            => YehNah(Message, Value, false, Indent, CallingMethod);

        public static Indent CheckNah(
            string Message,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
            => YehNah(Message, null, false, Indent, CallingMethod);

        private static string SafeInvoke<T>(this Func<string, string> PostProc, Func<T, string> Proc, T Element, string NoArg)
        {
            string proc = Proc?.Invoke(Element) ?? Element?.ToString() ?? NoArg;
            if (PostProc != null)
                proc = PostProc(proc);
            return proc;
        }
        public static Indent Loggregrate<T>(
            IEnumerable<T> Source,
            Func<T, string> Proc = null,
            string Empty = null,
            Func<string, string> PostProc = null,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = ""
            )
            => Source.IsNullOrEmpty()
            ? Log(PostProc?.Invoke(Empty) ?? Empty, Indent: Indent, CallingMethod)
            : Source.Aggregate(
                seed: Indent,
                func: (a, n) => Log(PostProc.SafeInvoke(Proc, n, "NO_ELEMENT"), Indent: a, CallingMethod));

        public static Indent LogTime(
            string Message,
            Stopwatch StopWatch,
            bool Stop = false,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
        {
            if (Stop)
                StopWatch.Stop();
            return Log(
                Label: Message ?? StopWatch.Elapsed.ValueUnits(),
                Value: !Message.IsNullOrEmpty() ? StopWatch.Elapsed.ValueUnits() : null,
                Indent: Indent,
                CallingMethod: CallingMethod);
        }

        public static Indent LogTimeStop(
            string Message,
            Stopwatch StopWatch,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
            => LogTime(Message, StopWatch, true, Indent, CallingMethod);

        public static void MetricsManager_LogCallingModError(object Message)
        {
            if (!TryGetFirstCallingModNot(ThisMod, out ModInfo callingMod))
                callingMod = ThisMod;

            MetricsManager.LogModError(callingMod, Message);
        }

        #region Extensions

        public static string GenericsString(this Type[] GenericTypes)
            => !GenericTypes.IsNullOrEmpty()
            ? "<" + 
                GenericTypes
                    ?.Select(t => t.ToStringWithGenerics())
                    ?.Aggregate("", CommaSpaceDelimitedAggregator) + 
                ">"
            : null;

        public static string ParamsString(this ParameterInfo[] ParamTypes)
            => "(" + 
                ParamTypes
                    ?.Select(p => p.ParameterType.ToStringWithGenerics())
                    ?.Aggregate("", CommaSpaceDelimitedAggregator) + 
                ")";

        public static string MethodSignature<T>(this T MethodBase, bool IndicateNull = false)
            where T : MethodBase
        {
            if (MethodBase is null)
                return IndicateNull
                    ? "NULL_METHOD"
                    : null;

            string genericsString = null;
            if (!MethodBase.IsConstructor
                && !MethodBase.Name.StartsWith("get_")
                && !MethodBase.Name.StartsWith("set_")
                && MethodBase.IsGenericMethod)
                genericsString = MethodBase.GetGenericArguments().GenericsString();

            return MethodBase.Name +
                genericsString +
                MethodBase.GetParameters().ParamsString();
        }

        public static bool SuperficiallyEquivalent<Tx, Ty>(this Tx X, Ty Y)
            where Tx : MethodBase
            where Ty : MethodBase
            => X?.Name == Y?.Name
            && Equals(X?.DeclaringType, Y?.DeclaringType)
            && Equals(X?.DeclaringType?.Assembly, Y?.DeclaringType?.Assembly);

        public static bool MatchingGenerics<Tx, Ty>(this Tx X, Ty Y)
            where Tx : MethodBase
            where Ty : MethodBase
        {
            if (EitherNull(X, Y, out bool areEqual))
                return areEqual;

            if (X.ContainsGenericParameters != Y.ContainsGenericParameters)
                return false;

            if (X.IsConstructor || Y.IsConstructor)
                return X.IsConstructor == Y.IsConstructor;

            Type[] xGenerics = X.GetGenericArguments();
            Type[] yGenerics = Y.GetGenericArguments();

            return xGenerics.ElementsMatch(yGenerics);
        }

        public static bool MatchingParams<Tx, Ty>(this Tx X, Ty Y)
            where Tx : MethodBase
            where Ty : MethodBase
        {
            if (EitherNull(X, Y, out bool areEqual))
                return areEqual;

            Type[] xParams = X.GetParameters()?.Select(p => p.ParameterType)?.ToArray();
            Type[] yParams = Y.GetParameters()?.Select(p => p.ParameterType)?.ToArray();

            return xParams.ElementsMatch(yParams);
        }


        #endregion

        /*
         * 
         * Wishes!
         * 
         */

    }
}
