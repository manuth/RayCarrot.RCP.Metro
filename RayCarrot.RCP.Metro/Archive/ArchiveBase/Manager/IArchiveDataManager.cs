﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RayCarrot.Binary;
using RayCarrot.Common;
using RayCarrot.IO;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// Defines an archive data manager
    /// </summary>
    public interface IArchiveDataManager
    {
        /// <summary>
        /// The path separator character to use. This is usually \ or /.
        /// </summary>
        char PathSeparatorCharacter { get; }

        /// <summary>
        /// The file extension for the archive file
        /// </summary>
        FileExtension ArchiveFileExtension { get; }

        /// <summary>
        /// The serializer settings to use for the archive
        /// </summary>
        BinarySerializerSettings SerializerSettings { get; }

        /// <summary>
        /// The default archive file name to use when creating an archive
        /// </summary>
        string DefaultArchiveFileName { get; }

        /// <summary>
        /// Gets the configuration UI to use for creator
        /// </summary>
        object GetCreatorUIConfig { get; }

        /// <summary>
        /// Encodes the file data from the input stream, or nothing if the data does not need to be encoded
        /// </summary>
        /// <param name="inputStream">The input data stream to encode</param>
        /// <param name="outputStream">The output data stream for the encoded data</param>
        /// <param name="fileEntry">The file entry for the file to encode</param>
        void EncodeFile(Stream inputStream, Stream outputStream, object fileEntry);

        /// <summary>
        /// Decodes the file data from the input stream, or nothing if the data is not encoded
        /// </summary>
        /// <param name="inputStream">The input data stream to decode</param>
        /// <param name="outputStream">The output data stream for the decoded data</param>
        /// <param name="fileEntry">The file entry for the file to decode</param>
        void DecodeFile(Stream inputStream, Stream outputStream, object fileEntry);

        /// <summary>
        /// Gets the file data from the archive using a generator
        /// </summary>
        /// <param name="generator">The generator</param>
        /// <param name="fileEntry">The file entry</param>
        /// <returns>The encoded file data</returns>
        byte[] GetFileData(IDisposable generator, object fileEntry);

        /// <summary>
        /// Writes the files to the archive
        /// </summary>
        /// <param name="generator">The generator</param>
        /// <param name="archive">The loaded archive data</param>
        /// <param name="outputFileStream">The file output stream for the archive</param>
        /// <param name="files">The files to include</param>
        void WriteArchive(IDisposable generator, object archive, Stream outputFileStream, IList<ArchiveFileItem> files);

        /// <summary>
        /// Loads the archive data
        /// </summary>
        /// <param name="archive">The archive data</param>
        /// <param name="archiveFileStream">The archive file stream</param>
        /// <returns>The archive data</returns>
        ArchiveData LoadArchiveData(object archive, Stream archiveFileStream);

        /// <summary>
        /// Loads the archive from a stream
        /// </summary>
        /// <param name="archiveFileStream">The file stream for the archive</param>
        /// <returns>The archive data</returns>
        object LoadArchive(Stream archiveFileStream);

        /// <summary>
        /// Creates a new archive object
        /// </summary>
        /// <returns>The archive object</returns>
        object CreateArchive();
    }

    // TODO-UPDATE: Use this for path stuff
    public static class ArchiveDataManagerExtensions
    {
        public static string CombinePaths(this IArchiveDataManager manager, params string[] paths)
        {
            return paths.Select(x => x.TrimEnd(manager.PathSeparatorCharacter)).JoinItems(manager.PathSeparatorCharacter.ToString());
        }
    }
}