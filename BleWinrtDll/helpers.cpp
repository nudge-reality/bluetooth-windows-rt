#include "Helpers.h"

long hsh(wchar_t* wstr)
{
	long hash = 5381;
	int c;
	while (c = *wstr++)
		hash = ((hash << 5) + hash) + c;
	return hash;
}

const uint8_t BYTE_ORDER[] = { 3, 2, 1, 0, 5, 4, 7, 6, 8, 9, 10, 11, 12, 13, 14, 15 };
guid make_guid(const wchar_t* value)
{
	to_guid to_guid;
	memset(&to_guid, 0, sizeof(to_guid));
	int offset = 0;
	for (int i = 0; i < wcslen(value); i++) {
		if (value[i] >= '0' && value[i] <= '9')
		{
			uint8_t digit = value[i] - '0';
			to_guid.buf[BYTE_ORDER[offset / 2]] += offset % 2 == 0 ? digit << 4 : digit;
			offset++;
		}
		else if (value[i] >= 'A' && value[i] <= 'F')
		{
			uint8_t digit = 10 + value[i] - 'A';
			to_guid.buf[BYTE_ORDER[offset / 2]] += offset % 2 == 0 ? digit << 4 : digit;
			offset++;
		}
		else if (value[i] >= 'a' && value[i] <= 'f')
		{
			uint8_t digit = 10 + value[i] - 'a';
			to_guid.buf[BYTE_ORDER[offset / 2]] += offset % 2 == 0 ? digit << 4 : digit;
			offset++;
		}
		else
		{
			// skip char
		}
	}
	return to_guid.guid;
}


// global flag to release calling thread
mutex quitLock;
bool quitFlag = false;

bool QuittableWait(condition_variable& signal, unique_lock<mutex>& waitLock) {
	{
		lock_guard quit_lock(quitLock);
		if (quitFlag)
			return true;
	}
	signal.wait(waitLock);
	lock_guard quit_lock(quitLock);
	return quitFlag;
}