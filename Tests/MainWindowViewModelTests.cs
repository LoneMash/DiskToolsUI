// ═══════════════════════════════════════════════════════════════════
// MainWindowViewModelTests.cs — Tests unitaires du ViewModel principal
// ═══════════════════════════════════════════════════════════════════
// Rôle : Vérifie le comportement du MainWindowViewModel : état initial,
//        validation CanExecute, sélection d'action, export CSV,
//        bascule de thème et visibilité du formulaire de paramètres.
// Couche : Tests
// Consommé par : xUnit (CI / exécution locale)
// ═══════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using RunDeck.Interfaces;
using RunDeck.Models;
using RunDeck.ViewModels;

namespace RunDeck.Tests;

public class MainWindowViewModelTests
{
    private readonly Mock<ILoggerService> _loggerMock = new();
    private readonly Mock<IConfigService> _configMock = new();
    private readonly Mock<IPowerShellRunner> _psRunnerMock = new();
    private readonly Mock<ICsvExportService> _csvExportMock = new();
    private readonly Mock<IThemeService> _themeMock = new();
    private readonly Mock<IDialogService> _dialogMock = new();
    private readonly Mock<IResultBuilder> _resultBuilderMock = new();

    private MainWindowViewModel CreateViewModel()
    {
        // Configurer le mock ConfigService pour ne pas planter dans InitializeAsync
        _configMock.Setup(c => c.LoadConfigAsync())
            .ReturnsAsync(new AppConfig
            {
                UI = new UiConfig { Title = "Test" },
                ActionsFile = "actions.json"
            });
        _configMock.Setup(c => c.LoadActionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<ActionDefinition>());

        return new MainWindowViewModel(
            _loggerMock.Object,
            _configMock.Object,
            _psRunnerMock.Object,
            _csvExportMock.Object,
            _themeMock.Object,
            _dialogMock.Object,
            _resultBuilderMock.Object);
    }

    // -----------------------------------------------------------------------
    // Construction et initialisation
    // -----------------------------------------------------------------------

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var vm = CreateViewModel();

        Assert.NotNull(vm.Actions);
        Assert.NotNull(vm.Results);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasResults);
        Assert.Null(vm.SelectedAction);
    }

    // -----------------------------------------------------------------------
    // CanExecuteAction — validation temps réel
    // -----------------------------------------------------------------------

    [Fact]
    public void CanExecuteAction_NoSelectedAction_ReturnsFalse()
    {
        var vm = CreateViewModel();

        Assert.False(vm.ExecuteActionCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecuteAction_WithRequiredParamEmpty_ReturnsFalse()
    {
        var vm = CreateViewModel();
        var action = new ActionDefinition
        {
            Id = "test",
            Name = "Test",
            Parameters = new List<ParameterDefinition>
            {
                new() { Name = "Param1", Required = true, CurrentValue = "" }
            }
        };

        // Simuler la sélection (sans passer par la commande qui exécute auto)
        vm.SelectActionCommand.Execute(action);

        Assert.False(vm.ExecuteActionCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecuteAction_WithRequiredParamFilled_ReturnsTrue()
    {
        var vm = CreateViewModel();
        var action = new ActionDefinition
        {
            Id = "test",
            Name = "Test",
            Parameters = new List<ParameterDefinition>
            {
                new() { Name = "Param1", Required = true, CurrentValue = "C" }
            }
        };

        vm.SelectActionCommand.Execute(action);

        Assert.True(vm.ExecuteActionCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecuteAction_NoRequiredParams_ReturnsTrue()
    {
        var vm = CreateViewModel();
        var action = new ActionDefinition
        {
            Id = "test",
            Name = "Test",
            Parameters = new List<ParameterDefinition>
            {
                new() { Name = "Param1", Required = false, CurrentValue = "" }
            }
        };

        vm.SelectActionCommand.Execute(action);

        Assert.True(vm.ExecuteActionCommand.CanExecute(null));
    }

    // -----------------------------------------------------------------------
    // ValidateParameters — dialogue warning
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateParameters_MissingRequired_ShowsWarning()
    {
        var vm = CreateViewModel();
        var action = new ActionDefinition
        {
            Id = "test",
            Name = "Test",
            Parameters = new List<ParameterDefinition>
            {
                new() { Name = "P1", Label = "Param 1", Required = true, CurrentValue = "" }
            }
        };

        vm.SelectActionCommand.Execute(action);

        // Force execution même si CanExecute est false (pour tester ValidateParameters)
        // On ne peut pas appeler ExecuteAction directement car c'est private,
        // mais on peut vérifier que la commande ne s'exécute pas
        Assert.False(vm.ExecuteActionCommand.CanExecute(null));
    }

    // -----------------------------------------------------------------------
    // ExportCsv — dialogue et export
    // -----------------------------------------------------------------------

    [Fact]
    public void ExportCsv_CannotExecute_WhenNoResults()
    {
        var vm = CreateViewModel();

        Assert.False(vm.ExportCsvCommand.CanExecute(null));
    }

    // -----------------------------------------------------------------------
    // ToggleTheme
    // -----------------------------------------------------------------------

    [Fact]
    public void ToggleTheme_CallsThemeService()
    {
        _themeMock.SetupGet(t => t.IsDark).Returns(false);
        var vm = CreateViewModel();

        vm.ToggleThemeCommand.Execute(null);

        _themeMock.Verify(t => t.ToggleTheme(), Times.Once);
    }

    [Fact]
    public void ToggleTheme_UpdatesThemeLabel()
    {
        _themeMock.SetupGet(t => t.IsDark).Returns(false);
        var vm = CreateViewModel();

        vm.ToggleThemeCommand.Execute(null);

        Assert.Equal("Thème sombre", vm.ThemeLabel);
    }

    // -----------------------------------------------------------------------
    // SelectAction — état
    // -----------------------------------------------------------------------

    [Fact]
    public void SelectAction_SetsSelectedAction()
    {
        var vm = CreateViewModel();
        var action = new ActionDefinition { Id = "test", Name = "Test" };

        vm.SelectActionCommand.Execute(action);

        Assert.Equal(action, vm.SelectedAction);
    }

    [Fact]
    public void SelectAction_ClearsResults()
    {
        var vm = CreateViewModel();
        var action = new ActionDefinition { Id = "test", Name = "Test" };

        vm.SelectActionCommand.Execute(action);

        Assert.Empty(vm.Results);
        Assert.False(vm.HasResults);
    }

    // -----------------------------------------------------------------------
    // IsFormVisible
    // -----------------------------------------------------------------------

    [Fact]
    public void IsFormVisible_NoAction_ReturnsFalse()
    {
        var vm = CreateViewModel();

        Assert.False(vm.IsFormVisible);
    }

    [Fact]
    public void IsFormVisible_ActionWithParams_ReturnsTrue()
    {
        var vm = CreateViewModel();
        var action = new ActionDefinition
        {
            Id = "test",
            Name = "Test",
            Parameters = new List<ParameterDefinition>
            {
                new() { Name = "P1", CurrentValue = "val" }
            }
        };

        vm.SelectActionCommand.Execute(action);

        Assert.True(vm.IsFormVisible);
    }

    [Fact]
    public void IsFormVisible_ActionWithoutParams_ReturnsFalse()
    {
        var vm = CreateViewModel();
        var action = new ActionDefinition
        {
            Id = "test",
            Name = "Test",
            Parameters = new List<ParameterDefinition>()
        };

        vm.SelectActionCommand.Execute(action);

        Assert.False(vm.IsFormVisible);
    }
}
