#pragma once

#include <ntifs.h>

PLOAD_IMAGE_NOTIFY_ROUTINE ImageLoadCallBack(PUNICODE_STRING fullImageName, HANDLE processId, PIMAGE_INFO imageInfo);