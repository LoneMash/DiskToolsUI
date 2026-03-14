// Version 2.0
// Changelog :
//   1.0 - Initial : structure couplée à un script PS unique
//   2.0 - v3.00 : Suppression de PowerShellConfig, ajout de ActionsFile et LoggingConfig
//                 UiConfig allégé (plus de Actions ni Parameters embarqués)

using System.Collections.Generic;

namespace RunDeck.Models
{
    public class AppConfig
    {
        public UiConfig UI { get; set; } = new();
        public string ActionsFile { get; set; } = "actions.json";
        public LoggingConfig Logging { get; set; } = new();
    }

    public class UiConfig
    {
        public string Title { get; set; } = "RunDeck";
        public string Icon { get; set; } = "⚡";
        public int Width { get; set; } = 900;
        public int Height { get; set; } = 600;
    }

    public class LoggingConfig
    {
        public string LogFileName { get; set; } = "error.log";
    }

    // ---------------------------------------------------------------------------
    // Modèles de désérialisation JSON pour actions.json
    // Séparés d'ActionDefinition pour isoler la couche de config de la couche modèle
    // ---------------------------------------------------------------------------

    public class ActionsConfig
    {
        public List<ActionConfig> Actions { get; set; } = new();
    }

    public class ActionConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ScriptPath { get; set; } = string.Empty;
        public string? FunctionName { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<ParameterConfig> Parameters { get; set; } = new();
    }

    public class ParameterConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "Text";
        public string DefaultValue { get; set; } = string.Empty;
        public bool Required { get; set; } = false;
        public List<string> Options { get; set; } = new();
    }
}
