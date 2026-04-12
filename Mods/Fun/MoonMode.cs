using MelonLoader;

namespace DescendersModMenu.Mods
{
    public static class MoonMode
    {
        public static bool IsActive = false;

        private static int _savedGravityLevel = -1;
        private static int _savedTravelLevel = -1;
        private static int _savedDampingLevel = -1;

        public static void Toggle()
        {
            try
            {
                if (!IsActive)
                {
                    _savedGravityLevel = Gravity.Level;
                    _savedTravelLevel = Suspension.TravelLevel;
                    _savedDampingLevel = Suspension.DampingLevel;
                    Gravity.SetLevel(1);
                    Suspension.SetTravelLevel(10);
                    Suspension.SetDampingLevel(1);
                    IsActive = true;
                }
                else
                {
                    Gravity.SetLevel(_savedGravityLevel > 0 ? _savedGravityLevel : 5);
                    Suspension.SetTravelLevel(_savedTravelLevel > 0 ? _savedTravelLevel : 5);
                    Suspension.SetDampingLevel(_savedDampingLevel > 0 ? _savedDampingLevel : 5);
                    IsActive = false;
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[MoonMode] Toggle: " + ex.Message); }
        }
    }
}
