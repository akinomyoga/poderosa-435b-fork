/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TerminalBase.cs,v 1.8 2011/06/19 08:02:15 kzmi Exp $
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

using Poderosa.Util;
using Poderosa.Sessions;
using Poderosa.Document;
using Poderosa.ConnectionParam;
using Poderosa.Protocols;
using Poderosa.Forms;
using Poderosa.View;

namespace Poderosa.Terminal
{
	//TODO 名前とは裏腹にあんまAbstractじゃねーな またフィールドが多すぎるので整理する。
	/// <summary>
	/// <ja>
	/// ターミナルエミュレータの中枢となるクラスです。
	/// </ja>
	/// <en>
	/// Class that becomes core of terminal emulator.
	/// </en>
	/// </summary>
	/// <remarks>
	/// <ja>
	/// このクラスの解説は、まだありません。
	/// </ja>
	/// <en>
	/// This class has not explained yet. 
	/// </en>
	/// </remarks>
	public abstract class AbstractTerminal : ICharProcessor, IByteAsyncInputStream {
		/// <summary>
		/// 
		/// </summary>
		/// <exclude/>
		public delegate void AfterExitLockDelegate();

		private ScrollBarValues _scrollBarValues;
		private ICharDecoder     _decoder;
		private TerminalDocument _document;
		private IAbstractTerminalHost  _session;
		private LogService _logService;
		private IModalTerminalTask _modalTerminalTask;
		private PromptRecognizer _promptRecognizer;
		private IntelliSense _intelliSense;
		private PopupStyleCommandResultRecognizer _commandResultRecognizer;

		private bool _cleanup = false;

		protected List<AfterExitLockDelegate> _afterExitLockActions;
		protected GLineManipulator _manipulator;
		protected TextDecoration _currentdecoration;
		protected TerminalMode _terminalMode;
		protected TerminalMode _cursorKeyMode; //_terminalModeは別物。AIXでのviで、カーソルキーは不変という例が確認されている

		protected abstract void ChangeMode(TerminalMode tm);
		protected abstract void ResetInternal();

		protected ProcessCharResult _processCharResult;

		//ICharDecoder
		public abstract void ProcessChar(char ch);

		internal virtual int EncodeInputMouse(Keys key,int x,int y,out byte[] seq,bool release){
			// Keys.LButton
			// Keys.RButton
			// Keys.MButton
			// Keys.XButton1
			// Keys.XButton2
			seq=null;
			return 0;
		}
		/// <summary>
		/// <ja>指定したキー入力に対する送信バイトを取得します。</ja>
		/// </summary>
		/// <param name="key"><ja>キー入力を指定します。</ja></param>
		/// <returns><ja>1バイトでキー入力を表現出来る場合に、その値を返します。1バイトで表現できない場合には 0xFF を返します。</ja></returns>
		internal virtual byte EncodeInputByte(Keys key){
			byte r=KeyboardInfo.ConvertToChar(key);
			if(r!=0){
				if(0!=(key&Keys.Control))r&=0x1F;
				return r;
			}
			return 0xFF;
		}
		/// <summary>
		/// <ja>指定したキー入力に対する Control Sequence を生成・取得します。</ja>
		/// </summary>
		/// <param name="key"><ja>押されたキー本体についての情報を指定します。</ja></param>
		/// <param name="seq"><ja>キー入力に対応する Control Sequence を格納した配列を返します。</ja></param>
		/// <returns>
		/// <ja>Control Sequence の長さを返します。
		/// 対応する Control Sequence が見付からなかった場合は 0 を返します。
		/// これは以前の実装 SequenceKeyData 関数の動作と異なります。
		/// SequenceKeyData は例外を投げました。
    /// </ja>
		/// </returns>
		internal abstract int EncodeInputKey1(Keys key,out byte[] seq);

