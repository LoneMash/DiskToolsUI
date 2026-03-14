// Version 1.0
// Changelog :
//   1.0 - Initial — inchangé en v3.00

using System.Collections.ObjectModel;

namespace DiskToolsUi.Models
{
    public class TableRow
    {
        public ObservableCollection<string> Cells { get; set; } = new();
    }
}
