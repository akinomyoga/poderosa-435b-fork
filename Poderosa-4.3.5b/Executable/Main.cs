/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: Main.cs,v 1.1 2010/11/19 15:40:51 kzmi Exp $
 */
#if EXECUTABLE
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using Poderosa.Util;
using Poderosa.Boot;
using Poderosa.Plugins;

using System.Text.RegularExpressions;

namespace Poderosa.Executable
{
	internal class Root
	{
        private static IPoderosaApplication _poderosaApplication;

#if MONOLITHIC
        private static string CreatePluginManifest() {
            StringBuilder bld = new StringBuilder();
            bld.Append("manifest {\r\n  Poderosa.Monolithic.exe {\r\n");
            bld.Append("  plugin=Poderosa.Preferences.PreferencePlugin\r\n");
            bld.Append("  plugin=Poderosa.Serializing.SerializeServicePlugin\r\n");
            bld.Append("  plugin=Poderosa.Commands.CommandManagerPlugin\r\n");
            bld.Append("  plugin=Poderosa.Forms.WindowManagerPlugin\r\n");
            bld.Append("  plugin=Poderosa.Protocols.ProtocolsPlugin\r\n");
            bld.Append("  plugin=Poderosa.Terminal.TerminalEmulatorPlugin\r\n");
            bld.Append("  plugin=Poderosa.Sessions.SessionManagerPlugin\r\n");
            bld.Append("  plugin=Poderosa.Sessions.TerminalSessionsPlugin\r\n");
            bld.Append("  plugin=Poderosa.Sessions.CygwinPlugin\r\n");
            bld.Append("  plugin=Poderosa.Sessions.TelnetSSHPlugin\r\n");
            bld.Append("  plugin=Poderosa.Sessions.ShortcutFilePlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.UsabilityPlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.MRUPlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.TerminalUIPlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.SSHUtilPlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.OptionDialogPlugin\r\n");
            bld.Append("  plugin=Poderosa.Usability.StartupActionPlugin\r\n");
            bld.Append("  plugin=Poderosa.SerialPort.SerialPortPlugin\r\n");
            bld.Append("  plugin=Poderosa.XZModem.XZModemPlugin\r\n");
            bld.Append("  plugin=Poderosa.PortForwardingCommand.PortForwardingCommandPlugin\r\n");
            bld.Append("  plugin=Poderosa.MacroInternal.MacroPlugin\r\n");
            bld.Append("  plugin=Poderosa.LogViewer.PoderosaLogViewerPlugin\r\n");
            //bld.Append("  plugin=Poderosa.HelloWorldPlugin\r\n");
            //bld.Append("  plugin=Poderosa.MitiDemo.MitiDemoPlugin\r\n");
            //bld.Append("  plugin=PluginTest.Program\r\n");
#if TESTSESSION
            bld.Append("  plugin=Poderosa.Sessions.SessionTestPlugin\r\n");
#endif
            bld.Append("}\r\n}");
		    return bld.ToString();
        }
#endif
        public static void Run(string[] args) {
#if MONOLITHIC
            _poderosaApplication = PoderosaStartup.CreatePoderosaApplication(CreatePluginManifest(), AppDomain.CurrentDomain.BaseDirectory, args);
#else
            _poderosaApplication = PoderosaStartup.CreatePoderosaApplication(args);
#endif
            if(_poderosaApplication!=null) //アプリケーションが作成されなければ
                _poderosaApplication.Start();
        }

        //実行開始
        [STAThread]
        public static void Main(string[] args) {
            try {
                Run(args);
            }
            catch(Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
        }
	}

}
#endif