    private byte[] keycode_single_byte=null;
    /// <summary>
    /// 指定したキー入力に対する Control Sequence を生成・取得します。
    /// </summary>
    /// <param name="key">キー入力についての情報を指定します。</param>
    /// <param name="seq">Control sequence (符号化されたキー入力) を格納する配列を返します。</param>
    /// <returns>
    /// Control Sequence の長さをバイト単位で指定します。
    /// 対応する Control Sequence が見付からなかった場合は 0 を返します。
    /// </returns>
    internal virtual int EncodeInputKey2(Keys key,out byte[] seq){
      byte r=this.EncodeInputByte(key);
      if(r!=0){
        if(keycode_single_byte==null)
          keycode_single_byte=new byte[1];

        if(0!=(key&Keys.Control))r&=0x1F;
        keycode_single_byte[0]=r;
        seq=keycode_single_byte;
        return 1;
      }else{
        return this.EncodeInputKey1(key,out seq);
      }
    }

		public AbstractTerminal(TerminalInitializeInfo info) {
			TerminalEmulatorPlugin.Instance.LaterInitialize();

			_session = info.Session;
			
			//_invalidateParam = new InvalidateParam();
			_document = new TerminalDocument(info.InitialWidth, info.InitialHeight);
			_document.SetOwner(_session.ISession);
			_afterExitLockActions = new List<AfterExitLockDelegate>();

			_decoder = new ISO2022CharDecoder(this, EncodingProfile.Get(info.Session.TerminalSettings.Encoding));
			_terminalMode = TerminalMode.Normal;
			_currentdecoration = TextDecoration.Default;
			_manipulator = new GLineManipulator();
			_scrollBarValues = new ScrollBarValues();
			_logService = new LogService(info.TerminalParameter, _session.TerminalSettings);
			_promptRecognizer = new PromptRecognizer(this);
			_intelliSense = new IntelliSense(this);
			_commandResultRecognizer = new PopupStyleCommandResultRecognizer(this);

			if(info.Session.TerminalSettings.LogSettings!=null)
				_logService.ApplyLogSettings(_session.TerminalSettings.LogSettings, false);

			//event handlers
			ITerminalSettings ts = info.Session.TerminalSettings;
			ts.ChangeEncoding += delegate(EncodingType t) { this.Reset(); };
			ts.ChangeRenderProfile += delegate(RenderProfile prof) {
				TerminalControl tc = _session.TerminalControl;
				if(tc!=null) tc.ApplyRenderProfile(prof);
			};
		}

		//XTERMを表に出さないためのメソッド
		public static AbstractTerminal Create(TerminalInitializeInfo info) {
			//return new XTerm(info);
			return new mwg.RosaTerm.RosaTerminal(info);
		}

		public IPoderosaDocument IDocument {
			get {
				return _document;
			}
		}
		public TerminalDocument GetDocument() {
			return _document;
		}
		protected ITerminalSettings GetTerminalSettings() {
			return _session.TerminalSettings;
		}
		protected ITerminalConnection GetConnection() {
			return _session.TerminalConnection;
		}
		protected RenderProfile GetRenderProfile() {
			ITerminalSettings settings = _session.TerminalSettings;
			if(settings.UsingDefaultRenderProfile)
				return GEnv.DefaultRenderProfile;
			else
				return settings.RenderProfile;
		}

		public TerminalMode TerminalMode {
			get {
				return _terminalMode;
			}
		}
		public TerminalMode CursorKeyMode {
			get {
				return _cursorKeyMode;
			}
		}
		public ILogService ILogService {
			get {
				return _logService;
			}
		}
		internal LogService LogService {
			get {
				return _logService;
			}
		}
		internal PromptRecognizer PromptRecognizer {
			get {
				return _promptRecognizer;
			}
		}
		internal IntelliSense IntelliSense {
			get {
				return _intelliSense;
			}
		}
		internal PopupStyleCommandResultRecognizer PopupStyleCommandResultRecognizer {
			get {
				return _commandResultRecognizer;
			}
		}
		public IShellCommandExecutor ShellCommandExecutor {
			get {
				return _commandResultRecognizer;
			}
		}
		public IAbstractTerminalHost TerminalHost {
			get {
				return _session;
			}
		}

