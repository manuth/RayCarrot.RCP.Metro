﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RayCarrot.IO;
using RayCarrot.Logging;
using RayCarrot.WPF;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// Base for a Win32 Rayman Control Panel game
    /// </summary>
    public abstract class RCPWin32Game : RCPGameManager
    {
        #region Public Override Properties

        /// <summary>
        /// The game type
        /// </summary>
        public override GameType Type => GameType.Win32;

        /// <summary>
        /// The display name for the game type
        /// </summary>
        public override string GameTypeDisplayName => Resources.GameType_Desktop;

        /// <summary>
        /// Indicates if using <see cref="GameLaunchMode"/> is supported
        /// </summary>
        public override bool SupportsGameLaunchMode => true;

        /// <summary>
        /// Gets the info items for the game
        /// </summary>
        public override IList<DuoGridItemViewModel> GetGameInfoItems
        {
            get
            {
                // Get the launch info
                var launchInfo = GetLaunchInfo();

                // Return the items
                return new List<DuoGridItemViewModel>(base.GetGameInfoItems)
                {
                    new DuoGridItemViewModel(Resources.GameInfo_LaunchPath, launchInfo.Path, UserLevel.Technical),
                    new DuoGridItemViewModel(Resources.GameInfo_LaunchArgs, launchInfo.Args, UserLevel.Technical)
                };
            }
        }

        #endregion

        #region Protected Virtual Properties

        /// <summary>
        /// Gets the icon resource path for the game based on its launch information
        /// </summary>
        /// <returns>The icon resource path</returns>
        protected virtual FileSystemPath IconResourcePath => GetLaunchInfo().Path;

        #endregion

        #region Public Virtual Properties

        /// <summary>
        /// Gets the launch arguments for the game
        /// </summary>
        public virtual string GetLaunchArgs => null;

        #endregion

        #region Protected Override Methods

        /// <summary>
        /// The implementation for launching the game
        /// </summary>
        /// <param name="forceRunAsAdmin">Indicated if the game should be forced to run as admin</param>
        /// <returns>The launch result</returns>
        protected override async Task<GameLaunchResult> LaunchAsync(bool forceRunAsAdmin)
        {
            // Get the launch info
            GameLaunchInfo launchInfo = GetLaunchInfo();

            RL.Logger?.LogTraceSource($"The game {Game} launch info has been retrieved as Path = {launchInfo.Path}, Args = {launchInfo.Args}");

            // Launch the game
            var process = await RCPServices.File.LaunchFileAsync(launchInfo.Path, forceRunAsAdmin || Game.GetLaunchMode() == GameLaunchMode.AsAdmin, launchInfo.Args);

            RL.Logger?.LogInformationSource($"The game {Game} has been launched");

            return new GameLaunchResult(process, process != null);
        }

        #endregion

        #region Public Override Methods

        /// <summary>
        /// Locates the game
        /// </summary>
        /// <returns>Null if the game was not found. Otherwise a valid or empty path for the install directory</returns>
        public override async Task<FileSystemPath?> LocateAsync()
        {
            // Have user browse for directory
            var result = await Services.BrowseUI.BrowseDirectoryAsync(new DirectoryBrowserViewModel()
            {
                Title = Resources.LocateGame_BrowserHeader,
                DefaultDirectory = Environment.SpecialFolder.ProgramFilesX86.GetFolderPath(),
                MultiSelection = false
            });

            // Make sure the user did not cancel
            if (result.CanceledByUser)
                return null;

            // Make sure the selected directory exists
            if (!result.SelectedDirectory.DirectoryExists)
                return null;

            // Make sure the directory is valid
            if (!await IsValidAsync(result.SelectedDirectory))
            {
                RL.Logger?.LogInformationSource($"The selected install directory for {Game} is not valid");

                await Services.MessageUI.DisplayMessageAsync(Resources.LocateGame_InvalidLocation,
                    Resources.LocateGame_InvalidLocationHeader, MessageType.Error);

                return null;
            }

            // Return the valid directory
            return result.SelectedDirectory;
        }

        /// <summary>
        /// Gets the available jump list items for this game
        /// </summary>
        /// <returns>The items</returns>
        public override IList<JumpListItemViewModel> GetJumpListItems()
        {
            var launchInfo = GetLaunchInfo();

            if (launchInfo.Path.FileExists)
            {
                return new JumpListItemViewModel[]
                {
                    new JumpListItemViewModel(Game.GetGameInfo().DisplayName, IconResourcePath, launchInfo.Path, launchInfo.Path.Parent, launchInfo.Args, Game.ToString())
                };
            }
            else
            {
                return new JumpListItemViewModel[0];
            }
        }

        /// <summary>
        /// Creates a shortcut to launch the game from
        /// </summary>
        /// <param name="shortcutName">The name of the shortcut</param>
        /// <param name="destinationDirectory">The destination directory for the shortcut</param>
        public override void CreateGameShortcut(FileSystemPath shortcutName, FileSystemPath destinationDirectory)
        {
            // Get the launch info
            var launchInfo = GetLaunchInfo();

            // Create the shortcut
            RCPServices.File.CreateFileShortcut(shortcutName, destinationDirectory, launchInfo.Path, launchInfo.Args);

            RL.Logger?.LogTraceSource($"A shortcut was created for {Game} under {destinationDirectory}");
        }

        #endregion

        #region Public Virtual Methods

        /// <summary>
        /// Gets the launch info for the game
        /// </summary>
        /// <returns>The launch info</returns>
        public virtual GameLaunchInfo GetLaunchInfo()
        {
            return new GameLaunchInfo(Game.GetInstallDir() + Game.GetGameInfo().DefaultFileName, GetLaunchArgs);
        }

        #endregion
    }
}