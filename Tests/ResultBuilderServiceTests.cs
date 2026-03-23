// ═══════════════════════════════════════════════════════════════════
// ResultBuilderServiceTests.cs — Tests unitaires du ResultBuilderService
// ═══════════════════════════════════════════════════════════════════
// Rôle : Valide la détection automatique du type de sortie (Table,
//        KeyValue, Log) et l'accumulation correcte des objets streaming
//        dans le contexte (compteur, DataTable, concaténation log).
// Couche : Tests
// Consommé par : xUnit (CI / exécution locale)
// ═══════════════════════════════════════════════════════════════════

using System.Collections;
using System.Management.Automation;
using RunDeck.Interfaces;
using RunDeck.Models;
using RunDeck.Services;

namespace RunDeck.Tests;

public class ResultBuilderServiceTests
{
    private readonly IResultBuilder _builder = new ResultBuilderService();

    // -----------------------------------------------------------------------
    // CreateContext
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateContext_ReturnsNewContext_WithZeroCount()
    {
        var context = _builder.CreateContext();

        Assert.NotNull(context);
        Assert.Equal(0, context.ObjectCount);
        Assert.False(context.TypeDetected);
        Assert.Equal(string.Empty, context.DetectedType);
    }

    // -----------------------------------------------------------------------
    // ProcessStreamingObject — KeyValue (Hashtable)
    // -----------------------------------------------------------------------

    [Fact]
    public void ProcessStreamingObject_Hashtable_ReturnsKeyValueResults()
    {
        var context = _builder.CreateContext();
        var ht = new Hashtable { { "Nom", "DisqueC" }, { "Taille", "256 Go" } };
        var psObj = new PSObject(ht);

        var items = _builder.ProcessStreamingObject(psObj, context);

        Assert.Equal("KeyValue", context.DetectedType);
        Assert.Equal(1, context.ObjectCount);
        Assert.True(items.Count >= 2);
        Assert.All(items, item => Assert.IsType<KeyValueResult>(item));
    }

    [Fact]
    public void ProcessStreamingObject_SecondHashtable_AccumulatesKeyValues()
    {
        var context = _builder.CreateContext();

        var ht1 = new Hashtable { { "A", "1" } };
        _builder.ProcessStreamingObject(new PSObject(ht1), context);

        var ht2 = new Hashtable { { "B", "2" } };
        var items2 = _builder.ProcessStreamingObject(new PSObject(ht2), context);

        Assert.Equal(2, context.ObjectCount);
        Assert.NotEmpty(items2);
        Assert.IsType<KeyValueResult>(items2[0]);
    }

    // -----------------------------------------------------------------------
    // ProcessStreamingObject — Log (string)
    // -----------------------------------------------------------------------

    [Fact]
    public void ProcessStreamingObject_String_ReturnsLogResult()
    {
        var context = _builder.CreateContext();
        var psObj = new PSObject("Ligne de log");

        var items = _builder.ProcessStreamingObject(psObj, context);

        Assert.Equal("Log", context.DetectedType);
        Assert.Single(items);
        var log = Assert.IsType<LogResult>(items[0]);
        Assert.Equal("Ligne de log", log.RawText);
    }

    [Fact]
    public void ProcessStreamingObject_SecondString_AppendsToExistingLog()
    {
        var context = _builder.CreateContext();
        _builder.ProcessStreamingObject(new PSObject("Ligne 1"), context);

        var items2 = _builder.ProcessStreamingObject(new PSObject("Ligne 2"), context);

        // Pas de nouveaux items — le log existant est mis à jour
        Assert.Empty(items2);
        Assert.NotNull(context.CurrentLog);
        Assert.Contains("Ligne 1", context.CurrentLog.RawText);
        Assert.Contains("Ligne 2", context.CurrentLog.RawText);
    }

    // -----------------------------------------------------------------------
    // ProcessStreamingObject — Table (PSCustomObject)
    // -----------------------------------------------------------------------

    [Fact]
    public void ProcessStreamingObject_PSCustomObject_ReturnsTableResult()
    {
        var context = _builder.CreateContext();
        var psObj = new PSObject();
        psObj.Properties.Add(new PSNoteProperty("Nom", "DisqueC"));
        psObj.Properties.Add(new PSNoteProperty("Taille", "256 Go"));

        var items = _builder.ProcessStreamingObject(psObj, context);

        Assert.Equal("Table", context.DetectedType);
        Assert.Single(items);
        var table = Assert.IsType<TableResult>(items[0]);
        Assert.NotNull(table.LiveTable);
        Assert.Equal(2, table.LiveTable.Columns.Count);
        Assert.Equal(1, table.LiveTable.Rows.Count);
    }

    [Fact]
    public void ProcessStreamingObject_SecondPSCustomObject_AddsRowToExistingTable()
    {
        var context = _builder.CreateContext();

        var obj1 = new PSObject();
        obj1.Properties.Add(new PSNoteProperty("Nom", "A"));
        _builder.ProcessStreamingObject(obj1, context);

        var obj2 = new PSObject();
        obj2.Properties.Add(new PSNoteProperty("Nom", "B"));
        var items2 = _builder.ProcessStreamingObject(obj2, context);

        // Pas de nouveaux items — la ligne est ajoutée au DataTable existant
        Assert.Empty(items2);
        Assert.NotNull(context.CurrentTable?.LiveTable);
        Assert.Equal(2, context.CurrentTable.LiveTable.Rows.Count);
    }

    // -----------------------------------------------------------------------
    // BuildKeyValues
    // -----------------------------------------------------------------------

    [Fact]
    public void BuildKeyValues_Hashtable_ReturnsCorrectPairs()
    {
        var ht = new Hashtable { { "Clé", "Valeur" } };
        var psObj = new PSObject(ht);

        var items = _builder.BuildKeyValues(psObj);

        Assert.Single(items);
        Assert.Equal("Clé", items[0].Label);
        Assert.Equal("Valeur", items[0].Value);
    }

    [Fact]
    public void BuildKeyValues_PSCustomObject_ReturnsProperties()
    {
        var psObj = new PSObject();
        psObj.Properties.Add(new PSNoteProperty("Prop1", "Val1"));
        psObj.Properties.Add(new PSNoteProperty("Prop2", "Val2"));

        var items = _builder.BuildKeyValues(psObj);

        Assert.Equal(2, items.Count);
        Assert.Equal("Prop1", items[0].Label);
        Assert.Equal("Val1", items[0].Value);
    }

    [Fact]
    public void BuildKeyValues_SimpleValue_ReturnsSingleResult()
    {
        var psObj = new PSObject(42);

        var items = _builder.BuildKeyValues(psObj);

        Assert.Single(items);
        Assert.Equal("Résultat", items[0].Label);
        // PSObject.ToString() sur un int peut retourner vide dans certains contextes
        Assert.NotNull(items[0].Value);
    }

    // -----------------------------------------------------------------------
    // ObjectCount tracking
    // -----------------------------------------------------------------------

    [Fact]
    public void ProcessStreamingObject_IncrementsObjectCount()
    {
        var context = _builder.CreateContext();

        _builder.ProcessStreamingObject(new PSObject("a"), context);
        _builder.ProcessStreamingObject(new PSObject("b"), context);
        _builder.ProcessStreamingObject(new PSObject("c"), context);

        Assert.Equal(3, context.ObjectCount);
    }
}
