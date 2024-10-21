#pragma once

#include "stdafx.h"
#include "BleWinrtDll.h"

using namespace std;

using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Web::Syndication;

using namespace Windows::Devices::Bluetooth;
using namespace Windows::Devices::Bluetooth::GenericAttributeProfile;
using namespace Windows::Devices::Enumeration;

using namespace Windows::Storage::Streams;

//Internal class for handling a coroutine constantly Reading data from characteristic
//Thread safe?
class Reading {
	bool reading = true;
	mutex readingMutex;

	mutex valuesMutex;
	condition_variable doneSignal;
	uint8_t values[512] = {};
	uint32_t valueCount = 0;
	uint32_t readIndex = -1;
	BLECharacteristic characteristics = {};
	bool done = false;

public:
	wchar_t* getDevice() { return this->characteristics.deviceId; }
	wchar_t* getService() { return this->characteristics.serviceUuid; }
	wchar_t* getCharacteristic() { return this->characteristics.characteristicUuid; }

	bool operator<(Reading& other);
	bool operator==(Reading& other);
	bool operator==(BLECharacteristic& other);

	void waitDone();
	void finish();
	void reuseRead();
	bool isReading();
	void stopReading(bool lock = false);
	void setData(uint8_t* data, uint32_t len);
	void getData(uint8_t* data, uint32_t len);

};