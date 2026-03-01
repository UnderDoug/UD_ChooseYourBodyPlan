using System;
using System.Collections.Generic;
using System.Linq;

using Qud.UI;

using XRL;
using XRL.UI;
using XRL.World;

using static XRL.XmlDataHelper;

namespace UD_BodyPlan_Selection.Mod
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasCallAfterGameLoaded]
    public static class Startup
    {
        [ModSensitiveCacheInit]
        public static void ModSensitiveCacheInit()
        {
            // Called at game startup and whenever mod configuration changes

            AddAttributeParser(typeof(Dictionary<string, string>), new AttributeParser<Dictionary<string, string>>
            {
                Parse = delegate (string s)
                {
                    Exception innerException = null;
                    try
                    {
                        return s.CachedDictionaryExpansion();
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
                        return s.CachedNumericDictionaryExpansion();
                    }
                    catch (Exception ex)
                    {
                        innerException = ex;
                    }
                    throw new Exception("Could not figure out dictionary format. Separate KeyValuePairs by \";;\" and separate key and value with \"::\".", innerException);
                }
            });
        }

        [GameBasedCacheInit]
        public static void GameBasedCacheInit()
        {
            // Called once when world is first generated.

            // The.Game registered events should go here.
        }

        // [PlayerMutator]

        // The.Player.FireEvent("GameRestored");
        // AfterGameLoadedEvent.Send(Return);  // Return is the game.

        [CallAfterGameLoaded]
        public static void OnLoadGameCallback()
        {
            // Gets called every time the game is loaded but not during generation

        }
    }

    // [ModSensitiveCacheInit]

    // [GameBasedCacheInit]

    [PlayerMutator]
    public class OnPlayerLoad : IPlayerMutator
    {
        public void mutate(GameObject player)
        {
            // Gets called once when the player is first generated
        }
    }

    // [CallAfterGameLoaded]
}