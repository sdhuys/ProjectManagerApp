using MauiApp1.Models;
using MauiApp1.ViewModels;
using MauiApp1.Views;

namespace MauiApp1
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            if (Settings.AreSet)
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