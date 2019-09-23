﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Nito.AsyncEx;
using RayCarrot.CarrotFramework.Abstractions;
using RayCarrot.Extensions;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// View model for the games page
    /// </summary>
    public class GamesPageViewModel : BaseRCPViewModel
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public GamesPageViewModel()
        {
            // Create properties
            AsyncLock = new AsyncLock();
            InstalledGames = new ObservableCollection<GameDisplayViewModel>();
            NotInstalledGames = new ObservableCollection<GameDisplayViewModel>();

            // Enable collection synchronization
            BindingOperations.EnableCollectionSynchronization(InstalledGames, Application.Current);
            BindingOperations.EnableCollectionSynchronization(NotInstalledGames, Application.Current);

            // Refresh on app refresh
            App.RefreshRequired += async (s, e) =>
            {
                if (e.LaunchInfoModified && e.ModifiedGames?.Any() == true)
                    foreach (Games game in e.ModifiedGames)
                        await RefreshGameAsync(game);

                else if (e.LaunchInfoModified || e.GameCollectionModified)
                    await RefreshAsync();
            };

            // Refresh on culture changed
            RCFCore.Data.CultureChanged += async (s, e) => await RefreshAsync();

            // Refresh on startup
            Metro.App.Current.StartupComplete += async (s, e) => await RefreshAsync();
        }

        #endregion

        #region Private Properties

        /// <summary>
        /// The async lock to use for the game page
        /// </summary>
        private AsyncLock AsyncLock { get; }

        #endregion

        #region Public Properties

        /// <summary>
        /// The installed games
        /// </summary>
        public ObservableCollection<GameDisplayViewModel> InstalledGames { get; }

        /// <summary>
        /// The not installed games
        /// </summary>
        public ObservableCollection<GameDisplayViewModel> NotInstalledGames { get; }

        /// <summary>
        /// Indicates if there are any installed games
        /// </summary>
        public bool AnyInstalledGames { get; set; }

        /// <summary>
        /// Indicates if there are any not installed games
        /// </summary>
        public bool AnyNotInstalledGames { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the added game
        /// </summary>
        /// <returns>The task</returns>
        public async Task RefreshGameAsync(Games game)
        {
            RCFCore.Logger?.LogInformationSource($"The displayed game {game} is being refreshed...");

            using (await AsyncLock.LockAsync())
            {
                await Task.Run(() =>
                {
                    try
                    {
                        // Make sure the game has been added
                        if (!game.IsAdded())
                            throw new Exception("Only added games can be refreshed individually");

                        // Get the collection containing the game
                        var collection = InstalledGames.Any(x => x.Game == game) ? InstalledGames : NotInstalledGames;

                        // Refresh the game
                        collection[collection.FindItemIndex(x => x.Game == game)] = game.GetDisplayViewModel();
                    }
                    catch (Exception ex)
                    {
                        ex.HandleCritical("Refreshing game", game);
                        throw;
                    }
                });
            }

            RCFCore.Logger?.LogInformationSource($"The displayed game {game} has been refreshed");
        }

        /// <summary>
        /// Refreshes the games
        /// </summary>
        /// <returns>The task</returns>
        public async Task RefreshAsync()
        {
            RCFCore.Logger?.LogInformationSource($"The displayed games are being refreshed...");

            using (await AsyncLock.LockAsync())
            {
                await Task.Run(() =>
                {
                    try
                    {
                        InstalledGames.Clear();
                        NotInstalledGames.Clear();

                        AnyInstalledGames = false;
                        AnyNotInstalledGames = false;

                        // Enumerate each game
                        foreach (Games game in RCFRCP.App.GetGames)
                        {
                            // Check if it has been added
                            if (game.IsAdded())
                            {
                                // Add the game to the collection
                                InstalledGames.Add(game.GetDisplayViewModel());
                                AnyInstalledGames = true;
                            }
                            else
                            {
                                NotInstalledGames.Add(game.GetDisplayViewModel());
                                AnyNotInstalledGames = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.HandleCritical("Refreshing games");
                        throw;
                    }
                });
            }

            RCFCore.Logger?.LogInformationSource($"The displayed games have been refreshed");
        }

        #endregion
    }

    // TODO: Allow multiple display modes. Have separate collections of games in VM for grouped view. Grouped view has three groups: "Rayman Games", "Rabbids Games" and "Spin-Offs".

    //public enum GameDisplayMode
    //{
    //    Default,
    //    Grouped,
    //    List
    //}

    //public enum GameGroup
    //{
    //    Main,
    //    SpinOffs,
    //    Rabbids
    //}

    //public static class GamesExtensions2
    //{
    //    public static GameGroup GetGroupName(this Games game)
    //    {
    //        switch (game)
    //        {
    //            case Games.Rayman1:
    //            case Games.Rayman2:
    //            case Games.Rayman3:
    //            case Games.RaymanOrigins:
    //            case Games.RaymanLegends:
    //                return GameGroup.Main;

    //            case Games.RaymanDesigner:
    //            case Games.RaymanByHisFans:
    //            case Games.Rayman60Levels:
    //            case Games.EducationalDos:
    //            case Games.RaymanM:
    //            case Games.RaymanArena:
    //            case Games.RaymanJungleRun:
    //            case Games.RaymanFiestaRun:
    //                return GameGroup.SpinOffs;

    //            case Games.RaymanRavingRabbids:
    //            case Games.RaymanRavingRabbids2:
    //            case Games.RabbidsGoHome:
    //            case Games.RabbidsBigBang:
    //                return GameGroup.Rabbids;

    //            default:
    //                throw new ArgumentOutOfRangeException(nameof(game), game, null);
    //        }
    //    }
    //}
}