		public void CloseBySession() {
			CleanupCommon();
		}

		protected virtual void ChangeCursorKeyMode(TerminalMode tm) {
			_cursorKeyMode = tm;
		}

		internal ScrollBarValues TransientScrollBarValues {
			get {
				return _scrollBarValues;
			}
		}

		#region ICharProcessor
		ProcessCharResult ICharProcessor.State {
			get {
				return _processCharResult;
			}
		}
		public void UnsupportedCharSetDetected(char code) {
			string desc;
			if(code=='0')
				desc = "0 (DEC Special Character)"; //これはよくあるので但し書きつき
			else
				desc = new String(code, 1);

			CharDecodeError(String.Format(GEnv.Strings.GetString("Message.AbstractTerminal.UnsupportedCharSet"), desc));
		}
		public void InvalidCharDetected(byte[] buf) {
			CharDecodeError(String.Format(GEnv.Strings.GetString("Message.AbstractTerminal.UnexpectedChar"), EncodingProfile.Get(GetTerminalSettings().Encoding).Encoding.WebName));
		}
		#endregion

		//受信側からの簡易呼び出し
		protected void Transmit(byte[] data) {
			_session.TerminalConnection.Socket.Transmit(new ByteDataFragment(data, 0, data.Length));
		}

		//文字系のエラー通知
		protected void CharDecodeError(string msg) {
			IPoderosaMainWindow window = _session.OwnerWindow;
			if(window==null) return;
			Debug.Assert(window.AsForm().InvokeRequired);

			Monitor.Exit(GetDocument()); //これは忘れるな
			switch(GEnv.Options.CharDecodeErrorBehavior) {
				case WarningOption.StatusBar:
					window.StatusBar.SetMainText(msg);
					break;
				case WarningOption.MessageBox:
					window.AsForm().Invoke(new CharDecodeErrorDialogDelegate(CharDecodeErrorDialog), window, msg);
					break;
			}
			Monitor.Enter(GetDocument());
		}
		private delegate void CharDecodeErrorDialogDelegate(IPoderosaMainWindow window, string msg);
		private void CharDecodeErrorDialog(IPoderosaMainWindow window, string msg) {
			WarningWithDisableOption dlg = new WarningWithDisableOption(msg);
			dlg.ShowDialog(window.AsForm());
			if(dlg.CheckedDisableOption) {
				GEnv.Options.CharDecodeErrorBehavior = WarningOption.Ignore;
			}
		}

		public void Reset() {
			//Encodingが同じ時は簡単に済ませることができる
			if(_decoder.CurrentEncoding.Type==GetTerminalSettings().Encoding)
				_decoder.Reset(_decoder.CurrentEncoding);
			else
				_decoder = new ISO2022CharDecoder(this, EncodingProfile.Get(GetTerminalSettings().Encoding));
		}

		//これはメインスレッドから呼び出すこと
		public virtual void FullReset() {
			lock(_document) {
				ChangeMode(TerminalMode.Normal);
				_document.ClearScrollingRegion();
				ResetInternal();
				_decoder = new ISO2022CharDecoder(this, EncodingProfile.Get(GetTerminalSettings().Encoding));
			}
		}

		//ModalTerminalTask周辺
		public virtual void StartModalTerminalTask(IModalTerminalTask task) {
			_modalTerminalTask = task;
			new ModalTerminalTaskSite(this).Start(task);
		}
		public virtual void EndModalTerminalTask() {
			_modalTerminalTask = null;
		}
		public IModalTerminalTask CurrentModalTerminalTask {
			get {
				return _modalTerminalTask;
			}
		}

		//コマンド結果の処理割り込み
		public void ProcessCommandResult(ICommandResultProcessor processor, bool start_with_linebreak) {
			_commandResultRecognizer.StartCommandResultProcessor(processor, start_with_linebreak);
		}

