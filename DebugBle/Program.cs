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
                    deviceId = _deviceId;
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

            foreach(var service in profile)
            {
                Console.WriteLine("Service: " + service.Key);
                foreach(var characteristics in service.Value)
                {
                    Console.WriteLine("Characteristics: " + characteristics.Key + " " + characteristics.Value);
                }
            }


            bool done = false;
            while (!done)
            {
                var data = BLE.ReadData(deviceId, profile);
                foreach (var dat in data)
                {
                    if (dat.Value != 0)
                    {
                        done = true;
                        break;
                    }
                }
            }

            Console.WriteLine("Press enter to exit the program...");
            Console.ReadLine();
        }
    }
}
