
namespace mwg.IO{
	using Gen=System.Collections.Generic;
	using System.Text;
	using Process=System.Diagnostics.Process;
	using Stream=System.IO.Stream;
	using Diag=System.Diagnostics;
	using Thr=System.Threading;


	internal class AsyncStringReader:System.IDisposable{
		const int DEFAULT_BUFFSIZE=0x400;
		const int MIN_BUFFSIZE=0x80;

		//--------------------------------------------------------------------------
		//	Initialize & Dispose
		//--------------------------------------------------------------------------
		System.IO.Stream stream=null;
		System.Text.Encoding encoding=null;
		System.Action<string> callback;
		public virtual Stream BaseStream{
			get{return this.stream;}
		}
		public virtual Encoding CurrentEncoding{
			get{return this.encoding;}
		}

		internal AsyncStringReader(Stream stream,System.Action<string> callback,Encoding encoding){
			this.Initialize(stream,callback,encoding,DEFAULT_BUFFSIZE);
		}
		internal AsyncStringReader(Stream stream,System.Action<string> callback,Encoding encoding,int bufferSize){
			this.Initialize(stream,callback,encoding,bufferSize);
		}

		void Initialize(Stream stream,System.Action<string> callback,Encoding encoding,int bufferSize){
			if(bufferSize<MIN_BUFFSIZE)bufferSize=MIN_BUFFSIZE;
			this.stream=stream;
			this.callback=callback;
			this.encoding=encoding;
			this.bbuff=new byte[bufferSize];
			this.cbuff=new char[encoding.GetMaxCharCount(bufferSize)];
			this.decoder=this.encoding.GetDecoder();
			this.mtxEof=new Thr::ManualResetEvent(false);
		}

		public void Dispose(){
			if(this.mtxEof!=null){
				this.mtxEof.Close();
				this.mtxEof=null;
			}
		}

		//--------------------------------------------------------------------------
		// Read
		//--------------------------------------------------------------------------
		Thr::ManualResetEvent mtxEof;
		System.Text.Decoder decoder=null;
		byte[] bbuff=null;
		char[] cbuff=null;
		bool reading=false;
		public void BeginRead(){
			if(!this.reading){
				this.reading=true;
				this.stream.BeginRead(this.bbuff,0,this.bbuff.Length,this.ReadCallBack,null);
			}
		}

		void ReadCallBack(System.IAsyncResult ar){
			int c;
			try{
				c=this.stream.EndRead(ar);
			}catch(System.IO.IOException){
				c=0;
			}catch(System.OperationCanceledException){
				c=0;
			}

			// eof
			if(c==0){
				this.AddString(null);
				this.mtxEof.Set();
				return;
			}

			c=this.decoder.GetChars(this.bbuff,0,c,this.cbuff,0);
			this.AddString(new string(this.cbuff,0,c));
			this.stream.BeginRead(this.bbuff,0,this.bbuff.Length,this.ReadCallBack,null);
		}

		private void AddString(string value){
			this.callback(value);
		}
		//--------------------------------------------------------------------------
		public void WaitEof(){
			if(this.mtxEof!=null){
				this.mtxEof.WaitOne();
				this.mtxEof.Close();
				this.mtxEof=null;
			}
		}
	}
	internal class AsyncStreamReader:System.IDisposable{
		public delegate void UserCallBack(string data);

		// Fields
		private int _maxCharsPerBuffer;
		private bool bLastCarriageReturn;
		private byte[] byteBuffer;
		private bool cancelOperation;
		private char[] charBuffer;
		private Decoder decoder;
		internal const int DefaultBufferSize=0x400;
		private Encoding encoding;
		private System.Threading.ManualResetEvent eofEvent;
		private System.Collections.Queue messageQueue;
		private const int MinBufferSize=0x80;
		private System.Diagnostics.Process process;
		private StringBuilder sb;
		private System.IO.Stream stream;

		private UserCallBack userCallBack;

		// Properties
		public virtual Stream BaseStream{
			//[System.Runtime.TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get{return this.stream;}
		}

		public virtual Encoding CurrentEncoding{
			//[System.Runtime.TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get{return this.encoding;}
		}

		//--------------------------------------------------------------------------
		// Initialization & Disposing
		//--------------------------------------------------------------------------
		internal AsyncStreamReader(Process process,Stream stream,UserCallBack callback,Encoding encoding)
			:this(process,stream,callback,encoding,0x400){}

		internal AsyncStreamReader(Process process,Stream stream,UserCallBack callback,Encoding encoding,int bufferSize){
			this.Init(process,stream,callback,encoding,bufferSize);
			this.messageQueue=new System.Collections.Queue();
		}

