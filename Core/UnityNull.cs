using UnityEngine;

namespace DescendersModMenu
{
    /// <summary>
    /// Unity destroys scene objects on map change but leaves the managed
    /// wrapper non-null. <c>(object)x == null</c> misses that; Unity's
    /// overloaded <c>==</c> / truthiness catches it.
    /// </summary>
    public static class UnityNull
    {
        public static bool Alive(Object obj)
        {
            return (object)obj != null && obj;
        }
    }
}
