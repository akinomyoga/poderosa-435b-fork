using Ref=System.Reflection;
using Diag=System.Diagnostics;

namespace mwg.Term{
	internal static class ProcessUtils{
#if true
		class ProcessReflector{
			const Ref::BindingFlags PRIVATE_INSTANCE=Ref::BindingFlags.Instance|Ref::BindingFlags.NonPublic;

			static Ref::FieldInfo fProcess_outputStreamReadMode=typeof(Diag::Process).GetField("outputStreamReadMode",PRIVATE_INSTANCE);
			static Ref::FieldInfo fProcess_pendingOutputRead=typeof(Diag::Process).GetField("pendingOutputRead",PRIVATE_INSTANCE);
			static Ref::FieldInfo fProcess_standardOutput=typeof(Diag::Process).GetField("standardOutput",PRIVATE_INSTANCE);

			static Ref::FieldInfo fProcess_errorStreamReadMode=typeof(Diag::Process).GetField("errorStreamReadMode",PRIVATE_INSTANCE);
			static Ref::FieldInfo fProcess_pendingErrorRead=typeof(Diag::Process).GetField("pendingErrorRead",PRIVATE_INSTANCE);
			static Ref::FieldInfo fProcess_standardError=typeof(Diag::Process).GetField("standardError",PRIVATE_INSTANCE);

			Diag::Process proc;
			public ProcessReflector(Diag::Process proc){
				this.proc=proc;
			}

			public SyncReadMode outputStreamReadMode{
				get{return (SyncReadMode)fProcess_outputStreamReadMode.GetValue(proc);}
				set{fProcess_outputStreamReadMode.SetValue(proc,value);}
			}
			public bool pendingOutputRead{
				get{return (bool)fProcess_pendingOutputRead.GetValue(proc);}
				set{fProcess_pendingOutputRead.SetValue(proc,value);}
			}
			public System.IO.StreamReader standardOutput{
				get{return (System.IO.StreamReader)fProcess_standardOutput.GetValue(proc);}
			}
			public SyncReadMode errorStreamReadMode{
				get{return (SyncReadMode)fProcess_errorStreamReadMode.GetValue(proc);}
				set{fProcess_errorStreamReadMode.SetValue(proc,value);}
			}
			public bool pendingErrorRead{
				get{return (bool)fProcess_pendingErrorRead.GetValue(proc);}
				set{fProcess_pendingErrorRead.SetValue(proc,value);}
			}
			public System.IO.StreamReader standardError{
				get{return (System.IO.StreamReader)fProcess_standardError.GetValue(proc);}
			}

			public System.Action<string> OutputReadNotifyUser{
				get{
					return (System.Action<string>)System.Delegate.CreateDelegate(
						typeof(System.Action<string>),proc,"OutputReadNotifyUser");
				}
			}
			public System.Action<string> ErrorReadNotifyUser{
				get{
					return (System.Action<string>)System.Delegate.CreateDelegate(
						typeof(System.Action<string>),proc,"ErrorReadNotifyUser");
				}
			}
		}
		enum SyncReadMode{
			MODE_UNDEF=0,
			MODE_SYNC=1,
			MODE_ASYNC=2
		}
		/// <summary>
		/// System.Diagnostics.Process.BeginOutputReadLine の一文字毎版。
		/// BeginOutputReadLine だと、行末に来るか、プログラムが終了するまで、次のデータが送られてこない。
		/// 其処で、行末が来なくても読み取った側から文字列を通知する様に改造した。
		/// </summary>
		public static mwg.IO.AsyncStringReader BeginOutputReadString(this Diag::Process process){
			ProcessReflector proc=new ProcessReflector(process);

			switch(proc.outputStreamReadMode){
				case SyncReadMode.MODE_UNDEF:
					proc.outputStreamReadMode=SyncReadMode.MODE_ASYNC;
					break;
				case SyncReadMode.MODE_SYNC:
					throw new System.InvalidOperationException("CantMixSyncAsyncOperation");
			}

			if(proc.pendingOutputRead){
			  throw new System.InvalidOperationException("PendingAsyncOperation");
			}
			proc.pendingOutputRead=true;

			System.IO.StreamReader standardOutput=proc.standardOutput;
			if(standardOutput==null){
				throw new System.InvalidOperationException("CantGetStandardOut");
			}

			mwg.IO.AsyncStringReader reader=new mwg.IO.AsyncStringReader(
				standardOutput.BaseStream,
				proc.OutputReadNotifyUser,
				standardOutput.CurrentEncoding
				);
			reader.BeginRead();
			return reader;
		}
		/// <summary>
		/// System.Diagnostics.Process.BeginErrorReadLine の一文字毎版。
		/// BeginErrorReadLine だと、行末に来るか、プログラムが終了するまで、次のデータが送られてこない。
		/// 其処で、行末が来なくても読み取った側から文字列を通知する様に改造した。
		/// </summary>
		public static mwg.IO.AsyncStringReader BeginErrorReadString(this Diag::Process process){
			ProcessReflector proc=new ProcessReflector(process);

			switch(proc.errorStreamReadMode){
				case SyncReadMode.MODE_UNDEF:
					proc.errorStreamReadMode=SyncReadMode.MODE_ASYNC;
					break;
				case SyncReadMode.MODE_SYNC:
					throw new System.InvalidOperationException("CantMixSyncAsyncOperation");
			}

			if(proc.pendingErrorRead){
			  throw new System.InvalidOperationException("PendingAsyncOperation");
			}
			proc.pendingErrorRead=true;

			System.IO.StreamReader standardError=proc.standardError;
			if(standardError==null){
				throw new System.InvalidOperationException("CantGetStandardOut");
			}

			mwg.IO.AsyncStringReader reader=new mwg.IO.AsyncStringReader(
				standardError.BaseStream,
				proc.ErrorReadNotifyUser,
				standardError.CurrentEncoding
				);
			reader.BeginRead();
			return reader;
		}

#else
		const Ref::BindingFlags PRIVATE_INSTANCE=Ref::BindingFlags.Instance|Ref::BindingFlags.NonPublic;

