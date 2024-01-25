using MauiApp1.ViewModels;
using System.Diagnostics;

namespace MauiApp1.Views;

public partial class ProjectsPage : ContentPage
{
    private readonly ProjectsViewModel viewModel;

    public ProjectsPage(ProjectsViewModel vm)
    {
        viewModel = vm;
        BindingContext = vm;
        SizeChanged += OnPageSizeChanged;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine(viewModel.Projects.First().Profit);
        Debug.WriteLine(viewModel.Projects.First().IsOnGoing);
    }

    private async void OnPageSizeChanged(object sender, EventArgs e)
    {
        await viewModel.ReloadProjects();
    }
}