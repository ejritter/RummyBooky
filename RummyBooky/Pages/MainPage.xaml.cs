using System;
using Microsoft.Maui.Controls;
using RummyBooky.Extensions;
using RummyBooky.ViewModels;

namespace RummyBooky.Pages;

public partial class MainPage : BasePage<MainPageViewModel>, IQueryAttributable
{
    public MainPage(MainPageViewModel vm) : base(vm)
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is MainPageViewModel vm && vm.AppearingCommand.CanExecute(null))
        {
            _ = vm.AppearingCommand.ExecuteAsync(null);
        }
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (BindingContext is MainPageViewModel vm && vm.AppearingCommand.CanExecute(null))
        {
            await vm.AppearingCommand.ExecuteAsync(null);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MainPageViewModel vm && vm.AppearingCommand.CanExecute(null))
        {
            await vm.AppearingCommand.ExecuteAsync(null);
        }
    }

    private async void OnLogoTapped(object? sender, TappedEventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
        if (BindingContext is MainPageViewModel vm && vm.MuteUnmuteGamblerCommand.CanExecute(null))
        {
            _ = vm.MuteUnmuteGamblerCommand.ExecuteAsync(null);
        }
    }

    private async void OnNewGameClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }

    private async void OnLeaderboardClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }

    private async void OnResumeGameClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.AnimatePressAsync();
        }
    }
}