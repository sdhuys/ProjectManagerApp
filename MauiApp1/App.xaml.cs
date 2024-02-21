using MauiApp1.StaticHelpers;
using MauiApp1.ViewModels;
using MauiApp1.Views;

namespace MauiApp1
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            if (SettingsManager.FileExists)
            {
                MainPage = new AppShell();
            }

            else
            {
                SettingsViewModel vm = new();
                MainPage = new NavigationPage(new SettingsPage(vm, true));
            }

        }

        public void SetMainPageToAppShell()
        {
            MainPage = new AppShell();
        }
    }
}