// ═══════════════════════════════════════════════════════════════════
// ResultBuilderService.cs — Construction incrémentale des résultats streaming
// ═══════════════════════════════════════════════════════════════════
// Rôle : Transforme chaque PSObject reçu en temps réel en ResultItem
//        (Table, KeyValue ou Log) avec auto-détection du type de sortie
//        au premier objet, puis accumulation dans le contexte de streaming.
// Couche : Services
// Consommé par : MainWindowViewModel (streaming temps réel)
// ═══════════════════════════════════════════════════════════════════

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Management.Automation;
using RunDeck.Interfaces;
using RunDeck.Models;

namespace RunDeck.Services
{
    public class ResultBuilderService : IResultBuilder
    {
        public StreamingContext CreateContext() => new();

        /// <summary>
        /// Traite un PSObject en streaming. Retourne les nouveaux items à ajouter
        /// à la collection de résultats (liste vide si l'objet met à jour un item existant).
        /// </summary>
        public List<ResultItem> ProcessStreamingObject(PSObject obj, StreamingContext context)
        {
            context.ObjectCount++;
            var baseObj = obj.BaseObject;
            var newItems = new List<ResultItem>();

            // Première détection du type de sortie
            if (!context.TypeDetected)
            {
                context.TypeDetected = true;

                if (baseObj is Hashtable)
                {
                    context.DetectedType = "KeyValue";
                    newItems.AddRange(BuildKeyValues(obj));
                    return newItems;
                }

                if (baseObj is string text)
                {
                    context.DetectedType = "Log";
                    context.CurrentLog = new LogResult { RawText = text };
                    newItems.Add(context.CurrentLog);
                    return newItems;
                }

                // PSCustomObject avec propriétés → Table streaming
                context.DetectedType = "Table";
                var dt = new DataTable();
                context.TableColumns = new List<string>();

                foreach (var prop in obj.Properties)
                {
                    context.TableColumns.Add(prop.Name);
                    dt.Columns.Add(prop.Name, typeof(string));
                }

                AddTableRow(dt, obj, context.TableColumns);

                context.CurrentTable = new TableResult
                {
                    LiveTable = dt,
                    Columns = new ObservableCollection<string>(context.TableColumns)
                };
                newItems.Add(context.CurrentTable);
                return newItems;
            }

            // Objets suivants — mise à jour d'un résultat existant
            switch (context.DetectedType)
            {
                case "Table" when context.CurrentTable?.LiveTable != null:
                    AddTableRow(context.CurrentTable.LiveTable, obj, context.TableColumns!);
                    break;

                case "Log" when context.CurrentLog != null:
                    context.CurrentLog.RawText += Environment.NewLine + (baseObj?.ToString() ?? string.Empty);
                    break;

                case "KeyValue":
                    newItems.AddRange(BuildKeyValues(obj));
                    break;
            }

            return newItems;
        }

        /// <summary>Convertit un PSObject en KeyValueResult(s).</summary>
        public List<KeyValueResult> BuildKeyValues(PSObject obj)
        {
            var items = new List<KeyValueResult>();
            var baseObj = obj.BaseObject;

            if (baseObj is Hashtable hashtable)
            {
                foreach (DictionaryEntry entry in hashtable)
                {
                    items.Add(new KeyValueResult
                    {
                        Label = entry.Key?.ToString() ?? string.Empty,
                        Value = entry.Value?.ToString() ?? string.Empty
                    });
                }
                return items;
            }

            var props = obj.Properties.ToList();
            if (props.Count > 0)
            {
                foreach (var prop in props)
                {
                    items.Add(new KeyValueResult
                    {
                        Label = prop.Name,
                        Value = prop.Value?.ToString() ?? string.Empty
                    });
                }
                return items;
            }

            items.Add(new KeyValueResult
            {
                Label = "Résultat",
                Value = obj.ToString() ?? string.Empty
            });

            return items;
        }

        private static void AddTableRow(DataTable dt, PSObject obj, List<string> columns)
        {
            var row = dt.NewRow();
            for (int i = 0; i < columns.Count; i++)
            {
                var prop = obj.Properties[columns[i]];
                row[i] = prop?.Value?.ToString() ?? string.Empty;
            }
            dt.Rows.Add(row);
        }
    }
}
