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
    ObservableCollection<Project> projects;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditProjectCommand))]
    private Project selectedProject;

    public ProjectsViewModel()
    {
        Projects = new(ProjectManager.LoadProjects());
    }

    //Method called on page resize
    //Dirty fix to make projects collectionview grid correctly scale with window downsizing
    //Without this the grid only correctly scales when upsizing window (.NET MAUI bug?)
    public async Task ReloadProjects()
    {
        Projects.Clear();
        await Task.Delay(1);

        Projects = new(ProjectManager.LoadProjects());
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
            Projects.Remove(SelectedProject);
            ProjectManager.SaveProjects(Projects);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteOrEditProject))]
    public async Task EditProject()
    {
        await Shell.Current.GoToAsync($"{nameof(ProjectDetailsPage)}?",
            new Dictionary<string, object>
            {
                ["selectedProject"] = SelectedProject,
                ["projects"] = Projects
            });
    }

    private async Task<bool> DisplayConfirmationDialog(string title, string message)
    {
        return await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
    }

    private bool CanDeleteOrEditProject()
    {
        return SelectedProject != null;
    }
}
