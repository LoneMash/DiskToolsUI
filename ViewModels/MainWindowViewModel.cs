using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Management;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DiskToolsUi.Models;

namespace DiskToolsUi.ViewModels
{
    // VERSION 1.30 + Icône
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _windowTitle = "Disk Tools UI";
        private double _windowWidth = 900;
        private double _windowHeight = 600;
        private string _driveLetter = "C:";
        private bool _isLoading = false;

        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        public double WindowWidth
        {
            get => _windowWidth;
            set { _windowWidth = value; OnPropertyChanged(); }
        }

        public double WindowHeight
        {
            get => _windowHeight;
            set { _windowHeight = value; OnPropertyChanged(); }
        }

        public string DriveLetter
        {
            get => _driveLetter;
            set { _driveLetter = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ActionItem> Actions { get; set; }
        public ObservableCollection<ResultItem> Results { get; set; }

        public MainWindowViewModel()
        {
            Actions = new ObservableCollection<ActionItem>
            {
                new ActionItem
                {
                    Name = "📀 Infos Disque",
                    Command = new RelayCommand(async () => await GetDiskInfoAsync())
                },
                new ActionItem
                {
                    Name = "🔢 Numéro de Série",
                    Command = new RelayCommand(async () => await GetSerialNumberAsync())
                },
                new ActionItem
                {
                    Name = "🗂️ Système de Fichiers",
                    Command = new RelayCommand(async () => await GetFileSystemAsync())
                },
                new ActionItem
                {
                    Name = "🧹 Effacer Résultats",
                    Command = new RelayCommand(ClearResults)
                }
            };

            Results = new ObservableCollection<ResultItem>();
            
            LoadConfiguration();
        }

        private async Task GetDiskInfoAsync()
        {
            IsLoading = true;
            Results.Clear();

            try
            {
                await Task.Run(() =>
                {
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID='{DriveLetter}'");

                    foreach (ManagementObject disk in searcher.Get())
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Results.Add(new ResultItem
                            {
                                Label = "Lecteur",
                                Value = disk["DeviceID"]?.ToString() ?? "N/A"
                            });
                            Results.Add(new ResultItem
                            {
                                Label = "Nom du volume",
                                Value = disk["VolumeName"]?.ToString() ?? "N/A"
                            });
                            Results.Add(new ResultItem
                            {
                                Label = "Système de fichiers",
                                Value = disk["FileSystem"]?.ToString() ?? "N/A"
                            });

                            var size = disk["Size"] != null
                                ? $"{Convert.ToDouble(disk["Size"]) / (1024 * 1024 * 1024):F2} Go"
                                : "N/A";
                            Results.Add(new ResultItem { Label = "Capacité totale", Value = size });

                            var freeSpace = disk["FreeSpace"] != null
                                ? $"{Convert.ToDouble(disk["FreeSpace"]) / (1024 * 1024 * 1024):F2} Go"
                                : "N/A";
                            Results.Add(new ResultItem { Label = "Espace libre", Value = freeSpace });
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Results.Add(new ResultItem
                    {
                        Label = "Erreur",
                        Value = ex.Message
                    });
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GetSerialNumberAsync()
        {
            IsLoading = true;
            Results.Clear();

            try
            {
                await Task.Run(() =>
                {
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID='{DriveLetter}'");

                    foreach (ManagementObject disk in searcher.Get())
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Results.Add(new ResultItem
                            {
                                Label = "Numéro de série",
                                Value = disk["VolumeSerialNumber"]?.ToString() ?? "N/A"
                            });
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Results.Add(new ResultItem
                    {
                        Label = "Erreur",
                        Value = ex.Message
                    });
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GetFileSystemAsync()
        {
            IsLoading = true;
            Results.Clear();

            try
            {
                await Task.Run(() =>
                {
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID='{DriveLetter}'");

                    foreach (ManagementObject disk in searcher.Get())
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Results.Add(new ResultItem
                            {
                                Label = "Système de fichiers",
                                Value = disk["FileSystem"]?.ToString() ?? "N/A"
                            });
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Results.Add(new ResultItem
                    {
                        Label = "Erreur",
                        Value = ex.Message
                    });
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearResults()
        {
            Results.Clear();
        }

        private void LoadConfiguration()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    
                    // Parse simple du JSON
                    if (json.Contains("WindowTitle"))
                    {
                        var titleStart = json.IndexOf("\"WindowTitle\"") + 16;
                        var titleEnd = json.IndexOf("\"", titleStart);
                        if (titleEnd > titleStart)
                        {
                            WindowTitle = json.Substring(titleStart, titleEnd - titleStart);
                        }
                    }
                    
                    if (json.Contains("DefaultDriveLetter"))
                    {
                        var driveStart = json.IndexOf("\"DefaultDriveLetter\"") + 23;
                        var driveEnd = json.IndexOf("\"", driveStart);
                        if (driveEnd > driveStart)
                        {
                            DriveLetter = json.Substring(driveStart, driveEnd - driveStart);
                        }
                    }
                }
            }
            catch
            {
                // Valeurs par défaut si échec
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
