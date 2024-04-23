using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class SettingsPage : ContentPage
{
    private SettingsViewModel viewModel;

    public SettingsPage(SettingsViewModel vm)
    {
        BindingContext = vm;
        viewModel = vm;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.SaveSettings();
    }
}