using FeatureManager.Models;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json.Linq;

namespace FeatureManager.Importer.JsonImporter
{
    public class JsonFeatureImporter : IFeatureImporter
    {
        public ObservableCollection<FeatureEntry> Import(string filePath)
        {
            var features = new ObservableCollection<FeatureEntry>();
            string json = File.ReadAllText(filePath);

            try
            {
                var token = JToken.Parse(json);
                var items = token.Type == JTokenType.Array ? token.Children() : token.SelectTokens("$..*");

                foreach (var item in items)
                {
                    if (item.Type != JTokenType.Object) continue;
                    var obj = (JObject)item;

                    var idToken = obj["Id"] ?? obj["id"] ?? obj["ID"];
                    var nameToken = obj["Name"] ?? obj["name"];
                    var descToken = obj["Description"] ?? obj["description"];
                    var priorityToken = obj["Priority"] ?? obj["priority"];

                    if (idToken == null || nameToken == null || descToken == null)
                        continue;

                    if (!int.TryParse(idToken.ToString(), out int id))
                        continue;

                    string name = nameToken.ToString();
                    string description = descToken.ToString();

                    int priority = 0;
                    if (priorityToken != null && int.TryParse(priorityToken.ToString(), out int p))
                    {
                        if (p >= 1 && p <= 10) priority = p;
                    }

                    features.Add(new FeatureEntry
                    {
                        Id = id,
                        Name = name,
                        Description = description,
                        Priority = priority
                    });
                }
            }
            catch
            {
                
            }

            return features;
        }
    }
}