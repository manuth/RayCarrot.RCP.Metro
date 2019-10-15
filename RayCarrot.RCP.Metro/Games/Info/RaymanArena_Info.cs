﻿using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// The Rayman Arena game info
    /// </summary>
    public sealed class RaymanArena_Info : RCPGameInfo
    {
        #region Protected Override Properties

        /// <summary>
        /// Gets the backup directories for the game
        /// </summary>
        protected override IList<BackupDir> GetBackupDirectories => new BackupDir[]
        {
            new BackupDir(GameData.InstallDirectory + "Menu" + "SaveGame", SearchOption.TopDirectoryOnly, "*", "0", 0)
        };

        #endregion

        #region Public Override Properties

        /// <summary>
        /// The game
        /// </summary>
        public override Games Game => Games.RaymanArena;

        /// <summary>
        /// The game display name
        /// </summary>
        public override string DisplayName => "Rayman Arena";

        /// <summary>
        /// The game backup name
        /// </summary>
        public override string BackupName => "Rayman Arena";

        /// <summary>
        /// Gets the launch name for the game
        /// </summary>
        public override string DefaultFileName => "R_Arena.exe";

        /// <summary>
        /// The config UI, if any is available
        /// </summary>
        public override FrameworkElement ConfigUI => new Ray_M_Arena_3_Config(Game);

        /// <summary>
        /// Gets the purchase links for the game
        /// </summary>
        public override IList<GamePurchaseLink> GetGamePurchaseLinks => new GamePurchaseLink[0];

        /// <summary>
        /// Gets the file links for the game
        /// </summary>
        public override IList<GameFileLink> GetGameFileLinks => new GameFileLink[]
        {
            new GameFileLink(Resources.GameLink_Setup, GameData.InstallDirectory + "RM_Setup_DX8.exe")
        };

        #endregion
    }
}