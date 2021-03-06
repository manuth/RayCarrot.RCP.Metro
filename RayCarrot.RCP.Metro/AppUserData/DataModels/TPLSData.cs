﻿using System;
using System.IO;
using System.Threading.Tasks;
using RayCarrot.IO;
using RayCarrot.Logging;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// TPLS data for the Rayman 1 utility
    /// </summary>
    public class TPLSData
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="installDir">The directory it is installed under</param>
        public TPLSData(FileSystemPath installDir)
        {
            InstallDir = installDir;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The directory it is installed under
        /// </summary>
        public FileSystemPath InstallDir { get; }

        /// <summary>
        /// The selected Rayman version to search for
        /// </summary>
        public TPLSRaymanVersion RaymanVersion { get; set; }

        /// <summary>
        /// Indicates if the utility is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// The DOSBox TPLS config file path
        /// </summary>
        public FileSystemPath ConfigFilePath => InstallDir + "TPLS.conf";

        /// <summary>
        /// The DOSBox exe file path
        /// </summary>
        public FileSystemPath DOSBoxFilePath => InstallDir + "dosbox.exe";

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the config file
        /// </summary>
        /// <returns>The task</returns>
        public async Task UpdateConfigAsync()
        {
            static string GetVersionName(TPLSRaymanVersion version) => version switch
            {
                TPLSRaymanVersion.Auto => "auto",
                TPLSRaymanVersion.Ray_1_12_0 => "1.12.0",
                TPLSRaymanVersion.Ray_1_20 => "1.20",
                TPLSRaymanVersion.Ray_1_21 => "1.21",
                TPLSRaymanVersion.Ray_1_00 => "1.00",
                TPLSRaymanVersion.Ray_1_12_1 => "1.12.1",
                TPLSRaymanVersion.Ray_1_12_2 => "1.12.2",
                TPLSRaymanVersion.Ray_1_10 => "1.10",
                TPLSRaymanVersion.Ray_1_21_Chinese => "1.21_Chinese",
                _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
            };

            try
            {
                lock (this)
                {
                    File.WriteAllLines(ConfigFilePath, new[]
                    {
                        "[rayman]",
                        $"gameversion={GetVersionName(RaymanVersion)}",
                        "musicfile=Music.dat"
                    });
                }
            }
            catch (Exception ex)
            {
                ex.HandleError("Updating TPLS config");

                await WPF.Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.TPLS_UpdateVersionError);
            }
        }

        #endregion
    }
}