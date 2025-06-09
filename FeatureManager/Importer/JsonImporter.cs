using FeatureManager.Models;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FeatureManager.Importer.JsonImporter
{
    public class JsonFeatureImporter : IFeatureImporter
    {
        public ObservableCollection<FeatureEntry> Import(string filePath)
        {
            string json = File.ReadAllText(filePath);

            ObservableCollection<FeatureEntry>? features;

            features = TryParseListOfObjects(json);
            if (features != null && features.Count > 0)
            {
                Console.WriteLine("Parsed as ListOfObjects");
                return SanitizeFeatures(features);
            }

            features = TryParseObjectWithArrays(json);
            if (features != null && features.Count > 0)
            {
                Console.WriteLine("Parsed as ObjectWithArrays");
                return SanitizeFeatures(features);
            }

            features = TryParseNestedFeatureObject(json);
            if (features != null && features.Count > 0)
            {
                Console.WriteLine("Parsed as NestedFeatureObject");
                return SanitizeFeatures(features);
            }

            features = TryParseArrayOfArrays(json);
            if (features != null && features.Count > 0)
            {
                Console.WriteLine("Parsed as ArrayOfArrays");
                return SanitizeFeatures(features);
            }

            Console.WriteLine("No parser succeeded");
            return new ObservableCollection<FeatureEntry>();
        }

        //11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
        private ObservableCollection<FeatureEntry>? TryParseListOfObjects(string json)
        {
            try
            {
                var array = JArray.Parse(json);
                var features = new ObservableCollection<FeatureEntry>();

                foreach (var token in array)
                {
                    if (token is not JObject obj)
                        continue;

                    int? id = null;
                    string? name = null;
                    string? description = null;
                    int? priority = null;
                    foreach (var prop in obj.Properties())
                    {
                        var key = prop.Name.Trim().ToLower();
                        var val = prop.Value;
                        if ((key == "id" || key == "i" || key == "idx") && id == null)
                        {
                            if (val.Type == JTokenType.Integer && val.ToObject<int>() > 0)
                                id = val.ToObject<int>();
                            else if (int.TryParse(val.ToString(), out int parsedId) && parsedId > 0)
                                id = parsedId;
                        }
                        else if ((key == "name" || key == "n") && name == null)
                        {
                            name = val.ToString();
                        }
                        else if ((key == "description" || key == "desc" || key == "d") && description == null)
                        {
                            description = val.ToString();
                        }
                        else if ((key == "priority" || key == "prio" || key == "priory" || key == "p") && priority == null)
                        {
                            if (val.Type == JTokenType.Integer)
                                priority = ClampPriority(val.ToObject<int>());
                            else if (int.TryParse(val.ToString(), out int parsedPrio))
                                priority = ClampPriority(parsedPrio);
                            else
                                priority = 0;
                        }
                    }
                    foreach (var prop in obj.Properties())
                    {
                        var val = prop.Value;

                        if (id == null && val.Type == JTokenType.Integer && val.ToObject<int>() > 0)
                        {
                            id = val.ToObject<int>();
                            continue;
                        }

                        if (name == null && val.Type == JTokenType.String)
                        {
                            name = val.ToString();
                            continue;
                        }

                        if (description == null && val.Type == JTokenType.String)
                        {
                            description = val.ToString();
                            continue;
                        }

                        if (priority == null)
                        {
                            if (val.Type == JTokenType.Integer)
                                priority = ClampPriority(val.ToObject<int>());
                            else if (int.TryParse(val.ToString(), out int parsedPrio))
                                priority = ClampPriority(parsedPrio);
                        }
                    }
                    if (id != null || name != null || description != null || priority != null)
                    {
                        features.Add(new FeatureEntry
                        {
                            Id = id ?? 0,
                            Name = name ?? "",
                            Description = description ?? "",
                            Priority = priority ?? 0
                        });
                    }
                }

                return features;
            }
            catch
            {
                return null;
            }
        }
        private int ClampPriority(int value)
        {
            return (value >= 1 && value <= 10) ? value : 0;
        }


        //2222222222222222222222222222222222222222222222222222222222222222222222222222222222
        private ObservableCollection<FeatureEntry>? TryParseObjectWithArrays(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(json);
                if (obj == null) return null;

                var ids = GetFlexibleIntList(obj, "ids", "id_list", "idarray");
                var names = GetFlexibleStringList(obj, "names", "titles", "featureNames");
                var descriptions = GetFlexibleStringList(obj, "descriptions", "descs", "featureDescriptions");
                var priorities = GetFlexibleIntList(obj, "priorities", "priorityList", "prio");

                int count = new[] { ids.Count, names.Count, descriptions.Count, priorities.Count }.Max();
                if (count == 0) return null;

                var features = new ObservableCollection<FeatureEntry>();
                for (int i = 0; i < count; i++)
                {
                    features.Add(new FeatureEntry
                    {
                        Id = (i < ids.Count && ids[i] > 0) ? ids[i] : 0,
                        Name = (i < names.Count) ? names[i] ?? "" : "",
                        Description = (i < descriptions.Count) ? descriptions[i] ?? "" : "",
                        Priority = (i < priorities.Count && priorities[i] >= 1 && priorities[i] <= 10) ? priorities[i] : 0
                    });
                }

                return features;
            }
            catch
            {
                return null;
            }
        }

        //33333333333333333333333333333333333333333333333333333333333333333333333333333333333333333
        private ObservableCollection<FeatureEntry>? TryParseNestedFeatureObject(string json)
        {
            try
            {
                var obj = JObject.Parse(json);
                if (obj == null) return null;

                var arrayToken = obj.Properties()
                    .FirstOrDefault(p =>
                        p.Value is JArray &&
                        (p.Name.ToLower().Contains("feature") || p.Name.ToLower().Contains("entries"))
                    )?.Value as JArray;

                if (arrayToken == null) return null;

                var features = new ObservableCollection<FeatureEntry>();

                foreach (var token in arrayToken)
                {
                    if (token is not JObject entry) continue;

                    int? id = null;
                    string? name = null;
                    string? description = null;
                    int? priority = null;

                    foreach (var prop in entry.Properties())
                    {
                        var key = prop.Name.ToLower();
                        var val = prop.Value;

                        if (id == null && (key == "id" || key == "id " || key == "identifier") &&
                            int.TryParse(val.ToString(), out int parsedId) && parsedId > 0)
                        {
                            id = parsedId;
                            continue;
                        }

                        if (name == null && key.Contains("name") && val.Type == JTokenType.String)
                        {
                            name = val.ToString();
                            continue;
                        }

                        if (description == null && key.Contains("desc") && val.Type == JTokenType.String)
                        {
                            description = val.ToString();
                            continue;
                        }

                        if (priority == null && key.Contains("prio"))
                        {
                            if (val.Type == JTokenType.Integer)
                            {
                                int p = val.ToObject<int>();
                                priority = (p >= 1 && p <= 10) ? p : 0;
                                continue;
                            }
                            else if (int.TryParse(val.ToString(), out int parsedP))
                            {
                                priority = (parsedP >= 1 && parsedP <= 10) ? parsedP : 0;
                                continue;
                            }
                        }
                    }

                    if (id != null || name != null || description != null || priority != null)
                    {
                        features.Add(new FeatureEntry
                        {
                            Id = id ?? 0,
                            Name = name ?? "",
                            Description = description ?? "",
                            Priority = priority ?? 0
                        });
                    }
                }

                return features;
            }
            catch
            {
                return null;
            }
        }



        //444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444
        private ObservableCollection<FeatureEntry>? TryParseArrayOfArrays(string json)
        {
            try
            {
                var array = JsonConvert.DeserializeObject<List<List<object>>>(json);
                if (array == null) return null;

                var features = new ObservableCollection<FeatureEntry>();

                foreach (var item in array)
                {
                    if (item == null || item.Count == 0) continue;

                    int id = 0;
                    string name = "";
                    string desc = "";
                    int priority = 0;

                    if (item.Count > 0 && int.TryParse(item[0]?.ToString(), out int idParsed) && idParsed > 0)
                        id = idParsed;

                    if (item.Count > 1)
                        name = item[1]?.ToString() ?? "";

                    if (item.Count > 2)
                        desc = item[2]?.ToString() ?? "";

                    if (item.Count > 3)
                    {
                        string? prioStr = item[3]?.ToString()?.ToLower();
                        if (int.TryParse(prioStr, out int prioParsed) && prioParsed >= 1 && prioParsed <= 10)
                            priority = prioParsed;
                    }

                    if (id > 0 || !string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(desc) || priority > 0)
                    {
                        features.Add(new FeatureEntry
                        {
                            Id = id,
                            Name = name,
                            Description = desc,
                            Priority = priority
                        });
                    }
                }

                return features;
            }
            catch
            {
                return null;
            }
        }


        //Cleanup Method
        private ObservableCollection<FeatureEntry> SanitizeFeatures(ObservableCollection<FeatureEntry> features)
        {
            foreach (var f in features)
            {
                if (f.Priority < 1 || f.Priority > 10)
                    f.Priority = 0;

                f.Name ??= "";
                f.Description ??= "";
            }
            return features;
        }

        //HelperMethods for TryParseObjectWithArrays
        private List<int> GetFlexibleIntList(JObject obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var token))
                {
                    var result = new List<int>();
                    if (token is JArray array)
                    {
                        foreach (var item in array)
                        {
                            if (int.TryParse(item?.ToString(), out int value))
                                result.Add(value);
                            else
                                result.Add(0);
                        }
                        return result;
                    }
                }
            }
            return new List<int>();
        }

        private List<string?> GetFlexibleStringList(JObject obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var token))
                {
                    var result = new List<string?>();
                    if (token is JArray array)
                    {
                        foreach (var item in array)
                        {
                            result.Add(item?.ToString() ?? "");
                        }
                        return result;
                    }
                }
            }
            return new List<string?>();
        }
    }
}