		#region IByteAsyncInputStream
		public void OnReception(ByteDataFragment data) {
			try {
				bool pass_to_terminal = true;
				if(_modalTerminalTask!=null) {
					bool show_input = _modalTerminalTask.ShowInputInTerminal;
					_modalTerminalTask.OnReception(data);
					if(!show_input) pass_to_terminal = false; //入力を見せない(XMODEMとか)のときはターミナルに与えない
				}

				//バイナリログの出力
				_logService.BinaryLogger.Write(data);

				if(pass_to_terminal) {
					TerminalDocument document = _document;
					lock(document) {
						//_invalidateParam.Reset();
						//ここから旧Input()
						_manipulator.Load(GetDocument().CurrentLine, 0);
						_manipulator.CaretColumn = GetDocument().CaretColumn;
						_manipulator.DefaultDecoration = _currentdecoration;

						//処理本体
						_decoder.OnReception(data);

						GetDocument().ReplaceCurrentLine(_manipulator.Export());
						GetDocument().CaretColumn = _manipulator.CaretColumn;
						//ここまで

						//右端にキャレットが来たときは便宜的に次行の頭にもっていく
						if(document.CaretColumn==document.TerminalWidth) {
							document.CurrentLineNumber++; //これによって次行の存在を保証
							document.CaretColumn = 0;
						}

						CheckDiscardDocument();
						AdjustTransientScrollBar();

						//現在行が下端に見えるようなScrollBarValueを計算
						int n = document.CurrentLineNumber-document.TerminalHeight+1-document.FirstLineNumber;
						if(n < 0) n = 0;

						//Debug.WriteLine(String.Format("E={0} C={1} T={2} H={3} LC={4} MAX={5} n={6}", _transientScrollBarEnabled, _tag.Document.CurrentLineNumber, _tag.Document.TopLineNumber, _tag.Connection.TerminalHeight, _transientScrollBarLargeChange, _transientScrollBarMaximum, n));
						if(IsAutoScrollMode(n)) {
							_scrollBarValues.Value = n;
							document.TopLineNumber = n + document.FirstLineNumber;
						}
						else
							_scrollBarValues.Value = document.TopLineNumber - document.FirstLineNumber;

						//Invalidateをlockの外に出す。このほうが安全と思われた
						
						//受信スレッド内ではマークをつけるのみ。タイマーで行うのはIntelliSenseに副作用あるので一時停止
						//_promptRecognizer.SetContentUpdateMark();
						_promptRecognizer.Recognize(); 
					}

					if(_afterExitLockActions.Count > 0) {
						Control main = _session.OwnerWindow.AsControl();
						foreach(AfterExitLockDelegate action in _afterExitLockActions) {
							main.Invoke(action);
						}
						_afterExitLockActions.Clear();
					}
				}

				if(_modalTerminalTask!=null) _modalTerminalTask.NotifyEndOfPacket();
				_session.NotifyViewsDataArrived();
			}
			catch(Exception ex) {
				RuntimeUtil.ReportException(ex);
			}
		}

		public void OnAbnormalTermination(string msg) {
			//TODO メッセージを GEnv.Strings.GetString("Message.TerminalDataReceiver.GenericError"),_tag.Connection.Param.ShortDescription, msg
			if(!GetConnection().IsClosed) { //閉じる指令を出した後のエラーは表示しない
				GetConnection().Close();
				ShowAbnormalTerminationMessage();
			}
			Cleanup(msg);
		}
		private void ShowAbnormalTerminationMessage() {
			IPoderosaMainWindow window = _session.OwnerWindow;
			if(window!=null) {
				Debug.Assert(window.AsForm().InvokeRequired);
				ITCPParameter tcp = (ITCPParameter)GetConnection().Destination.GetAdapter(typeof(ITCPParameter));
				if(tcp!=null) {
					string msg = String.Format(GEnv.Strings.GetString("Message.AbstractTerminal.TCPDisconnected"), tcp.Destination);

					switch(GEnv.Options.DisconnectNotification) {
						case WarningOption.StatusBar:
							window.StatusBar.SetMainText(msg);
							break;
						case WarningOption.MessageBox:
							window.Warning(msg); //TODO Disableオプションつきのサポート
							break;
					}
				}
			}
		}

