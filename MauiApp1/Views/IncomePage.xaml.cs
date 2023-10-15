using MauiApp1.ViewModels;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;

namespace MauiApp1.Views;

public partial class IncomePage : ContentPage
{
	IncomeViewModel viewModel;
	public IncomePage(IncomeViewModel vm)
	{
		BindingContext = vm;
		viewModel = vm;
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
		viewModel.FilterAndSetGroupings();
    }
}