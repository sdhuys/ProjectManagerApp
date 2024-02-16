﻿using MauiApp1.Models;

namespace MauiApp1.Selectors;
internal class SpendingViewModelTransactionTemplateSelector : DataTemplateSelector
{
    public DataTemplate ExpenseTemplate { get; set; }
    public DataTemplate TransferToSavingsTemplate { get; set; }
    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is SpendingCategory.TransferTransaction) return TransferToSavingsTemplate;
        else return ExpenseTemplate;
    }
}
