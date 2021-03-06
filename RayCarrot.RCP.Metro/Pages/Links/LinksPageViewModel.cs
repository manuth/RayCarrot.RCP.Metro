﻿using Microsoft.Win32;
using Nito.AsyncEx;
using RayCarrot.WPF;
using RayCarrot.IO;
using RayCarrot.UI;
using RayCarrot.Windows.Registry;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using RayCarrot.Logging;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// View model for the links page
    /// </summary>
    public class LinksPageViewModel : BaseRCPViewModel
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public LinksPageViewModel()
        {
            // Create the collections
            LocalLinkItems = new LinkItemCollection();
            CommunityLinkItems = new LinkItemCollection();
            ForumLinkItems = new LinkItemCollection();
            ToolsLinkItems = new LinkItemCollection();

            // Enable collection synchronization
            BindingOperations.EnableCollectionSynchronization(LocalLinkItems, this);
            BindingOperations.EnableCollectionSynchronization(CommunityLinkItems, this);
            BindingOperations.EnableCollectionSynchronization(ForumLinkItems, this);
            BindingOperations.EnableCollectionSynchronization(ToolsLinkItems, this);

            // Create commands
            RefreshCommand = new AsyncRelayCommand(async () => await Task.Run(async () => await RefreshAsync()));
            
            // Create properties
            AsyncLock = new AsyncLock();

            // Refresh on startup
            Metro.App.Current.StartupComplete += async (s, e) => await RefreshAsync();

            Services.Data.CultureChanged += async (s, e) => await Task.Run(async () => await RefreshAsync());
            Services.Data.UserLevelChanged += async (s, e) => await Task.Run(async () => await RefreshAsync());
        }

        #endregion

        #region Private Properties

        /// <summary>
        /// The async lock for refreshing the links
        /// </summary>
        private AsyncLock AsyncLock { get; }

        #endregion

        #region Public Properties

        /// <summary>
        /// The local link items
        /// </summary>
        public LinkItemCollection LocalLinkItems { get; }

        /// <summary>
        /// The community link items
        /// </summary>
        public LinkItemCollection CommunityLinkItems { get; }

        /// <summary>
        /// The forum link items
        /// </summary>
        public LinkItemCollection ForumLinkItems { get; }

        /// <summary>
        /// The tools link items
        /// </summary>
        public LinkItemCollection ToolsLinkItems { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the link items
        /// </summary>
        /// <returns>The task</returns>
        public async Task RefreshAsync()
        {
            using (await AsyncLock.LockAsync())
            {
                try
                {
                    Stopwatch time = new Stopwatch();

                    time.Start();

                    RL.Logger?.LogInformationSource("The links are refreshing...");

                    LocalLinkItems.Clear();

                    // Config files
                    LocalLinkItems.Add(new LinkItemViewModel[]
                    {
                        new LinkItemViewModel(CommonPaths.UbiIniPath1, Resources.Links_Local_PrimaryUbiIni),
                        new LinkItemViewModel(CommonPaths.UbiIniPath2, Resources.Links_Local_SecondaryUbiIni,
                            UserLevel.Advanced),
                        new LinkItemViewModel(Games.Rayman2.IsAdded()
                            ? Games.Rayman2.GetInstallDir() + "ubi.ini"
                            : FileSystemPath.EmptyPath, Resources.Links_Local_R2UbiIni, UserLevel.Advanced),
                        new LinkItemViewModel(Environment.SpecialFolder.CommonApplicationData.GetFolderPath() + @"Ubisoft\RGH Launcher\1.0.0.0\Launcher_5.exe.config", Resources.Links_Local_RGHConfig, UserLevel.Advanced)
                    });

                    // DOSBox files
                    LocalLinkItems.Add(new LinkItemViewModel[]
                    {
                        new LinkItemViewModel(new FileSystemPath(Data.DosBoxPath),
                            Resources.Links_Local_DOSBox),
                        new LinkItemViewModel(new FileSystemPath(Data.DosBoxConfig),
                            Resources.Links_Local_DOSBoxConfig, UserLevel.Technical)
                    });

                    // Steam paths
                    try
                    {
                        using RegistryKey key =
                            RegistryHelpers.GetKeyFromFullPath(
                                @"HKEY_CURRENT_USER\Software\Valve\Steam", RegistryView.Default);
                        if (key != null)
                        {
                            FileSystemPath steamDir = key.GetValue("SteamPath", null)?.ToString();
                            var steamExe = key.GetValue("SteamExe", null)?.ToString();

                            if (steamDir.DirectoryExists)
                                LocalLinkItems.Add(new LinkItemViewModel[]
                                {
                                    new LinkItemViewModel(steamDir + steamExe, Resources.Links_Local_Steam),
                                    new LinkItemViewModel(steamDir + @"steamapps\common",
                                        Resources.Links_Local_SteamGames, UserLevel.Advanced)
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.HandleUnexpected("Getting Steam Registry key");
                    }

                    // GOG paths
                    try
                    {
                        using RegistryKey key = RegistryHelpers.GetKeyFromFullPath(
                            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths",
                            RegistryView.Default);
                        if (key != null)
                        {
                            FileSystemPath gogDir = key.GetValue("client", null)?.ToString();

                            if (gogDir.DirectoryExists)
                                LocalLinkItems.Add(new LinkItemViewModel[]
                                {
                                    new LinkItemViewModel(gogDir + "GalaxyClient.exe",
                                        Resources.Links_Local_GOGClient),
                                    new LinkItemViewModel(gogDir + @"Games", Resources.Links_Local_GOGGames,
                                        UserLevel.Advanced)
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.HandleUnexpected("Getting GOG Galaxy Registry key");
                    }

                    // Registry paths
                    LocalLinkItems.Add(new LinkItemViewModel[]
                    {
                        new LinkItemViewModel(CommonPaths.RaymanRavingRabbidsRegistryKey,
                            Resources.Links_Local_RRRRegSettings, UserLevel.Technical),
                        new LinkItemViewModel(CommonPaths.RaymanRavingRabbids2RegistryKey,
                            Resources.Links_Local_RRR2RegSettings, UserLevel.Technical),
                        new LinkItemViewModel(CommonPaths.RaymanOriginsRegistryKey,
                            Resources.Links_Local_RORegSettings, UserLevel.Technical),
                        new LinkItemViewModel(CommonPaths.RaymanLegendsRegistryKey,
                            Resources.Links_Local_RLRegSettings, UserLevel.Technical),
                        new LinkItemViewModel(@"HKEY_CURRENT_USER\Software\Zeus Software\nGlide",
                            Resources.Links_Local_nGlideRegSettings, UserLevel.Technical),
                        new LinkItemViewModel(@"HKEY_CURRENT_USER\Software\Zeus Software\nGlide2",
                            Resources.Links_Local_nGlide2RegSettings, UserLevel.Technical)
                    });

                    // Debug paths
                    LocalLinkItems.Add(new LinkItemViewModel[]
                    {
                        new LinkItemViewModel(CommonPaths.UserDataBaseDir, Resources.Links_Local_AppData,
                            UserLevel.Technical),
                        new LinkItemViewModel(CommonPaths.LogFile, Resources.Links_Local_LogFile,
                            UserLevel.Debug),
                        new LinkItemViewModel(CommonPaths.UtilitiesBaseDir, Resources.Links_Local_Utilities,
                            UserLevel.Debug),
                        new LinkItemViewModel(CommonPaths.RegistryBaseKey, Resources.Links_Local_RegAppData,
                            UserLevel.Technical)
                    });

                    // Community links
                    CommunityLinkItems.Clear();

                    CommunityLinkItems.AddRange(new LinkItemViewModel[][]
                    {
                        new LinkItemViewModel[]
                        {
                            new LinkItemViewModel(new Uri("https://raymanpc.com/"),
                                Resources.Links_Community_RPC),
                            new LinkItemViewModel(new Uri("https://raymanpc.com/wiki/en/Main_Page"),
                                Resources.Links_Community_RayWiki),
                            new LinkItemViewModel(new Uri("https://raytunes.raymanpc.com/"),
                                Resources.Links_Community_RayTunes),
                            new LinkItemViewModel(new Uri("https://raysaves.raymanpc.com/"),
                                Resources.Links_Community_RaySaves)
                        },
                        new LinkItemViewModel[]
                        {
                            new LinkItemViewModel(new Uri("https://twitter.com/RaymanCentral"),
                                Resources.Links_Community_RaymanCentral),
                            new LinkItemViewModel(new Uri("https://twitter.com/RaymanTogether"),
                                Resources.Links_Community_RaymanTogether),
                            new LinkItemViewModel(new Uri("https://twitter.com/RaymanReanimate"),
                                Resources.Links_Community_RaymanReanimated)
                        },
                        new LinkItemViewModel[]
                        {
                            new LinkItemViewModel(new Uri("https://raym.app/"),
                                Resources.Links_Community_raym_app),
                            new LinkItemViewModel(new Uri("https://raym.app/maps/"),
                                Resources.Links_Community_Raymap),
                            new LinkItemViewModel(new Uri("https://raym.app/menezis/"),
                                Resources.Links_Community_Menezis_Browser)
                        },
                        new LinkItemViewModel[]
                        {
                            new LinkItemViewModel(new Uri("http://www.kmgassociates.com/rayman/index.html"),
                                Resources.Links_Community_KMG),
                            new LinkItemViewModel(new Uri("http://www.rayman-fanpage.de/"),
                                Resources.Links_Community_Fanpage)
                        }
                    });

                    // Forum links
                    ForumLinkItems.Clear();

                    ForumLinkItems.AddRange(new LinkItemViewModel[][]
                    {
                        new LinkItemViewModel[]
                        {
                            new LinkItemViewModel(new Uri("https://raymanpc.com/forum/index.php"),
                                Resources.Links_Forums_RPC)
                        },
                        new LinkItemViewModel[]
                        {
                            new LinkItemViewModel(new Uri("https://forums.ubi.com/forumdisplay.php/47-Rayman"),
                                Resources.Links_Forums_Ubisoft),
                            new LinkItemViewModel(new Uri("https://www.gog.com/forum/rayman_series"),
                                Resources.Links_Forums_GOG)
                        },
                        new LinkItemViewModel[]
                        {
                            new LinkItemViewModel(new Uri("https://steamcommunity.com/app/15060/discussions/"),
                                Resources.Links_Forums_Steam_R2),
                            new LinkItemViewModel(new Uri("https://steamcommunity.com/app/15080/discussions/"),
                                Resources.Links_Forums_Steam_RRR),
                            new LinkItemViewModel(new Uri("https://steamcommunity.com/app/207490/discussions/"),
                                Resources.Links_Forums_Steam_RO),
                            new LinkItemViewModel(new Uri("https://steamcommunity.com/app/242550/discussions/"),
                                Resources.Links_Forums_Steam_RL)
                        }
                    });

                    // Tools links
                    ToolsLinkItems.Clear();

                    ToolsLinkItems.AddRange(new LinkItemViewModel[][]
                    {
                        new LinkItemViewModel[]
                        {
                            new LinkItemViewModel(new Uri("https://raymanpc.com/forum/viewtopic.php?t=5755"),
                                Resources.Links_Tools_RDEditor),
                            new LinkItemViewModel(new Uri("https://raymanpc.com/forum/viewtopic.php?t=25867"),
                                Resources.Links_Tools_RayPlus)
                        },
                        new LinkItemViewModel[]
                        {
                            new LinkItemViewModel(new Uri("https://raymanpc.com/forum/viewtopic.php?t=25013"),
                                Resources.Links_Tools_RayTwol),
                            new LinkItemViewModel(new Uri("https://raymanpc.com/forum/viewtopic.php?t=23423"),
                                Resources.Links_Tools_R2Tools),
                            new LinkItemViewModel(new Uri("https://raymanpc.com/forum/viewtopic.php?t=23420"),
                                Resources.Links_Tools_R2Moonjump),
                            new LinkItemViewModel(
                                new Uri("https://github.com/rtsonneveld/Rayman2FunBox/releases"),
                                Resources.Links_Tools_R2FunBox)
                        },
                        new LinkItemViewModel[]
                        {
                            new LinkItemViewModel(new Uri("https://raymanpc.com/forum/viewtopic.php?t=12854"),
                                Resources.Links_Tools_BetterR3),
                            new LinkItemViewModel(new Uri("https://raymanpc.com/forum/viewtopic.php?t=25053"),
                                Resources.Links_Tools_R3GCTexturePack)
                        }
                    });

                    time.Stop();

                    RL.Logger?.LogInformationSource("The links have refreshed");
                    RL.Logger?.LogDebugSource($"The link refresh time was {time.ElapsedMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    ex.HandleError("Refreshing links");
                }
            }
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }

        #endregion

        #region Classes

        /// <summary>
        /// A collection of link item groups
        /// </summary>
        public class LinkItemCollection : ObservableCollection<LinkItemViewModel[]>
        {
            /// <summary>
            /// Adds a new group to the collection
            /// </summary>
            /// <param name="group">The group to add</param>
            public new void Add(LinkItemViewModel[] group)
            {
                // Get the valid items
                var validItems = group.Where(x => x.IsValid && x.MinUserLevel <= RCPServices.Data.UserLevel).ToArray();

                // If there are valid items, add them
                if (validItems.Any())
                    base.Add(validItems);
            }
        }

        #endregion
    }
}