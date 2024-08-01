using Spectrometer.Views.Pages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spectrometer.Helpers
{
    public class StartingTabPageHelper
    {
        private KeyValueConfigurationCollection _configuration;
        public StartingTabPageHelper() 
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            _configuration = configFile.AppSettings.Settings;
        }

        public Type GetStartingTabPageFromAppSettings()
        {
            var pageFromSettings = _configuration["StartingTab"].Value;

            switch (pageFromSettings)
            {
                case "Dashboard":
                    return typeof(DashboardPage);

                case "Graphs":
                    return typeof(GraphsPage);

                case "Sensors":
                    return typeof(GraphsPage);

                case "Settings":
                    return typeof(SettingsPage);

                default:
                    return typeof(DashboardPage);
            }
        }

        public void UpdateStartingTabInAppSettings(string pageType)
        {
            _configuration["StartingTab"].Value = pageType;
        }

        public List<string> GetAllPageNames()
        {
            return new List<string>()
            {
                "Dashboard",
                "Graphs",
                "Sensors",
                "Settings"
            };
        }
    }
}
