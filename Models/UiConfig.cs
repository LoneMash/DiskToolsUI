// UiConfig.cs - Version 2.5
// Changelog : FunctionsScriptPath renommé en ScriptPath pour plus de généricité

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
        public List<ParameterConfig> Parameters { get; set; } = new();
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
        public string ResultField { get; set; } = string.Empty;
    }

    public class PowerShellConfig
    {
        // Nom générique : peut pointer vers n'importe quel script PS1
        public string ScriptPath { get; set; } = "Scripts/PowerShell_Base.ps1";
    }
}
