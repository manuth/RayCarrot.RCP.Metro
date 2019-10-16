﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using RayCarrot.IO;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// The Rayman Raving Rabbids game info
    /// </summary>
    public sealed class RaymanRavingRabbids_Info : RCPGameInfo
    {
        #region Protected Override Properties

        /// <summary>
        /// Gets the backup directories for the game
        /// </summary>
        protected override IList<BackupDir> GetBackupDirectories
        {
            get
            {
                var dirs = new List<BackupDir>()
                {
                    new BackupDir(GameData.InstallDirectory, SearchOption.TopDirectoryOnly, "*.sav", "0", 0)
                };

                if (GameData.GameType == GameType.Win32)
                    dirs.Add(new BackupDir(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VirtualStore", GameData.InstallDirectory.RemoveRoot()), SearchOption.TopDirectoryOnly, "*.sav", "0", 0));

                return dirs;
            }
        }

        #endregion

        #region Public Override Properties

        /// <summary>
        /// The game
        /// </summary>
        public override Games Game => Games.RaymanRavingRabbids;

        /// <summary>
        /// The game display name
        /// </summary>
        public override string DisplayName => "Rayman Raving Rabbids";

        /// <summary>
        /// The game backup name
        /// </summary>
        public override string BackupName => "Rayman Raving Rabbids";

        /// <summary>
        /// Gets the launch name for the game
        /// </summary>
        public override string DefaultFileName => "CheckApplication.exe";

        /// <summary>
        /// The config UI, if any is available
        /// </summary>
        public override FrameworkElement ConfigUI => new RaymanRavingRabbidsConfig();

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