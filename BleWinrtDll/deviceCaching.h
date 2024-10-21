#pragma once
#include "stdafx.h"

using namespace winrt::Windows::Devices::Bluetooth::GenericAttributeProfile;
using namespace winrt::Windows::Devices::Bluetooth;
using namespace std;

using namespace winrt::Windows::Foundation;

// implement own caching instead of using the system-provicded cache as there is an AccessDenied error when trying to
// call GetCharacteristicsAsync on a service for which a reference is hold in global scope
// cf. https://stackoverflow.com/a/36106137
struct CharacteristicCacheEntry {
	GattCharacteristic characteristic = nullptr;
};
struct ServiceCacheEntry {
	GattDeviceService service = nullptr;
	map<long, CharacteristicCacheEntry> characteristics = { };
};
struct DeviceCacheEntry {
	BluetoothLEDevice device = nullptr;
	map<long, ServiceCacheEntry> services = { };
};

extern map<long, DeviceCacheEntry> cache;

IAsyncOperation<BluetoothLEDevice> retrieveDevice(wchar_t* deviceId);
IAsyncOperation<GattDeviceService> retrieveService(wchar_t* deviceId, wchar_t* serviceId);
IAsyncOperation<GattCharacteristic> retrieveCharacteristic(wchar_t* deviceId, wchar_t* serviceId, wchar_t* characteristicId);