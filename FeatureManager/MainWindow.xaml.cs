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
    }
}
