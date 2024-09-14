#pragma UnsafeClientAccess error

/proc/RunTest()
	var/mob/M = usr
	var/client/C = M.client
	if (C == null)
		return
	return C.ckey
