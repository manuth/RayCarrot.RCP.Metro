﻿using RayCarrot.Binary;
using RayCarrot.Common;
using RayCarrot.IO;
using RayCarrot.Logging;
using RayCarrot.Rayman;
using RayCarrot.Rayman.OpenSpace;
using RayCarrot.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RayCarrot.RCP.Metro
{
    /// <summary>
    /// View model for the Rayman M/Arena progression
    /// </summary>
    public abstract class RaymanMArenaProgressionViewModel : BaseProgressionViewModel
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        protected RaymanMArenaProgressionViewModel(Games game) : base(game)
        {
            // Get the save data directory
            SaveDir = game.GetInstallDir(false) + "MENU" + "SaveGame";
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Get the progression slot view models for the save data from the specified file
        /// </summary>
        /// <param name="filePath">The slot file path</param>
        /// <returns>The progression slot view models</returns>
        protected IEnumerable<ProgressionSlotViewModel> GetProgressionSlotViewModels(FileSystemPath filePath)
        {
            RL.Logger?.LogInformationSource($"Rayman M/Arena save file {filePath.Name} is being loaded...");

            // Make sure the file exists
            if (!filePath.FileExists)
            {
                RL.Logger?.LogInformationSource($"Slot was not loaded due to not being found");

                yield break;
            }

            // Deserialize the save data
            var saveData = BinarySerializableHelpers.ReadFromFile<RaymanMPCSaveData>(filePath, OpenSpaceSettings.GetDefaultSettings(OpenSpaceGame.RaymanM, Platform.PC), RCPServices.App.GetBinarySerializerLogger());

            RL.Logger?.LogInformationSource($"Save file has been deserialized");

            // Helper for getting the entry with a specific key
            int[] GetValues(string key) => saveData.Items.First(x => x.Key == key).Values;

            // Helper for getting the time from an integer
            static TimeSpan GetTime(int value)
            {
                var milliSeconds = value % 1000;
                var calc2_1 = (value - milliSeconds) / 1000;
                var seconds = calc2_1 % 60;
                var minute = (calc2_1 - seconds) / 60;

                return new TimeSpan(0, 0, minute, seconds, milliSeconds);
            }

            // There is a max of 8 save slots in every version
            for (int slotIndex = 0; slotIndex < 8; slotIndex++)
            {
                // Get the save name
                var name = saveData.Items.First(x => x.Key == "sg_names").StringValues[slotIndex];

                // Make sure it's valid
                if (name.Contains("No Data"))
                    continue;

                // Create the collection with items
                var progressItems = new List<ProgressionInfoItemViewModel>();

                // Get completed challenges
                var raceCompleted = 0;
                var battleCompleted = 0;
                const int maxRace = 40;
                const int maxBattle = 13 * 3;

                void AddRaceCompleted(IEnumerable<int> values) => raceCompleted += values.Skip(17 * slotIndex).Take(17).Count(x => x == 2);
                void AddBattleCompleted(IEnumerable<int> values) => battleCompleted += values.Skip(13 * slotIndex).Take(13).Count(x => x == 2);

                AddRaceCompleted(GetValues("sg_racelevels_mode1"));
                AddRaceCompleted(GetValues("sg_racelevels_mode2"));
                AddRaceCompleted(GetValues("sg_racelevels_mode3"));
                AddBattleCompleted(GetValues("sg_battlelevels_mode1"));
                AddBattleCompleted(GetValues("sg_battlelevels_mode2"));
                AddBattleCompleted(GetValues("sg_battlelevels_mode3"));

                // Add completed challenges
                progressItems.Add(new ProgressionInfoItemViewModel(ProgressionIcons.RM_Race, new LocalizedString(() => $"{raceCompleted}/{maxRace}")));
                progressItems.Add(new ProgressionInfoItemViewModel(ProgressionIcons.RM_Battle, new LocalizedString(() => $"{battleCompleted}/{maxBattle}")));

                // Add records for every race
                for (int raceIndex = 0; raceIndex < 16; raceIndex++)
                {
                    // Get the index to use for this race in this slot
                    var index = slotIndex * 16 + raceIndex;

                    // Helper method for adding a race item
                    void AddRaceItem(string key, Func<string> getDescription, bool isTime = true)
                    {
                        // Get the value
                        var value = GetValues(key)[index];

                        // Only add if it has valid data
                        if ((isTime && value > 0) || (!isTime && value > -22))
                            progressItems.Add(new ProgressionInfoItemViewModel(
                                // Get the level icon
                                Enum.Parse(typeof(ProgressionIcons), $"RM_R{raceIndex}").CastTo<ProgressionIcons>(), 
                                // The value
                                new LocalizedString(() => $"{getDescription()}: {(isTime ? (GetTime(value).ToString("mm\\:ss\\:fff")) : value.ToString())}"),
                                // The description (level name)
                                new LocalizedString(() => Resources.ResourceManager.GetString($"RM_RaceName_{raceIndex}"))));
                    }

                    AddRaceItem("sg_racelevels_bestlap_training", () => Resources.Progression_RM_LapTraining);
                    AddRaceItem("sg_racelevels_bestlap_race", () => Resources.Progression_RM_LapRace);
                    AddRaceItem("sg_racelevels_bestlap_target", () => Resources.Progression_RM_LapTarget);
                    AddRaceItem("sg_racelevels_bestlap_lums", () => Resources.Progression_RM_LapLums);
                    AddRaceItem("sg_racelevels_besttime_race", () => Resources.Progression_RM_TimeRace);
                    AddRaceItem("sg_racelevels_besttime_target", () => Resources.Progression_RM_TimeTarget);
                    AddRaceItem("sg_racelevels_besttime_lums", () => Resources.Progression_RM_TimeLums);
                    AddRaceItem("sg_racelevels_bestnumber_target", () => Resources.Progression_RM_Targets, false);
                    AddRaceItem("sg_racelevels_bestnumber_lums", () => Resources.Progression_RM_Lums, false);
                }

                RL.Logger?.LogInformationSource($"General progress info has been set for slot {slotIndex}");

                // Calculate the percentage
                var percentage = (((raceCompleted + battleCompleted) / (double)(maxRace + maxBattle) * 100)).ToString("0.##");

                // Return the data for this slot
                yield return new RaymanMArenaProgressionSlotViewModel(new LocalizedString(() => $"{name.TrimEnd()} ({percentage}%)"), progressItems.ToArray(), filePath, this);
            }
        }

        #endregion

        #region Protected Override Methods

        /// <summary>
        /// Loads the current save data if available
        /// </summary>
        protected override void LoadData()
        {
            // Read and set slot data
            ProgressionSlots.AddRange(GetProgressionSlotViewModels(SaveDir + "raymanm.sav"));
        }

        #endregion
    }
}