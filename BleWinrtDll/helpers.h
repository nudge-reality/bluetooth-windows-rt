#pragma once
#include "stdafx.h"

using std::condition_variable;
using std::unique_lock;
using std::lock_guard;
using std::mutex;

using winrt::guid;

//Macro to determine the length of an C array
#define ARRAY_LENGTH(a) ((sizeof((a))) / (sizeof(*(a))))
#define UUID_LENGTH 256

extern mutex quitLock;
extern bool quitFlag;

union to_guid
{
	uint8_t buf[16];
	guid guid;
};

guid make_guid(const wchar_t* value);

// using hashes of uuids to omit storing the c-strings in reliable storage
long hsh(wchar_t* wstr);


bool QuittableWait(condition_variable& signal, unique_lock<mutex>& waitLock);