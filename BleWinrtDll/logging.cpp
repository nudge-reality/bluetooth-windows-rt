#include "Logging.h"
#include "BleWinrtDll.h"

using std::mutex;
using std::lock_guard;

mutex errorLock;
wchar_t last_error[2048];

void clearError() {
	lock_guard error_lock(errorLock);
	wcscpy_s(last_error, L"Ok");
}

void saveError(const wchar_t* message, ...) {
	lock_guard error_lock(errorLock);
	va_list args;
	va_start(args, message);
	vswprintf_s(last_error, message, args);
	va_end(args);
}

//Exported function: Definition in BleWinrtDll.h
void GetError(ErrorMessage* buf) {
	lock_guard error_lock(errorLock);
	wcscpy_s(buf->msg, last_error);
}