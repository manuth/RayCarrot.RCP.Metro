﻿using System;
using RayCarrot.Extensions;

namespace RayCarrot.RCP.Core
{
    /// <summary>
    /// Extension methods for <see cref="APIControllerSettings"/>
    /// </summary>
    public static class APIControllerSettingsExtensions
    {
        /// <summary>
        /// Sets the UI settings for the API controller settings
        /// </summary>
        /// <param name="controllerSettings">The API controller settings instance to use</param>
        /// <param name="settings">The UI settings to use</param>
        /// <returns>The API controller settings instance</returns>
        public static APIControllerSettings SetUISettings(this APIControllerSettings controllerSettings, APIControllerUISettings settings)
        {
            // Set the settings
            controllerSettings.CustomSettings["RayCarrot.RCP.Core"] = settings;

            // Return the controller settings
            return controllerSettings;
        }

        /// <summary>
        /// Gets the UI settings from the API controller settings
        /// </summary>
        /// <param name="controllerSettings">The API controller settings instance to get the UI settings from</param>
        /// <returns>The UI settings</returns>
        public static APIControllerUISettings GetUISettings(this APIControllerSettings controllerSettings)
        {
            // Get the UI settings or throw if not found
            return controllerSettings.CustomSettings.TryGetValue("RayCarrot.RCP.Core") as APIControllerUISettings ?? throw new Exception("Core settings have not been set");
        }
    }
}