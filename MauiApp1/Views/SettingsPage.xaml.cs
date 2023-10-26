using MauiApp1.ViewModels;
using CommunityToolkit.Maui.Markup;
using System.Diagnostics;

namespace MauiApp1.Views;

public partial class SettingsPage : ContentPage
{
    private SettingsViewModel viewModel;

    //Constructor called on app startup if settings.json file is not found
    public SettingsPage(SettingsViewModel vm, bool welcomeTextVisible)
    {
        BindingContext = vm;
        viewModel = vm;
        vm.WelcomeTextVisible = welcomeTextVisible;
        InitializeComponent();
    }

    public SettingsPage(SettingsViewModel vm)
    {
        BindingContext = vm;
        viewModel = vm;
        vm.WelcomeTextVisible = false;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        viewModel.TypeEntry = null;
        viewModel.CurrencyEntry = null;
        viewModel.AgentNameEntry = null;
        viewModel.AgentFeeEntry = null;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (!viewModel.WelcomeTextVisible)
            viewModel.SaveSettingsCommand.Execute(null);
    }
}