		public void OnNormalTermination() {
			Cleanup(null);
		}
		#endregion

		private void Cleanup(string msg) {
			CleanupCommon();
			//NOTE _session.CloseByReceptionThread()は、そのままアプリ終了と直結する場合がある。すると、_logService.Close()の処理が終わらないうちに強制終了になってログが書ききれない可能性がある
			_session.CloseByReceptionThread(msg);
		}

		private void CleanupCommon() {
			if (!_cleanup) {
				_cleanup = true;
				TerminalEmulatorPlugin.Instance.ShellSchemeCollection.RemoveDynamicChangeListener((IShellSchemeDynamicChangeListener)GetTerminalSettings().GetAdapter(typeof(IShellSchemeDynamicChangeListener)));
				_logService.Close(_document.CurrentLine);
			}
		}

		private bool IsAutoScrollMode(int value_candidate) {
			TerminalDocument doc = _document;
			return _terminalMode==TerminalMode.Normal && 
				doc.CurrentLineNumber>=doc.TopLineNumber+doc.TerminalHeight-1 &&
				(!_scrollBarValues.Enabled || value_candidate+_scrollBarValues.LargeChange>_scrollBarValues.Maximum);
		}
		private void CheckDiscardDocument() {

			//mwg20111225
			//  ApplicationMode でも上から流れて出て行く様に変更した為、ちゃんと discard しないといけない。
			//  * それとは別に、CheckDiscardDocument を呼び出すタイミングが OnReception である事も疑問である。
			//    OnReception というよりは Document の(行数)変更時にチェックするべきではないのか?
			//    →データを 4KB 単位で受け取っている為、此方の方が呼び出される回数が少ないので妥当
			if(_session==null) return;
			//if(_session==null || _terminalMode==TerminalMode.Application) return;

			TerminalDocument document = _document;
			int del = document.DiscardOldLines(GEnv.Options.TerminalBufferSize+document.TerminalHeight);
			if(del > 0) {
				int newvalue = _scrollBarValues.Value - del;
				if(newvalue<0) newvalue=0;
				_scrollBarValues.Value = newvalue;
				document.InvalidatedRegion.InvalidatedAll = true; //本当はここまでしなくても良さそうだが念のため
			}
		}

		public void AdjustTransientScrollBar() {
			TerminalDocument document = _document;
			int paneheight = document.TerminalHeight;
			int docheight = Math.Max(document.LastLineNumber, document.TopLineNumber+paneheight-1)-document.FirstLineNumber+1;

			_scrollBarValues.Dirty = true;
			if((_terminalMode==TerminalMode.Application && !GEnv.Options.AllowsScrollInAppMode)
				|| paneheight >= docheight) {
				_scrollBarValues.Enabled = false;
				_scrollBarValues.Value = 0;

        //mwg: I2.9 bugfix 2012/10/10
        _scrollBarValues.Maximum=Math.Max(docheight-1,paneheight);
			}else{
				_scrollBarValues.Enabled = true;
				_scrollBarValues.Maximum = docheight-1;
				_scrollBarValues.LargeChange = paneheight;
			}
			//Debug.WriteLine(String.Format("E={0} V={1}", _transientScrollBarEnabled, _transientScrollBarValue));
		}

		public void SetTransientScrollBarValue(int value) {
			_scrollBarValues.Value = value;
			_scrollBarValues.Dirty = true;
		}

		public void CommitScrollBar(VScrollBar sb, bool dirty_only) {
			if(dirty_only && !_scrollBarValues.Dirty) return;

			sb.Enabled = _scrollBarValues.Enabled;
			sb.Maximum = _scrollBarValues.Maximum;
			sb.LargeChange = _scrollBarValues.LargeChange;
			//!!本来このif文は不要なはずだが、範囲エラーになるケースが見受けられた。その原因を探ってリリース直前にいろいろいじるのは危険なのでここは逃げる。後でちゃんと解明する。
			if(_scrollBarValues.Value < _scrollBarValues.Maximum)
				sb.Value = _scrollBarValues.Value;
			_scrollBarValues.Dirty = false;
		}

