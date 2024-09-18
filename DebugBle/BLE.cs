using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

public class BLE
{
    const string DLL_NAME =
#if DEBUG
        "BleWinrtDllDebug.dll";
#else
        "BleWinrtDll.dll";
#endif
    // dll calls
    class Impl
    {
        public enum ScanStatus { PROCESSING, AVAILABLE, FINISHED };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DeviceUpdate
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string id;
            [MarshalAs(UnmanagedType.I1)]
            public bool isConnectable;
            [MarshalAs(UnmanagedType.I1)]
            public bool isConnectableUpdated;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string name;
            [MarshalAs(UnmanagedType.I1)]
            public bool nameUpdated;
        }

        [DllImport(DLL_NAME, EntryPoint = "StartDeviceScan")]
        public static extern void StartDeviceScan();

        [DllImport(DLL_NAME, EntryPoint = "PollDevice")]
        public static extern ScanStatus PollDevice(out DeviceUpdate device, bool block);

        [DllImport(DLL_NAME, EntryPoint = "StopDeviceScan")]
        public static extern void StopDeviceScan();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Service
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string uuid;
        };

        [DllImport(DLL_NAME, EntryPoint = "ScanServices", CharSet = CharSet.Unicode)]
        public static extern void ScanServices(string deviceId);

        [DllImport(DLL_NAME, EntryPoint = "PollService")]
        public static extern ScanStatus PollService(out Service service, bool block);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Characteristic
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string uuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string userDescription;
        };        

        [DllImport(DLL_NAME, EntryPoint = "ScanCharacteristics", CharSet = CharSet.Unicode)]
        public static extern void ScanCharacteristics(string deviceId, string serviceId);

        [DllImport(DLL_NAME, EntryPoint = "PollCharacteristic")]
        public static extern ScanStatus PollCharacteristic(out Characteristic characteristic, bool block);

        [DllImport(DLL_NAME, EntryPoint = "SubscribeCharacteristic", CharSet = CharSet.Unicode)]
        public static extern bool SubscribeCharacteristic(string deviceId, string serviceId, string characteristicId, bool block);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct BLEData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] buf;
            [MarshalAs(UnmanagedType.I2)]
            public short size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string deviceId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string serviceUuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string characteristicUuid;
        };

        [DllImport(DLL_NAME, EntryPoint = "PollData")]
        public static extern bool PollData(out BLEData data, bool block);

        [DllImport(DLL_NAME, EntryPoint = "SendData")]
        public static extern bool SendData(in BLEData data, bool block);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct BLECharacteristic
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string deviceId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string serviceUuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string characteristicUuid;
        };

        [DllImport(DLL_NAME, EntryPoint = "ReadData")]
        public static extern bool ReadData(in BLECharacteristic id, out BLEData dataOut, bool block);

        [DllImport(DLL_NAME, EntryPoint = "Quit")]
        public static extern void Quit();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ErrorMessage
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
            public string msg;
        };

        [DllImport(DLL_NAME, EntryPoint = "GetError")]
        public static extern void GetError(out ErrorMessage buf);
    }

    public static Thread scanThread;
    public static BLEScan currentScan = new BLEScan();
    public bool isConnected = false;

    public class BLEScan
    {
        public delegate void FoundDel(string deviceId, string deviceName);
        public delegate void FinishedDel();
        public FoundDel Found;
        public FinishedDel Finished;
        internal bool cancelled = false;

        public void Cancel()
        {
            cancelled = true;
            Impl.StopDeviceScan();
        }
    }

    // don't block the thread in the Found or Finished callback; it would disturb cancelling the scan
    public static BLEScan ScanDevices()
    {
        if (scanThread == Thread.CurrentThread)
            throw new InvalidOperationException("a new scan can not be started from a callback of the previous scan");
        else if (scanThread != null)
            throw new InvalidOperationException("the old scan is still running");
        currentScan.Found = null;
        currentScan.Finished = null;
        scanThread = new Thread(() =>
        {
            Impl.StartDeviceScan();
            Impl.DeviceUpdate res = new Impl.DeviceUpdate();
            List<string> deviceIds = new List<string>();
            Dictionary<string, string> deviceName = new Dictionary<string, string>();
            Dictionary<string, bool> deviceIsConnectable = new Dictionary<string, bool>();
            Impl.ScanStatus status = Impl.ScanStatus.PROCESSING;
            do
            {
                status = Impl.PollDevice(out res, true);
                if (!deviceIds.Contains(res.id))
                {
                    deviceIds.Add(res.id);
                    deviceName[res.id] = "";
                    deviceIsConnectable[res.id] = false;
                }
                if (res.nameUpdated)
                    deviceName[res.id] = res.name;
                if (res.isConnectableUpdated)
                    deviceIsConnectable[res.id] = res.isConnectable;
                // connectable device
                if (deviceName[res.id] != "")
                    currentScan.Found?.Invoke(res.id, deviceName[res.id]);
                // check if scan was cancelled in callback
                if (currentScan.cancelled)
                    break;
            } while (status != Impl.ScanStatus.FINISHED);
            currentScan.Finished?.Invoke();
            scanThread = null;
        });
        scanThread.Start();
        return currentScan;
    }

    public static Dictionary<string, ushort> ReadData(string deviceId, Dictionary<string, Dictionary<string, string>> profile)
    {
        Dictionary<string, ushort> ret = new Dictionary<string, ushort>();
        foreach (var service in profile)
        {
            foreach (var characteristics in service.Value)
            {
                Impl.BLECharacteristic characteristic = new Impl.BLECharacteristic
                {
                    deviceId = deviceId,
                    serviceUuid = service.Key,
                    characteristicUuid = characteristics.Key
                };
                Impl.BLEData data = new Impl.BLEData
                {
                    buf = new byte[512]
                };
                data.buf[0] = 1;
                bool gotData = Impl.ReadData(characteristic, out data, true);
                if (gotData)
                {
                    if (data.size > 0)
                    {
                        Debug.Log(data.size);
                        //Debug.Log(data.buf);
                    }
                    ret.Add(characteristics.Key, (ushort)(data.buf[0] + (data.buf[1] << 8)));
                }
            }
        }
        return ret;
    }

    public static void RetrieveProfile(string deviceId, string serviceUuid)
    {
        Impl.ScanServices(deviceId);
        Impl.Service service = new Impl.Service();
        while (Impl.PollService(out service, true) != Impl.ScanStatus.FINISHED)
            Debug.Log("service found: " + service.uuid);
        // wait some delay to prevent error
        Thread.Sleep(200);
        Impl.ScanCharacteristics(deviceId, serviceUuid);
        Impl.Characteristic c = new Impl.Characteristic();
        while (Impl.PollCharacteristic(out c, true) != Impl.ScanStatus.FINISHED)
            Debug.Log("characteristic found: " + c.uuid + ", user description: " + c.userDescription);
    }

    public static bool Subscribe(string deviceId, string serviceUuid, string[] characteristicUuids)
    {
        foreach (string characteristicUuid in characteristicUuids)
        {
            bool res = Impl.SubscribeCharacteristic(deviceId, serviceUuid, characteristicUuid, true);
            if (!res)
                return false;
        }
        return true;
    }

    public bool Connect(string deviceId, string serviceUuid, string[] characteristicUuids)
    {
        if (isConnected)
            return false;
        Debug.Log("retrieving ble profile...");
        RetrieveProfile(deviceId, serviceUuid);
        if (GetError() != "Ok")
            throw new Exception("Connection failed: " + GetError());
        Debug.Log("subscribing to characteristics...");
        bool result = Subscribe(deviceId, serviceUuid, characteristicUuids);
        if (GetError() != "Ok" || !result)
            throw new Exception("Connection failed: " + GetError());
        isConnected = true;
        return true;
    }

    public static Dictionary<string, Dictionary<string, string>> GetProfile(string deviceId)
    {
        Dictionary<string, Dictionary<string, string>> device = new Dictionary<string, Dictionary<string, string>>();
        Impl.ScanServices(deviceId);
        Impl.Service service = new Impl.Service();
        Impl.ScanStatus status;
        do
        {
            status = Impl.PollService(out service, false);
            if (status == Impl.ScanStatus.AVAILABLE)
            {
                Debug.Log("service found: " + service.uuid);
                if (!string.IsNullOrEmpty(service.uuid) && !device.ContainsKey(service.uuid))
                {
                    device.Add(service.uuid, new Dictionary<string, string>());
                }
            }
            Thread.Sleep(10);
        } while (status != Impl.ScanStatus.FINISHED);
        // wait some delay to prevent error
        Thread.Sleep(200);
        Impl.Characteristic c = new Impl.Characteristic();
        foreach (var s in device)
        {
            Impl.ScanCharacteristics(deviceId, s.Key);
            do {
                status = Impl.PollCharacteristic(out c, false);
                if (status == Impl.ScanStatus.AVAILABLE)
                {
                    Debug.Log("characteristic found: " + c.uuid + ", user description: " + c.userDescription);
                    if (!s.Value.ContainsKey(c.uuid))
                    {
                        s.Value.Add(c.uuid, c.userDescription);
                    }
                }
            } while (status != Impl.ScanStatus.FINISHED);
        }
        return device;
    }

    public static bool WritePackage(string deviceId, string serviceUuid, string characteristicUuid, byte[] data)
    {
        Impl.BLEData packageSend;
        packageSend.buf = new byte[512];
        packageSend.size = (short)data.Length;
        packageSend.deviceId = deviceId;
        packageSend.serviceUuid = serviceUuid;
        packageSend.characteristicUuid = characteristicUuid;
        for (int i = 0; i < data.Length; i++)
            packageSend.buf[i] = data[i];
        return Impl.SendData(in packageSend, true);
    }

    public static void ReadPackage()
    {
        Impl.BLEData packageReceived;
        bool result = Impl.PollData(out packageReceived, true);
        if (result)
        {
            if (packageReceived.size > 512)
                throw new ArgumentOutOfRangeException("Please keep your ble package at a size of maximum 512, cf. spec!\n" 
                    + "This is to prevent package splitting and minimize latency.");
            Debug.Log("received package from characteristic: " + packageReceived.characteristicUuid 
                + " and size " + packageReceived.size + " use packageReceived.buf to access the data.");
        }
    }

    public void Close()
    {
        Impl.Quit();
        isConnected = false;
    }

    public static string GetError()
    {
        Impl.ErrorMessage buf;
        Impl.GetError(out buf);
        return buf.msg;
    }

    ~BLE()
    {
        Close();
    }
}