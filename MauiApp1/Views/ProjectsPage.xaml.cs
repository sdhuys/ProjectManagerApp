using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class ProjectsPage : ContentPage
{
    private readonly ProjectsOverviewViewModel viewModel;

    public ProjectsPage(ProjectsOverviewViewModel vm)
    {
        viewModel = vm;
        BindingContext = vm;
        SizeChanged += OnPageSizeChanged;
        InitializeComponent();
    }

    private async void OnPageSizeChanged(object sender, EventArgs e)
    {
        await viewModel.ReloadProjects();
    }
}