using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using FeatureManager.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FeatureManager.Classes.Undo;
using FeatureManager.Importer.ExcelImporter;
using FeatureManager.Importer;
using FeatureManager.Importer.JsonImporter;
using FeatureManager.Importer.TxtImporter;
using System.Windows;

namespace FeatureManager.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<string> JsonFiles { get; set; } = new();
        public ObservableCollection<FeatureEntry> Features { get; set; } = new();
        public ObservableCollection<FeatureEntry> AllFeatures { get; set; } = new();
        public string SelectedJsonFilePath { get; set; } = string.Empty;
        public UndoManager UndoManager { get; } = new();

        public DelegateCommand UndoCommand { get; }
        public DelegateCommand RedoCommand { get; }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ApplyFilter();
                }
            }
        }

        public MainViewModel()
        {
            UndoCommand = new DelegateCommand(Undo, CanUndo);
            RedoCommand = new DelegateCommand(Redo, CanRedo);
        }

        public void LoadFilesFromFolder(string folderPath)
        {
            JsonFiles.Clear();
            foreach (var file in Directory.GetFiles(folderPath, "*.json"))
            {
                JsonFiles.Add(Path.GetFileName(file));
            }
        }

        public void LoadFeaturesFromFile(string folderPath, string fileName)
        {
            Features.Clear();
            AllFeatures.Clear();

            string fullPath = Path.Combine(folderPath, fileName);
            if (!File.Exists(fullPath)) return;

            try
            {
                var importer = new JsonFeatureImporter();
                var entries = importer.Import(fullPath);

                if (entries != null && entries.Count > 0)
                {
                    foreach (var entry in entries.OrderBy(e => e.Id))
                    {
                        Features.Add(entry);
                        AllFeatures.Add(entry);
                    }

                    UndoManager.Clear();
                    UndoManager.SaveState(Features);
                    RaiseCommandStates();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Fehler beim Laden der JSON-Datei:\n" + ex.Message, "Ladefehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public void SaveToJson(string path)
        {
            var json = JsonConvert.SerializeObject(Features, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void AddFeature()
        {
            UndoManager.SaveState(Features);
            Features.Add(new FeatureEntry { Id = Features.Count + 1, Name = "Neues Feature", Priority = 5 });
            RaiseCommandStates();
        }

        public void DeleteSelected(FeatureEntry selected)
        {
            if (selected == null) return;
            UndoManager.SaveState(Features);
            Features.Remove(selected);
            RaiseCommandStates();
        }

        private void Undo()
        {
            var prev = UndoManager.Undo(Features);
            Features.Clear();
            foreach (var f in prev)
                Features.Add(f);
            RaiseCommandStates();
        }

        private void Redo()
        {
            var next = UndoManager.Redo(Features);
            Features.Clear();
            foreach (var f in next)
                Features.Add(f);
            RaiseCommandStates();
        }

        private bool CanUndo()
        {
            return UndoManager.CanUndo;
        }

        private bool CanRedo()
        {
            return UndoManager.CanRedo;
        }

        private void RaiseCommandStates()
        {
            UndoCommand.RaiseCanExecuteChanged();
            RedoCommand.RaiseCanExecuteChanged();
        }
        private void ApplyFilter()
        {
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? AllFeatures
                : new ObservableCollection<FeatureEntry>(
                    AllFeatures.Where(f =>
                        (f.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (f.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        f.Id.ToString().Contains(SearchText) ||
                        f.Priority.ToString().Contains(SearchText)
                    )
                );

            Features.Clear();
            foreach (var f in filtered)
                Features.Add(f);
        }
        public void ExportFeatures(string path)
        {
            var json = JsonConvert.SerializeObject(Features, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        public void ImportFile(string sourcePath, string currentFolder)
        {
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(currentFolder))
                return;

            if (!File.Exists(sourcePath))
            {
                System.Windows.MessageBox.Show("Die ausgewählte Datei existiert nicht!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fileName = Path.GetFileName(sourcePath);
            string destinationPath = Path.Combine(currentFolder, fileName);

            if (!string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.Copy(sourcePath, destinationPath, overwrite: false);
                }
                catch (IOException)
                {
                    System.Windows.MessageBox.Show("Eine Datei mit diesem Namen existiert bereits im Zielordner!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Fehler beim Kopieren der Datei:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(SelectedJsonFilePath))
            {
                string oldFullPath = Path.Combine(currentFolder, SelectedJsonFilePath);
                SaveToJson(oldFullPath);
            }

            SelectedJsonFilePath = fileName;

            if (!JsonFiles.Contains(fileName))
            {
                JsonFiles.Add(fileName);
            }

            try
            {
                string json = File.ReadAllText(destinationPath);
                var importer = new JsonFeatureImporter();
                var importedFeatures = importer.Import(json);

                Features.Clear();
                AllFeatures.Clear();

                if (importedFeatures != null)
                {
                    foreach (var f in importedFeatures)
                    {
                        Features.Add(f);
                        AllFeatures.Add(f);
                    }
                }

                UndoManager.Clear();
                UndoManager.SaveState(Features);
                RaiseCommandStates();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Einlesen der Datei:\n{ex.Message}", "Fehler");
            }
        }

        public void ImportFeatures(string filePath, string extension)
        {
            IFeatureImporter importer = extension.ToLower() switch
            {
                ".txt" => new TxtFeatureImporter(),
                ".json" => new JsonFeatureImporter(),
                ".xlsx" => new ExcelFeatureImporter(),
                _ => null
            };
            if (importer != null)
            {
                var imported = importer.Import(filePath);
                var existingIds = new HashSet<int>(Features.Select(f => f.Id));
                var newIdMap = new Dictionary<int, int>();
                foreach (var feature in imported)
                {
                    int originalId = feature.Id;
                    int newId = originalId;
                    while (existingIds.Contains(newId) || newIdMap.ContainsValue(newId))
                    {
                        newId++;
                    }
                    feature.Id = newId;
                    newIdMap[originalId] = newId;
                    Features.Add(feature);
                    existingIds.Add(newId);
                }
            }
        }
    }
}
