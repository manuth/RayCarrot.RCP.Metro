﻿using MahApps.Metro.IconPacks;
using RayCarrot.IO;
using RayCarrot.Logging;
using RayCarrot.UI;
using RayCarrot.WPF;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// View model for a file in an archive
    /// </summary>
    [DebuggerDisplay("{FileName}")]
    public class ArchiveFileViewModel : BaseViewModel, IDisposable
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="fileData">The file data</param>
        /// <param name="archive">The archive the file belongs to</param>
        public ArchiveFileViewModel(ArchiveFileItem fileData, ArchiveViewModel archive)
        {
            // Set properties
            FileData = fileData;
            Archive = archive;
            IconKind = PackIconMaterialKind.FileSyncOutline;

            // Create commands
            ImportCommand = new AsyncRelayCommand(ImportFileAsync);
        }

        #endregion

        #region Commands

        public ICommand ImportCommand { get; }

        #endregion

        #region Public Properties

        /// <summary>
        /// The archive the file belongs to
        /// </summary>
        public ArchiveViewModel Archive { get; }

        /// <summary>
        /// The file data
        /// </summary>
        public ArchiveFileItem FileData { get; }

        /// <summary>
        /// The image source for the thumbnail
        /// </summary>
        public ImageSource ThumbnailSource { get; set; }

        /// <summary>
        /// The archive file stream
        /// </summary>
        public FileStream ArchiveFileStream => Archive.ArchiveFileStream;

        /// <summary>
        /// The archive data manager
        /// </summary>
        public IArchiveDataManager Manager => Archive.Manager;

        /// <summary>
        /// The name of the file
        /// </summary>
        public string FileName => FileData.FileName;

        // TODO-UPDATE: Implement
        /// <summary>
        /// The info about the file to display
        /// </summary>
        public string FileDisplayInfo => "";

        /// <summary>
        /// The icon kind to use for the file
        /// </summary>
        public PackIconMaterialKind IconKind { get; set; }

        /// <summary>
        /// Indicates if the file is initialized
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// The file export options for the file
        /// </summary>
        public ObservableCollection<ArchiveFileExportViewModel> FileExports { get; set; }

        /// <summary>
        /// The file type (available after having been initialized once)
        /// </summary>
        public IArchiveFileType FileType { get; set; }

        /// <summary>
        /// The native file format
        /// </summary>
        public FileExtension NativeFormat => FileType is ArchiveFileType_Default ? FileData.FileExtension : FileType.NativeFormat;

        /// <summary>
        /// Indicates if the file has pending imports
        /// </summary>
        public bool HasPendingImport { get; set; }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets the decoded file data in a stream
        /// </summary>
        /// <returns>The file stream with the decoded data</returns>
        protected ArchiveFileStream GetDecodedFileStream()
        {
            ArchiveFileStream encodedStream = null;
            ArchiveFileStream decodedStream = null;

            try
            {
                // Get the encoded file bytes
                encodedStream = FileData.GetFileData(Archive.ArchiveFileGenerator);

                // Create a stream for the decoded bytes
                decodedStream = new ArchiveFileStream(new MemoryStream(), true);

                // Decode the bytes
                Manager.DecodeFile(encodedStream.Stream, decodedStream.Stream, FileData.ArchiveEntry);

                // Check if the data was decoded
                if (decodedStream.Stream.Length > 0)
                {
                    encodedStream?.Dispose();
                    decodedStream.SeekToBeginning();
                    return decodedStream;
                }
                else
                {
                    decodedStream?.Dispose();
                    encodedStream.SeekToBeginning();
                    return encodedStream;
                }
            }
            catch (Exception ex)
            {
                // Dispose both streams if an exception is thrown
                encodedStream?.Dispose();
                decodedStream?.Dispose();

                ex.HandleError("Getting decoded archive file data");

                throw;
            }
        }

        #endregion

        #region Public Methods

        public void InitializeFile(ArchiveFileStream fileStream = null)
        {
            try
            {
                // Get the file data
                using (fileStream ??= GetDecodedFileStream())
                {
                    // Get the type
                    FileType = FileData.GetFileType(() => fileStream.Stream);

                    // Add native export format
                    FileExports = new ObservableCollection<ArchiveFileExportViewModel>()
                    {
                        new ArchiveFileExportViewModel(NativeFormat, NativeFormat.DisplayName, new AsyncRelayCommand(async () => await ExportFileAsync(NativeFormat)))
                    };

                    // Get export formats
                    FileExports.AddRange(FileType.ExportFormats.Select(x => new ArchiveFileExportViewModel(x, x.DisplayName, new AsyncRelayCommand(async () => await ExportFileAsync(x)))));

                    fileStream.SeekToBeginning();

                    // Get the thumbnail
                    var img = FileType.GetThumbnail(fileStream, 64, Manager);

                    // Freeze the image to avoid thread errors
                    img?.Freeze();

                    // Set the image source
                    ThumbnailSource = img;

                    // Set icon
                    IconKind = FileType.Icon;

                    IsInitialized = true;
                }
            }
            catch (Exception ex)
            {
                // If the stream has closed it's not an error
                if (ArchiveFileStream.CanRead)
                    ex.HandleExpected("Initializing file");
                else
                    ex.HandleError("Initializing file");
            }
        }

        public async Task ExportFileAsync(FileExtension format)
        {
            RL.Logger?.LogTraceSource($"The archive file {FileName} is being exported as {format.FileExtensions}");

            // Run as a load operation
            using (Archive.LoadOperation.Run())
            {
                // Lock the access to the archive
                using (await Archive.ArchiveLock.LockAsync())
                {
                    // Run as a task
                    await Task.Run(async () =>
                    {
                        // Get the output path
                        var result = await Services.BrowseUI.SaveFileAsync(new SaveFileViewModel()
                        {
                            Title = Resources.Archive_ExportHeader,
                            DefaultName = new FileSystemPath(FileName).ChangeFileExtension(format.GetPrimaryFileExtension(), true),
                            Extensions = format.GetFileFilterItem.ToString()
                        });

                        if (result.CanceledByUser)
                            return;

                        Archive.SetDisplayStatus(String.Format(Resources.Archive_ExportingFileStatus, FileName));

                        try
                        {
                            // Get the file data
                            using var fileStream = GetDecodedFileStream();

                            // Export the file
                            ExportFile(result.SelectedFileLocation, fileStream.Stream, format);
                        }
                        catch (Exception ex)
                        {
                            ex.HandleError("Exporting archive file", FileName);

                            await Services.MessageUI.DisplayExceptionMessageAsync(ex, String.Format(Resources.Archive_ExportError, FileName));

                            return;
                        }
                        finally
                        {
                            Archive.SetDisplayStatus(String.Empty);
                        }

                        RL.Logger?.LogTraceSource($"The archive file has been exported");

                        await Services.MessageUI.DisplaySuccessfulActionMessageAsync(Resources.Archive_ExportFileSuccess);
                    });
                }
            }
        }

        /// <summary>
        /// Exports the specified file
        /// </summary>
        /// <param name="filePath">The file path to export to</param>
        /// <param name="stream">The file stream to export</param>
        /// <param name="format">The format to export as</param>
        /// <returns>The task</returns>
        public void ExportFile(FileSystemPath filePath, Stream stream, FileExtension format)
        {
            RL.Logger?.LogTraceSource($"An archive file is being exported as {format}");

            // Create the output file and open it
            using var fileStream = File.Create(filePath);

            // Write the bytes directly to the stream if the specified format is the native one
            if (format == NativeFormat)
                stream.CopyTo(fileStream);
            // Convert the file if the format is not the native one
            else
                FileType.ConvertTo(format, stream, fileStream, Manager);
        }

        /// <summary>
        /// Imports a file
        /// </summary>
        /// <returns>The task</returns>
        public async Task ImportFileAsync()
        {
            RL.Logger?.LogTraceSource($"The archive file {FileName} is being imported...");

            // Run as a load operation
            using (Archive.LoadOperation.Run())
            {
                // Lock the access to the archive
                using (await Archive.ArchiveLock.LockAsync())
                {
                    // Run as a task
                    await Task.Run(async () =>
                    {
                        // Get the file
                        var result = await Services.BrowseUI.BrowseFileAsync(new FileBrowserViewModel()
                        {
                            Title = Resources.Archive_ImportFileHeader,
                            ExtensionFilter = new FileFilterItemCollection(FileType.ExportFormats.Select(x => x.GetFileFilterItem)).CombineAll(Resources.Archive_FileSelectionGroupName).ToString()
                        });

                        if (result.CanceledByUser)
                            return;

                        // TODO-UPDATE: Try/catch all of this

                        // Open the file to be imported
                        using (var importFile = File.OpenRead(result.SelectedFile))
                        {
                            // Memory stream for converted data
                            using (var memStream = new MemoryStream())
                            {
                                // If it's being imported from a non-native format, convert it
                                var convert = result.SelectedFile.FileExtension != NativeFormat;

                                if (convert)
                                    // Convert from the imported file to the memory stream
                                    FileType.ConvertFrom(result.SelectedFile.FileExtension, GetDecodedFileStream(), importFile, memStream, Manager);

                                // Get the temp stream to store the pending import data
                                FileData.SetPendingImport(File.Create(Path.GetTempFileName()));

                                // Get the file stream with the decoded data
                                var decodedStream = convert ? (Stream)memStream : importFile;

                                decodedStream.Position = 0;

                                // Encode the data to the pending import stream
                                Manager.EncodeFile(decodedStream, FileData.PendingImport, FileData.ArchiveEntry);

                                // If no data was encoded we copy over the decoded data
                                if (FileData.PendingImport.Length == 0)
                                    decodedStream.CopyTo(FileData.PendingImport);

                                HasPendingImport = true;
                                Archive.UpdateModifiedFilesCount();

                                // Re-initialize the file
                                InitializeFile(new ArchiveFileStream(decodedStream, false));
                            }
                        }

                        RL.Logger?.LogTraceSource($"The archive file is pending to be imported");
                    });
                }
            }
        }

        /// <summary>
        /// Imports the specified file
        /// </summary>
        /// <param name="filePath">The file path to import from</param>
        /// <returns>The import data</returns>
        public ArchiveImportData ImportFile(FileSystemPath filePath)
        {
            throw new NotImplementedException();
            //// Return the import data
            //return new ArchiveImportData(FileData.FileEntryData, file =>
            //{
            //    Archive.SetDisplayStatus(String.Format(Resources.Archive_ImportingFileStatus, FileName));

            //    // Get the file format
            //    var format = filePath.FileExtension;

            //    byte[] importBytes;

            //    // Convert the file if the format is not the native one
            //    if (format != NativeFileExtension)
            //    {
            //        // Get the file bytes
            //        var bytes = FileData.GetDecodedFileBytes(ArchiveFileStream, Archive.ArchiveFileGenerator, false);

            //        // Open the file to import from
            //        using var importFileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);

            //        // Create a memory stream for the bytes to encode
            //        using var importStream = new MemoryStream();

            //        // Convert the file
            //        FileData.ConvertImportData(bytes, importFileStream, importStream, format);

            //        importBytes = importStream.ToArray();
            //    }
            //    else
            //    {
            //        // Read the file bytes to import from
            //        importBytes = File.ReadAllBytes(filePath);
            //    }

            //    // Return the encoded file
            //    return Archive.Manager.EncodeFile(importBytes, file);
            //});
        }

        public void Dispose() => FileData?.Dispose();

        #endregion

        #region Classes

        /// <summary>
        /// View model for an archive file export option
        /// </summary>
        public class ArchiveFileExportViewModel : BaseViewModel
        {
            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="exportFormat">The format to export as</param>
            /// <param name="displayName">The format display name</param>
            /// <param name="exportCommand">The export command</param>
            public ArchiveFileExportViewModel(FileExtension exportFormat, string displayName, ICommand exportCommand)
            {
                ExportFormat = exportFormat;
                DisplayName = displayName;
                ExportCommand = exportCommand;
            }

            /// <summary>
            /// The format to export as
            /// </summary>
            public FileExtension ExportFormat { get; }

            /// <summary>
            /// The format display name
            /// </summary>
            public string DisplayName { get; }

            /// <summary>
            /// The export command
            /// </summary>
            public ICommand ExportCommand { get; }
        }

        #endregion
    }
}