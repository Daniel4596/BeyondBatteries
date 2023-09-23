using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices.ObjectiveC;
using System.Threading.Tasks;

namespace BeyondBatteries
{
    public partial class MainPage : ContentPage
    {
        private readonly IAdapter _bluetoothAdapter;
        private readonly List<IDevice> _gattDevices = new List<IDevice>();
        IDevice _connectedDevice;
        String action;
        public MainPage()
        {
            InitializeComponent();
            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;
            _bluetoothAdapter.DeviceDiscovered += async (sender, foundBleDevice) =>
            {
                if (foundBleDevice != null && !string.IsNullOrEmpty(foundBleDevice.Device.Name) && foundBleDevice.Device.ToString() == BleConfiguration.device_name)
                {
                    _gattDevices.Add(foundBleDevice.Device);
                    IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(BLEConnect.IsEnabled = true);
                    await _bluetoothAdapter.StopScanningForDevicesAsync();
                    action = await DisplayActionSheet("Choose device", "Cancel", null, foundBleDevice.Device.Name);
                    
                    if (action == foundBleDevice.Device.Name)
                    {
                        _connectedDevice = foundBleDevice.Device;
                        
                        var connectParameters = new ConnectParameters(false, true);
                        await _bluetoothAdapter.ConnectToDeviceAsync(_connectedDevice, connectParameters);
                        await Navigation.PushAsync(new DevicePage(_connectedDevice));
                    }
                }
                    
            };
        }

        private async Task<bool> PermissionGrantedAsync()
        {

            if (Microsoft.Maui.Devices.DeviceInfo.Version.Major >= 12)
            {

                PermissionStatus status = await Permissions.CheckStatusAsync<MyBluetoothPermission>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<MyBluetoothPermission>();
                }
                else
                {
                    return status == PermissionStatus.Granted;
                }

                if (Permissions.ShouldShowRationale<MyBluetoothPermission>())
                {
                    await Shell.Current.DisplayAlert("Needs permissions", "Location permission is required for bluetooth scanning. " +
                        "The location will not be stored or used", "Ok");
                    status = await Permissions.RequestAsync<MyBluetoothPermission>();
                }
                return status == PermissionStatus.Granted;

            }
            else
            {
                PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }
                return status == PermissionStatus.Granted;
                /*
                if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
                {
                    await Shell.Current.DisplayAlert("Needs permissions", "Because", "Ok");
                }
                */
            }
        }

        private async void OnPress(object sender, EventArgs e)
        {
            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(BLEConnect.IsEnabled = false);
            foundBleDevicesListView.ItemsSource = null;
            if (!await PermissionGrantedAsync())                                                           // Make sure there is permission to use Bluetooth
            {
                await DisplayAlert("Permission required", "Application needs location permission." + "Permissions needs to be set manually!", "OK");
                return;
            }

            _gattDevices.Clear();

            if(!_bluetoothAdapter.IsScanning)
            {
                await _bluetoothAdapter.StartScanningForDevicesAsync();
            }
            /*
            foreach (var device in _bluetoothAdapter.ConnectedDevices)
                _gattDevices.Add(device);
            */
            //string 
            foundBleDevicesListView.ItemsSource = _gattDevices.ToArray();
            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(BLEConnect.IsEnabled = true);

        }

        private async void FoundBluetoothDevicesListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(BLEConnect.IsEnabled = false);
            IDevice selectedItem = e.Item as IDevice;

            if (selectedItem.State == DeviceState.Connected)
            {
                await Navigation.PushAsync(new DevicePage(selectedItem));
            }
            else
            {
                try
                {
                    var connectParameters = new ConnectParameters(false, true);
                    await _bluetoothAdapter.ConnectToDeviceAsync(selectedItem, connectParameters);
                    await Navigation.PushAsync(new DevicePage(selectedItem));
                }
                catch
                {
                    await DisplayAlert("Error connecting", $"Error connecting to BLE device: {selectedItem.Name}", "OK");
                }
            }
            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(BLEConnect.IsEnabled = true);
        }

    }
}