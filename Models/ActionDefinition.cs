// Version 1.0
// Changelog :
//   1.0 - v3.00 : Création — remplace ActionItem, entièrement piloté par actions.json
//                 Ajout de Id, Description, ScriptPath, FunctionName (optionnel),
//                 OutputType, et liste de ParameterDefinition

using System.Collections.Generic;
using System.Linq;

namespace DiskToolsUi.Models
{
    public class ActionDefinition
    {
        /// <summary>Identifiant unique de l'action (ex: "disk-serial")</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Nom affiché dans la sidebar (ex: "Numéro de série disque")</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Description affichée sous le titre dans ParametersView</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Chemin vers le script .ps1 (absolu ou relatif au dossier de l'exe).
        /// Peut être un script avec fonction ou un script autonome.
        /// </summary>
        public string ScriptPath { get; set; } = string.Empty;

        /// <summary>
        /// Nom de la fonction PS à appeler après chargement du script.
        /// Si vide ou null, le script est exécuté directement (script autonome).
        /// </summary>
        public string? FunctionName { get; set; }

        /// <summary>Catégorie pour le regroupement dans la sidebar (ex: "Disque", "Système")</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Icône emoji affichée dans la sidebar</summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>Paramètres du formulaire, construits depuis actions.json</summary>
        public List<ParameterDefinition> Parameters { get; set; } = new();

        /// <summary>True si l'action possède au moins un paramètre à afficher</summary>
        public bool HasParameters => Parameters.Any();
    }
}
