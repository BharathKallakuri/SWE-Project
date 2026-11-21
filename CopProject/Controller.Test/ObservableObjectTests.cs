// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using Controller.ViewModels;

namespace Controller.Test;

public class ObservableObjectTests
{
    private class TestViewModel : ObservableObject
    {
        private string _title = string.Empty;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public void NotifyManual(string name)
        {
            OnPropertyChanged(name);
        }

        public void CallRefresh()
        {
            Refresh();
        }
    }

    [Fact]
    public void SetProperty_UpdatesValueAndRaisesEvent()
    {
        var vm = new TestViewModel();
        string? notifiedProperty = null;
        vm.PropertyChanged += (s, e) => notifiedProperty = e.PropertyName;

        vm.Title = "New Title";

        Assert.Equal("New Title", vm.Title);
        Assert.Equal("Title", notifiedProperty);
    }

    [Fact]
    public void OnPropertyChanged_ExplicitName_RaisesEvent()
    {
        var vm = new TestViewModel();
        string? notifiedProperty = null;
        vm.PropertyChanged += (s, e) => notifiedProperty = e.PropertyName;

        vm.NotifyManual("CustomProp");

        Assert.Equal("CustomProp", notifiedProperty);
    }

    [Fact]
    public void Refresh_RaisesPropertyChangedWithEmptyString()
    {
        var vm = new TestViewModel();
        string? notifiedProperty = null;
        vm.PropertyChanged += (s, e) => notifiedProperty = e.PropertyName;

        vm.CallRefresh();

        Assert.Equal(string.Empty, notifiedProperty);
    }
}

