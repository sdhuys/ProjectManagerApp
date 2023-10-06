using MauiApp1.ViewModels;
using CommunityToolkit.Maui.Markup;

namespace MauiApp1.Views;
public partial class ProjectDetailsPage : ContentPage
{
    public ProjectDetailsPage(ProjectDetailsViewModel vm)
    {
        BindingContext = vm;
        InitializeComponent();
    }
}