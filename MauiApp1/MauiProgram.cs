using MauiApp1.ViewModels;
using MauiApp1.Views;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui;
using Microcharts.Maui;
using MauiApp1.Models;

namespace MauiApp1
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMarkup()
                .UseMicrocharts()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<ProjectsPage>();
            builder.Services.AddSingleton<ProjectsOverviewViewModel>();
            builder.Services.AddSingleton<SettingsPage>();
            builder.Services.AddSingleton<SettingsViewModel>();
            builder.Services.AddSingleton<PaymentsOverviewViewModel>();
            builder.Services.AddSingleton<PaymentsOverviewPage>();
            builder.Services.AddSingleton<SpendingOverviewViewModel>();
            builder.Services.AddSingleton<SpendingOverviewPage>();
            builder.Services.AddSingleton<ProjectJsonIOManager>();
            builder.Services.AddSingleton<SettingsJsonIOManager>();
            builder.Services.AddSingleton<SpendingDataJsonIOManager>();

            builder.Services.AddTransient<ProjectDetailsPage>();
            builder.Services.AddTransient<ProjectDetailsViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}