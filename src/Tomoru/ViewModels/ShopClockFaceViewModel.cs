using CommunityToolkit.Mvvm.ComponentModel;
using Tomoru.Services;

namespace Tomoru.ViewModels;

/// <summary>One clock-face row in the shop. Same owned / active / affordable
/// states as a theme row, minus the swatch — a face is a layout, so the only
/// honest preview is wearing it.</summary>
public partial class ShopClockFaceViewModel : ViewModelBase
{
    public ClockFace Face { get; }

    public string Jp => Face.Jp;
    public string En => Face.En;
    public string Blurb => Face.Blurb;
    public string PriceLabel => Face.Price == 0 ? "free" : $"火種 {Face.Price:N0}";

    [ObservableProperty] private bool _isOwned;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _canAfford;

    public ShopClockFaceViewModel(ClockFace face) => Face = face;

    public void SetState(bool owned, bool active, int balance)
    {
        IsOwned = owned;
        IsActive = active;
        CanAfford = balance >= Face.Price;
    }

    /// <summary>What the button says: the state it will move you to.</summary>
    public string ActionLabel => IsActive ? "worn" : IsOwned ? "wear" : "buy";
}
