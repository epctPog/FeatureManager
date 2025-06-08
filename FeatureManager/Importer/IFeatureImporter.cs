using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using FeatureManager.Models;

namespace FeatureManager.Importer
{
    public interface IFeatureImporter
    {
        ObservableCollection<FeatureEntry> Import(string filePath);
    }
}