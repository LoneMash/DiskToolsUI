// ActionItem.cs - Version 3.0
// Changelog : Ajout de Parameters propres à chaque action

using System.Collections.ObjectModel;

namespace DiskToolsUi.Models
{
    public class ActionItem
    {
        public string Name { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;

        // Paramètres propres à cette action (alimentés depuis config.json)
        public ObservableCollection<ParameterItem> Parameters { get; set; } = new();

        // True si l'action n'a aucun paramètre → exécution immédiate au clic
        public bool HasParameters => Parameters.Count > 0;
    }
}
