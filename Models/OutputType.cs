// Version 1.0
// Changelog :
//   1.0 - v3.00 : Création — enum des types de sortie supportés par action

namespace DiskToolsUi.Models
{
    public enum OutputType
    {
        KeyValue,   // Grille label / valeur
        Table,      // Tableau avec en-têtes colonnes
        Log         // Texte brut scrollable
    }
}
