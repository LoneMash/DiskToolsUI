// SilentArgs.cs - Version 1.0
// Changelog : Modèle représentant les arguments de ligne de commande en mode silencieux

using System.Collections.Generic;
namespace DiskToolsUi.Models
{
    public class SilentArgs
    {
        public bool IsSilent { get; set; } = false;
        public string Action { get; set; } = string.Empty;
        public string ExportPath { get; set; } = string.Empty;

        // Paramètres dynamiques (ex: DriveLetter=C:)
        public Dictionary<string, string> Parameters { get; set; } = new();

        /// <summary>
        /// Parse les arguments de ligne de commande.
        /// Exemple : --silent --action Get-DiskInfo --DriveLetter C: --export C:\log.csv
        /// </summary>
        public static SilentArgs Parse(string[] args)
        {
            var result = new SilentArgs();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--silent":
                    case "-silent":
                        result.IsSilent = true;
                        break;

                    case "--action":
                    case "-action":
                        if (i + 1 < args.Length)
                            result.Action = args[++i];
                        break;

                    case "--export":
                    case "-export":
                        if (i + 1 < args.Length)
                            result.ExportPath = args[++i];
                        break;

                    default:
                        // Paramètre dynamique : --DriveLetter C:
                        if (args[i].StartsWith("--") && i + 1 < args.Length)
                        {
                            var key = args[i].TrimStart('-');
                            result.Parameters[key] = args[++i];
                        }
                        break;
                }
            }

            return result;
        }
    }
}
