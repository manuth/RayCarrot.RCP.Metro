﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using RayCarrot.Common;
using RayCarrot.IO;
using RayCarrot.Logging;
using RayCarrot.UI;
using RayCarrot.WPF;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// View model for the downloader
    /// </summary>
    public class DownloaderViewModel : BaseViewModel
    {
        #region Constructors

        /// <summary>
        /// Downloads a list of files to a specified output directory
        /// </summary>
        /// <param name="inputSources">The files to download</param>
        /// <param name="outputDirectory">The output directory to download to</param>
        /// <param name="isCompressed">Indicates if the download is compressed</param>
        public DownloaderViewModel(IEnumerable<Uri> inputSources, FileSystemPath outputDirectory, bool isCompressed)
        {
            // Get properties
            InputSources = new List<Uri>(inputSources);
            OutputDirectory = outputDirectory;
            IsCompressed = isCompressed;
            ProcessedPaths = new List<FileSystemPath>();
            FileManager = RCPServices.File;

            // Set properties
            DownloadState = DownloadStates.Paused;
            TotalMaxProgress = InputSources.Count * 100;

            if (IsCompressed)
                TotalMaxProgress += 10 * InputSources.Count;

            RL.Logger?.LogInformationSource($"A download operation has started with the {(isCompressed ? "compressed" : "")} files {InputSources.JoinItems(", ")}");
        }

        #endregion

        #region Event Handlers

        private void WCServer_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                ItemMaxProgress = e.TotalBytesToReceive;
                ItemCurrentProgress = e.BytesReceived;

                TotalCurrentProgress = CurrentStep * 100 + e.ProgressPercentage;

                OnStatusUpdated(new Progress(TotalCurrentProgress, TotalMaxProgress));
            }
            catch (Exception ex)
            {
                ex.HandleError("Updating server download progress", e);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Restores a canceled or crashed download
        /// </summary>
        protected async Task RestoreStoppedDownloadAsync()
        {
            // Restore and clean up failed download
            try
            {
                // Delete the downloaded processed files
                foreach (FileSystemPath path in ProcessedPaths)
                {
                    try
                    {
                        if (path.FileExists)
                            FileManager.DeleteFile(path);
                        else if (path.DirectoryExists)
                            FileManager.DeleteDirectory(path);
                    }
                    catch (Exception ex)
                    {
                        ex.HandleError("Deleting processed file from incomplete download operation");
                    }
                }

                // Restore backed up files
                foreach (FileSystemPath item in Directory.EnumerateFiles(LocalTempDir.TempPath, "*", SearchOption.AllDirectories))
                {
                    // Get the path to move to
                    FileSystemPath file = OutputDirectory + (item - LocalTempDir.TempPath);

                    try
                    {
                        // Move back the file
                        FileManager.MoveFile(item, file, true);
                    }
                    catch (Exception ex)
                    {
                        ex.HandleError("Restoring backed up file from incomplete download operation");
                    }
                }
            }
            catch (Exception ex)
            {
                ex.HandleError("Cleaning up incomplete download operation");

                // Let the user know the restore failed
                await Services.MessageUI.DisplayExceptionMessageAsync(ex, String.Format(Resources.Download_RestoreStoppedDownloadError, LocalTempDir.TempPath, ServerTempDir.TempPath));
            }
        }

        /// <summary>
        /// Processes the downloaded compressed file
        /// </summary>
        /// <returns>The task</returns>
        protected async Task ProcessCompressedDownloadAsync()
        {
            await Task.Run(async () =>
            {
                // Handle each input source
                foreach (var inputSource in InputSources)
                {
                    // Reset file progress
                    ItemCurrentProgress = 0;

                    // Get the absolute output path
                    FileSystemPath file = ServerTempDir.TempPath + Path.GetFileName(inputSource.AbsolutePath);

                    // Open the zip file
                    using (var zip = ZipFile.OpenRead(file))
                    {
                        // Set file progress to its entry count
                        ItemMaxProgress = zip.Entries.Count;

                        // Extract each entry
                        foreach (var entry in zip.Entries)
                        {
                            ThrowIfCancellationRequested();

                            // Get the full entry name
                            var entryName = entry.FullName.Replace('/', '\\');

                            // Get the absolute output path
                            var outputPath = OutputDirectory + entryName;

                            // Check if the entry is a directory
                            if (entryName.EndsWith("\\") && entry.Name == String.Empty)
                            {
                                // Create directory if it doesn't exist
                                if (!outputPath.DirectoryExists)
                                {
                                    Directory.CreateDirectory(outputPath);

                                    ProcessedPaths.Add(outputPath);
                                }

                                continue;
                            }

                            // Backup conflict file
                            if (outputPath.FileExists)
                                await Task.Run(() => FileManager.MoveFile(outputPath, LocalTempDir.TempPath + entryName, false));

                            // Create directory if it doesn't exist
                            if (!outputPath.Parent.DirectoryExists)
                                Directory.CreateDirectory(outputPath.Parent);

                            // Extract the compressed file
                            entry.ExtractToFile(outputPath);

                            // Flag the file as processed
                            ProcessedPaths.Add(outputPath);

                            // Increase file progress
                            ItemCurrentProgress++;

                            // Set total progress
                            TotalCurrentProgress = (int)Math.Floor(100 * InputSources.Count + ((ItemCurrentProgress / ItemMaxProgress) * 10));
                            OnStatusUpdated(new Progress(TotalCurrentProgress, TotalMaxProgress));
                        }
                    }

                    // Delete the zip file
                    FileManager.DeleteFile(file);
                }
            });
        }

        /// <summary>
        /// Processes the downloaded files
        /// </summary>
        /// <returns>The task</returns>
        protected async Task ProcessDownloadedFilesAsync()
        {
            // Back up existing file
            foreach (var item in InputSources)
            {
                ThrowIfCancellationRequested();

                // Get the absolute output path
                FileSystemPath file = OutputDirectory + Path.GetFileName(item.AbsolutePath);

                // Check if it's a file conflict
                if (!file.FileExists)
                    continue;

                // Move the file to temp
                await Task.Run(() => FileManager.MoveFile(file, LocalTempDir.TempPath + file.Name, false));
            }

            // Move downloaded files to output
            foreach (var item in InputSources)
            {
                ThrowIfCancellationRequested();

                // Get the absolute path
                var file = ServerTempDir.TempPath + Path.GetFileName(item.AbsolutePath);

                var outputPath = OutputDirectory + Path.GetFileName(item.AbsolutePath);

                // Move the downloaded file from temp to the output
                FileManager.MoveFile(file, outputPath, false);

                // Flag the file as processed
                ProcessedPaths.Add(outputPath);
            }
        }

        /// <summary>
        /// Downloads the files specified in <see cref="InputSources"/> to temp
        /// </summary>
        /// <returns>The task</returns>
        protected async Task DownloadFromServerToTempAsync()
        {
            ItemMaxProgress = 100;

            // Create the server web client
            using (WCServer = new WebClient())
            {
                WCServer.DownloadProgressChanged += WCServer_DownloadProgressChanged;

                // Download files
                foreach (var item in InputSources)
                {
                    RL.Logger?.LogInformationSource($"The file {item} is being downloaded");

                    await WCServer.DownloadFileTaskAsync(item, ServerTempDir.TempPath + Path.GetFileName(item.AbsolutePath));
                    ItemCurrentProgress = 100;
                    CurrentStep++;
                }

                WCServer.DownloadProgressChanged -= WCServer_DownloadProgressChanged;
            }
        }

        /// <summary>
        /// Throws a <see cref="OperationCanceledException"/> if cancellation has been requested
        /// </summary>
        protected void ThrowIfCancellationRequested()
        {
            if (CancellationRequested)
                throw new OperationCanceledException();
        }

        /// <summary>
        /// Invokes the <see cref="StatusUpdated"/> event
        /// </summary>
        /// <param name="e">The event args</param>
        protected void OnStatusUpdated(Progress e)
        {
            StatusUpdated?.Invoke(this, new ValueEventArgs<Progress>(e));
        }

        /// <summary>
        /// Invokes the <see cref="DownloadComplete"/> event
        /// </summary>
        protected void OnDownloadComplete()
        {
            DownloadComplete?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to cancel the installation
        /// </summary>
        public async Task AttemptCancelAsync()
        {
            if (CancellationRequested)
            {
                await Services.MessageUI.DisplayMessageAsync(Resources.Download_OperationCanceling, Resources.Download_OperationCancelingHeader, MessageType.Information);
                return;
            }

            if (await Services.MessageUI.DisplayMessageAsync(Resources.Download_Cancel, Resources.Download_CancelHeader, MessageType.Question, true))
            {
                RL.Logger?.LogInformationSource($"The downloader has been requested to cancel");
                WCServer?.CancelAsync();
                CancellationRequested = true;
            }
        }

        /// <summary>
        /// Start the download
        /// </summary>
        /// <returns>The task</returns>
        public async Task StartAsync()
        {
            if (OperationRunning)
                throw new InvalidOperationException("The downloader can not start while running");

            DownloadState = DownloadStates.Running;

            // Flag the operation as running
            OperationRunning = true;

            // Reset the progress counter
            CurrentStep = 0;

            using (LocalTempDir = new TempDirectory(true))
            {
                using (ServerTempDir = new TempDirectory(true))
                {
                    try
                    {
                        // Download files to temp
                        await DownloadFromServerToTempAsync();

                        // Create directory if it doesn't exist
                        if (!OutputDirectory.DirectoryExists)
                        {
                            Directory.CreateDirectory(OutputDirectory);
                            ProcessedPaths.Add(OutputDirectory);
                        }

                        // Process the download
                        if (IsCompressed)
                            await ProcessCompressedDownloadAsync();
                        else
                            await ProcessDownloadedFilesAsync();
                    }
                    catch (Exception ex)
                    {
                        if (CancellationRequested)
                        {
                            ex.HandleExpected("Downloading files");
                            await Services.MessageUI.DisplayMessageAsync(Resources.Download_Canceled, Resources.Download_CanceledHeader, MessageType.Information);
                        }
                        else
                        {
                            ex.HandleError("Downloading files");
                            await Services.MessageUI.DisplayMessageAsync(Resources.Download_Failed, Resources.Download_FailedHeader, MessageType.Error);
                        }

                        // Restore the stopped download
                        await RestoreStoppedDownloadAsync();

                        // Flag that the operation is no longer running
                        OperationRunning = false;
                        DownloadState = CancellationRequested ? DownloadStates.Canceled : DownloadStates.Failed;
                        OnDownloadComplete();
                        return;
                    }
                }
            }

            // Max out the progress
            ItemCurrentProgress = ItemMaxProgress;
            TotalCurrentProgress = TotalMaxProgress;
            OnStatusUpdated(new Progress(TotalCurrentProgress, TotalMaxProgress));

            // Flag the operation as complete
            OperationRunning = false;

            DownloadState = DownloadStates.Succeeded;

            RL.Logger?.LogInformationSource($"The download operation has completed");

            await Services.MessageUI.DisplayMessageAsync(Resources.Download_Success, Resources.Download_SuccessHeader, MessageType.Success);
            OnDownloadComplete();
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Occurs when the download status is updated
        /// </summary>
        public event EventHandler<ValueEventArgs<Progress>> StatusUpdated;

        /// <summary>
        /// Occurs when the download is complete or canceled
        /// </summary>
        public event EventHandler DownloadComplete;

        #endregion

        #region Protected Properties

        /// <summary>
        /// The file manager to use
        /// </summary>
        protected IFileManager FileManager { get; }

        /// <summary>
        /// The current step in the operation
        /// </summary>
        protected int CurrentStep { get; set; }

        /// <summary>
        /// The web client for server files
        /// </summary>
        protected WebClient WCServer { get; set; }

        /// <summary>
        /// The input sources. This collection only has one item if <see cref="IsCompressed"/> is true
        /// </summary>
        protected List<Uri> InputSources { get; }

        /// <summary>
        /// The output directory
        /// </summary>
        protected FileSystemPath OutputDirectory { get; }

        /// <summary>
        /// The list of processed files
        /// </summary>
        protected List<FileSystemPath> ProcessedPaths { get; }

        /// <summary>
        /// The temp directory for the local files
        /// </summary>
        protected TempDirectory LocalTempDir { get; set; }

        /// <summary>
        /// The temp directory for the sever files
        /// </summary>
        protected TempDirectory ServerTempDir { get; set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// The current total progress
        /// </summary>
        public double TotalCurrentProgress { get; set; }

        /// <summary>
        /// The max total progress
        /// </summary>
        public double TotalMaxProgress { get; set; }

        /// <summary>
        /// The current item progress
        /// </summary>
        public double ItemCurrentProgress { get; set; }

        /// <summary>
        /// The max item progress
        /// </summary>
        public double ItemMaxProgress { get; set; }

        /// <summary>
        /// The current state of the download
        /// </summary>
        public DownloadStates DownloadState { get; private set; }

        /// <summary>
        /// Indicates if the operation is running
        /// </summary>
        public bool OperationRunning { get; set; }

        /// <summary>
        /// Indicates if a cancellation has been requested
        /// </summary>
        public bool CancellationRequested { get; set; }

        /// <summary>
        /// Indicates if the download is compressed
        /// </summary>
        public bool IsCompressed { get; }

        #endregion

        #region Enums

        /// <summary>
        /// The state of a download
        /// </summary>
        public enum DownloadStates
        {
            /// <summary>
            /// The download is paused or has not started
            /// </summary>
            Paused,

            /// <summary>
            /// The download is currently running
            /// </summary>
            Running,

            /// <summary>
            /// The download failed
            /// </summary>
            Failed,

            /// <summary>
            /// The download was canceled
            /// </summary>
            Canceled,

            /// <summary>
            /// The download succeeded
            /// </summary>
            Succeeded
        }

        #endregion
    }
}