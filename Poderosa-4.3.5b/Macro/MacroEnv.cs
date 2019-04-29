/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: MacroEnv.cs,v 1.3 2011/06/27 16:06:55 kzmi Exp $
 */
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;

using Poderosa.Macro;
using Poderosa.Terminal;
using Poderosa.Sessions;
using Poderosa.ConnectionParam;
using Poderosa.Commands;
using PConnection = Poderosa.Macro.Connection;

namespace Poderosa.MacroInternal
{
	internal class ConnectionListImpl : ConnectionList {
		public override IEnumerator GetEnumerator() {
			return new EnumeratorWrapper(MacroPlugin.Instance.SessionManager.AllSessions.GetEnumerator());
		}
		public override PConnection ActiveConnection {
			get {
                ITerminalSession ts = (ITerminalSession)MacroPlugin.Instance.WindowManager.ActiveWindow.DocumentTabFeature.ActiveDocument.OwnerSession.GetAdapter(typeof(ITerminalSession));
                Debug.Assert(ts!=null);
				return new ConnectionImpl(ts);
			}
		}
		public override PConnection Open(TerminalParam param) {
            ITerminalSession ts = MacroUtil.InvokeOpenSessionOrNull(MacroPlugin.Instance.WindowManager.ActiveWindow, param);
			return ts==null? null : new ConnectionImpl(ts);
		}
	}
	internal class EnumeratorWrapper : IEnumerator {
		private IEnumerator<ISession> _inner;

		public EnumeratorWrapper(IEnumerator<ISession> i) {
			_inner = i;
		}

		public void Reset() {
			_inner.Reset();
		}
		public bool MoveNext() {
			return _inner.MoveNext();
		}
		public object Current {
			get {
                ITerminalSession ts = (ITerminalSession)_inner.Current.GetAdapter(typeof(ITerminalSession));
				return new ConnectionImpl(ts);
			}
		}
	}

	internal class ConnectionImpl : PConnection {

		private ITerminalSession _session;

		public ConnectionImpl(ITerminalSession ts) {
            _session = ts;
		}

		public override void Activate() {
            MacroUtil.InvokeActivate(_session.Terminal.IDocument);
		}
		
		public override void Close() {
            MacroUtil.InvokeClose(_session.Terminal.IDocument);
        }

		public override void Transmit(string data) {
            _session.TerminalTransmission.SendString(data.ToCharArray());
		}
		public override void TransmitLn(string data) {
            if(data.Length>0)
                _session.TerminalTransmission.SendString(data.ToCharArray());
            _session.TerminalTransmission.SendLineBreak();
		}
		public override void SendBreak() {
            _session.TerminalTransmission.Connection.TerminalOutput.SendBreak();
		}

		public override string ReceiveLine() {
			return GetCurrentReceptionPool().ReadLineFromMacroBuffer();
		}
		public override string ReceiveLine(int timeoutMillisecs) {
			return GetCurrentReceptionPool().ReadLineFromMacroBuffer(timeoutMillisecs);
		}
		public override string ReceiveData() {
			return GetCurrentReceptionPool().ReadAllFromMacroBuffer();
		}
		public override string ReceiveData(int timeoutMillisecs) {
			return GetCurrentReceptionPool().ReadAllFromMacroBuffer(timeoutMillisecs);
		}
        public override void WriteComment(string comment) {
            _session.Terminal.ILogService.Comment(comment);
		}

        private ReceptionDataPool GetCurrentReceptionPool() {
            return (ReceptionDataPool)_session.Terminal.CurrentModalTerminalTask.GetAdapter(typeof(ReceptionDataPool));
        }
	}

	internal class UtilImpl : Poderosa.Macro.Util {
		public override void MessageBox(string msg) {
            MacroUtil.InvokeMessageBox(msg);
		}
		public override void ShellExecute(string verb, string filename) {
			int r = Win32.ShellExecute(Win32.GetDesktopWindow(), verb, filename, "", "", 1).ToInt32(); //1‚ÍSW_SHOWNORMAL
			if(r<=31) 
				throw new ArgumentException(String.Format(MacroPlugin.Instance.Strings.GetString("Message.MacroEnv.ShellExecuteError"), verb, filename));
		}
		public override void Exec(string command) {
			int r = Win32.WinExec(command, 1);
			if(r<=31)
                throw new ArgumentException(String.Format(MacroPlugin.Instance.Strings.GetString("Message.MacroEnv.ExecError"), command));
		}
	}

	internal class DebugServiceImpl : DebugService {

		private readonly ITraceWindowManager _traceWindowManager;

		public DebugServiceImpl(ITraceWindowManager traceWindowManager) {
			_traceWindowManager = traceWindowManager;
		}

		public override void ShowTraceWindow() {
			_traceWindowManager.ShowTraceWindow();
		}

		public override void HideTraceWindow() {
			_traceWindowManager.HideTraceWindow();
		}

		public override void Trace(string msg) {
			_traceWindowManager.OperateTraceWindow(
				delegate(MacroTraceWindow window) {
					window.AddLine(msg);
				}
			);
		}

		public override void PrintStackTrace() {
			string st = System.Environment.StackTrace;
			_traceWindowManager.OperateTraceWindow(
				delegate(MacroTraceWindow window) {
					string[] s = st.Split(new char[] { '\n', '\r' });
					bool f = false;
					foreach (string l in s) {
						if (f && l.Length > 0)
							window.AddLine(l);
						if (!f && l.IndexOf("PrintStackTrace") != -1)
							f = true;
					}
				}
			);
		}
	}
}
