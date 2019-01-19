/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: LocalShell.cs,v 1.4 2010/12/24 22:26:28 kzmi Exp $
 */
#define MWG20111106

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using Microsoft.Win32;

using Poderosa.Util;
using Poderosa.Forms;
using Poderosa.Plugins;

using EncodingType=Poderosa.ConnectionParam.EncodingType;
 
namespace Poderosa.Protocols
{
	internal abstract class LocalShellUtil {
		
		//接続用ソケットのサポート
		protected static Socket _listener;
		protected static int _localPort;
		//同期
		protected static object _lockObject = new object();

		//接続先のSocketを準備して返す。失敗すればparentを親にしてエラーを表示し、nullを返す。
		internal static ITerminalConnection PrepareSocket(IPoderosaForm parent, ICygwinParameter param) {
			try {
				return new Connector(param).Connect();
			}
			catch(Exception ex) {
				//string key = IsCygwin(param)? "Message.CygwinUtil.FailedToConnect" : "Message.SFUUtil.FailedToConnect";
				string key = "Message.CygwinUtil.FailedToConnect";
				parent.Warning(PEnv.Strings.GetString(key)+ex.Message);
				return null;
			}
		}
		public static Connector AsyncPrepareSocket(IInterruptableConnectorClient client, ICygwinParameter param) {
			Connector c = new Connector(param, client);
			new Thread(new ThreadStart(c.AsyncConnect)).Start();
			return c;
		}


		/// <summary>
		/// Exception from LocalShellUtil
		/// </summary>
		internal class LocalShellUtilException : Exception {
			public LocalShellUtilException(string message, Exception innerException)
				: base(message, innerException) { }
		}


		/// <summary>
		/// 
		/// </summary>
		/// <exclude/>
		public class Connector : IInterruptable {
			private ICygwinParameter _param;
			private Process _process;
			private IInterruptableConnectorClient _client;
			private Thread _asyncThread;
			private bool _interrupted;

			public Connector(ICygwinParameter param) {
				_param = param;
			}
			public Connector(ICygwinParameter param, IInterruptableConnectorClient client) {
				_param = param;
				_client = client;
			}

			public void AsyncConnect() {
				bool success = false;
				_asyncThread = Thread.CurrentThread;
				try {
					ITerminalConnection result = Connect();
					if (!_interrupted) {
						success = true;
						Debug.Assert(result != null);
						ProtocolUtil.FireConnectionSucceeded(_param);
						_client.SuccessfullyExit(result);
					}
				}
				catch (Exception ex) {
					if (!(ex is LocalShellUtilException)) {
						RuntimeUtil.ReportException(ex);
					}
					if (!_interrupted) {
						_client.ConnectionFailed(ex.Message);
						ProtocolUtil.FireConnectionFailure(_param, ex.Message);
					}
				}
				finally {
					if (!success && _process != null && !_process.HasExited)
						_process.Kill();
				}
			}
			public void Interrupt() {
				_interrupted = true;
			}

