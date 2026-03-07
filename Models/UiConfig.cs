// UiConfig.cs - Version 3.0
// Changelog : Suppression des Parameters globaux, chaque ActionConfig porte ses propres Parameters

using System.Collections.Generic;

namespace DiskToolsUi.Models
{
    public class AppConfig
    {
        public UiConfig UI { get; set; } = new();
        public PowerShellConfig PowerShell { get; set; } = new();
    }

    public class UiConfig
    {
        public string Title { get; set; } = "PowerShell UI";
        public int WindowWidth { get; set; } = 900;
        public int WindowHeight { get; set; } = 600;

        // Plus de Parameters globaux : chaque action a les siens
        public List<ActionConfig> Actions { get; set; } = new();
    }

    public class ParameterConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public string Default { get; set; } = string.Empty;
    }

    public class ActionConfig
    {
        public string Name { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;

        // Paramètres propres à cette action
        public List<ParameterConfig> Parameters { get; set; } = new();
    }

    public class PowerShellConfig
    {
        public string ScriptPath { get; set; } = "Scripts/PowerShell_Base.ps1";
    }
}