		private void Init(Process process,Stream stream,UserCallBack callback,Encoding encoding,int bufferSize){
			this.process=process;
			this.stream=stream;
			this.encoding=encoding;
			this.userCallBack=callback;
			this.decoder=encoding.GetDecoder();
			if(bufferSize<0x80){
				bufferSize=0x80;
			}
			this.byteBuffer=new byte[bufferSize];
			this._maxCharsPerBuffer=encoding.GetMaxCharCount(bufferSize);
			this.charBuffer=new char[this._maxCharsPerBuffer];
			this.cancelOperation=false;
			this.eofEvent=new Thr::ManualResetEvent(false);
			this.sb=null;
			this.bLastCarriageReturn=false;
		}

		//[System.Runtime.TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public virtual void Close(){
			this.Dispose(true);
		}

		void System.IDisposable.Dispose(){
			this.Dispose(true);
			System.GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing){
			if(this.stream!=null){
				if(disposing)this.stream.Close();
				this.stream=null;
				this.encoding=null;
				this.decoder=null;
				this.byteBuffer=null;
				this.charBuffer=null;
			}

			if(this.eofEvent!=null){
				this.eofEvent.Close();
				this.eofEvent=null;
			}
		}
		//--------------------------------------------------------------------------
		// BeginRead
		//--------------------------------------------------------------------------
		internal void BeginReadLine(){
			if(this.cancelOperation){
				this.cancelOperation=false;
			}
			if(this.sb==null){
				this.sb=new StringBuilder(0x400);
				this.stream.BeginRead(
					this.byteBuffer,0,this.byteBuffer.Length,
					new System.AsyncCallback(this.ReadBuffer),
					null);
			}else{
				this.FlushMessageQueue();
			}
		}

		internal void CancelOperation(){
			this.cancelOperation=true;
		}

		internal void WaitUtilEOF(){
			if(this.eofEvent!=null){
				this.eofEvent.WaitOne();
				this.eofEvent.Close();
				this.eofEvent=null;
			}
		}

		// 1. Read and Decode String
		private void ReadBuffer(System.IAsyncResult ar){
			int num;
			try{
				num=this.stream.EndRead(ar);
			}catch(System.IO.IOException){
				num=0;
			}catch(System.OperationCanceledException){
				num=0;
			}

			if(num==0){
				lock(this.messageQueue){
					if(this.sb.Length!=0){
						this.messageQueue.Enqueue(this.sb.ToString());
						this.sb.Length=0;
					}
					this.messageQueue.Enqueue(null);
				}

				try{
					this.FlushMessageQueue();
				}finally{
					this.eofEvent.Set();
				}
			}else{
				int charCount=this.decoder.GetChars(this.byteBuffer,0,num,this.charBuffer,0);
				this.sb.Append(this.charBuffer,0,charCount);
				this.GetLinesFromStringBuilder();
				this.stream.BeginRead(
					this.byteBuffer,0,this.byteBuffer.Length,
					new System.AsyncCallback(this.ReadBuffer),
					null);
			}
		}

		// 2. Extract Line and Store in the Queue
		private void GetLinesFromStringBuilder(){
			int startIndex=0;
			int endIndex=0;
			int length=this.sb.Length;
			if(this.bLastCarriageReturn&&length>0&&this.sb[0]=='\n'){
				startIndex=1;
				endIndex=1;
				this.bLastCarriageReturn=false;
			}
			while(endIndex<length){
				char ch=this.sb[endIndex];
				switch(ch){
					case '\r':
					case '\n':{
						string str=this.sb.ToString(startIndex,endIndex-startIndex);
						startIndex=endIndex+1;
						if(ch=='\r'&&startIndex<length&&this.sb[startIndex]=='\n'){
							startIndex++;
							endIndex++;
						}
						lock(this.messageQueue){
							this.messageQueue.Enqueue(str);
						}
						break;
					}
				}
				endIndex++;
			}
			if(this.sb[length-1]=='\r'){
				this.bLastCarriageReturn=true;
			}
			if(startIndex<length){
				this.sb.Remove(0,startIndex);
			}else{
				this.sb.Length=0;
			}
			this.FlushMessageQueue();
		}

		// 3. Call callback
		private void FlushMessageQueue(){
			while(this.messageQueue.Count>0){
				lock(this.messageQueue){
					if(this.messageQueue.Count<=0)continue;

					string data=(string)this.messageQueue.Dequeue();
					if(this.cancelOperation)continue;

					this.userCallBack(data);
				}
			}
		}

	}

}
