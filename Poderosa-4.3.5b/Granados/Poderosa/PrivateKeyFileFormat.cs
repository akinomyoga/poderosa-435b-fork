/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PrivateKeyFileFormat.cs,v 1.2 2011/05/03 08:22:24 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace Granados.Poderosa {

	/// <summary>
	/// SSH private key file format
	/// </summary>
	internal enum PrivateKeyFileFormat {
		UNKNOWN,
		SSH1,
		SSH2_OPENSSH,
		SSH2_SSHCOM,
		SSH2_PUTTY,
	}

}
