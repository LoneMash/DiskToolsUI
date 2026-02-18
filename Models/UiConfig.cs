// UiConfig.cs - Version 2.4
// Changelog : Suppression de SourceFunction et dropdown, retour au TextBox simple

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
        public string Title { get; set; } = "Disk Tools UI";
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
        public string FunctionsScriptPath { get; set; } = "./Scripts/DiskFunctions.ps1";
    }
}
