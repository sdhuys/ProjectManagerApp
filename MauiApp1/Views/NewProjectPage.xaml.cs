using MauiApp1.ViewModels;
using CommunityToolkit.Maui.Markup;

namespace MauiApp1.Views;
public partial class NewProjectPage : ContentPage
{
    public NewProjectPage(NewProjectViewModel vm)
    {
        BindingContext = vm;
        InitializeComponent();
    }
}