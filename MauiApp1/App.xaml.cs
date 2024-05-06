using MauiApp1.Models;

namespace MauiApp1
{
    public partial class App : Application
    {
        public App(SettingsJsonIOManager settings)
        {
            InitializeComponent();
            MainPage = new AppShell();
            if (!settings.FileExists)
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