			public ITerminalConnection Connect() {
				lock(_lockObject) {
					if(_localPort==0)
						PrepareListener();
				}

				//string cygtermPath = "cygterm\\"+(IsCygwin(_param)? "cygterm.exe" : "sfuterm.exe");
				//string connectionName = IsCygwin(_param)? "Cygwin" : "SFU";
				string cygtermPath = String.Format("{0}cygterm\\cygterm.exe", ProtocolUtil.ProtocolsPluginHomeDir);
				//string connectionName = "Cygwin";

				ITerminalParameter term = (ITerminalParameter)_param.GetAdapter(typeof(ITerminalParameter));

				//mwg: Construct args for cygterm.exe
				System.Text.StringBuilder args=new System.Text.StringBuilder();
				args.AppendFormat(" -p {0}",_localPort);
				args.AppendFormat(" -v HOME=\"{0}\"",_param.Home);
				args.AppendFormat(" -v TERM=\"{0}\"",term.TerminalType);
				args.AppendFormat(" -s \"{0}\"",_param.ShellName);
				args.AppendFormat(" -v ROSATERM=\"{0}\"","rosaterm");

				ProcessStartInfo psi = new ProcessStartInfo(cygtermPath, args.ToString());
				PrepareEnv(psi, _param);
				psi.CreateNoWindow = true;
				psi.ErrorDialog = true;
				psi.UseShellExecute = false;
				psi.WindowStyle = ProcessWindowStyle.Hidden;

				//mwg: Set working directory/mwg
				string wdir = _param.Home;
				if (wdir.StartsWith("/")) wdir = wdir.Substring(1);
				wdir = System.IO.Path.Combine(CygwinUtil.GuessRootDirectory(_param.CygwinDir), wdir);
				psi.WorkingDirectory = wdir;

				try {
					_process = Process.Start(psi);
				}catch (System.ComponentModel.Win32Exception ex) {
					throw new LocalShellUtilException(PEnv.Strings.GetString("Message.CygwinUtil.FailedToRunCygterm") + ": " + cygtermPath, ex);
				}
				while (true) {
					List<Socket> chk = new List<Socket>();
					chk.Add(_listener);
					Socket.Select(chk, null, null, 100);
					if (_interrupted) return null;
					if (chk.Count > 0)
						break;
				}
				Socket sock = _listener.Accept();
				if(_interrupted) return null;

				TelnetNegotiator neg = new TelnetNegotiator(term.TerminalType, term.InitialWidth, term.InitialHeight);
				TelnetParameter shellparam = new TelnetParameter();
				shellparam.Destination = "localhost";
				shellparam.SetTerminalName(term.TerminalType);
				shellparam.SetTerminalSize(term.InitialWidth, term.InitialHeight);
				TelnetTerminalConnection r = new TelnetTerminalConnection(shellparam, neg, new PlainPoderosaSocket(sock));
				r.Destination = (ITerminalParameter)_param.GetAdapter(typeof(ITerminalParameter)); //TelnetでなくオリジナルのCygwinParamで上書き
				r.UsingSocks = false;
				return r;
			}

		}

		protected static void PrepareListener() {
			_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_localPort = 20345;
			do {
				try {
					_listener.Bind(new IPEndPoint(IPAddress.Loopback, _localPort));
					_listener.Listen(1);
					break;
				}
                catch (Exception) {
					if(_localPort++==20360) throw new Exception("port overflow!!"); //さすがにこれはめったにないはず
				}
			} while(true);

		}

		protected static void PrepareEnv(ProcessStartInfo psi, ICygwinParameter p) {
			string cygdir = CygwinUtil.GuessRootDirectory(p.CygwinDir);
			string cygbin = System.IO.Path.Combine(cygdir, "bin");

			string path = psi.EnvironmentVariables["PATH"];
			psi.EnvironmentVariables.Remove("PATH");
			psi.EnvironmentVariables.Add("PATH", PathListAppend(cygbin, path));
		}
		private static string PathListAppend(string list, string newpath) {
			if (string.IsNullOrEmpty(list)) return newpath;
			return list[list.Length-1] == ';' ? list + newpath : list + ";" + newpath;
		}

		public static void Terminate() {
			if(_listener!=null) _listener.Close();
		}

		private static bool IsCygwin(LocalShellParameter tp) {
			return true;
		}

	}

