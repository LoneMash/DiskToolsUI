// SilentArgs.cs - Version 2.0
// Changelog :
//   1.0 - Initial : --silent, --action, --export, paramètres dynamiques
//   2.0 - Ajout --all, --list, --help, --format (json/csv)
//         Export auto-nommé si --export est un dossier

using System.Collections.Generic;

namespace RunDeck.Models
{
    public class SilentArgs
    {
        public bool IsSilent { get; set; }
        public bool ShowHelp { get; set; }
        public bool ListActions { get; set; }
        public bool RunAll { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ExportPath { get; set; } = string.Empty;
        public string Format { get; set; } = "csv";

        /// <summary>Paramètres dynamiques (ex: DriveLetter=C)</summary>
        public Dictionary<string, string> Parameters { get; set; } = new();

        /// <summary>
        /// Parse les arguments de ligne de commande.
        /// </summary>
        public static SilentArgs Parse(string[] args)
        {
            var result = new SilentArgs();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--silent":
                    case "-s":
                        result.IsSilent = true;
                        break;

                    case "--help":
                    case "-h":
                    case "-?":
                        result.ShowHelp = true;
                        break;

                    case "--list":
                    case "-l":
                        result.ListActions = true;
                        break;

                    case "--all":
                    case "-a":
                        result.RunAll = true;
                        result.IsSilent = true;
                        break;

                    case "--action":
                        if (i + 1 < args.Length)
                            result.Action = args[++i];
                        break;

                    case "--export":
                    case "-e":
                        if (i + 1 < args.Length)
                            result.ExportPath = args[++i];
                        break;

                    case "--format":
                    case "-f":
                        if (i + 1 < args.Length)
                            result.Format = args[++i].ToLower();
                        break;

                    default:
                        // Paramètre dynamique : --DriveLetter C
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
