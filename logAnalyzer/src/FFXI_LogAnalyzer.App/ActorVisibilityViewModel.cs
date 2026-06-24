using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FFXI_LogAnalyzer.App;

public sealed class ActorVisibilityViewModel : INotifyPropertyChanged
{
    private bool _isVisible = true;

    public ActorVisibilityViewModel(string actor)
    {
        Actor = actor;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Actor { get; }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
