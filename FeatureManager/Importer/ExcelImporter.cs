using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Windows.Forms;
using FeatureManager.Models;
using FeatureManager.ViewModels;
using ClosedXML.Excel;
using System.Collections.ObjectModel;


namespace FeatureManager.Importer.ExcelImporter
{
    public class ExcelFeatureImporter : IFeatureImporter
    {
        public ObservableCollection<FeatureEntry> Import(string filePath)
        {
            var features = new ObservableCollection<FeatureEntry>();
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            int rowIndex = 1;

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                rowIndex++;
                var idStr = row.Cell(1).GetValue<string>();
                var name = row.Cell(2).GetValue<string>();
                var description = row.Cell(3).GetValue<string>();
                var priorityStr = row.Cell(4).GetValue<string>();

                if (int.TryParse(idStr, out int id) && int.TryParse(priorityStr, out int priority))
                {
                    features.Add(new FeatureEntry
                    {
                        Id = id,
                        Name = name,
                        Description = description,
                        Priority = priority
                    });
                }
                else
                {
                    Console.WriteLine($"[Excel] Ungültige Daten in Zeile {rowIndex}: ID='{idStr}', Priority='{priorityStr}'");
                }
            }

            return features;
        }
    }
}
