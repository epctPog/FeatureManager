using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using FeatureManager.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FeatureManager.Classes.Config;
using FeatureManager.Classes.Undo;

namespace FeatureManager.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<string> JsonFiles { get; set; } = new();
        public ObservableCollection<FeatureEntry> Features { get; set; } = new();
        public string SelectedJsonFilePath { get; set; } = string.Empty;
        public UndoManager UndoManager { get; } = new();

        public DelegateCommand UndoCommand { get; }
        public DelegateCommand RedoCommand { get; }

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
            string fullPath = Path.Combine(folderPath, fileName);
            if (!File.Exists(fullPath)) return;

            string json = File.ReadAllText(fullPath);
            var entries = JsonConvert.DeserializeObject<List<FeatureEntry>>(json);
            if (entries == null) return;

            foreach (var entry in entries.OrderBy(e => e.Id))
            {
                Features.Add(entry);
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
    }
}
