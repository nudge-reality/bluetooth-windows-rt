using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DebugBle
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("You can use this program to test the BleWinrtDll.dll. Make sure your Computer has Bluetooth enabled.");

            BLE ble = new BLE();
            string deviceId = null;

            BLE.BLEScan scan = BLE.ScanDevices();
            scan.Found = (_deviceId, deviceName) =>
            {
                Console.WriteLine("found device with name: " + deviceName);
                if (deviceId == null && deviceName == "D_Glove_IMU_L_F")
                {
                    deviceId = _deviceId;
                }
            };
            scan.Finished = () =>
            {
                Console.WriteLine("scan finished");
                if (deviceId == null)
                    deviceId = "-1";
            };
            while (deviceId == null)
                Thread.Sleep(500);

            scan.Cancel();
            if (deviceId == "-1")
            {
                Console.WriteLine("no device found!");
                Console.WriteLine("Press enter to exit the program...");
                Console.ReadLine();
                return;
            }

            var profile = BLE.GetProfile(deviceId);

            BLE.Impl.BLECharacteristic characteristic = new BLE.Impl.BLECharacteristic();

            foreach (var service in profile)
            {
                Console.WriteLine("Service: " + service.Key);
                foreach(var characteristics in service.Value)
                {
                    Console.WriteLine("Characteristics: " + characteristics.Key + " " + characteristics.Value);
                    if (characteristics.Key.StartsWith("{f9dcb357-be34-4c6f-9017-dd5e0bdd7b20}"))
                    {
                        characteristic.deviceId = deviceId;
                        characteristic.serviceUuid = service.Key;
                        characteristic.characteristicUuid = characteristics.Key;
                    }
                }
            }

            if (string.IsNullOrEmpty(characteristic.characteristicUuid))
            {
                Console.WriteLine("Could not find the requested characteristic!");
            }
            else
            {
                BLE.ReadData(characteristic);

                bool done = false;
                UInt32 i = 0;
                while (!done)
                {
                    byte[] data = new byte[512];
                    BLE.Impl.PollReadData(characteristic, data);
                    if(data != null)
                        Console.WriteLine("[{0}]", string.Join(", ", data));
                    if (i >= 100) done = true;
                    ++i;
                }
            }
            Console.WriteLine("Press enter to exit the program...");
            Console.ReadLine();
            BLE.Impl.Quit();
        }
    }
}
