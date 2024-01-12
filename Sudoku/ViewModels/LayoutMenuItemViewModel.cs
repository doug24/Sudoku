using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Sudoku;

public partial class LayoutMenuItemViewModel : ObservableObject
{
    public LayoutMenuItemViewModel(string header, int layoutId, bool isChecked)
    {
        Header = header;
        LayoutId = layoutId;
        IsChecked = isChecked;
        if (layoutId < 0 || layoutId >= 999)
        {
            TooltipImage = @$"pack://application:,,,/Sudoku;component/images/{header.ToLowerInvariant()}.png";
        }
        else
        {
            TooltipImage = @$"pack://application:,,,/Sudoku;component/images/irr{layoutId + 1}.png";
        }
    }

    public int LayoutId { get; private set; }

    [ObservableProperty]
    private string header = string.Empty;

    [ObservableProperty]
    private bool isChecked = false;

    [ObservableProperty]
    private string tooltipImage = string.Empty;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IsChecked) && IsChecked)
        {
            WeakReferenceMessenger.Default.Send(new SectionLayoutChangedMessage(LayoutId));
        }

        base.OnPropertyChanged(e);
    }
}

public class SectionLayoutChangedMessage(int id) : ValueChangedMessage<int>(id)
{
}
