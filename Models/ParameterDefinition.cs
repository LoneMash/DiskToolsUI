// Version 2.0
// Changelog :
//   1.0 - v3.00 : Création — remplace ParameterItem, désormais piloté par actions.json
//                 Ajout de Type, Required, Options (pour Dropdown)
//   2.0 - Adoption CommunityToolkit.Mvvm : ObservableObject + [ObservableProperty]

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DiskToolsUi.Models
{
    public partial class ParameterDefinition : ObservableObject
    {
        [ObservableProperty]
        private string _currentValue = string.Empty;

        /// <summary>Nom technique du paramètre PowerShell (ex: "DriveLetter")</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Libellé affiché dans l'UI (ex: "Lettre du lecteur")</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Type de contrôle à générer dans ParametersView</summary>
        public ParameterType Type { get; set; } = ParameterType.Text;

        /// <summary>Valeur par défaut définie dans actions.json</summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>Indique si le paramètre est obligatoire avant exécution</summary>
        public bool Required { get; set; } = false;

        /// <summary>Options disponibles pour le type Dropdown</summary>
        public List<string> Options { get; set; } = new();
    }
}
