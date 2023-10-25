using MauiApp1.ViewModels;

namespace MauiApp1.Views;
public partial class ProjectDetailsPage : ContentPage
{
    public ProjectDetailsPage(ProjectDetailsViewModel vm)
    {
        BindingContext = vm;
        InitializeComponent();
    }
}