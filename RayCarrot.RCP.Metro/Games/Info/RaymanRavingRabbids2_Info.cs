﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// The Rayman Raving Rabbids 2 game info
    /// </summary>
    public sealed class RaymanRavingRabbids2_Info : RCPGameInfo
    {
        #region Protected Override Properties

        /// <summary>
        /// Gets the backup directories for the game
        /// </summary>
        protected override IList<BackupDir> GetBackupDirectories => new BackupDir[]
        {
            new BackupDir(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RRR2"), SearchOption.TopDirectoryOnly, "*", "0", 0)
        };

        #endregion

        #region Public Overrides

        /// <summary>
        /// The game
        /// </summary>
        public override Games Game => Games.RaymanRavingRabbids2;

        /// <summary>
        /// The game display name
        /// </summary>
        public override string DisplayName => "Rayman Raving Rabbids 2";

        /// <summary>
        /// The game backup name
        /// </summary>
        public override string BackupName => "Rayman Raving Rabbids 2";

        /// <summary>
        /// Gets the launch name for the game
        /// </summary>
        public override string DefaultFileName => "Jade.exe";

        /// <summary>
        /// The options UI, if any is available
        /// </summary>
        public override FrameworkElement OptionsUI => new RavingRabbids2Options();

        /// <summary>
        /// Gets the file links for the game
        /// </summary>
        public override IList<GameFileLink> GetGameFileLinks => new GameFileLink[]
        {
            new GameFileLink(Resources.GameLink_Setup, GameData.InstallDirectory + "SettingsApplication.exe")
        };

        #endregion
    }
}