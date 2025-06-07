using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using FeatureManager.ViewModels;
using FeatureManager.Models;
using FeatureManager.Classes.Config;

namespace FeatureManager
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel;
        private string? currentFolder;
        private const double ZoomStep = 0.1;
        private const double MinZoom = 0.5;
        private const double MaxZoom = 3.0;
        private string? oldName;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainViewModel();
            DataContext = viewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(viewModel.SelectedJsonFilePath))
            {
                string fullPath = Path.Combine(Config.ProjectRoot, "Listen", viewModel.SelectedJsonFilePath);
                viewModel.SaveToJson(fullPath);
                System.Windows.MessageBox.Show("Gespeichert!", "Feature Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentFolder = dialog.SelectedPath;
                viewModel.LoadFilesFromFolder(currentFolder);
            }
        }

        private void AddNewFeature_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddFeature();
        }

        private void DeleteFeature_Click(object sender, RoutedEventArgs e)
        {
            if (FeatureDataGrid.SelectedItem is FeatureEntry selectedFeature)
            {
                viewModel.DeleteSelected(selectedFeature);
            }
        }

        private void JsonFile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (JsonListBox.SelectedItem is string filePath)
            {
                viewModel.SelectedJsonFilePath = filePath;
                string fullPath = Path.Combine(Config.ProjectRoot, "Listen", filePath);
                viewModel.LoadFeaturesFromFile(currentFolder ?? "", filePath);
            }
        }

        private bool isDarkMode = false;

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;
            var app = (App)System.Windows.Application.Current;
            app.SetTheme(isDarkMode ? AppTheme.Dark : AppTheme.Light);
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                double zoom = MainScaleTransform.ScaleX;
                zoom += e.Delta > 0 ? ZoomStep : -ZoomStep;
                zoom = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));
                MainScaleTransform.ScaleX = zoom;
                MainScaleTransform.ScaleY = zoom;
                e.Handled = true;
            }
        }

        private void CreateNewFile_Click(object sender, RoutedEventArgs e)
        {
            if (currentFolder == null)
            {
                System.Windows.MessageBox.Show("Bitte zuerst einen Ordner auswählen!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string newFileName = "NeueDatei.json";
            int counter = 1;
            while (File.Exists(Path.Combine(currentFolder, newFileName)))
            {
                newFileName = $"NeueDatei_{counter}.json";
                counter++;
            }
            var emptyFeatures = new ObservableCollection<FeatureEntry>();
            string json = JsonConvert.SerializeObject(emptyFeatures, Formatting.Indented);
            string fullPath = Path.Combine(currentFolder, newFileName);
            File.WriteAllText(fullPath, json);
            viewModel.JsonFiles.Add(newFileName);
            JsonListBox.SelectedItem = newFileName;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "JSON Files|*.json";

            if (dlg.ShowDialog() == true)
            {
                viewModel.ImportFeatures(dlg.FileName);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "JSON Files|*.json";
            dlg.FileName = "export.json";

            if (dlg.ShowDialog() == true)
            {
                viewModel.ExportFeatures(dlg.FileName);
                System.Windows.MessageBox.Show("Export erfolgreich!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                tb.IsReadOnly = false;
                tb.Background = System.Windows.Media.Brushes.White;
                tb.Focus();
                tb.SelectAll();
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                oldName = tb.Text;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                string newName = tb.Text;

                tb.IsReadOnly = true;
                tb.Background = System.Windows.Media.Brushes.Transparent;

                if (!string.IsNullOrEmpty(oldName) && oldName != newName)
                {
                    RenameJsonFile(oldName, newName);
                }
            }
        }

        private void RenameJsonFile(string oldName, string newName)
        {
            string listenPath = Path.Combine(Config.ProjectRoot, "Listen");
            string oldPath = Path.Combine(listenPath, oldName);
            string newPath = Path.Combine(listenPath, newName);

            try
            {
                if (!File.Exists(newPath))
                {
                    File.Move(oldPath, newPath);
                    int index = viewModel.JsonFiles.IndexOf(oldName);
                    if (index >= 0)
                    {
                        viewModel.JsonFiles[index] = newName;
                        viewModel.SelectedJsonFilePath = newName;
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Datei mit diesem Namen existiert bereits!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Fehler beim Umbenennen der Datei:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
