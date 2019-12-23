﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using RayCarrot.IO;
using RayCarrot.Rayman;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// Archived file data for an OpenSpace .cnt file
    /// </summary>
    public class OpenSpaceCntArchiveFileData : IArchiveImageFileData
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="fileData">The file data</param>
        /// <param name="settings">The settings when serializing the data</param>
        /// <param name="directory">The directory the file is located under</param>
        public OpenSpaceCntArchiveFileData(OpenSpaceCntFile fileData, OpenSpaceSettings settings, string directory)
        {
            Directory = directory;
            FileData = fileData;
            FileName = FileData.FileName;
            Settings = settings;
        }

        #endregion

        #region Protected Properties

        /// <summary>
        /// The settings when serializing the data
        /// </summary>
        protected OpenSpaceSettings Settings { get; }

        #endregion

        #region Public Properties

        /// <summary>
        /// The directory the file is located under
        /// </summary>
        public string Directory { get; }

        /// <summary>
        /// The file data
        /// </summary>
        public OpenSpaceCntFile FileData { get; }

        /// <summary>
        /// The file name
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The name of the file format
        /// </summary>
        public string FileFormatName => "GF";

        /// <summary>
        /// The available file formats for the file
        /// </summary>
        public FileFilterItemCollection AvailableFileFormats => new FileFilterItemCollection()
        {
            new FileFilterItem("*.gf", "OpenSpace GF file"),
            new FileFilterItem("*.bmp", "Bitmap file"),
            new FileFilterItem("*.png", "PNG file"),
            new FileFilterItem("*.jpeg", "JPEG file"),
            new FileFilterItem("*.jpg", "JPG file"),
        };

        /// <summary>
        /// The path to the temporary file containing the data to be imported
        /// </summary>
        public FileSystemPath PendingImportTempPath { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the contents of the file from the stream
        /// </summary>
        /// <param name="archiveFileStream">The file stream for the archive</param>
        /// <returns>The contents of the file</returns>
        public byte[] GetFileContent(Stream archiveFileStream)
        {
            return FileData.GetFileBytes(archiveFileStream);
        }

        /// <summary>
        /// Gets the image as a bitmap
        /// </summary>
        /// <param name="archiveFileStream">The file stream for the archive</param>
        /// <returns>The image as a bitmap</returns>
        public Bitmap GetBitmap(Stream archiveFileStream)
        {
            return FileData.GetFileContent(archiveFileStream, Settings).GetBitmap();
        }

        /// <summary>
        /// Gets the image as a bitmap with a specified width, while maintaining the aspect ratio
        /// </summary>
        /// <param name="archiveFileStream">The file stream for the archive</param>
        /// <param name="width">The width</param>
        /// <returns>The image as a bitmap</returns>
        public Bitmap GetBitmap(Stream archiveFileStream, int width)
        {
            var file = FileData.GetFileContent(archiveFileStream, Settings);

            return file.GetBitmapThumbnail(width);
        }

        /// <summary>
        /// Saves the file to the specified path
        /// </summary>
        /// <param name="archiveFileStream">The file stream for the archive</param>
        /// <param name="filePath">The path to save the file to</param>
        /// <param name="fileFormat">The file extension to use</param>
        /// <returns>The task</returns>
        public Task SaveFileAsync(Stream archiveFileStream, FileSystemPath filePath, string fileFormat)
        {
            // Open the file
            using var file = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

            // Check if the file should be saved as its native format
            if (fileFormat == ".gf")
            {
                // Get the file bytes
                var bytes = GetFileContent(archiveFileStream);

                // Write to the stream
                file.Write(bytes, 0, bytes.Length);
            }
            // Convert the file and save
            else
            {
                // Get the bitmap
                using var bmp = GetBitmap(archiveFileStream);

                // Get the format
                var format = fileFormat switch
                {
                    ".bmp" => ImageFormat.Bmp,
                    ".png" => ImageFormat.Png,
                    ".jpeg" => ImageFormat.Jpeg,
                    ".jpg" => ImageFormat.Jpeg,
                    _ => throw new Exception($"The specified file format {fileFormat} is not supported")
                };

                // Save the file
                bmp.Save(file, format);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Imports the file from the specified path to the <see cref="PendingImportTempPath"/> path
        /// </summary>
        /// <param name="archiveFileStream">The file stream for the archive</param>
        /// <param name="filePath">The path of the file to import</param>
        /// <returns>The task</returns>
        public Task ImportFileAsync(Stream archiveFileStream, FileSystemPath filePath)
        {
            // Get the temporary file to save to, without disposing it
            var tempFile = new TempFile(false);

            // Check if the file is in the native format
            if (filePath.FileExtension.Equals(".gf", StringComparison.InvariantCultureIgnoreCase))
            {
                // Copy the file
                RCFRCP.File.CopyFile(filePath, tempFile.TempPath, true);
            }
            // Import as bitmap
            else
            {
                // Load the bitmap
                using var bmp = new Bitmap(filePath);

                // Load the current file
                var file = FileData.GetFileContent(archiveFileStream, Settings);

                // Import the bitmap
                file.ImportFromBitmap(bmp);

                // Serialize to the file
                using var stream = File.Open(tempFile.TempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                
                // Serialize the data to get the bytes
                new OpenSpaceGfSerializer(Settings).Serialize(stream, file);
            }

            // NOTE: This is unused as we're removing the encryption to speed up the modding process
            //// Encrypt the file
            //using (var fileSteam = File.Open(tempFile.TempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            //{
            //    for (int i = 0; i < fileSteam.Length; i++)
            //    {
            //        if ((fileSteam.Length % 4) + i < fileSteam.Length)
            //        {
            //            var b = fileSteam.ReadByte();

            //            fileSteam.Position--;

            //            fileSteam.WriteByte((byte)(b ^ FileData.FileXORKey[i % 4]));
            //        }
            //    }
            //}

            // Set the pending path
            PendingImportTempPath = tempFile.TempPath;

            return Task.CompletedTask;
        }

        #endregion

    }
}