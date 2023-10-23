using MauiApp1.ViewModels;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;

namespace MauiApp1.Views;

public partial class EarningsPage : ContentPage
{
	EarningsViewModel viewModel;
	public EarningsPage(EarningsViewModel vm)
	{
		BindingContext = vm;
		viewModel = vm;
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
		viewModel.ApplyFilters();
		viewModel.CreateCurrencyExpectedEarningsViewModels();
    }
}