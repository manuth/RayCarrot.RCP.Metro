﻿using RayCarrot.Extensions;
using RayCarrot.RCP.Core;
using RayCarrot.RCP.UI;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// The API controller manager for the Rayman Control Panel Metro
    /// </summary>
    public class RCPMetroAPIControllerManager : RCPAPIControllerManager
    {
        /// <summary>
        /// Gets the available game data for the specified game, or null if not found
        /// </summary>
        /// <param name="game">The game to get the game data for</param>
        /// <returns>The game data for the specified game, or null if not found</returns>
        public override GameData GetGameData(Games game)
        {
            return RCFRCP.Data.Games.TryGetValue(game);
        }
    }
}