using System.Windows.Input;

namespace DiskToolsUi.Models
{
    // VERSION 1.00
    public class ActionItem
    {
        public string Name { get; set; } = string.Empty;
        public ICommand? Command { get; set; }
    }
}
