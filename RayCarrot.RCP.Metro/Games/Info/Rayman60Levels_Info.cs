﻿using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// The Rayman 60 Levels game info
    /// </summary>
    public sealed class Rayman60Levels_Info : RCPGameInfo
    {
        #region Protected Override Properties

        /// <summary>
        /// Gets the backup directories for the game
        /// </summary>
        protected override IList<BackupDir> GetBackupDirectories => new BackupDir[]
        {
            new BackupDir(GameData.InstallDirectory, SearchOption.TopDirectoryOnly, "*.cfg", "0", 0),
            new BackupDir(GameData.InstallDirectory + "PCMAP", SearchOption.TopDirectoryOnly, "*.sct", "1", 0)
        };

        #endregion

        #region Public Override Properties

        /// <summary>
        /// The game
        /// </summary>
        public override Games Game => Games.Rayman60Levels;

        /// <summary>
        /// The game display name
        /// </summary>
        public override string DisplayName => "Rayman 60 Levels";

        /// <summary>
        /// The game backup name
        /// </summary>
        public override string BackupName => "Rayman 60 Levels";

        /// <summary>
        /// Gets the launch name for the game
        /// </summary>
        public override string DefaultFileName => "Rayman.bat";

        /// <summary>
        /// The config UI, if any is available
        /// </summary>
        public override FrameworkElement ConfigUI => new DosBoxConfig(Game);

        /// <summary>
        /// Gets the file links for the game
        /// </summary>
        public override IList<GameFileLink> GetGameFileLinks => new GameFileLink[0];

        #endregion
    }
}