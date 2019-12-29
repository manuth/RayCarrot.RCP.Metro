﻿using RayCarrot.Extensions;
using RayCarrot.IO;
using RayCarrot.RCP.Core;
using RayCarrot.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RayCarrot.RCP.ArchiveExplorer
{
    /// <summary>
    /// View model for an archive explorer dialog
    /// </summary>
    public class ArchiveExplorerDialogViewModel : UserInputViewModel, IDisposable
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="manager">The archive data manager</param>
        /// <param name="filePaths">The archive file paths</param>
        public ArchiveExplorerDialogViewModel(IArchiveDataManager manager, IEnumerable<FileSystemPath> filePaths)
        {
            // Set the default title
            Title = Resources.Archive_Title;

            // Get the manager
            Manager = manager;

            // Create the load action
            var load = new Operation(() => IsLoading = true, () => IsLoading = false, true);

            // Get the archives
            Archives = filePaths.Select(x => new ArchiveViewModel(x, manager, load)).ToArray();

            // Make sure we got an archive
            if (!Archives.Any())
                throw new ArgumentException("At least one archive path needs to be available");

            // Read each archive
            foreach (var archive in Archives)
                archive.LoadArchive();

            // Select and expand the first item
            Archives.First().IsSelected = true;
            Archives.First().IsExpanded = true;
        }

        /// <summary>
        /// Indicates if a process is running, such as importing/exporting
        /// </summary>
        public bool IsLoading { get; set; }

        /// <summary>
        /// The directories
        /// </summary>
        public ArchiveViewModel[] Archives { get; }

        /// <summary>
        /// The archive data manager
        /// </summary>
        public IArchiveDataManager Manager { get; }

        public void Dispose()
        {
            // Dispose every archive
            Archives?.ForEach(x => x.Dispose());
        }
    }
}