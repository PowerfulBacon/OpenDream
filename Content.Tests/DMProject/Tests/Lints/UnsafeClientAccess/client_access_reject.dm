// COMPILE ERROR
#pragma UnsafeClientAccess error

/proc/RunTest()
	var/mob/M = usr
	var/client/C = M.client
	return C.ckey
