using MauiApp1.ViewModels;
using CommunityToolkit.Maui.Markup;
using System.Diagnostics;
using MauiApp1.StaticHelpers;

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

        viewModel.TypeEntry = null;
        viewModel.CurrencyEntry = null;
        viewModel.AgentNameEntry = null;
        viewModel.AgentFeeEntry = null;

        viewModel.WelcomeTextVisible = SettingsManager.FileExists ? false : true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.SaveSettings();
    }
}