﻿using System.Collections.Generic;
using System.Windows;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// The Rayman 2 translation utility
    /// </summary>
    public class R2TranslationUtility : IRCPUtility
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public R2TranslationUtility()
        {
            ViewModel = new R2TranslationUtilityViewModel();
        }

        #endregion

        #region Interface Members

        /// <summary>
        /// The utility ID
        /// </summary>
        public string ID => "82622dc8-8212-40b3-b6d5-ba84678d017b";

        /// <summary>
        /// The header for the utility. This property is retrieved again when the current culture is changed.
        /// </summary>
        public string DisplayHeader => Resources.R2U_TranslationsHeader;

        /// <summary>
        /// The utility information text (optional). This property is retrieved again when the current culture is changed.
        /// </summary>
        public string InfoText => Resources.R2U_TranslationsInfo;

        /// <summary>
        /// The utility warning text (optional). This property is retrieved again when the current culture is changed.
        /// </summary>
        public string WarningText => null;

        /// <summary>
        /// Indicates if the utility requires additional files to be downloaded remotely
        /// </summary>
        public bool RequiresAdditionalFiles => true;

        /// <summary>
        /// The utility UI content
        /// </summary>
        public UIElement UIContent => new R2TranslationUtilityUI()
        {
            DataContext = ViewModel
        };

        /// <summary>
        /// Indicates if the utility requires administration privileges
        /// </summary>
        public bool RequiresAdmin => !RCFRCP.File.CheckFileWriteAccess(ViewModel.GetFixSnaFilePath());

        /// <summary>
        /// Indicates if the utility is available to the user
        /// </summary>
        public bool IsAvailable => ViewModel.GameInfo.InstallDirectory.DirectoryExists && ViewModel.GetFixSnaFilePath().FileExists && ViewModel.GetTexturesCntFilePath().FileExists;

        /// <summary>
        /// The developers of the utility
        /// </summary>
        public string Developers => "RayCarrot";

        /// <summary>
        /// Any additional developers to credit for the utility
        /// </summary>
        public string AdditionalDevelopers => "PluMGMK, Haruka Tavares, MixerX";

        /// <summary>
        /// The game which the utility was made for
        /// </summary>
        public Games Game => Games.Rayman2;

        /// <summary>
        /// Retrieves a list of applied utilities from this utility
        /// </summary>
        /// <returns>The applied utilities</returns>
        public IEnumerable<string> GetAppliedUtilities()
        {
            var translation = ViewModel.GetAppliedRayman2Translation();

            if (translation != R2TranslationUtilityViewModel.Rayman2Translation.Original && translation != null)
                yield return Resources.R2U_TranslationsHeader;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The view model
        /// </summary>
        public R2TranslationUtilityViewModel ViewModel { get; }

        #endregion
    }
}