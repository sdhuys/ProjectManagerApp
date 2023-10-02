using MauiApp1.ViewModels;

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

        viewModel.CopySettingsFromModel();
        viewModel.TypeEntry = null;
        viewModel.CurrencyEntry = null;
        viewModel.AgentNameEntry = null;
        viewModel.AgentFeeEntry = null;
    }
}