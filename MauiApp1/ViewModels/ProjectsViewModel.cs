using System.Collections.ObjectModel;
using System.Windows.Input;
using MauiApp1.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiApp1.Views;
using System.Globalization;

namespace MauiApp1.ViewModels;
public partial class ProjectsViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinishedProjects))]
    [NotifyPropertyChangedFor(nameof(InvoicedProjects))]
    [NotifyPropertyChangedFor(nameof(CancelledProjects))]
    [NotifyPropertyChangedFor(nameof(ActiveProjects))]
    [NotifyPropertyChangedFor(nameof(AwaitedPaymentsTotal))]
    ObservableCollection<ProjectViewModel> projects;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditProjectCommand))]
    ProjectViewModel selectedProjectVM;

    public List<ProjectViewModel> FinishedProjects => Projects.Where(x => x.Status == Project.ProjectStatus.Finished).ToList();
    public List<ProjectViewModel> InvoicedProjects => Projects.Where(x => x.Status == Project.ProjectStatus.Invoiced).ToList();
    public List<ProjectViewModel> CancelledProjects => Projects.Where(x => x.Status == Project.ProjectStatus.Cancelled).ToList();
    public List<ProjectViewModel> ActiveProjects => Projects.Where(x => x.Status == Project.ProjectStatus.Active).ToList();
    public decimal AwaitedPaymentsTotal => InvoicedProjects.Select(x => x.Fee).Sum() - (InvoicedProjects.Select(x => x.Payments.Select(x => x.Amount).Sum()).Sum());

    public ProjectsViewModel()
    {
        var pr = ProjectManager.LoadProjects();
        Projects = new(pr.Select(x => new ProjectViewModel(x)));
    }

    //Method called on page resize
    //Dirty fix to make projects collectionview grid correctly scale with window downsizing
    //Without this the grid only correctly scales when upsizing window (.NET MAUI bug?)
    public async Task ReloadProjects()
    {
        Projects.Clear();
        await Task.Delay(1);

        PaymentManager.AllPayments.Clear();
        Projects = new(ProjectManager.LoadProjects().Select(x => new ProjectViewModel(x)));
    }

    [RelayCommand]
    async Task GoToNewProjectPage()
    {
        await Shell.Current.GoToAsync($"{nameof(ProjectDetailsPage)}?",
            new Dictionary<string, object>
            {
                ["projects"] = Projects
            });
    }

    [RelayCommand(CanExecute = nameof(CanDeleteOrEditProject))]
    public async Task DeleteProject()
    {
        bool confirmed = await DisplayConfirmationDialog("Confirm Deletion", "Are you sure you want to delete this project?");
        if (confirmed)
        {
            ProjectManager.AllProjects.Remove(SelectedProjectVM.Project);
            Projects.Remove(SelectedProjectVM);
            ProjectManager.SaveProjects(Projects.Select(x => x.Project).ToList());
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteOrEditProject))]
    public async Task EditProject()
    {
        await Shell.Current.GoToAsync($"{nameof(ProjectDetailsPage)}?",
            new Dictionary<string, object>
            {
                ["selectedProjectVM"] = SelectedProjectVM,
                ["projects"] = Projects
            });
    }

    private async Task<bool> DisplayConfirmationDialog(string title, string message)
    {
        return await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
    }

    private bool CanDeleteOrEditProject()
    {
        return SelectedProjectVM != null;
    }
}
