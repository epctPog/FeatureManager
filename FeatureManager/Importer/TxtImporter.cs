using FeatureManager.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace FeatureManager.Importer.TxtImporter
{
    public class TxtFeatureImporter : IFeatureImporter
    {
        public ObservableCollection<FeatureEntry> Import(string filePath)
        {
            var features = new ObservableCollection<FeatureEntry>();
            int lineNumber = 0;

            foreach (var line in File.ReadLines(filePath))
            {
                lineNumber++;
                var parts = line.Split(';');
                if (parts.Length >= 4 &&
                    int.TryParse(parts[0], out int id) &&
                    int.TryParse(parts[3], out int priority))
                {
                    features.Add(new FeatureEntry
                    {
                        Id = id,
                        Name = parts[1],
                        Description = parts[2],
                        Priority = priority
                    });
                }
                else
                {
                    Console.WriteLine($"[TXT] Ungültige Zeile {lineNumber}: \"{line}\"");
                }
            }

            return features;
        }
    }
}