		//ドキュメントロック中でないと呼んではだめ
		public void IndicateBell() {
			IPoderosaMainWindow window = _session.OwnerWindow;
			if(window!=null) {
				Debug.Assert(window.AsForm().InvokeRequired);
				Monitor.Exit(GetDocument());
				window.StatusBar.SetStatusIcon(Poderosa.TerminalEmulator.Properties.Resources.Bell16x16);
				Monitor.Enter(GetDocument());
			}
 			if(GEnv.Options.BeepOnBellChar) Win32.MessageBeep(-1);
		}
	}
	
	//Escape Sequenceを使うターミナル
	internal abstract class EscapeSequenceTerminal : AbstractTerminal {
		private StringBuilder _escapeSequence;
		private IModalCharacterTask _currentCharacterTask;

		public EscapeSequenceTerminal(TerminalInitializeInfo info) : base(info) {
			_escapeSequence = new StringBuilder();
			_processCharResult = ProcessCharResult.Processed;
		}

		protected override void ResetInternal() {
			_escapeSequence = new StringBuilder();
			_processCharResult = ProcessCharResult.Processed;
		}

		public override void ProcessChar(char ch) {
			if(_processCharResult != ProcessCharResult.Escaping) {
				if(ch==0x1B) {
					_processCharResult = ProcessCharResult.Escaping;
				} else {
					if(_currentCharacterTask!=null) { //マクロなど、charを取るタイプ
						_currentCharacterTask.ProcessChar(ch);
					}

					this.LogService.XmlLogger.Write(ch);

					if(ch < 0x20 || (ch>=0x80 && ch<0xA0))
						_processCharResult = ProcessControlChar(ch);
					else
						_processCharResult = ProcessNormalChar(ch);
				}
			}
			else {
				if(ch=='\0') return; //シーケンス中にNULL文字が入っているケースが確認された なお今はXmlLoggerにもこのデータは行かない。
				_escapeSequence.Append(ch);
				bool end_flag = false; //escape sequenceの終わりかどうかを示すフラグ
				if(_escapeSequence.Length==1) { //ESC+１文字である場合
					end_flag = ('0'<=ch && ch<='9') || ('a'<=ch && ch<='z') || ('A'<=ch && ch<='Z') || ch=='>' || ch=='=' || ch=='|' || ch=='}' || ch=='~';
				}
				else if(_escapeSequence[0]==']') { //OSCの終端はBELかST(String Terminator)
					end_flag = ch==0x07 || ch==0x9c; 
					if (ch == '\\' && _escapeSequence[_escapeSequence.Length-2] == 0x1b) {
						// ESC \ も OSC の終端
						_escapeSequence.Remove(_escapeSequence.Length - 1, 1);
						end_flag = true;
					}
				}
				else if (this._escapeSequence[0] == '@') {
					end_flag = (ch == '0') || (ch == '1');
				}
				else {
					end_flag = ('a'<=ch && ch<='z') || ('A'<=ch && ch<='Z') || ch=='@' || ch=='~' || ch=='|' || ch=='{';
				}
				
				if(end_flag) { //シーケンスのおわり
					char[] seq = _escapeSequence.ToString().ToCharArray();

					this.LogService.XmlLogger.EscapeSequence(seq);
					
					try {
						char code = seq[0];
						_processCharResult = ProcessCharResult.Unsupported; //ProcessEscapeSequenceで例外が来た後で状態がEscapingはひどい結果を招くので
						_processCharResult = ProcessEscapeSequence(code, seq, 1);
						if(_processCharResult==ProcessCharResult.Unsupported)
							throw new UnknownEscapeSequenceException(String.Format("ESC {0}", new string(seq)));
					}
					catch(UnknownEscapeSequenceException ex) {
						CharDecodeError(GEnv.Strings.GetString("Message.EscapesequenceTerminal.UnsupportedSequence")+ex.Message);
						RuntimeUtil.SilentReportException(ex);
					}
					finally {
						_escapeSequence.Remove(0, _escapeSequence.Length);
					}
				}
				else
					_processCharResult = ProcessCharResult.Escaping;
			}
		}

