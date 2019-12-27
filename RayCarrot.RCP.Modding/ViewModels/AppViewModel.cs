﻿using RayCarrot.CarrotFramework.Abstractions;
using RayCarrot.Extensions;
using RayCarrot.IO;
using RayCarrot.Rayman;
using RayCarrot.RCP.ArchiveExplorer;
using RayCarrot.RCP.Core;
using RayCarrot.UI;
using RayCarrot.Windows.Shell;
using RayCarrot.WPF;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RayCarrot.RCP.Modding
{
    // TODO: Generalize app view model (selected page with generic, is running as admin etc.)
    /// <summary>
    /// The main application view model
    /// </summary>
    public class AppViewModel : BaseViewModel
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public AppViewModel()
        {
            try
            {
                IsRunningAsAdmin = WindowsHelpers.RunningAsAdmin;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                IsRunningAsAdmin = false;
            }

            // Create commands
            OpenArchiveExplorerCommand = new AsyncRelayCommand(OpenArchiveExplorerAsync);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Indicates if the application is running as administrator
        /// </summary>
        public bool IsRunningAsAdmin { get; set; }

        /// <summary>
        /// The currently selected page
        /// </summary>
        public Pages SelectedPage { get; set; }

        // TODO: Use
        public bool IsStartupRunning { get; set; }


        /// <summary>
        /// The selected archive explorer
        /// </summary>
        public ArchiveExplorerMode SelectedArchiveExplorer { get; set; }

        /// <summary>
        /// The selected OpenSpace game mode
        /// </summary>
        public OpenSpaceGameMode SelectedOpenSpaceGameMode { get; set; }

        /// <summary>
        /// The available OpenSpace game modes
        /// </summary>
        public string[] OpenSpaceGameModes => OpenSpaceGameMode.DinosaurPC.GetValues().Select(x => x.GetDisplayName()).ToArray();

        #endregion

        #region Commands

        public ICommand OpenArchiveExplorerCommand { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Opens the selected archive explorer
        /// </summary>
        /// <returns>The task</returns>
        public async Task OpenArchiveExplorerAsync()
        {
            // Helper method for getting the game for the selected archive explorer mode
            Games? GetGame()
            {
                return SelectedArchiveExplorer switch
                {
                    ArchiveExplorerMode.OpenSpace_CNT => (SelectedOpenSpaceGameMode switch
                    {
                        OpenSpaceGameMode.Rayman2PC => Games.Rayman2,
                        OpenSpaceGameMode.RaymanMPC => Games.RaymanM,
                        OpenSpaceGameMode.RaymanArenaPC => Games.RaymanArena,
                        OpenSpaceGameMode.Rayman3PC => Games.Rayman3,
                        _ => (null as Games?)
                    }),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            // Helper method for getting the extension filter for the selected archive explorer mode
            FileFilterItemCollection GetExtensionFilter() => SelectedArchiveExplorer switch
            {
                ArchiveExplorerMode.OpenSpace_CNT => new FileFilterItemCollection()
                {
                    new FileFilterItem("*.cnt", "CNT files")
                },
                _ => throw new ArgumentOutOfRangeException()
            };

            // Allow the user to select the file
            var fileResult = await RCFUI.BrowseUI.BrowseFileAsync(new FileBrowserViewModel()
            {
                Title = "Select archive files",
                DefaultDirectory = GetGame()?.GetInstallDir(false).FullPath,
                ExtensionFilter = GetExtensionFilter().ToString(),
                MultiSelection = true
            });

            if (fileResult.CanceledByUser)
                return;

            try
            {
                await new ArchiveExplorerUI(new ArchiveExplorerDialogViewModel(new OpenSpaceCntArchiveDataManager(SelectedOpenSpaceGameMode.GetSettings()), fileResult.SelectedFiles)).ShowWindowAsync();
            }
            catch (Exception ex)
            {
                ex.HandleError("Archive explorer");

                // TODO: Exception message
                //await RCFUI.MessageUI.DisplayExceptionMessageAsync(ex, "An error occurred in the Archive Explorer and it had to close");
            }
        }

        #endregion

    }

    /// <summary>
    /// The available application pages
    /// </summary>
    public enum Pages
    {
        Utilities
    }

    /// <summary>
    /// The available archive explorer modes
    /// </summary>
    public enum ArchiveExplorerMode
    {
        /// <summary>
        /// OpenSpace .cnt archives
        /// </summary>
        OpenSpace_CNT
    }
}