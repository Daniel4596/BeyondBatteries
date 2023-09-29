using Plugin.BLE.Abstractions.Contracts;
using System.Diagnostics;

namespace BeyondBatteries;

public partial class RoomPage : ContentPage
{
    public RoomPage(ICharacteristic characteristic)
    {
        InitializeComponent();
        GetNameAsync(characteristic);
    }

    private async void GetNameAsync(ICharacteristic characteristic)
    {
        
    }
}