﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EnvDTE80;
using Newtonsoft.Json;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace Enterwell.CI.Changelog.VSIX
{
    /// <summary>
    /// Interaction logic for AddChangeDialog.xaml
    /// </summary>
    public partial class AddChangeDialog : Window
    {
        private string solutionPath;

        private readonly string[] changeTypes = {"Added", "Changed", "Deprecated", "Removed", "Fixed", "Security"};
        private const string ConfigurationFileName = ".changelog.json";

        public string ChangeType => TypeComboBox.Text;
        public string ChangeCategory => CategoryComboBox.Text != string.Empty ? CategoryComboBox.Text : CategoryTextBox.Text;
        public string ChangeDescription => DescriptionTextBox.Text;

        public AddChangeDialog(string solutionPath)
        {
            this.solutionPath = solutionPath;

            InitializeComponent();
            InitializeChangeTypes();
            InitializeChangeCategories();
        }

        private void InitializeChangeTypes()
        {
            TypeComboBox.ItemsSource = changeTypes;
            TypeComboBox.SelectedIndex = 0;
        }

        private void InitializeChangeCategories()
        {
            var configuration = LoadConfiguration(this.solutionPath);

            if (configuration != null)
            {
                CategoryComboBox.ItemsSource = configuration.Categories;
                CategoryComboBox.SelectedIndex = 0;
            }
            else
            {
                CategoryComboBox.Visibility = Visibility.Hidden;
                CategoryTextBox.Visibility = Visibility.Visible;
            }
        }

        private void AddChangeBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private Configuration LoadConfiguration(string? solutionPath)
        {
            if (string.IsNullOrWhiteSpace(solutionPath))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(solutionPath));

            var configurationFilePath = Path.Combine(solutionPath, ConfigurationFileName);

            // First check to see if the configuration file exists.
            if (File.Exists(configurationFilePath))
            {
                return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configurationFilePath));
            }

            return null;
        }
    }
}
