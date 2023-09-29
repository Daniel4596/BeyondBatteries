using Plugin.BLE.Abstractions.Contracts;
using System.Diagnostics;
using Plugin.BLE.Abstractions.EventArgs;
using Microsoft.Maui.Controls;
using System.Runtime.CompilerServices;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions;
using System.Text;

namespace BeyondBatteries;

public partial class DevicePage : ContentPage
{
    private readonly IDevice _connectedDevice;
    ICharacteristic characteristic_soll;
    ICharacteristic characteristic_ist;
    ICharacteristic characteristic_fenster;
    ICharacteristic characteristic_thermostat;
    ICharacteristic characteristic_name;
    IService service;
    private bool _isUpdating = true;
    private bool _nameRegistered = false;
    
	public DevicePage(IDevice connectedDevice)
	{
		InitializeComponent();
        _connectedDevice = connectedDevice;

        Connect();
            Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (_connectedDevice.State == DeviceState.Disconnected)
                {
                    Disconnect();
                }
                OnUpdate();
                return _isUpdating;
            });   
    }

    private async void Disconnect()
    {
        await Navigation.PopToRootAsync();
    }
    private async void Connect()
    {
        try
        {
            service = await _connectedDevice.GetServiceAsync(Guid.Parse(BleConfiguration.service_guid));
            characteristic_soll = await service.GetCharacteristicAsync(Guid.Parse(BleConfiguration.characteristic_sollTemp));
            characteristic_ist = await service.GetCharacteristicAsync(Guid.Parse(BleConfiguration.characteristic_istTemp));
            characteristic_fenster = await service.GetCharacteristicAsync(Guid.Parse(BleConfiguration.characteristic_fensterStatus));
            characteristic_thermostat = await service.GetCharacteristicAsync(Guid.Parse(BleConfiguration.characteristic_thermostatStatus));
            characteristic_name = await service.GetCharacteristicAsync(Guid.Parse(BleConfiguration.characteristic_name));
            while (characteristic_soll == null && characteristic_ist == null && characteristic_fenster == null && characteristic_thermostat == null && characteristic_name == null)
            {
                
            }
            if (BleConfiguration.device_name == _connectedDevice.Name)
            {
                _nameRegistered = false;
            }
            else
            {
                _nameRegistered = true;
            }
            if (!_nameRegistered)
            {
                bool result_valid = false;
                while (!result_valid)
                {
                    string result = await DisplayPromptAsync("Erstkonfiguration", "Bitte geben Sie die Raumnummer ein", "Ok", "Cancel", "Raumnummer", maxLength: 3, keyboard: Keyboard.Telephone, initialValue: "11");
                    if (result != null && Convert.ToInt32(result) < 256)
                    {
                        byte bytes = Convert.ToByte(result);
                        await characteristic_name.WriteAsync(new byte[] { bytes });
                        result = "Beyond-Battery Raum " + result;
                        result_valid = true;
                    }
                    else
                    {
                        await DisplayAlert("Ungültige Raumnummer", "Bitte geben Sie eine gültige Raumnummer ein. Sie darf nicht größer als 256 sein.", "Ok");
                    }
                }
                
                
            }
            

            if (characteristic_soll.CanUpdate)
            {

                characteristic_soll.ValueUpdated += (o, args) =>
                {
                    var bytes = args.Characteristic.Value;
                    Debug.WriteLine("Soll-Temperatur : " + bytes[0] + " °C");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UpdatedValues(bytes, 1);
                    });
                };
            }
            await characteristic_soll.StartUpdatesAsync();

            if (characteristic_ist.CanUpdate)
            {
                characteristic_ist.ValueUpdated += (o, args) =>
                {
                    var bytes = args.Characteristic.Value;
                    Debug.WriteLine("Ist-Temperatur : " + bytes[0] + " °C");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UpdatedValues(bytes, 2);
                    });


                };
                await characteristic_ist.StartUpdatesAsync();
            }
            if (characteristic_fenster.CanUpdate)
            {
                characteristic_fenster.ValueUpdated += (o, args) =>
                {
                    var bytes = args.Characteristic.Value;
                    Debug.WriteLine("Fenster-Status: " + bytes[0]);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UpdatedValues(bytes, 3);
                    });
                };
                await characteristic_fenster.StartUpdatesAsync();
            }
            
            if (characteristic_thermostat.CanUpdate)
            {
                characteristic_thermostat.ValueUpdated += (o, args) =>
                {
                    var bytes = args.Characteristic.Value;
                    Debug.WriteLine("Thermostat-Status: " + bytes[0]);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UpdatedValues(bytes, 4);
                    });
                };
                await characteristic_thermostat.StartUpdatesAsync();
            }
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Fehler beim Initialisieren: {ex.Message}");
            await DisplayAlert("Error initializing", $"Error initializing UART GATT service. Please try again.  ", "Ok");
            Disconnect();
        }
    }
    private async void OnUpdate()
    {
        try
        {
            if (characteristic_ist != null && characteristic_soll != null && characteristic_fenster != null & characteristic_thermostat != null)
            {
                if(characteristic_soll.CanRead)
                {
                    await characteristic_soll.ReadAsync();

                }
                /*
                if(characteristic_soll.CanWrite)
                {
                    await characteristic_soll.WriteAsync(new byte[] { (byte)_sollTempUpdate });
                }
                 */
                if (characteristic_ist.CanRead)
                {
                    await characteristic_ist.ReadAsync();
                }
                if (characteristic_fenster.CanRead)
                {
                    await characteristic_fenster.ReadAsync();
                }
                if (characteristic_thermostat.CanRead)
                {
                    await characteristic_thermostat.ReadAsync();
                }
            }
        }
        catch (CharacteristicReadException ex)
        {
            Debug.WriteLine($"Fehler beim Lesen der Charakteristik: {ex.Message}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error pairing: ", "Gerät ist offline, nicht gekoppelt oder der PIN ist falsch. Bitte versuchen Sie es erneut!", "Ok");
            Debug.WriteLine($"Diconnected while Reading: {ex.Message}");
            await Navigation.PopToRootAsync();
        }
        
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        /*
        while(DeviceBondState.Bonding)
        {
            Debug.WriteLine("Bonding...");
        }
        */
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if(service != null)
        {
            service.Dispose();
        }
        _isUpdating = false;
        _connectedDevice.Dispose();
    }

    private void Stepper_Value(object sender, EventArgs e)
    {
        try
        {
            SollTempText.Text = string.Format("{0} °C", SollTempStepper.Value);
            characteristic_soll.WriteAsync(new byte[] { (byte)SollTempStepper.Value });
        }
        catch (CharacteristicReadException ex)
        {
            Debug.WriteLine($"Fehler beim Lesen der Charakteristik: {ex.Message}");
        }
        
    }

    private void UpdatedValues(byte[] bytes, byte option)
    {
        try
        {
            if (option == 1)
            {
                SollTempText.Text = string.Format("{0} °C", bytes[0]);
                SollTempStepper.Value = bytes[0];
            }
            else if (option == 2)
            {
                IstTempText.Text = string.Format("{0} °C", bytes[0]);
            }
            else if (option == 3)
            {
                if (bytes[0] == 0)
                {
                    FensterStatusText.Text = "Fenster geschlossen";
                }
                else
                {
                    FensterStatusText.Text = "Fenster geöffnet";
                }
            }
            else if (option == 4)
            {
                if (bytes[0] == 0)
                {
                    ThermostatStatusText.Text = "Thermostat aus";
                }
                else
                {
                    ThermostatStatusText.Text = "Thermostat an";
                }
            }
        }
        catch (CharacteristicReadException ex)
        {
            Debug.WriteLine($"Fehler beim Lesen der Charakteristik: {ex.Message}");
        }
            
        
    }

}