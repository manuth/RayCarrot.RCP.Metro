﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using RayCarrot.WPF;
using System.Windows.Input;
using RayCarrot.Extensions;
using RayCarrot.RCP.Core;
using RayCarrot.UI;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// View model for the about page
    /// </summary>
    public class AboutPageViewModel : BaseRCPViewModel
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public AboutPageViewModel()
        {
            // Create commands
            OpenUrlCommand = new RelayCommand(x => App.OpenUrl(x?.ToString()));
            ShowVersionHistoryCommand = new RelayCommand(ShowVersionHistory);
            CheckForUpdatesCommand = new AsyncRelayCommand(async () => await App.CheckForUpdatesAsync(true));
            UninstallCommand = new AsyncRelayCommand(UninstallAsync);

            // Refresh the update badge property based on if new update is available
            Data.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Data.IsUpdateAvailable))
                    OnPropertyChanged(nameof(UpdateBadge));
            };
        }

        #endregion

        #region Commands

        public ICommand OpenUrlCommand { get; }

        public ICommand ShowVersionHistoryCommand { get; }

        public ICommand CheckForUpdatesCommand { get; }

        public ICommand UninstallCommand { get; }

        #endregion

        #region Public Properties

        /// <summary>
        /// The update badge, indicating if new updates are available
        /// </summary>
        public string UpdateBadge => Data.IsUpdateAvailable ? "1" : null;

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the application version history
        /// </summary>
        public void ShowVersionHistory()
        {
            WindowHelpers.ShowWindow<AppNewsDialog>();
        }

        /// <summary>
        /// Runs the uninstaller
        /// </summary>
        /// <returns>The task</returns>
        public async Task UninstallAsync()
        {
            // Confirm
            if (!await RCFUI.MessageUI.DisplayMessageAsync(Resources.About_ConfirmUninstall, Resources.About_ConfirmUninstallHeader, MessageType.Question, true))
                return;

            // Run the uninstaller
            if (await RCFRCPA.File.LaunchFileAsync(RCFRCP.Path.UninstallFilePath, true, $"\"{Assembly.GetExecutingAssembly().Location}\"") == null)
            {
                string[] appDataLocations = 
                {
                    RCFRCP.Path.AppUserDataBaseDir,
                    RCPMetroApplicationPaths.RegistryBaseKey
                };

                await RCFUI.MessageUI.DisplayMessageAsync(String.Format(Resources.About_UninstallFailed, appDataLocations.JoinItems(Environment.NewLine)), MessageType.Error);

                return;
            }

            // Shut down the app
            await Metro.App.Current.ShutdownRCFAppAsync(true);
        }

        #endregion
    }
}