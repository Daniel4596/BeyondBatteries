using System.Text;
using Plugin.BLE.Abstractions.Contracts;
using System.Diagnostics;

namespace BeyondBatteries;

public partial class DevicePage : ContentPage
{
    private readonly IDevice _connectedDevice;
    private IService _serivce;
	public DevicePage(IDevice connectedDevice)
	{
		InitializeComponent();
        _connectedDevice = connectedDevice;
	}

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var service = await _connectedDevice.GetServiceAsync(Guid.Parse(BleConfiguration.service_guid));
            var characteristic = await service.GetCharacteristicAsync(Guid.Parse(BleConfiguration.characteristic_sollTemp));
            var characteristic2 = await service.GetCharacteristicAsync(Guid.Parse(BleConfiguration.characteristic_istTemp));
            var characteristic3 = await service.GetCharacteristicAsync(Guid.Parse(BleConfiguration.characteristic_fensterStatus));
            var characteristic4 = await service.GetCharacteristicAsync(Guid.Parse(BleConfiguration.characteristic_thermostatStatus));

        }
        catch
        {
            await DisplayAlert("Error initializing", $"Error initializing UART GATT service.", "Ok");
        }
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _connectedDevice.Dispose();
    }

    private void Stepper_Value(object sender, EventArgs e)
    {

        SollTempText.Text = string.Format("{0} °C", SollTempStepper.Value);
    }
}