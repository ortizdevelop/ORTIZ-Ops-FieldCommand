using System.Collections.Generic;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Command mode, 3D view: missiles, rockets and guided shells in flight are shown with the game's own icons
    /// (the same ones the HUD and the map use) — yours and allied at their position, enemy ones where your side
    /// tracks them. On the big map the game shows them by itself.
    /// </summary>
    internal static class Projectiles
    {
        public static void Draw3D(Camera cam, HashSet<Unit> mine)
        {
            CommandMarkers.DrawMissiles(cam, mine);
        }
    }
}
