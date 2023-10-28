using MauiApp1.ViewModels;

namespace MauiApp1.Views;
public partial class ProjectDetailsPage : ContentPage
{
    ProjectDetailsViewModel viewModel;
    public ProjectDetailsPage(ProjectDetailsViewModel vm)
    {
        BindingContext = vm;
        viewModel = vm;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.OnPageAppearing();
    }
}