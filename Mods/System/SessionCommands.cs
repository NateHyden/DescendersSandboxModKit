using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Safe wrappers around fragile DevCommandsGameplay helpers.
    /// The stock JumpToFinish NREs when finish line / player / bike is missing.
    /// </summary>
    public static class SessionCommands
    {
        public static bool TryJumpToFinish(out string error)
        {
            error = null;
            try
            {
                FinishLine fl = FinishLine.GetAFinishLine();
                if (!UnityNull.Alive(fl))
                {
                    error = "No finish line here";
                    return false;
                }

                GameObject playerGo = PlayerCache.PlayerHuman;
                if (!UnityNull.Alive(playerGo))
                {
                    error = "Not in a session";
                    return false;
                }

                Vehicle bike = playerGo.GetComponent<Vehicle>();
                if (!UnityNull.Alive(bike))
                {
                    error = "No bike";
                    return false;
                }

                Vector3 p = fl.transform.position;
                bike.transform.position = new Vector3(p.x, p.y + 10f, p.z);
                return true;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SessionCommands] JumpToFinish: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "SessionCommands");
                error = "Failed - see log";
                return false;
            }
        }
    }
}
