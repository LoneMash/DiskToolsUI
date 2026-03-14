// Version 2.0
// Changelog :
//   1.0 - Initial : chargement appsettings.json uniquement
//   2.0 - v3.00 : Ajout LoadActionsAsync() — charge actions.json séparément
//                 Résolution du chemin relatif à AppContext.BaseDirectory
//                 Mapping ActionConfig → ActionDefinition avec enum parsing

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DiskToolsUi.Helpers;
using DiskToolsUi.Models;

namespace DiskToolsUi.Services
{
    public class ConfigService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // -----------------------------------------------------------------------
        // Charge appsettings.json
        // -----------------------------------------------------------------------
        public async Task<AppConfig> LoadConfigAsync()
        {
            var path = PathHelper.ResolveRelativePath("appsettings.json");

            if (!File.Exists(path))
                throw new FileNotFoundException($"appsettings.json introuvable : {path}");

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions)
                   ?? throw new InvalidOperationException("appsettings.json désérialisé en null.");
        }

        // -----------------------------------------------------------------------
        // Charge actions.json et le mappe vers une liste d'ActionDefinition
        // -----------------------------------------------------------------------
        public async Task<List<ActionDefinition>> LoadActionsAsync(string actionsFilePath)
        {
            var path = PathHelper.ResolveRelativePath(actionsFilePath);

            if (!File.Exists(path))
                throw new FileNotFoundException($"Fichier actions introuvable : {path}");

            var json = await File.ReadAllTextAsync(path);
            var actionsConfig = JsonSerializer.Deserialize<ActionsConfig>(json, _jsonOptions)
                                ?? throw new InvalidOperationException("actions.json désérialisé en null.");

            return MapToDefinitions(actionsConfig.Actions);
        }

        // -----------------------------------------------------------------------
        // Mapping ActionConfig (JSON) → ActionDefinition (modèle applicatif)
        // -----------------------------------------------------------------------
        private static List<ActionDefinition> MapToDefinitions(List<ActionConfig> configs)
        {
            var definitions = new List<ActionDefinition>();

            foreach (var config in configs)
            {
                // Parsing OutputType avec fallback sur KeyValue
                if (!Enum.TryParse<OutputType>(config.OutputType, ignoreCase: true, out var outputType))
                    outputType = OutputType.KeyValue;

                var action = new ActionDefinition
                {
                    Id           = config.Id,
                    Name         = config.Name,
                    Description  = config.Description,
                    ScriptPath   = config.ScriptPath,
                    FunctionName = string.IsNullOrWhiteSpace(config.FunctionName)
                                   ? null
                                   : config.FunctionName,
                    OutputType   = outputType
                };

                foreach (var p in config.Parameters)
                {
                    // Parsing ParameterType avec fallback sur Text
                    if (!Enum.TryParse<ParameterType>(p.Type, ignoreCase: true, out var paramType))
                        paramType = ParameterType.Text;

                    action.Parameters.Add(new ParameterDefinition
                    {
                        Name         = p.Name,
                        Label        = p.Label,
                        Type         = paramType,
                        DefaultValue = p.DefaultValue,
                        CurrentValue = p.DefaultValue,  // pré-remplissage avec la valeur par défaut
                        Required     = p.Required,
                        Options      = p.Options
                    });
                }

                definitions.Add(action);
            }

            return definitions;
        }

    }
}
