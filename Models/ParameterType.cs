// ═══════════════════════════════════════════════════════════════════
// ParameterType.cs — Énumération des types de paramètres
// ═══════════════════════════════════════════════════════════════════
// Rôle : Définit les types de contrôles UI disponibles pour les
//        paramètres d'action (Text, Number, Bool, Dropdown).
// Couche : Models
// Consommé par : ParameterDefinition, ParametersView (DataTemplates)
// ═══════════════════════════════════════════════════════════════════

// Version 1.0
// Changelog :
//   1.0 - v3.00 : Création — enum des types de paramètres supportés dans actions.json

namespace RunDeck.Models
{
    public enum ParameterType
    {
        Text,
        Number,
        Bool,
        Dropdown
    }
}