	/// <summary>
	/// 
	/// </summary>
	/// <exclude/>
	public class SFUUtil
	{
		public static string DefaultHome {
			get {
				string a = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); 
				//最後の\の後にApplication Dataがあるので
				int t = a.LastIndexOf('\\');
				char drive = a[0];
				return "/dev/fs/"+Char.ToUpper(drive)+a.Substring(2, t-2).Replace('\\','/');
			}
		}
		public static string DefaultShell {
			get {
				return "/bin/csh -l";
			}
		}
		public static string GuessRootDirectory() {
			RegistryKey reg = null;
			string keyname = "SOFTWARE\\Microsoft\\Services for UNIX";
			reg = Registry.LocalMachine.OpenSubKey(keyname);
			if(reg==null) {
				//GUtil.Warning(GEnv.Frame, String.Format(PEnv.Strings.GetString("Message.SFUUtil.KeyNotFound"), keyname));
				return "";
			}
			string t = (string)reg.GetValue("InstallPath");
			reg.Close();
			return t;
		}

	}

	/// <summary>
	/// <ja>
	/// Cygwin接続時のパラメータを示すヘルパクラスです。
	/// </ja>
	/// <en>
	/// Helper class that shows parameter when Cygwin connecting.
	/// </en>
	/// </summary>
	/// <exclude/>
	public class CygwinUtil {
		/// <summary>
		/// <ja>
		/// デフォルトのホームディレクトリを返します。
		/// </ja>
		/// <en>
		/// Return the default home directory.
		/// </en>
		/// </summary>
		/// <remarks>
		/// <ja>
		/// このプロパティは、「"/home/"+Environment.UserName」の値を返します。
		/// </ja>
		/// <en>
		/// This property returns a value of ["/home/"+Environment.UserName].
		/// </en>
		/// </remarks>
		public static string DefaultHome {
			get {
				return "/home/"+Environment.UserName;
			}
		}
		/// <summary>
		/// <ja>
		/// デフォルトのシェルを返します。
		/// </ja>
		/// <en>
		/// Return the default shell.
		/// </en>
		/// </summary>
		/// <remarks>
		/// <ja>
		/// このプロパティは、「/bin/bash -i -l」という文字列を返します。
		/// </ja>
		/// <en>
		/// This property returns the string "/bin/bash -i -l".
		/// </en>
		/// </remarks>
		public static string DefaultShell {
			get {
				return "/bin/bash -i -l";
			}
		}

		/// <summary>
		/// <ja>
		/// デフォルトのCygwinのパスを返します。
		/// </ja>
		/// <en>
		/// Return the default Cygwin path.
		/// </en>
		/// </summary>
		public static string DefaultCygwinDir {
			get {
				return String.Empty;    // not specify
			}
		}

		/// <summary>
		/// <ja>
		/// デフォルトの端末タイプを返します。
		/// </ja>
		/// <en>
		/// Return the default terminal type.
		/// </en>
		/// </summary>
		public static string DefaultTerminalType {
			get {
				return "xterm";
			}
		}

#if MWG20111106
		/// <summary>
		/// <ja>レジストリを検索し、Cygwinのルートディレクトリを返します。</ja>
		/// <en>Searches registry keys to retrieve the root directory of Cygwin.</en>
		/// </summary>
		/// <returns>
		/// <ja>Cygwinのルートディレクトリと思わしき場所が返されます。</ja>
		/// <en>Returns a directory that seems to be the root directory of Cygwin.</en>
		/// </returns>
		public static string GuessRootDirectory(){
			return CygwinRootDirectory;
		}
        /// <summary>
        /// 指定した候補またはレジストリを検索し Cygwin のルートディレクトリを決定します。
        /// </summary>
        /// <param name="cygwinDirectory">ルートディレクトリの候補を指定します。</param>
        /// <returns>Cygwin のルートディレクトリのパス候補を返します。</returns>
        public static string GuessRootDirectory(string cygwinDirectory) {
            if (!string.IsNullOrEmpty(cygwinDirectory))
                return cygwinDirectory;
            return GuessRootDirectory();
        }
		//--------------------------------------------------------------------------
		static bool isOlderThan17=false;
		static string cygdir=null;
		static EncodingType cygencoding=EncodingType.UTF8;

		public static string CygwinRootDirectory{
			get{
				if(cygdir==null)InitCygwinDirectory();
				return cygdir;
			}
		}
		public static EncodingType CygwinDefaultEncoding{
			get{
				if(cygdir==null)InitCygwinDirectory();
				return cygencoding;
			}
		}

		static void InitCygwinDirectory(){
			if(cygdir!=null)return;

			string dir=InitCygdir_SearchInRegistry();
			cygdir=dir??"";

			if(isOlderThan17){
				Poderosa.Boot.PoderosaCulture culture=new Poderosa.Boot.PoderosaCulture();
				//if(Poderosa.Sessions.TerminalSessionsPlugin.Instance.CoreServicesPreference.Language==Language.Japanese)
				if(culture.IsJapaneseOS)
					cygencoding=EncodingType.SHIFT_JIS;
				else if(culture.IsSimplifiedChineseOS)
					cygencoding=EncodingType.GB2312;
				else if(culture.IsTraditionalChineseOS)
					cygencoding=EncodingType.BIG5;
				else if(culture.IsKoreanOS)
					cygencoding=EncodingType.EUC_KR;
				else
					cygencoding=EncodingType.ISO8859_1;
			}else
				cygencoding=EncodingType.UTF8;
		}
		static string InitCygdir_SearchInRegistry(){
			const string KEY17_32=@"SOFTWARE\Cygwin\setup";
			const string KEY17_64=@"SOFTWARE\Wow6432Node\Cygwin\setup";
			const string VAL17="rootdir";
			const string KEY16_32=@"SOFTWARE\Cygnus Solutions\Cygwin\mounts v2\/";
			const string KEY16_64=@"SOFTWARE\Wow6432Node\Cygnus Solutions\Cygwin\\mounts v2\/";
			const string VAL16="native";

			string data;

			isOlderThan17=false;
			if(TryGetRegKeyValue(Registry.CurrentUser,KEY17_32,VAL17,out data))return data;
			if(TryGetRegKeyValue(Registry.LocalMachine,KEY17_32,VAL17,out data))return data;
			if(TryGetRegKeyValue(Registry.CurrentUser,KEY17_64,VAL17,out data))return data;
			if(TryGetRegKeyValue(Registry.LocalMachine,KEY17_64,VAL17,out data))return data;

			isOlderThan17=true;
			if(TryGetRegKeyValue(Registry.CurrentUser,KEY16_32,VAL16,out data))return data;
			if(TryGetRegKeyValue(Registry.LocalMachine,KEY16_32,VAL16,out data))return data;
			if(TryGetRegKeyValue(Registry.CurrentUser,KEY16_64,VAL16,out data))return data;
			if(TryGetRegKeyValue(Registry.LocalMachine,KEY16_64,VAL16,out data))return data;

			isOlderThan17=false;
			PEnv.ActiveForm.Warning(String.Format(
				PEnv.Strings.GetString("Message.CygwinUtil.KeyNotFound2"),
				KEY17_32+"\\"+VAL17,
				KEY16_32+"\\"+VAL16
				));

			return null;
		}
		//--------------------------------------------------------------------------

		private static bool TryGetRegKeyValue(RegistryKey root,string keyname,string valname,out string data){
			data=null;
			RegistryKey kCygwin=root.OpenSubKey(keyname);
			if(kCygwin==null)return false;
			data=(string)kCygwin.GetValue(valname);
			return data!=null&&data!="";
		}
		private static bool TryCygwinDir(string place,out string data){
			data=null;
			if(!System.IO.File.Exists(System.IO.Path.Combine(place,@"bin\bash.exe")))return false;
			data=place;
			return false;
		}
#else
		/// <summary>
		/// <ja>
		/// レジストリを検索し、Cygwinのルートディレクトリを返します。
		/// </ja>
		/// <en>
		/// The registry is retrieved, and the root directory of Cygwin is returned. 
		/// </en>
		/// </summary>
		/// <returns><ja>Cygwinのルートディレクトリと思わしき場所が返されます。</ja><en>A root directory of Cygwin and a satisfactory place are returned. </en></returns>
		public static string GuessRootDirectory() {
			//HKCU -> HKLMの順でサーチ
			string rootDir;
			rootDir = GetCygwinRootDirectory(Registry.CurrentUser, false);
			if (rootDir != null)
				return rootDir;
			rootDir = GetCygwinRootDirectory(Registry.LocalMachine, false);
			if (rootDir != null)
				return rootDir;
			if (IntPtr.Size == 8) {	// we're in 64bit
				rootDir = GetCygwinRootDirectory(Registry.LocalMachine, true);
				if (rootDir != null)
					return rootDir;
			}

			//TODO 必ずしもActiveFormでいいのか、というのはあるけどな
			PEnv.ActiveForm.Warning(PEnv.Strings.GetString("Message.CygwinUtil.KeyNotFound"));
			return String.Empty;
		}

		private static string GetCygwinRootDirectory(RegistryKey baseKey, bool check64BitHive) {
			string software = check64BitHive ? "SOFTWARE\\Wow6432Node" : "SOFTWARE";
			string[][] keyValueNameArray = new string[][] {
					new string[] { software + "\\Cygnus Solutions\\Cygwin\\mounts v2\\/", "native" },
					new string[] { software + "\\Cygwin\\setup", "rootdir" }
			};

			foreach (string[] keyValueName in keyValueNameArray) {
				RegistryKey subKey = baseKey.OpenSubKey(keyValueName[0]);
				if (subKey != null) {
					try {
						string val = subKey.GetValue(keyValueName[1]) as string;
						if (val != null)
							return val;
					}
					finally {
						subKey.Close();
					}
				}
			}

			return null;
		}
#endif

	}
}
