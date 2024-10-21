#pragma once

#include "stdafx.h"

//Macro to determine the length of an C array
#define ARRAY_LENGTH(a) ((sizeof((a))) / (sizeof(*(a))))

struct DeviceUpdate {
	wchar_t id[100];
	bool isConnectable = false;
	bool isConnectableUpdated = false;
	wchar_t name[50];
	bool nameUpdated = false;
};

struct Service {
	wchar_t uuid[100];
};

struct Characteristic {
	wchar_t uuid[100];
	wchar_t userDescription[100];
};

#define UUID_LENGTH 256

struct BLEData {
	uint8_t buf[512];
	uint16_t size;
	wchar_t deviceId[UUID_LENGTH];
	wchar_t serviceUuid[UUID_LENGTH];
	wchar_t characteristicUuid[UUID_LENGTH];
};

struct BLECharacteristic {
	wchar_t deviceId[UUID_LENGTH];
	wchar_t serviceUuid[UUID_LENGTH];
	wchar_t characteristicUuid[UUID_LENGTH];
};

struct ErrorMessage {
	wchar_t msg[1024];
};

enum class ScanStatus { PROCESSING, AVAILABLE, FINISHED };

extern "C" {

	__declspec(dllexport) void StartDeviceScan();

	__declspec(dllexport) ScanStatus PollDevice(DeviceUpdate* device, bool block);

	__declspec(dllexport) void StopDeviceScan();

	__declspec(dllexport) void ScanServices(wchar_t* deviceId);

	__declspec(dllexport) ScanStatus PollService(Service* service, bool block);

	__declspec(dllexport) void ScanCharacteristics(wchar_t* deviceId, wchar_t* serviceId);

	__declspec(dllexport) ScanStatus PollCharacteristic(Characteristic* characteristic, bool block);

	__declspec(dllexport) bool SubscribeCharacteristic(wchar_t* deviceId, wchar_t* serviceId, wchar_t* characteristicId, bool block);

	__declspec(dllexport) bool PollData(BLEData* data, bool block);

	__declspec(dllexport) bool SendData(BLEData* data, bool block);

	__declspec(dllexport) void ReadData(BLECharacteristic* id);

	__declspec(dllexport) void StopReadData(BLECharacteristic* id);

	__declspec(dllexport) void PollReadData(BLECharacteristic* id, uint8_t* data, uint16_t size);
	
	__declspec(dllexport) void Quit();

	__declspec(dllexport) void GetError(ErrorMessage* buf);
}
