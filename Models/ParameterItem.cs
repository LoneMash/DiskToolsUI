// ParameterItem.cs - Version 2.0
// Changelog : Adapté pour être porté par ActionItem (plus de liste globale dans le ViewModel)

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DiskToolsUi.Models
{
    public class ParameterItem : INotifyPropertyChanged
    {
        private string _currentValue = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text";

        public string CurrentValue
        {
            get => _currentValue;
            set { _currentValue = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
