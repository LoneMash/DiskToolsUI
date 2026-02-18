// ParameterItem.cs - Version 1.1
// Changelog : Suppression des Choices (dropdown supprimé), TextBox uniquement

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
