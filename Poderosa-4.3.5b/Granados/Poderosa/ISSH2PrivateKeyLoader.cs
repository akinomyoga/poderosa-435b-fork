/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: ISSH2PrivateKeyLoader.cs,v 1.1 2011/04/30 09:50:02 kzmi Exp $
 */
using System;

using Granados;
using Granados.PKI;

namespace Granados.Poderosa {

	internal interface ISSH2PrivateKeyLoader {

		/// <summary>
		/// Read private key parameters.
		/// </summary>
		/// <param name="passphrase">passphrase for decrypt the key file</param>
		/// <param name="keyPair">key pair is set</param>
		/// <param name="comment">comment is set. empty if it didn't exist</param>
		/// <exception cref="SSHException">failed to parse</exception>
		void Load(
			string passphrase,
			out KeyPair keyPair,
			out string comment);

	}

}
