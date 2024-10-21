#include "Reading.h"


void Reading::waitDone() {
	unique_lock valuesLock(valuesMutex);
	doneSignal.wait(valuesLock, [&] { return done; });
}

void Reading::finish()
{
	done = true;
	doneSignal.notify_one();
}

bool Reading::operator<(Reading& other) {
	int val = wcscmp(this->characteristics.deviceId, other.characteristics.deviceId);
	if (val != 0) return val < 0;
	val = wcscmp(this->characteristics.serviceUuid, other.characteristics.serviceUuid);
	if (val != 0) return val < 0;
	val = wcscmp(this->characteristics.characteristicUuid, other.characteristics.characteristicUuid);
	return val < 0;
}

bool Reading::operator==(Reading& other) {
	return *this == other.characteristics;
}

bool Reading::operator==(BLECharacteristic& other) {
	return (
		(wcscmp(this->characteristics.characteristicUuid, other.characteristicUuid) == 0) &&
		(wcscmp(this->characteristics.serviceUuid, other.serviceUuid) == 0) &&
		(wcscmp(this->characteristics.deviceId, other.deviceId) == 0));
}

void Reading::reuseRead() {
	lock_guard readingLock(readingMutex);
	this->reading = true;
}

bool Reading::isReading() {
	lock_guard readingLock(readingMutex);
	return this->reading;
}

void Reading::stopReading(bool lock)
{
	if (lock) {
		unique_lock readingLock(readingMutex);
	}
	else {
		lock_guard readingLock(readingMutex);
		this->reading = false;
	}
}

void Reading::setData(uint8_t* data, uint32_t len) {
	lock_guard lock(valuesMutex);
	uint64_t l = min(len, sizeof(values));
	memcpy(values, data, l);
	++readIndex;
}

void Reading::getData(uint8_t* data, uint32_t len)
{
	lock_guard lock(valuesMutex);
	uint64_t l = min(len, sizeof(values));
	memcpy(data, values, l);
}