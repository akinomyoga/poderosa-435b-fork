/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PipeTerminalOutput.cs,v 1.1 2011/08/04 15:28:58 kzmi Exp $
 */
using System;

using Poderosa.Protocols;

namespace Poderosa.Pipe
{

    /// <summary>
    /// Implementation of ITerminalOutput
    /// </summary>
    internal class PipeTerminalOutput : ITerminalOutput {

        /// <summary>
        /// Constructor
        /// </summary>
        public PipeTerminalOutput() {
        }

		public void SendBreak() {
            // do nothing
		}

        public void SendKeepAliveData() {
            // do nothing
        }

        public void AreYouThere() {
            // do nothing
        }

        public void Resize(int width, int height) {
            // do nothing
        }
    }

}
