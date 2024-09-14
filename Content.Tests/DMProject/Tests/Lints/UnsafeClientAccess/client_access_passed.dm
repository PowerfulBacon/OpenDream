#pragma UnsafeClientAccess error

/proc/RunTest()
	var/mob/M = usr
	var/client/C = M.client
	if (C == null)
		return
	var/client/D = C
	return D.ckey
