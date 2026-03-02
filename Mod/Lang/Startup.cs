using System;
using System.Collections.Generic;
using System.Linq;

using XRL;
using XRL.Serialization.Parsers;

using static UD_BodyPlan_Selection.Mod.Utils;
using static XRL.XmlDataHelper;

namespace UD_BodyPlan_Selection.Mod
{
    public static partial class Startup
    {
        [ModSensitiveCacheInit]
        public static void ModSensitiveCacheInit_Lang()
        {
            // Called at game startup and whenever mod configuration changes

            IValueParser.Add(new DelegateParser<Dictionary<string, string>>(
                ParseDelegate: delegate (string s)
                {
                    Exception innerException = null;
                    try
                    {
                        return new(s.CachedDictionaryExpansion());
                    }
                    catch (Exception ex)
                    {
                        innerException = ex;
                    }
                    throw new Exception("Could not figure out dictionary format. Separate KeyValuePairs by \";;\" and separate key and value with \"::\".", innerException);
                },
                ComposeDelegate: d => d.ToStringForCachedDictionaryExpansion()
                ));
            IValueParser.Add(new DelegateParser<Dictionary<string, int>>(
                ParseDelegate: delegate (string s)
                {
                    Exception innerException = null;
                    try
                    {
                        return new(s.CachedNumericDictionaryExpansion());
                    }
                    catch (Exception ex)
                    {
                        innerException = ex;
                    }
                    throw new Exception("Could not figure out dictionary format. Separate KeyValuePairs by \";;\" and separate key and value with \"::\".", innerException);
                },
                ComposeDelegate: delegate (Dictionary<string, int> d)
                {
                    string output = null;
                    foreach ((string key, int value) in d)
                    {
                        if (!output.IsNullOrEmpty())
                            output += ";;";
                        output += key + "::" + value;
                    }
                    return d.Aggregate("", (a, n) => $"{a}{(!a.IsNullOrEmpty() ? ";;" : null)}{n.Key}::{n.Value}");
                }));
        }

        public static Func<string, T> GetParser<T>()
            => TryGetParser(out IValueParser<T> parser)
            ? parser.Parse
            : null
            ;
    }
}