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
            MainPage = new AppShell();
            if (!SettingsManager.FileExists)
            {
                Shell.Current.CurrentItem = Shell.Current.Items.ElementAt(3);
            }
            else
            {
                Shell.Current.FlyoutBehavior = FlyoutBehavior.Locked;
            }
        }
    }
}