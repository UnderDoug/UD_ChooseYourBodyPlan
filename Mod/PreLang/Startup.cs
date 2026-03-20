using System;
using System.Collections.Generic;

using XRL;

using static UD_ChooseYourBodyPlan.Mod.Utils;
using static XRL.XmlDataHelper;

namespace UD_ChooseYourBodyPlan.Mod
{
    public static partial class Startup
    {
        [ModSensitiveCacheInit]
        public static void BetaModSensitiveCacheInit_Beta()
        {
            // Called at game startup and whenever mod configuration changes

            AddAttributeParser(typeof(Dictionary<string, string>), new AttributeParser<Dictionary<string, string>>
            {
                Parse = delegate (string s)
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
                }
            });
            AddAttributeParser(typeof(Dictionary<string, int>), new AttributeParser<Dictionary<string, int>>
            {
                Parse = delegate (string s)
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
                }
            });
        }

        public static Func<string,T> GetParser<T>()
            => TryGetAttributeParser<T>() is AttributeParser<T> parser
            ? parser.Invoke
            : null
            ;
    }
}