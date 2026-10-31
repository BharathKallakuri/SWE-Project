using System;
using Microsoft.UI.Xaml.Controls;

namespace Incident_module.ViewModels.Utils;

public interface INavigationService
{
    void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
    void GoBack();
    bool CanGoBack { get; }
}

public class NavigationService : INavigationService
{
    private Frame? _frame;

    public void Initialize(Frame frame)
    {
        _frame = frame ?? throw new ArgumentNullException(nameof(frame));
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("NavigationService is not initialized with a Frame.");
        }

        Type pageType = typeof(TViewModel).Name switch {
            nameof(AllIncidentsPageViewModel) => typeof(Views.AllIncidentsPage),
            nameof(CreateIncidentPageViewModel) => typeof(Views.CreateIncidentPage),
            _ => throw new ArgumentException($"No matching Page found for ViewModel: {typeof(TViewModel).Name}")
        };

        _frame.Navigate(pageType, parameter);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
        {
            _frame.GoBack();
        }
    }
}
