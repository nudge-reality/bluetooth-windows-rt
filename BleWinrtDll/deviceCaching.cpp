#include "DeviceCaching.h"
#include "Logging.h"
#include "Helpers.h"
#define __WFILE__ L"DeviceCaching.cpp"

map<long, DeviceCacheEntry> cache;

IAsyncOperation<BluetoothLEDevice> retrieveDevice(wchar_t* deviceId) {
	if (cache.count(hsh(deviceId)))
		co_return cache[hsh(deviceId)].device;
	// !!!! BluetoothLEDevice.FromIdAsync may prompt for consent, in this case bluetooth will fail in unity!
	BluetoothLEDevice result = co_await BluetoothLEDevice::FromIdAsync(deviceId);
	if (result == nullptr) {
		saveError(L"%s:%d Failed to connect to device.", __WFILE__, __LINE__);
		co_return nullptr;
	}
	else {
		clearError();
		cache[hsh(deviceId)] = { result };
		co_return cache[hsh(deviceId)].device;
	}
}

IAsyncOperation<GattDeviceService> retrieveService(wchar_t* deviceId, wchar_t* serviceId) {
	auto device = co_await retrieveDevice(deviceId);
	if (device == nullptr)
		co_return nullptr;
	if (cache[hsh(deviceId)].services.count(hsh(serviceId)))
		co_return cache[hsh(deviceId)].services[hsh(serviceId)].service;
	GattDeviceServicesResult result = co_await device.GetGattServicesForUuidAsync(make_guid(serviceId), BluetoothCacheMode::Cached);
	if (result.Status() != GattCommunicationStatus::Success) {
		saveError(L"%s:%d Failed retrieving services.", __WFILE__, __LINE__);
		co_return nullptr;
	}
	else if (result.Services().Size() == 0) {
		saveError(L"%s:%d No service found with uuid ", __WFILE__, __LINE__);
		co_return nullptr;
	}
	else {
		clearError();
		cache[hsh(deviceId)].services[hsh(serviceId)] = { result.Services().GetAt(0) };
		co_return cache[hsh(deviceId)].services[hsh(serviceId)].service;
	}
}

IAsyncOperation<GattCharacteristic> retrieveCharacteristic(wchar_t* deviceId, wchar_t* serviceId, wchar_t* characteristicId) {
	auto service = co_await retrieveService(deviceId, serviceId);
	if (service == nullptr)
		co_return nullptr;
	if (cache[hsh(deviceId)].services[hsh(serviceId)].characteristics.count(hsh(characteristicId)))
		co_return cache[hsh(deviceId)].services[hsh(serviceId)].characteristics[hsh(characteristicId)].characteristic;
	GattCharacteristicsResult result = co_await service.GetCharacteristicsForUuidAsync(make_guid(characteristicId), BluetoothCacheMode::Cached);
	if (result.Status() != GattCommunicationStatus::Success) {
		saveError(L"%s:%d Error scanning characteristics from service %s with status %d", __WFILE__, __LINE__, serviceId, result.Status());
		co_return nullptr;
	}
	else if (result.Characteristics().Size() == 0) {
		saveError(L"%s:%d No characteristic found with uuid %s", __WFILE__, __LINE__, characteristicId);
		co_return nullptr;
	}
	else {
		clearError();
		cache[hsh(deviceId)].services[hsh(serviceId)].characteristics[hsh(characteristicId)] = { result.Characteristics().GetAt(0) };
		co_return cache[hsh(deviceId)].services[hsh(serviceId)].characteristics[hsh(characteristicId)].characteristic;
	}
}
