﻿using System;
using RayCarrot.IO;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// A finder item base
    /// </summary>
    public abstract class BaseFinderItem
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="possibleWin32Names">The possible names of the game to search for. This is not case sensitive, but most match entire string.</param>
        /// <param name="shortcutName">The shortcut name when searching shortcuts</param>
        /// <param name="verifyInstallDirectory">Optional method for verifying the found install directory</param>
        /// <param name="foundAction">An optional action to add when the item gets found</param>
        /// <param name="customFinderAction">Custom game finder action which returns the game install directory if found</param>
        protected BaseFinderItem(string[] possibleWin32Names, string shortcutName, Func<FileSystemPath, FileSystemPath?> verifyInstallDirectory, Action<FileSystemPath, object> foundAction, Func<FoundActionResult> customFinderAction)
        {
            PossibleWin32Names = possibleWin32Names;
            ShortcutName = shortcutName;
            VerifyInstallDirectory = verifyInstallDirectory;
            FoundAction = foundAction;
            CustomFinderAction = customFinderAction;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The possible names of the game to search for. This is not case sensitive, but most match entire string.
        /// </summary>
        public string[] PossibleWin32Names { get; }

        /// <summary>
        /// The shortcut name when searching shortcuts
        /// </summary>
        public string ShortcutName { get; }

        /// <summary>
        /// Optional method for verifying the found install directory
        /// </summary>
        public Func<FileSystemPath, FileSystemPath?> VerifyInstallDirectory { get; }

        /// <summary>
        /// An optional action to add when the item gets found
        /// </summary>
        public Action<FileSystemPath, object> FoundAction { get; }

        /// <summary>
        /// Custom game finder action which returns the game install directory if found
        /// </summary>
        public Func<FoundActionResult> CustomFinderAction { get; }

        #endregion
    }
}