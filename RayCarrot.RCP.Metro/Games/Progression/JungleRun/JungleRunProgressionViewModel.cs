﻿using RayCarrot.CarrotFramework.Abstractions;
using RayCarrot.IO;
using RayCarrot.Rayman.UbiArt;
using RayCarrot.UI;
using System;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// View model for the Rayman Jungle Run progression
    /// </summary>
    public class JungleRunProgressionViewModel : BaseProgressionViewModel
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public JungleRunProgressionViewModel() : base(Games.RaymanJungleRun)
        {
            // Get the save data directory
            SaveDir = Environment.SpecialFolder.LocalApplicationData.GetFolderPath() + "Packages" + Game.GetManager<RCPWinStoreGame>().FullPackageName + "LocalState";
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Get the progression slot view model for the save data from the specified file
        /// </summary>
        /// <param name="fileName">The slot file name, relative to the save directory</param>
        /// <param name="slotNamegenerator">The generator for the name of the save slot</param>
        /// <returns>The progression slot view model</returns>
        protected ProgressionSlotViewModel GetProgressionSlotViewModel(FileSystemPath fileName, Func<string> slotNamegenerator)
        {
            RCFCore.Logger?.LogInformationSource($"Jungle Run slot {fileName.Name} is being loaded...");

            // Get the file path
            var filePath = SaveDir + fileName;

            // Make sure the file exists
            if (!filePath.FileExists)
            {
                RCFCore.Logger?.LogInformationSource($"Slot was not loaded due to not being found");

                return null;
            }

            // Deserialize and return the data
            var saveData = JungleRunPCSaveData.GetSerializer().Deserialize(filePath);

            RCFCore.Logger?.LogInformationSource($"Slot has been deserialized");

            // Create the collection with items for each time trial level + general information
            var progressItems = new ProgressionInfoItemViewModel[(saveData.Levels.Count / 10) + 2];

            // Get data values
            int collectedLums = 0;
            int availableLums = 0;
            int collectedTeeth = 0;
            int availableTeeth = saveData.Levels.Count;

            RCFCore.Logger?.LogTraceSource($"Levels are being enumerated...");

            // Enumerate each level
            for (int i = 0; i < saveData.Levels.Count; i++)
            {
                // Get the level data
                var levelData = saveData.Levels[i];

                // Check if the level is a normal level
                if ((i + 1) % 10 != 0)
                {
                    RCFCore.Logger?.LogTraceSource($"Level index {i} is a normal level");

                    // Get the collected lums
                    collectedLums += levelData.LumsRecord;
                    availableLums += 100;

                    RCFCore.Logger?.LogTraceSource($"{levelData.LumsRecord} Lums have been collected");

                    // Check if the level is 100% complete
                    if (levelData.LumsRecord >= 100)
                        collectedTeeth++;

                    continue;
                }

                RCFCore.Logger?.LogTraceSource($"Level index {i} is a time trial level");

                // Make sure the level has been completed
                if (levelData.RecordTime == TimeSpan.Zero)
                {
                    RCFCore.Logger?.LogTraceSource($"Level has not been completed");

                    continue;
                }

                RCFCore.Logger?.LogTraceSource($"Level has been completed with the record time {levelData.RecordTime}");

                collectedTeeth++;

                // Get the level number, starting at 10
                var fullLevelNumber = (i + 11).ToString();

                // Get the world and level numbers
                var worldNum = fullLevelNumber[0].ToString();
                var lvlNum = fullLevelNumber[1].ToString();

                // If the level is 0, correct the numbers to be level 10
                if (lvlNum == "0")
                {
                    worldNum = (Int32.Parse(worldNum) - 1).ToString();
                    lvlNum = "10";
                }
                
                // Create the view model
                progressItems[((i + 1) / 10) - 1 + 2] = new ProgressionInfoItemViewModel(ProgressionIcons.RO_Clock, new LocalizedString(() => $"{worldNum}-{lvlNum}: {levelData.RecordTime:mm\\:ss\\:fff}"));
            }

            // Set general progress info
            progressItems[0] = new ProgressionInfoItemViewModel(ProgressionIcons.RO_Lum, new LocalizedString(() => $"{collectedLums}/{availableLums}"));
            progressItems[1] = new ProgressionInfoItemViewModel(ProgressionIcons.RO_RedTooth, new LocalizedString(() => $"{collectedTeeth}/{availableTeeth}"));

            RCFCore.Logger?.LogInformationSource($"General progress info has been set");

            // Calculate the percentage
            var percentage = ((collectedLums / (double)availableLums * 50) + (collectedTeeth / (double)availableTeeth * 50)).ToString("0.##");

            RCFCore.Logger?.LogInformationSource($"Slot percentage is {percentage}%");

            // Return the data with the collection
            return new JungleRunProgressionSlotViewModel(new LocalizedString(() => $"{slotNamegenerator()} ({percentage}%)"), progressItems, filePath, this);
        }

        #endregion

        #region Protected Override Methods

        /// <summary>
        /// Loads the current save data if available
        /// </summary>
        protected override void LoadData()
        {
            // Read and set slot data
            ProgressionSlots.AddRange(new ProgressionSlotViewModel[]
            {
                GetProgressionSlotViewModel("slot1.dat", () => String.Format(Resources.Progression_GenericSlot, "1")),
                GetProgressionSlotViewModel("slot2.dat", () => String.Format(Resources.Progression_GenericSlot, "2")),
                GetProgressionSlotViewModel("slot3.dat", () => String.Format(Resources.Progression_GenericSlot, "3"))
            });
        }

        #endregion
    }
}