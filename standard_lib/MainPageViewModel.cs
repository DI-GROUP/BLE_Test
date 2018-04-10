using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Xamarin.Forms;

namespace BLETest
{
    public class MainPageViewModel : BindableBase
    {
        private readonly double _disappearingTime = 3;
        private bool _isBtOn;
        private readonly object _locker = new object();
        private readonly IBluetoothLE _manager;

        public MainPageViewModel()
        {
            var manager = CrossBluetoothLE.Current;
            if (!manager.IsAvailable)
                throw new Exception("BLE is not available.");

            _manager = manager;
            IsBTOn = _manager.IsOn;
            if (IsBTOn)
                StartScan();
            _manager.Adapter.DeviceDiscovered += OnDeviceDiscovered;
            _manager.StateChanged += OnStateChanged;

            Devices = new ObservableCollection<IDeviceInTest>();
            StartScanCommand = new Command(StartStacExecute);
        }

        public Command StartScanCommand { get; set; }


        public ObservableCollection<IDeviceInTest> Devices { get; set; }

        public bool IsBTOn
        {
            get => _isBtOn;
            set => SetProperty(ref _isBtOn, value);
        }

        private void StartStacExecute(object obj)
        {
            StartScan();
        }

        private void StartScan()
        {
            _manager.Adapter.ScanMode = ScanMode.LowLatency;
            var cts = new CancellationTokenSource();
            _manager.Adapter.StartScanningForDevicesAsync(allowDuplicatesKey: false, cancellationToken: cts.Token);
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs e)
        {
            Task.Run(() => { UpdateDevices(e); });
        }

        private void UpdateDevices(DeviceEventArgs e)
        {
            lock (_locker)
            {
                var devicesToRemove = new List<IDeviceInTest>();
                var all = true;
                foreach (var device in Devices)
                {
                    if (device.ID == e.Device.Id)
                    {
                        all = false;
                        device.DiscoveryTimer.Restart();
                    }
                    if (device.DiscoveryTimer.Elapsed > TimeSpan.FromMinutes(_disappearingTime))
                        devicesToRemove.Add(device);
                }
                if (all)
                {
                    var testDevice = CreateDevice(e.Device);
                    Devices.Insert(0, testDevice);
                }
                foreach (var device in devicesToRemove)
                {
                    _manager.Adapter.DisconnectDeviceAsync(device.Device);
                    Devices.Remove(device);
                }
            }
        }

        private IDeviceInTest CreateDevice(IDevice device)
        {
            var deviceInTest = new DeviceInTest(device, _manager.Adapter);
            return deviceInTest;
        }

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e)
        {
            IsBTOn = e.NewState == BluetoothState.On;
            if (IsBTOn)
                StartScan();
        }
    }

    internal class DeviceInTest : BindableBase, IDeviceInTest
    {
        private readonly IAdapter _adapter;
        private int _isTestRunning;
        private bool _isTestSuccessful;
        private bool _isTesting;

        public DeviceInTest(IDevice device, IAdapter adapter)
        {
            Device = device;
            _adapter = adapter;
            DiscoveryTimer = new Stopwatch();
            DiscoveryTimer.Start();
            StartTestCommand = new Command(async () => await TestAsync().ConfigureAwait(false));
            DisconnectCommand = new Command(async () => await DisconnectAsync().ConfigureAwait(false));
        }


        public Guid ID => Device.Id;
        public string Name => Device.Name;
        public IDevice Device { get; }

        public Command StartTestCommand { get; set; }
        public Command DisconnectCommand { get; set; }

        public async Task<bool> TestAsync()
        {
            if (Interlocked.CompareExchange(ref _isTestRunning, 1, 0) == 1)
                return false;

            var result = false;
            try
            {
                IsTesting = true;
                result = await Tests.Test1(Device, _adapter);
                return result;
            }
            catch (Exception)
            {
                result = false;
            }
            finally
            {
                IsTesting = false;
                IsTestSuccessful = result;
                _isTestRunning = 0;
            }
            return IsTestSuccessful;
        }

        public bool IsTesting
        {
            get { return _isTesting; }
            set { SetProperty(ref _isTesting , value); }
        }

        public async Task DisconnectAsync()
        {
            await _adapter.DisconnectDeviceAsync(Device).ConfigureAwait(false);
        }

        public bool IsTestSuccessful
        {
            get => _isTestSuccessful;
            set => SetProperty(ref _isTestSuccessful, value);
        }

        public Stopwatch DiscoveryTimer { get; set; }
    }

    public interface IDeviceInTest
    {
        Guid ID { get; }
        string Name { get; }
        IDevice Device { get; }
        Command StartTestCommand { get; set; }
        Command DisconnectCommand { get; set; }
        bool IsTestSuccessful { get; }
        Stopwatch DiscoveryTimer { get; set; }
        Task<bool> TestAsync();
        Task DisconnectAsync();
    }
}