		protected virtual ProcessCharResult ProcessControlChar(char ch) {
			if(ch=='\n' || ch==0xB) { //Vertical TabはLFと等しい
				LineFeedRule rule = GetTerminalSettings().LineFeedRule;
				if(rule==LineFeedRule.Normal || rule==LineFeedRule.LFOnly) {
					if(rule==LineFeedRule.LFOnly) //LFのみの動作であるとき
						DoCarriageReturn();
					DoLineFeed();
				}
				return ProcessCharResult.Processed;
			}
			else if(ch=='\r') {
				LineFeedRule rule = GetTerminalSettings().LineFeedRule;
				if(rule==LineFeedRule.Normal || rule==LineFeedRule.CROnly) {
					DoCarriageReturn();
					if(rule==LineFeedRule.CROnly)
						DoLineFeed();
				}
				return ProcessCharResult.Processed;
			}
			else if(ch==0x07) {
				this.IndicateBell();
				return ProcessCharResult.Processed;
			}
			else if(ch==0x08) {
				//行頭で、直前行の末尾が継続であった場合行を戻す
				if(_manipulator.CaretColumn==0) {
					TerminalDocument doc = GetDocument();
					int line = doc.CurrentLineNumber-1;
					if(line>=0 && doc.FindLineOrEdge(line).EOLType==EOLType.Continue) {
						doc.InvalidatedRegion.InvalidateLine(doc.CurrentLineNumber);
						doc.CurrentLineNumber = line;
						if(doc.CurrentLine==null)
							_manipulator.Clear(doc.TerminalWidth);
						else
							_manipulator.Load(doc.CurrentLine, doc.CurrentLine.DisplayLength-1); //NOTE ここはCharLengthだったが同じだと思って改名した
						doc.InvalidatedRegion.InvalidateLine(doc.CurrentLineNumber);
					}
				}
				else
					_manipulator.BackCaret();

				return ProcessCharResult.Processed;
			}
			else if(ch==0x09) {
				_manipulator.CaretColumn = GetNextTabStop(_manipulator.CaretColumn);
				return ProcessCharResult.Processed;
			}
			else if(ch==0x0E) {
				return ProcessCharResult.Processed; //以下２つはCharDecoderの中で処理されているはずなので無視
			}
			else if(ch==0x0F) {
				return ProcessCharResult.Processed;
			}
			else if(ch==0x00) {
				return ProcessCharResult.Processed; //null charは無視 !!CR NULをCR LFとみなす仕様があるが、CR LF CR NULとくることもあって難しい
			}
			else {
				//Debug.WriteLine("Unknown char " + (int)ch);
				//適当なグラフィック表示ほしい
				return ProcessCharResult.Unsupported;
			}
		}
		private void DoLineFeed() {
			GLine nl = _manipulator.Export();
			nl.EOLType = (nl.EOLType==EOLType.CR || nl.EOLType==EOLType.CRLF)? EOLType.CRLF : EOLType.LF;
			this.LogService.TextLogger.WriteLine(nl); //ログに行をcommit
			GetDocument().ReplaceCurrentLine(nl);
			GetDocument().LineFeed();
				
			//カラム保持は必要。サンプル:linuxconf.log
			int col = _manipulator.CaretColumn;
			_manipulator.Load(GetDocument().CurrentLine, col);
		}
		private void DoCarriageReturn() {
			_manipulator.CarriageReturn();
		}

		protected virtual int GetNextTabStop(int start) {
			int t = start;
			//tよりで最小の８の倍数へもっていく
			t += (8 - t % 8);
			if(t >= GetDocument().TerminalWidth) t = GetDocument().TerminalWidth-1;
			return t;
		}
		
