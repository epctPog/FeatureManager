using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FeatureManager.Models
{
    public class FeatureEntry : INotifyPropertyChanged
    {
        private int id;
        private string? name;
        private string? description;
        private int priority;

        public int Id { get => id; set { id = value; OnPropertyChanged(); } }
        public string Name { get => name; set { name = value; OnPropertyChanged(); } }
        public string Description { get => description; set { description = value; OnPropertyChanged(); } }
        public int Priority { get => priority; set { priority = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public FeatureEntry Clone()
        {
            return new FeatureEntry
            {
                Id = this.Id,
                Name = this.Name,
                Description = this.Description,
                Priority = this.Priority
            };
        }
    }
}
