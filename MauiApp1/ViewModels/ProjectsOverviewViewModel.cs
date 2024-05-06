using System.Collections.ObjectModel;
using MauiApp1.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiApp1.Views;
using System.Diagnostics;

namespace MauiApp1.ViewModels;
public partial class ProjectsOverviewViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompletedProjects))]
    [NotifyPropertyChangedFor(nameof(InvoicedProjects))]
    [NotifyPropertyChangedFor(nameof(CancelledProjects))]
    [NotifyPropertyChangedFor(nameof(ActiveProjects))]
    [NotifyPropertyChangedFor(nameof(AwaitedPaymentsTotal))]
    ObservableCollection<ProjectViewModel> projectsViewModels;

    public IEnumerable<Project> Projects => ProjectsViewModels.Select(x => x.Project);

    [ObservableProperty]
    ObservableCollection<ProjectViewModel> queriedProjectsViewModels;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditProjectCommand))]
    ProjectViewModel selectedProjectVM;

    [ObservableProperty]
    string queryString;

    [ObservableProperty]
    bool isQueryStringEmpty;

    public List<ProjectViewModel> ActiveProjects => GetProjectsByStatus(Project.ProjectStatus.Active);
    public List<ProjectViewModel> InvoicedProjects => GetProjectsByStatus(Project.ProjectStatus.Invoiced);

    public List<ProjectViewModel> CompletedProjects => GetProjectsByStatus(Project.ProjectStatus.Completed);
    public List<ProjectViewModel> CancelledProjects => GetProjectsByStatus(Project.ProjectStatus.Cancelled);

    public decimal AwaitedPaymentsTotal => InvoicedProjects.Select(x => x.Fee).Sum() - (InvoicedProjects.Select(x => x.Payments.Select(x => x.Amount).Sum()).Sum());

    public string SortIndicatorText
    {
        get { return _sortAscending ? "▲" : "▼"; }
    }
    public int SortIndicatorColumn { get; set; }

    private bool _sortAscending;
    private string _sortProperty;

    private ProjectJsonIOManager _projectsJsonIOManager;
    public ProjectsOverviewViewModel(ProjectJsonIOManager projectsJsonManager)
    {
        _projectsJsonIOManager = projectsJsonManager;
        var projects = _projectsJsonIOManager.LoadProjects();
        ProjectsViewModels = new(projects.Select(x => new ProjectViewModel(x)));
        QueriedProjectsViewModels = new();
        IsQueryStringEmpty = true;
        _sortProperty = "Date";
        _sortAscending = false; // will be switched to true in SortProjects()
        SortProjects(_sortProperty);
    }

    //Method called on page resize
    //Dirty fix to make projects collectionview grid correctly scale with window downsizing
    //Without this the grid only correctly scales when upsizing window (.NET MAUI bug?)
    public async Task ReloadProjects()
    {
        var temp = ProjectsViewModels.ToList();
        ProjectsViewModels.Clear();
        await Task.Delay(1);
        foreach (var project in temp)
        {
            ProjectsViewModels.Add(project);
        }
    }

    [RelayCommand]
    async Task GoToNewProjectPage()
    {
        await Shell.Current.GoToAsync($"{nameof(ProjectDetailsPage)}?",
            new Dictionary<string, object>
            {
                ["projects"] = ProjectsViewModels
            });
    }

    [RelayCommand(CanExecute = nameof(CanDeleteOrEditProject))]
    public async Task DeleteProject()
    {
        bool confirmed = await DisplayConfirmationDialog("Confirm Deletion", $"Are you sure you want to delete this {SelectedProjectVM.Type} project for client {SelectedProjectVM.Client}?");
        if (confirmed)
        {
            ProjectsViewModels.Remove(SelectedProjectVM);
            SaveProjects();
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
                    temp = ProjectsViewModels.OrderBy(p => p.Client).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.Client).ToList();
                }
                SortIndicatorColumn = 0;
                break;

            case "Type":
                if (_sortAscending)
                {
                    temp = ProjectsViewModels.OrderBy(p => p.Type).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.Type).ToList();
                }
                SortIndicatorColumn = 2;
                break;

            case "Description":
                if (_sortAscending)
                {
                    temp = ProjectsViewModels.OrderBy(p => p.Description).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.Description).ToList();
                }
                SortIndicatorColumn = 4;
                break;

            case "Date":
                if (_sortAscending)
                {
                    temp = ProjectsViewModels.OrderBy(p => p.Date).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.Date).ToList();
                }
                SortIndicatorColumn = 6;
                break;

            case "Currency":
                if (_sortAscending)
                {
                    temp = ProjectsViewModels.OrderBy(p => p.Currency).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.Currency).ToList();
                }
                SortIndicatorColumn = 8;
                break;

            case "Fee":
                if (_sortAscending)
                {
                    temp = ProjectsViewModels.OrderBy(p => p.Currency).ThenBy(p => p.Fee).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.Currency).ThenByDescending(p => p.Fee).ToList();
                }
                SortIndicatorColumn = 10;
                break;

            case "VatRate":
                if (_sortAscending)
                {
                    temp = ProjectsViewModels.OrderBy(p => p.VatRateDecimal).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.VatRateDecimal).ToList();
                }
                SortIndicatorColumn = 12;
                break;

            case "TotalExpenses":
                if (_sortAscending)
                {
                    temp = ProjectsViewModels.OrderBy(p => p.Currency).ThenBy(p => p.TotalExpenses).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.Currency).ThenByDescending(p => p.TotalExpenses).ToList();
                }
                SortIndicatorColumn = 14;
                break;

            case "Agent":
                if (_sortAscending)
                {
                    temp = ProjectsViewModels.OrderBy(p => p.Agent).ThenBy(p => p.AgencyFeeDecimal).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.Agent).ThenByDescending(p => p.AgencyFeeDecimal).ToList();
                }
                SortIndicatorColumn = 16;
                break;

            case "PaidPercentage":
                if (_sortAscending)
                {
                    temp = ProjectsViewModels.OrderBy(p => p.PaidPercentage).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.PaidPercentage).ToList();
                }
                SortIndicatorColumn = 18;
                break;

            case "Profit":
                if (_sortAscending)
                {
                    temp = ProjectsViewModels.OrderBy(p => p.Currency).ThenBy(p => p.ActualProfit).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.Currency).ThenByDescending(p => p.ActualProfit).ToList();
                }
                SortIndicatorColumn = 20;
                break;


            case "Status":
                if (_sortAscending)
                {
                    temp = ProjectsViewModels.OrderBy(p => p.Status).ToList();
                }
                else
                {
                    temp = ProjectsViewModels.OrderByDescending(p => p.Status).ToList();
                }
                SortIndicatorColumn = 22;
                break;
        }

        ProjectsViewModels.Clear();
        foreach (var p in temp)
        {
            ProjectsViewModels.Add(p);
        }

        if (!SetAndGetQueryStringIsEmpty())
        {
            QueryProjects();
        }

        OnPropertyChanged(nameof(SortIndicatorColumn));
        OnPropertyChanged(nameof(SortIndicatorText));
    }

    // Splits the query string by whitespace and checks if each resulting string is a substring of either Client, Description, Type, Currency or Agent name
    // If there is no Agent, "None" will be a match
    public void QueryProjects()
    {
        QueriedProjectsViewModels.Clear();
        var queryWords = QueryString?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var p in ProjectsViewModels)
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
                QueriedProjectsViewModels.Add(p);
            }
        }
    }

    partial void OnQueryStringChanged(string value)
    {
        if (!SetAndGetQueryStringIsEmpty())
        {
            Debug.WriteLine(value[value.Length -1]);
            QueryProjects();

            OnPropertyChanged(nameof(ActiveProjects));
            OnPropertyChanged(nameof(InvoicedProjects));
            OnPropertyChanged(nameof(CompletedProjects));
            OnPropertyChanged(nameof(CancelledProjects));
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
        QueriedProjectsViewModels.Clear();
    }
    private async Task<bool> DisplayConfirmationDialog(string title, string message)
    {
        return await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
    }

    private bool CanDeleteOrEditProject()
    {
        return SelectedProjectVM != null;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("projectVM"))
        {
            SelectedProjectVM = query["projectVM"] as ProjectViewModel;
            if (!ProjectsViewModels.Any(p => p.Id == SelectedProjectVM.Id))
            {
                ProjectsViewModels.Add(SelectedProjectVM);
            }
            _sortAscending = !_sortAscending; // will be switched back to original state in SortProjects()
            SortProjects(_sortProperty);
            SaveProjects();
        }
    }

    private void SaveProjects()
    {
        _projectsJsonIOManager.SaveProjects(ProjectsViewModels.Select(x => x.Project).ToList());
    }

    private List<ProjectViewModel> GetProjectsByStatus(Project.ProjectStatus status)
    {
        var collection = IsQueryStringEmpty ? ProjectsViewModels : QueriedProjectsViewModels;
        return collection.Where(x => x.Status == status).ToList();
    }
}
