using XRL;
using XRL.World;

namespace UD_BodyPlan_Selection.Mod
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasCallAfterGameLoaded]
    public static partial class Startup
    {
        [ModSensitiveCacheInit]
        public static void BetaModSensitiveCacheInit()
        {
            // Called at game startup and whenever mod configuration changes

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