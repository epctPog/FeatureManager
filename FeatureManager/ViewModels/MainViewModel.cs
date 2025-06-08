using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using FeatureManager.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FeatureManager.Classes.Config;
using FeatureManager.Classes.Undo;
using FeatureManager.Importer.ExcelImporter;
using FeatureManager.Importer;
using FeatureManager.Importer.JsonImporter;
using FeatureManager.Importer.TxtImporter;

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

            string json = File.ReadAllText(fullPath);
            var entries = JsonConvert.DeserializeObject<List<FeatureEntry>>(json);
            if (entries == null) return;

            foreach (var entry in entries.OrderBy(e => e.Id))
            {
                Features.Add(entry);
                AllFeatures.Add(entry);
            }

            UndoManager.Clear();
            UndoManager.SaveState(Features);
            RaiseCommandStates();
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
        public void ImportFeatures(string path)
        {
            if (!File.Exists(path))
                return;

            string json = File.ReadAllText(path);
            var importedFeatures = JsonConvert.DeserializeObject<ObservableCollection<FeatureEntry>>(json);

            if (importedFeatures == null)
                return;

            Features.Clear();
            foreach (var f in importedFeatures)
                Features.Add(f);
            UndoManager.Clear();
            UndoManager.SaveState(Features);

            RaiseCommandStates();
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
