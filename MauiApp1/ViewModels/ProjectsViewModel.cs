using System.Collections.ObjectModel;
using MauiApp1.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiApp1.Views;
using MauiApp1.StaticHelpers;
using System.Diagnostics;

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
    ObservableCollection<ProjectViewModel> queriedProjects;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditProjectCommand))]
    ProjectViewModel selectedProjectVM;

    [ObservableProperty]
    string queryString;

    [ObservableProperty]
    bool isQueryStringEmpty;


    public List<ProjectViewModel> FinishedProjects => Projects.Where(x => x.Status == Project.ProjectStatus.Completed).ToList();
    public List<ProjectViewModel> InvoicedProjects => Projects.Where(x => x.Status == Project.ProjectStatus.Invoiced).ToList();
    public List<ProjectViewModel> CancelledProjects => Projects.Where(x => x.Status == Project.ProjectStatus.Cancelled).ToList();
    public List<ProjectViewModel> ActiveProjects => Projects.Where(x => x.Status == Project.ProjectStatus.Active).ToList();
    public decimal AwaitedPaymentsTotal => InvoicedProjects.Select(x => x.Fee).Sum() - (InvoicedProjects.Select(x => x.Payments.Select(x => x.Amount).Sum()).Sum());

    public string SortIndicatorText
    {
        get { return _sortAscending ? "▲" : "▼"; }
    }

    public int SortIndicatorColumn { get; set; }

    private bool _sortAscending;
    private string _sortProperty;
    public ProjectsViewModel()
    {
        var pr = ProjectManager.LoadProjects();
        Projects = new(pr.Select(x => new ProjectViewModel(x)).OrderBy(p => p.Date));
        QueriedProjects = new();
        IsQueryStringEmpty = true;
        _sortAscending = true;
        _sortProperty = "Date";
        SortIndicatorColumn = 3;
    }

    //Method called on page resize
    //Dirty fix to make projects collectionview grid correctly scale with window downsizing
    //Without this the grid only correctly scales when upsizing window (.NET MAUI bug?)
    public async Task ReloadProjects()
    {
        var temp = Projects.ToList();
        Projects.Clear();
        await Task.Delay(1);
        foreach (var project in temp)
        {
            Projects.Add(project);
        }
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
        bool confirmed = await DisplayConfirmationDialog("Confirm Deletion", $"Are you sure you want to delete this {SelectedProjectVM.Type} project for client {SelectedProjectVM.Client}?");
        if (confirmed)
        {
            ProjectManager.AllProjects.Remove(SelectedProjectVM.Project);
            Projects.Remove(SelectedProjectVM);
            ProjectManager.SaveProjects(Projects.Select(x => x.Project).ToList());
        }
    }

    [RelayCommand]
    public async Task RightClickDeleteProject(ProjectViewModel selected)
    {
        SelectedProjectVM = selected;
        await DeleteProject();
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

    [RelayCommand]
    public void SortProjects(string property)
    {
        // If clicking on current sorting property, change sorting mode
        if (_sortProperty == property)
        {
            _sortAscending = !_sortAscending;
        }

        // If changing sorting property, sort by ascending as default
        else
        {
            _sortAscending = true;
        }

        _sortProperty = property;

        List<ProjectViewModel> temp = new();
        switch (property)
        {
            case "Client":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.Client).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.Client).ToList();
                }
                SortIndicatorColumn = 0;
                break;

            case "Type":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.Type).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.Type).ToList();
                }
                SortIndicatorColumn = 1;
                break;

            case "Description":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.Description).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.Description).ToList();
                }
                SortIndicatorColumn = 2;
                break;

            case "Date":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.Date).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.Date).ToList();
                }
                SortIndicatorColumn = 3;
                break;

            case "Currency":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.Currency).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.Currency).ToList();
                }
                SortIndicatorColumn = 4;
                break;

            case "Fee":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.Currency).ThenBy(p => p.Fee).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.Currency).ThenByDescending(p => p.Fee).ToList();
                }
                SortIndicatorColumn = 5;
                break;

            case "VatRate":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.VatRateDecimal).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.VatRateDecimal).ToList();
                }
                SortIndicatorColumn = 6;
                break;

            case "TotalExpenses":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.Currency).ThenBy(p => p.TotalExpenses).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.Currency).ThenByDescending(p => p.TotalExpenses).ToList();
                }
                SortIndicatorColumn = 7;
                break;

            case "Agent":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.Agent).ThenBy(p => p.AgencyFeeDecimal).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.Agent).ThenByDescending(p => p.AgencyFeeDecimal).ToList();
                }
                SortIndicatorColumn = 8;
                break;

            case "PaidPercentage":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.PaidPercentage).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.PaidPercentage).ToList();
                }
                SortIndicatorColumn = 9;
                break;

            case "Profit":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.Currency).ThenBy(p => p.ExpectedProfit).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.Currency).ThenByDescending(p => p.ExpectedProfit).ToList();
                }
                SortIndicatorColumn = 10;
                break;


            case "Status":
                if (_sortAscending)
                {
                    temp = Projects.OrderBy(p => p.Status).ToList();
                }
                else
                {
                    temp = Projects.OrderByDescending(p => p.Status).ToList();
                }
                SortIndicatorColumn = 11;
                break;
        }

        Projects.Clear();
        foreach (var p in temp)
        {
            Projects.Add(p);
        }

        if (!SetAndGetQueryStringIsEmpty())
        {
            QueryProjects();         // call to make sure sorting is up consistent between Projects and QueriedProjects
        }

        OnPropertyChanged(nameof(SortIndicatorColumn));
        OnPropertyChanged(nameof(SortIndicatorText));
    }

    public void QueryProjects()
    {
        QueriedProjects.Clear();
        var queryWords = QueryString?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var p in Projects)
        {
            bool match = true;
            foreach (var w in queryWords)
            {
                if (p.Client.IndexOf(w, StringComparison.OrdinalIgnoreCase) < 0
                    && p.Description?.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0 == false
                    && p.Type.IndexOf(w, StringComparison.OrdinalIgnoreCase) < 0
                    && p.Currency.IndexOf(w, StringComparison.OrdinalIgnoreCase) < 0
                    && (p.Agent?.Name.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0 == false) && !(p.Agent == null && "none".IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                QueriedProjects.Add(p);
            }
        }
    }

    partial void OnQueryStringChanged(string value)
    {
        if (!SetAndGetQueryStringIsEmpty())
        {
            QueryProjects();
        }
    }

    private bool SetAndGetQueryStringIsEmpty()
    {
        IsQueryStringEmpty = String.IsNullOrEmpty(QueryString);
        return IsQueryStringEmpty;
    }

    [RelayCommand]
    public void ClearQuery()
    {
        QueryString = null;
        IsQueryStringEmpty = true;
        QueriedProjects.Clear();
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
