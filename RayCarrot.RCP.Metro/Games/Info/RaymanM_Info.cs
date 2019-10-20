﻿using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// The Rayman M game info
    /// </summary>
    public sealed class RaymanM_Info : RCPGameInfo
    {
        #region Protected Override Properties

        /// <summary>
        /// Gets the backup directories for the game
        /// </summary>
        protected override IList<BackupDir> GetBackupDirectories => new List<BackupDir>()
        {
            new BackupDir(GameData.InstallDirectory + "Menu" + "SaveGame", SearchOption.TopDirectoryOnly, "*", "0", 0)
        };

        #endregion

        #region Public Override Properties

        /// <summary>
        /// The game
        /// </summary>
        public override Games Game => Games.RaymanM;

        /// <summary>
        /// The game display name
        /// </summary>
        public override string DisplayName => "Rayman M";

        /// <summary>
        /// The game backup name
        /// </summary>
        public override string BackupName => "Rayman M";

        /// <summary>
        /// Gets the launch name for the game
        /// </summary>
        public override string DefaultFileName => "RaymanM.exe";

        /// <summary>
        /// The config UI, if any is available
        /// </summary>
        public override FrameworkElement ConfigUI => new Ray_M_Arena_3_Config(Game);

        /// <summary>
        /// Gets the file links for the game
        /// </summary>
        public override IList<GameFileLink> GetGameFileLinks => new GameFileLink[]
        {
            new GameFileLink(Resources.GameLink_Setup, GameData.InstallDirectory + "RM_Setup_DX8.exe")
        };

        /// <summary>
        /// The group names to use for the options, config and utility dialog
        /// </summary>
        public override IEnumerable<string> DialogGroupNames => new string[]
        {
            UbiIniFileGroupName
        };

        /// <summary>
        /// Indicates if the game can be installed from a disc in this program
        /// </summary>
        public override bool CanBeInstalledFromDisc => true;

        #endregion
    }
}