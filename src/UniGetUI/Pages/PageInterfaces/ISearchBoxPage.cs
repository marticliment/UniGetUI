using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace UniGetUI.Pages.PageInterfaces;
internal interface ISearchBoxPage
{
    public string QueryBackup { get; set; }
    public string SearchBoxPlaceholder { get; }
    public void SearchBox_TextChanged(object? sender, AutoSuggestBoxTextChangedEventArgs args);
    public void SearchBox_QuerySubmitted(object? sender, AutoSuggestBoxQuerySubmittedEventArgs args);
}