		/// <summary>
		/// System.Diagnostics.Process.BeginOutputReadLine の一文字毎版。
		/// BeginOutputReadLine だと、行末に来るか、プログラムが終了するまで、次のデータが送られてこない。
		/// 其処で、行末が来なくても読み取った側から文字列を通知する様に改造した。
		/// </summary>
		static Ref::FieldInfo fProcess_outputStreamReadMode=typeof(Diag::Process).GetField("outputStreamReadMode",PRIVATE_INSTANCE);
		static Ref::FieldInfo fProcess_pendingOutputRead=typeof(Diag::Process).GetField("pendingOutputRead",PRIVATE_INSTANCE);
		static Ref::FieldInfo fProcess_standardOutput=typeof(Diag::Process).GetField("standardOutput",PRIVATE_INSTANCE);
		public static mwg.IO.AsyncStringReader BeginOutputReadString(this Diag::Process proc){
			const int MODE_UNDEF=0;
			//const int MODE_SYNC=1;
			const int MODE_ASYNC=2;
			int outputStreamReadMode=(int)fProcess_outputStreamReadMode.GetValue(proc);
			if(outputStreamReadMode==MODE_UNDEF){
				fProcess_outputStreamReadMode.SetValue(proc,MODE_ASYNC);
			}else if(outputStreamReadMode!=MODE_ASYNC){
				throw new System.InvalidOperationException("CantMixSyncAsyncOperation");
			}

			if((bool)fProcess_pendingOutputRead.GetValue(proc)){
			  throw new System.InvalidOperationException("PendingAsyncOperation");
			}
			fProcess_pendingOutputRead.SetValue(proc,true);

			mwg.IO.AsyncStringReader reader;
			{
				System.IO.StreamReader standardOutput=(System.IO.StreamReader)fProcess_standardOutput.GetValue(proc);
				if(standardOutput==null){
					throw new System.InvalidOperationException("CantGetStandardOut");
				}
				System.IO.Stream stream=standardOutput.BaseStream;

				System.Action<string> callback=(System.Action<string>)System.Delegate.CreateDelegate(
					typeof(System.Action<string>),proc,"OutputReadNotifyUser");

				reader=new mwg.IO.AsyncStringReader(stream,(System.Action<string>)callback,standardOutput.CurrentEncoding);
			}
			reader.BeginRead();
			return reader;
		}
		/// <summary>
		/// System.Diagnostics.Process.BeginErrorReadLine の一文字毎版。
		/// BeginErrorReadLine だと、行末に来るか、プログラムが終了するまで、次のデータが送られてこない。
		/// 其処で、行末が来なくても読み取った側から文字列を通知する様に改造した。
		/// </summary>
		static Ref::FieldInfo fProcess_errorStreamReadMode=typeof(Diag::Process).GetField("errorStreamReadMode",PRIVATE_INSTANCE);
		static Ref::FieldInfo fProcess_pendingErrorRead=typeof(Diag::Process).GetField("pendingErrorRead",PRIVATE_INSTANCE);
		static Ref::FieldInfo fProcess_standardError=typeof(Diag::Process).GetField("standardError",PRIVATE_INSTANCE);
		public static mwg.IO.AsyncStringReader BeginErrorReadString(this Diag::Process proc){
			const int MODE_UNDEF=0;
			//const int MODE_SYNC=1;
			const int MODE_ASYNC=2;
			int errorStreamReadMode=(int)fProcess_errorStreamReadMode.GetValue(proc);
			if(errorStreamReadMode==MODE_UNDEF){
				fProcess_errorStreamReadMode.SetValue(proc,MODE_ASYNC);
			}else if(errorStreamReadMode!=MODE_ASYNC){
				throw new System.InvalidOperationException("CantMixSyncAsyncOperation");
			}

			if((bool)fProcess_pendingErrorRead.GetValue(proc)){
			  throw new System.InvalidOperationException("PendingAsyncOperation");
			}
			fProcess_pendingErrorRead.SetValue(proc,true);

			mwg.IO.AsyncStringReader reader;
			{
				System.IO.StreamReader standardError=(System.IO.StreamReader)fProcess_standardError.GetValue(proc);
				if(standardError==null){
					throw new System.InvalidOperationException("CantGetStandardError");
				}
				System.IO.Stream stream=standardError.BaseStream;

				System.Action<string> callback=(System.Action<string>)System.Delegate.CreateDelegate(
					typeof(System.Action<string>),proc,"ErrorReadNotifyUser");

				reader=new mwg.IO.AsyncStringReader(stream,(System.Action<string>)callback,standardError.CurrentEncoding);
			}
			reader.BeginRead();
			return reader;
		}
#endif
	}
}