		protected virtual ProcessCharResult ProcessNormalChar(char ch) {
			//既に画面右端にキャレットがあるのに文字が来たら改行をする
			int tw = GetDocument().TerminalWidth;
			if(_manipulator.CaretColumn+GLine.CalcDisplayLength(ch) > tw) {
				GLine l = _manipulator.Export();
				l.EOLType = EOLType.Continue;
				this.LogService.TextLogger.WriteLine(l); //ログに行をcommit
				GetDocument().ReplaceCurrentLine(l);
				GetDocument().LineFeed();
				_manipulator.Load(GetDocument().CurrentLine, 0);
			}

			//画面のリサイズがあったときは、_manipulatorのバッファサイズが不足の可能性がある
			if(tw > _manipulator.BufferSize)
				_manipulator.ExpandBuffer(tw);

			//通常文字の処理
			_manipulator.PutChar(ch, _currentdecoration);
			
			return ProcessCharResult.Processed;
		}

		protected abstract ProcessCharResult ProcessEscapeSequence(char code, char[] seq, int offset);

		//FormatExceptionのほかにOverflowExceptionの可能性もあるので
		protected static int ParseInt(string param, int default_value) {
			try {
				if(param.Length>0)
					return Int32.Parse(param);
				else
					return default_value;
			}
			catch(Exception ex) {
				throw new UnknownEscapeSequenceException(String.Format("bad number format [{0}] : {1}", param, ex.Message));
			}
		}

		protected static IntPair ParseIntPair(string param, int default_first, int default_second) {
			IntPair ret = new IntPair(default_first, default_second);

			string[] s = param.Split(';');
			
			if(s.Length >= 1 && s[0].Length>0) {
				try {
					ret.first = Int32.Parse(s[0]);
				}
				catch(Exception ex) {
					throw new UnknownEscapeSequenceException(String.Format("bad number format [{0}] : {1}", s[0], ex.Message));
				}
			}

			if(s.Length >= 2 && s[1].Length>0) {
				try {
					ret.second = Int32.Parse(s[1]);
				}
				catch(Exception ex) {
					throw new UnknownEscapeSequenceException(String.Format("bad number format [{0}] : {1}", s[1], ex.Message));
				}
			}

			return ret;
		}

		//ModalTaskのセットを見る
		public override void StartModalTerminalTask(IModalTerminalTask task) {
			base.StartModalTerminalTask(task);
			_currentCharacterTask = (IModalCharacterTask)task.GetAdapter(typeof(IModalCharacterTask));
		}
		public override void EndModalTerminalTask() {
			base.EndModalTerminalTask();
			_currentCharacterTask = null;
		}
	}

	//受信スレッドから次に設定すべきScrollBarの値を配置する。
	internal class ScrollBarValues {
		//受信スレッドでこれらの値を設定し、次のOnPaint等メインスレッドでの実行でCommitする
		private bool _dirty; //これが立っていると要設定
		private bool _enabled;
		private int  _value;
		private int  _largeChange;
		private int  _maximum;

		public bool Dirty {
			get {
				return _dirty;
			}
			set {
				_dirty = value;
			}
		}
		public bool Enabled {
			get {
				return _enabled;
			}
			set {
				_enabled = value;
			}
		}
		public int Value {
			get {
				return _value;
			}
			set {
				_value = value;
			}
		}
		public int LargeChange {
			get {
				return _largeChange;
			}
			set {
				_largeChange= value;
			}
		}
		public int Maximum {
			get {
				return _maximum;
			}
			set {
				_maximum = value;
			}
		}
	}

	internal interface ICharProcessor {
		void ProcessChar(char ch);
		ProcessCharResult State { get; }
		void UnsupportedCharSetDetected(char code);
		void InvalidCharDetected(byte[] data);
	}


	/// <summary>
	/// 
	/// </summary>
	/// <exclude/>
	public enum ProcessCharResult {
		Processed,
		Unsupported,
		Escaping
	}



	internal class UnknownEscapeSequenceException : Exception {
		public UnknownEscapeSequenceException(string msg) : base(msg) { }
	}

	internal struct IntPair {
		public int first;
		public int second;

		public IntPair(int f, int s) {
			first = f;
			second = s;
		}
	}
}
