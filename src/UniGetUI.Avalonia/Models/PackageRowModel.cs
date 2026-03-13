using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using UniGetUI.Avalonia.Infrastructure;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.Avalonia.Models;

internal sealed class PackageRowModel : INotifyPropertyChanged, IDisposable
{
    private readonly PackagePageMode _pageMode;
    private readonly AsyncCommand _primaryActionCommand;

    public PackageRowModel(
        IPackage package,
        bool showNewVersionColumn,
        PackagePageMode pageMode,
        Func<IPackage, Task> runPrimaryActionAsync
    )
    {
        Package = package;
        _pageMode = pageMode;
        Name = package.Name;
        Id = package.Id;
        Version = string.IsNullOrWhiteSpace(package.VersionString) ? "-" : package.VersionString;
        Status = showNewVersionColumn
            ? (string.IsNullOrWhiteSpace(package.NewVersionString) ? "-" : package.NewVersionString)
            : package.Manager.DisplayName;
        Source = package.Source.AsString_DisplayName;

        _primaryActionCommand = new AsyncCommand(
            () => runPrimaryActionAsync(Package),
            () => CanExecutePrimaryAction
        );

        Package.PropertyChanged += Package_OnPropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IPackage Package { get; }

    public string Name { get; }

    public string Id { get; }

    public string Version { get; }

    public string Status { get; }

    public string Source { get; }

    public ICommand PrimaryActionCommand => _primaryActionCommand;

    public string PrimaryActionLabel => GetPrimaryActionLabel();

    public bool CanExecutePrimaryAction => CanExecutePrimaryActionInternal();

    private void Package_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.PropertyName) && e.PropertyName != nameof(IPackage.Tag))
        {
            return;
        }

        OnPropertyChanged(nameof(PrimaryActionLabel));
        OnPropertyChanged(nameof(CanExecutePrimaryAction));
        _primaryActionCommand.RaiseCanExecuteChanged();
    }

    private string GetPrimaryActionLabel()
    {
        return Package.Tag switch
        {
            PackageTag.OnQueue => "Queued",
            PackageTag.BeingProcessed => _pageMode switch
            {
                PackagePageMode.Discover => "Installing",
                PackagePageMode.Updates => "Updating",
                PackagePageMode.Installed => "Removing",
                _ => "Working",
            },
            PackageTag.AlreadyInstalled when _pageMode == PackagePageMode.Discover => "Installed",
            PackageTag.Failed => "Retry",
            _ => _pageMode switch
            {
                PackagePageMode.Discover => "Install",
                PackagePageMode.Updates => "Update",
                PackagePageMode.Installed => "Uninstall",
                _ => "Open",
            },
        };
    }

    private bool CanExecutePrimaryActionInternal()
    {
        return _pageMode switch
        {
            PackagePageMode.Discover => Package.Tag is not PackageTag.AlreadyInstalled
                and not PackageTag.OnQueue
                and not PackageTag.BeingProcessed
                and not PackageTag.Unavailable,
            PackagePageMode.Updates => Package.Tag is not PackageTag.OnQueue
                and not PackageTag.BeingProcessed
                and not PackageTag.Unavailable,
            PackagePageMode.Installed => Package.Tag is not PackageTag.OnQueue
                and not PackageTag.BeingProcessed
                and not PackageTag.Unavailable,
            _ => false,
        };
    }

    public void Dispose()
    {
        Package.PropertyChanged -= Package_OnPropertyChanged;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}