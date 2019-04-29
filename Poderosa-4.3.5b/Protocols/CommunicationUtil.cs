/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: CommunicationUtil.cs,v 1.1 2010/11/19 15:41:03 kzmi Exp $
 */
using System;
using System.Threading;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;

using Granados;
using Poderosa.Util;
using Poderosa.Forms;

namespace Poderosa.Protocols
{
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class CommunicationUtil {
		//cygwinÇÃìØä˙ìIê⁄ë±
		public static ITerminalConnection CreateNewLocalShellConnection(IPoderosaForm form, ICygwinParameter param) {
			return LocalShellUtil.PrepareSocket(form, param);
		}


	}
}
