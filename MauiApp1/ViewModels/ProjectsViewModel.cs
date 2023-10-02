using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MauiApp1.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiApp1.Views;

namespace MauiApp1.ViewModels;

public partial class ProjectsViewModel : ObservableObject
{
    public ICommand DeleteProjectCommand { get; }
    public ICommand EditProjectCommand { get; }

    [ObservableProperty]
    ObservableCollection<Project> projects;

    private Project _selectedProject;

    public ProjectsViewModel()
    {
        Projects = new(ProjectManager.LoadProjects());
        DeleteProjectCommand = new Command(DeleteProject, CanDeleteOrEditProject);
        EditProjectCommand = new Command(EditProject, CanDeleteOrEditProject);
    }

    public Project SelectedProject
    {
        get => _selectedProject;
        set
        {
            _selectedProject = value;
            ((Command)DeleteProjectCommand).ChangeCanExecute();
            ((Command)EditProjectCommand).ChangeCanExecute();
        }
    }

    //Function called on page resize
    //Dirty fix to make projects collectionview grid correctly scale with window downsizing
    //Without this the grid only correctly scales when upsizing window (.NET MAUI bug?)
    public async Task ReloadProjects()
    {
        Projects.Clear();
        OnPropertyChanged(nameof(Projects));
        await Task.Delay(1);

        Projects = new(ProjectManager.LoadProjects());
    }

    [RelayCommand]
    async Task GoToNewProjectPage()
    {
        await Shell.Current.GoToAsync($"{nameof(NewProjectPage)}?",
            new Dictionary<string, object>
            {
                ["projects"] = Projects
            });
    }

    public async void DeleteProject()
    {
        bool confirmed = await DisplayConfirmationDialog("Confirm Deletion", "Are you sure you want to delete this project?");
        if (confirmed)
        {
            Projects.Remove(SelectedProject);
            ProjectManager.SaveProjects(Projects);
        }
    }

    private async Task<bool> DisplayConfirmationDialog(string title, string message)
    {
        return await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
    }

    public void EditProject()
    {

    }

    private bool CanDeleteOrEditProject()
    {
        return _selectedProject != null;
    }
}
