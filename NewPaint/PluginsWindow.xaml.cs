using PluginInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace NewPaint
{
    public partial class PluginsWindow : Window
    {
        public Dictionary<string, IFilterPlugin> plugins { get; set; }

        public string ConfigFilePath { get; set; }

        private bool configurationChanged = false;

        public PluginsWindow(Dictionary<string, IFilterPlugin> plugins)
        {
            this.plugins = plugins;

            InitializeComponent();

            foreach (var plugin in plugins.Values)
            {
                var versionAttribute = Attribute.GetCustomAttribute(plugin.GetType(), typeof(VersionAttribute)) as VersionAttribute;

                pluginsListBox.Items.Add(new { Name = plugin.Name, Author = plugin.Author, Version = $"{versionAttribute.Major}.{versionAttribute.Minor}" });
            }

            this.Closing += PluginsWindow_Closing;
        }

        private void PluginsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (configurationChanged)
            {
                MessageBox.Show("To apply the changes to the configuration file, you need to restart the program.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditConfiguration_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(ConfigFilePath))
            {
                System.Diagnostics.Process.Start("notepad.exe", ConfigFilePath);
                configurationChanged = true;
            }
            else
            {
                MessageBox.Show("Configuration file not found.");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {

            // Закрыть окно
            this.Close();
        }
    }
}
