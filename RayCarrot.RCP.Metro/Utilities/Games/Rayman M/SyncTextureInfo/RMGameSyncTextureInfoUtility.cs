﻿using RayCarrot.Rayman;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// The sync texture info utility for Rayman M
    /// </summary>
    public class RMGameSyncTextureInfoUtility : BaseGameSyncTextureInfoUtility
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public RMGameSyncTextureInfoUtility() : base(new BaseGameSyncTextureInfoUtilityViewModel(Games.RaymanM, GameMode.RaymanMPC, new string[]
        {
            "MenuBin",
            "TribeBin",
            "FishBin",
        }))
        { }
    }
}