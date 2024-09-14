#pragma UnsafeClientAccess error

/proc/RunTest()
	var/mob/M = usr
	// Istype should make a variable safe
	if (istype(M.client, /client))
		var/client/C = M.client
		return C.ckey
