/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TerminalControl.cs,v 1.6 2011/08/04 15:13:49 kzmi Exp $
 */
//#define DEBUG_UNRECOGNIZED_INPUT
#define MWG_MOUSE

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Threading;

using Poderosa.Document;
using Poderosa.View;
using Poderosa.Sessions;
using Poderosa.ConnectionParam;
using Poderosa.Protocols;
using Poderosa.Forms;
using Poderosa.Commands;

using Gen=System.Collections.Generic;
using Gdi=System.Drawing;
using mwg.RosaTerm.Utils;

using Mods=mwg.RosaTerm.KeyModifiers;

namespace Poderosa.Terminal {

	/// <summary>
	/// <ja>
	/// ターミナルを示すコントロールです。
	/// </ja>
	/// <en>
	/// Control to show the terminal.
	/// </en>
	/// </summary>
	/// <exclude/>
	/// 
	public class TerminalControl : CharacterDocumentViewer {
		//ID
		private int _instanceID;
		private static int _instanceCount = 1;
		public string InstanceID {
			get {
				return "TC"+_instanceID;
			}
		}

		private System.Windows.Forms.Timer _sizeTipTimer;
		private ITerminalControlHost _session;

		private TerminalEmulatorMouseHandler _terminalEmulatorMouseHandler;
		
		private Label _sizeTip;
		
		private delegate void AdjustIMECompositionDelegate();

		private bool _inIMEComposition; //IMEによる文字入力の最中であればtrueになる
		private bool _ignoreValueChangeEvent;

		private bool _escForVI;

		//再描画の状態管理
		private int _drawOptimizingState = 0; //この状態管理はOnWindowManagerTimer(), SmartInvalidate()参照
	 
		internal TerminalDocument GetDocument() {
			return _session.Terminal.GetDocument();
		}
		protected ITerminalSettings GetTerminalSettings() {
			return _session.TerminalSettings;
		}
		protected TerminalTransmission GetTerminalTransmission() {
			return _session.TerminalTransmission;
		}
		protected AbstractTerminal GetTerminal() {
			return _session.Terminal;
		}
		private bool IsConnectionClosed() {
			return _session.TerminalTransmission.Connection.IsClosed;
		}


		/// <summary>
		/// 必要なデザイナ変数です。
		/// </summary>
		private System.ComponentModel.Container components = null;

		public TerminalControl() {
			_instanceID = _instanceCount++;
			_enableAutoScrollBarAdjustment = false;
			_escForVI = false;
			this.EnabledEx = false;

			// この呼び出しは、Windows.Forms フォーム デザイナで必要です。
			InitializeComponent();

			_terminalEmulatorMouseHandler = new TerminalEmulatorMouseHandler(this);
			_mouseHandlerManager.AddLastHandler(_terminalEmulatorMouseHandler);
			//TODO タイマーは共用化？
			_sizeTipTimer = new System.Windows.Forms.Timer();
			_sizeTipTimer.Interval = 2000;
			_sizeTipTimer.Tick += new EventHandler(this.OnHideSizeTip);

			this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
		}
		public void Attach(ITerminalControlHost session) {
			_session = session;
			SetContent(session.Terminal.GetDocument());

			ITerminalEmulatorOptions opt = TerminalEmulatorPlugin.Instance.TerminalEmulatorOptions;
			_caret.Blink = opt.CaretBlink;
			_caret.Color = opt.CaretColor;
			_caret.Style = opt.CaretType;
			_caret.Reset();

			//KeepAliveタイマ起動は最も遅らせた場合でココ
			TerminalEmulatorPlugin.Instance.KeepAlive.Refresh(opt.KeepAliveInterval);

			//ASCIIWordBreakTable : 今は共有設定だが、Session固有にデータを持つようにするかもしれない含みを持たせて。
			ASCIIWordBreakTable table = ASCIIWordBreakTable.Default;
			table.Reset();
			foreach(char ch in opt.AdditionalWordElement)
				table.Set(ch, ASCIIWordBreakTable.LETTER);

			lock(GetDocument()) {
				_ignoreValueChangeEvent = true;
				_session.Terminal.CommitScrollBar(_VScrollBar, false);
				_ignoreValueChangeEvent = false;

				if(!IsConnectionClosed()) {
					Size ts = CalcTerminalSize(GetRenderProfile());

					//TODO ネゴ開始前はここを抑制したい
					if(ts.Width!=GetDocument().TerminalWidth || ts.Height!=GetDocument().TerminalHeight)
						ResizeTerminal(ts.Width, ts.Height);
				}
			}
			Invalidate(true);
		}
		public void Detach() {
			if(DebugOpt.DrawingPerformance) DrawingPerformance.Output();

			if(_inIMEComposition) ClearIMEComposition();
			_session = null;
			SetContent(null);
		}

		/// <summary>
		/// 使用されているリソースに後処理を実行します。
		/// </summary>
		protected override void Dispose( bool disposing ) {
			if( disposing ) {
				if(components != null) {
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		
		private void InitializeComponent() {
			this.SuspendLayout();
			this._sizeTip = new Label();
			// 
			// _sizeTip
			// 
			this._sizeTip.Visible = false;
			this._sizeTip.BorderStyle = BorderStyle.FixedSingle;
			this._sizeTip.TextAlign = ContentAlignment.MiddleCenter;
			this._sizeTip.BackColor = Color.FromKnownColor(KnownColor.Info);
			this._sizeTip.ForeColor = Color.FromKnownColor(KnownColor.InfoText);
			this._sizeTip.Size = new Size(64, 16);
			// 
			// TerminalPane
			// 
			this.TabStop = false;
			this.AllowDrop = true; 
			
			this.Controls.Add(_sizeTip);
			this.ImeMode = ImeMode.NoControl;
			this.ResumeLayout(false);

		}

		/*
		 * ↓  受信スレッドによる実行のエリア
		 */ 
		
		public void DataArrived() {
			//よくみると、ここを実行しているときはdocumentをロック中なので、上のパターンのようにSendMessageを使うとデッドロックの危険がある
			InternalDataArrived();
		}

		private void InternalDataArrived() {
			if(_session == null) return;	// ペインを閉じる時に _tag が null になっていることがある

			TerminalDocument document = GetDocument();
			if(!this.ITextSelection.IsEmpty) {
				document.InvalidatedRegion.InvalidatedAll = true; //面倒だし
				this.ITextSelection.Clear();
			}
			//Debug.WriteLine(String.Format("v={0} l={1} m={2}", _VScrollBar.Value, _VScrollBar.LargeChange, _VScrollBar.Maximum));
			if(DebugOpt.DrawingPerformance) DrawingPerformance.MarkReceiveData(GetDocument().InvalidatedRegion);
			SmartInvalidate();

			//部分変換中であったときのための調整
			if(_inIMEComposition) {
				if (this.InvokeRequired)
					this.Invoke(new AdjustIMECompositionDelegate(AdjustIMEComposition));
				else
					AdjustIMEComposition();
			}
		}

		private void SmartInvalidate() {
			//ここでDrawOptimizeStateをいじる。近接して到着するデータによる過剰な再描画を回避しつつ、タイマーで一定時間後には確実に描画されるようにする。
			//状態遷移は、データ到着とタイマーをトリガとする３状態の簡単なオートマトンである。
			switch (_drawOptimizingState) {
				case 0:
					_drawOptimizingState = 1;
					InvalidateEx();
					break;
				case 1:
					if (_session.TerminalConnection.Socket.Available)
						Interlocked.Exchange(ref _drawOptimizingState, 2); //間引きモードへ
					else
						InvalidateEx();
					break;
				case 2:
					break; //do nothing
			}
		}

		/*
		 * ↑  受信スレッドによる実行のエリア
		 * -------------------------------
		 * ↓  UIスレッドによる実行のエリア
		 */

		protected override void OnWindowManagerTimer() {
			base.OnWindowManagerTimer();

			switch (_drawOptimizingState) {
				case 0:
					break; //do nothing
				case 1:
					Interlocked.CompareExchange(ref _drawOptimizingState, 0, 1);
					break;
				case 2: //忙しくても偶には描画
					_drawOptimizingState = 1;
					InvalidateEx();
					break;
			}
		}

		private delegate void InvalidateDelegate1();
		private delegate void InvalidateDelegate2(Rectangle rc);
		private void DelInvalidate(Rectangle rc) {
			Invalidate(rc);
		}
		private void DelInvalidate() {
			Invalidate();
		}

		protected override void VScrollBarValueChanged() {
			if(_ignoreValueChangeEvent) return;
			TerminalDocument document = GetDocument();
			lock(document) {
				document.TopLineNumber = document.FirstLineNumber + _VScrollBar.Value;
				_session.Terminal.TransientScrollBarValues.Value = _VScrollBar.Value;
				Invalidate();
			}
		}

		/* キーボード処理系について
		 * 　送信は最終的にはSendChar/Stringへ行く。
		 * 
		 * 　そこに至る過程では、
		 *  ProcessCmdKey: Altキーの設定次第で、ベースクラスに渡す（＝コマンド起動を試みる）かどうか決める
		 *  ProcessDialogKey: 文字キー以外は基本的にここで処理。
		 *  OnKeyPress: 文字の送信
		 */
		private byte[] _sendCharBuffer = new byte[1];
		public void SendChar(char ch) { //ISからのコールバックあるので
			if(ch < 0x80) {
				//Debug.WriteLine("SendChar " + (int)ch);
				_sendCharBuffer[0] = (byte)ch;
				SendBytes(_sendCharBuffer);
			}
			else {
				byte[] data = EncodingProfile.Get(GetTerminalSettings().Encoding).GetBytes(ch);
				SendBytes(data);
			}
		}
		public void SendCharArray(char[] chs) {
			byte[] bytes = EncodingProfile.Get(GetTerminalSettings().Encoding).GetBytes(chs);
			SendBytes(bytes);
		}

		#region mwgSend
		//==========================================================================
		//    端末への入力をバイト列にして送信
		//--------------------------------------------------------------------------
		//CHK: 一文字を32文字以上にエンコードする Encoding は無いという仮定
		private byte[] mwgSendCharBuff=new byte[32];
		private void mwgSendChar(char ch){
			if(ch<0x80){
				// Unicode の 0x80<=ch<0x100 は、文字コードによって異なる
				mwgSendCharBuff[0]=(byte)ch;
				mwgSendBytes(mwgSendCharBuff,1);
			}else{
				int len=EncodingProfile.Get(GetTerminalSettings().Encoding).GetBytes(ch,mwgSendCharBuff);
				mwgSendBytes(mwgSendCharBuff,len);
			}
		}
		private void mwgSendBytes(byte[] data,int length){
			lock(GetDocument()){
				_caret.KeepActiveUntilNextTick();
				MakeCurrentLineVisible();
			}
			GetTerminalTransmission().Transmit(data,0,length);
		}
		//--------------------------------------------------------------------------
		private bool mwgSendKey(Keys key){
      if(key==Keys.Enter||key==Keys.LineFeed){
        SendCharArray(TerminalUtil.NewLineChars(GetTerminalSettings().TransmitNL));
        return true;
      }

      if((key&Keys.Alt)!=0&&GEnv.Options.LeftAltKey==AltKeyAction.Meta)
        key=key&~Keys.Alt|Mods.MetaE;

      byte[] data;
      int len=GetTerminal().EncodeInputKey2(key,out data);
      if(len>0){
        mwgSendBytes(data,len);
        return true;
      }else
        return false;
		}
		/// <summary>
		/// mwgSendKey の AltKeyAction を指定出来る版です。
		/// key は Keys.Alt を含んでいると想定します。
		/// </summary>
		/// <param name="key"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		private bool mwgSendAltKey(Keys key,AltKeyAction action){
      if(action==AltKeyAction.Meta&&(key&Mods.MetaE)!=0)
        key=key&~Mods.MetaE|Mods.Meta;

      byte[] data;
      int len=GetTerminal().EncodeInputKey2(key,out data);
      if(len>0){
        mwgSendBytes(data,len);
        return true;
      }else
        return false;
		}
		//--------------------------------------------------------------------------
		public void SendKey(Keys key){
			mwgSendKey(key);
		}
		#endregion

		#region Events to be notified to the terminal
		/// <summary>
		/// <en>Processes keyboard inputs modified by alter key, such as A-k, A-x, A-Up, etc.</en>
		/// <ja>Alt 修飾子付のキーボード入力を受け付けます。A-k / A-x / A-Up 等</ja>
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="key">押されたキーに関する情報を指定します。</param>
		/// <returns>キー入力が処理された時に true を返します。</returns>
		protected override bool ProcessCmdKey(ref Message msg, Keys key) {
			if(IsAcceptableUserInput()){
				if((key&Keys.Alt)!=0){
					if(GEnv.Options.LeftAltKey!=AltKeyAction.Menu&&(Win32.GetKeyState(Win32.VK_LMENU)&0x8000)!=0){
						mwgSendAltKey(key,GEnv.Options.LeftAltKey);
						return true; //mwg: 単なる Alt でも読み捨て
					}else if(GEnv.Options.RightAltKey!=AltKeyAction.Menu&&(Win32.GetKeyState(Win32.VK_RMENU)&0x8000)!=0){
						mwgSendAltKey(key,GEnv.Options.RightAltKey);
						return true; //mwg: 単なる Alt でも読み捨て
					}else
						return base.ProcessCmdKey(ref msg, key);
				}
				
				// Control が入っている場合は OnKeyPress を待たずに。
        byte[] seq;
        int len=GetTerminal().EncodeInputKey2(key,out seq);
        if(len>1||len==1&&(key&Keys.Control)!=0){
          mwgSendBytes(seq,len);
          //System.Console.WriteLine("dbg20121009:ProcessCmdKey: sent {0}",key);
          return true;
        }else{
          //System.Console.WriteLine("dbg20121009:ProcessCmdKey: skipped {0}",key);
        }

#if DEBUG_UNRECOGNIZED_INPUT
				System.Console.WriteLine("ProcessCmdKey: unknown key = "+key);
#endif
			}
			
			//これまでで処理できなければ上位へ渡す
			return base.ProcessCmdKey(ref msg, key);
		}

		/// <summary>
		/// <ja>特別なキーの入力を受け付けます。
		/// Enter Up Dn Right Left Del Ins Home End PgUp PgDn, C-記号, etc.</ja>
		/// <en>Processes special key inputs such as
		/// Enter, Up, Dn, Right, Left, Del, Ins, Home, End, PgUp, PgDn, C-symbols, etc.</en>
		/// </summary>
		/// <param name="key">押されたキーに関する情報を指定します。</param>
		/// <returns>キー入力が処理された場合に true を返します。</returns>
		protected override bool ProcessDialogKey(Keys key) {
			Keys modifiers = key & Keys.Modifiers;
			Keys keybody = key & Keys.KeyCode;

			//接続中でないとだめなキー
			if(IsAcceptableUserInput()) {
				//TODO Enter,Space,SequenceKey系もカスタムキーに入れてしまいたい
				char[] custom = TerminalEmulatorPlugin.Instance.CustomKeySettings.Scan(key); //カスタムキー
				if(custom!=null) {
					SendCharArray(custom);
					return true;
				}
				else if(ProcessAdvancedFeatureKey(modifiers, keybody)) {
					return true;
				}

				switch(key){
					case Keys.Enter: // Keys.Return
					case Keys.LineFeed:
						_escForVI = false;
						SendCharArray(TerminalUtil.NewLineChars(GetTerminalSettings().TransmitNL));
						return true;
					case Keys.Control|Keys.Space:
						SendChar('\0');
						return true;
					case Keys.Tab:
					case Keys.Tab|Keys.Shift:
						SendChar('\t');
						return true;
				}

				if((key&Keys.Alt)==0&&mwgSendKey(key))
					return true;

#if DEBUG_UNRECOGNIZED_INPUT
				System.Console.WriteLine("ProcessDialogKey: unknown key = "+key);
#endif
			}

			//常に送れるキー
			if(keybody==Keys.Apps) { //コンテキストメニュー
				TerminalDocument document = GetDocument();
				int x = document.CaretColumn;
				int y = document.CurrentLineNumber - document.TopLineNumber;
				SizeF p = GetRenderProfile().Pitch;
				_terminalEmulatorMouseHandler.ShowContextMenu(new Point((int)(p.Width * x), (int)(p.Height * y)));
				return true;
			}

			return base.ProcessDialogKey(key);
		}

		/// <summary>
		/// <ja>通常のキーボード入力を受け付けます。a / S-a 等。</ja>
		/// <en>Processes ordinary keyboard inputs such as a, S-a, etc.</en>
		/// </summary>
		/// <param name="e">キー入力に関連する情報を指定します。</param>
		protected override void OnKeyPress(KeyPressEventArgs e) {
			base.OnKeyPress(e);
			if (e.KeyChar == '\x001b') {
				_escForVI = true;
			}
			if(!IsAcceptableUserInput()) return;
			/* ここの処理について
			 * 　IMEで入力文字を確定すると（部分確定ではない）、WM_IME_CHAR、WM_ENDCOMPOSITION、WM_CHARの順でメッセージが送られてくる。
			 * 　Controlはその両方でKeyPressイベントを発生させるので、IMEの入力が２回送信されてしまう。
			 * 　一方部分確定のときはWM_IME_CHARのみである。
			 */
			//if((int)e.KeyChar>=100) {
			//    if(_currentMessage.Msg!=Win32.WM_IME_CHAR) return;
			//}
			if (this._escForVI) {
				this.SendChar(e.KeyChar);
			}
			else {
				this.SendChar(e.KeyChar);
				if (_session.TerminalSettings.EnabledCharTriggerIntelliSense && _session.Terminal.TerminalMode == TerminalMode.Normal)
					_session.Terminal.IntelliSense.ProcessChar(e.KeyChar);
			}
		}

		protected override void OnMouseWheel(MouseEventArgs e) {
			//base.OnMouseWheel(e); //CharacaterDocumentViewerは無視してよい
			if(!this.EnabledEx)return;
			
#if MWG_MOUSE
			if(this.IsAcceptableUserInput()){
				Keys flags=KeyboardInfo.GetModifierState();
				//if((flags&Keys.Shift)==0){ // shift は poderosa で処理。
				if(Control.IsKeyLocked(Keys.Scroll)){ // shift は poderosa で処理。
					// 便宜上 [↑]を XButton1 に [↓]を XButton2 に割り当てる。
					Keys buttons=e.Delta>0?Keys.XButton1:Keys.XButton2;
					buttons|=flags;

          //Gdi::Point pos=base.ClientPointToTextPosition(e.Location);
          //pos.X++;
          //pos.Y++;
          Gdi::Point pos=base.ClientPointToCharBorder(e.Location);
          pos.X++;
          pos.Y++;

					byte[] seq;
					int len=GetTerminal().EncodeInputMouse(buttons,pos.X,pos.Y,out seq,false);
					if(len>0){
						mwgSendBytes(seq,len);
						return;
					}
				}
			}
#endif

			//アプリケーションモードでは通常処理をやめてカーソル上下と同等の処理にする
			if(_session!=null && !GEnv.Options.AllowsScrollInAppMode && GetTerminal().TerminalMode==TerminalMode.Application) {
				int m = GEnv.Options.WheelAmount;
				for(int i=0; i<m; i++)
					mwgSendKey(Keys.None|(e.Delta>0? Keys.Up : Keys.Down));
				return;
			}

			if(_VScrollBar.Enabled) {
				int d = e.Delta / 120; //開発環境だとDeltaに120。これで1か-1が入るはず
				d *= GEnv.Options.WheelAmount;

				int newval = _VScrollBar.Value - d; 
				if(newval<0) newval=0;
				if(newval>_VScrollBar.Maximum-_VScrollBar.LargeChange) newval=_VScrollBar.Maximum-_VScrollBar.LargeChange+1;
				_VScrollBar.Value = newval;
			}
		}

#if MWG_MOUSE
    MouseButtons terminalMouseState=0;
    private bool UpdateMouseButtonState(MouseEventArgs e,MouseButtons button,Keys key,Gdi::Point pos,bool release){
      if((e.Button&button)!=button)return false;

      // 状態変化がなければ無視
      bool released=(terminalMouseState&button)==0;
      if(released==release)return false;

      byte[] seq;
      int len=GetTerminal().EncodeInputMouse(key,pos.X,pos.Y,out seq,release);
      if(len<=0)return false;
      
      mwgSendBytes(seq,len);
      if(release)
        terminalMouseState&=~button;
      else
        terminalMouseState|=button;
      return true;
    }
		protected override void OnMouseDown(MouseEventArgs e){
			if(this.IsAcceptableUserInput()){
				Keys flags=KeyboardInfo.GetModifierState();
				//if((flags&Keys.Shift)==0){
				if(Control.IsKeyLocked(Keys.Scroll)){
          Gdi::Point pos=base.ClientPointToCharBorder(e.Location);
          pos.X++;
          pos.Y++;
          //Gdi::Point pos=base.ClientPointToTextPosition(e.Location);
          //pos.X++;
          //pos.Y++;

					int f=0;
					if(UpdateMouseButtonState(e,MouseButtons.Left,flags|Keys.LButton,pos,false))f++;
					if(UpdateMouseButtonState(e,MouseButtons.Right,flags|Keys.RButton,pos,false))f++;
					if(UpdateMouseButtonState(e,MouseButtons.Middle,flags|Keys.MButton,pos,false))f++;
					if(f!=0)return;
				}
		  }
		  base.OnMouseDown(e);
		}
    protected override void OnMouseUp(MouseEventArgs e){
      if(this.IsAcceptableUserInput()){
        if(terminalMouseState==0)goto skip;

        // 位置・ボタン情報
        Keys flags=KeyboardInfo.GetModifierState();
        Gdi::Point pos=base.ClientPointToCharBorder(e.Location);
        pos.X++;
        pos.Y++;
        //Gdi::Point pos=base.ClientPointToTextPosition(e.Location);
        //pos.X++;
        //pos.Y++;

        // MouseTracking "Button Released"
        int f=0;
        if(UpdateMouseButtonState(e,MouseButtons.Left,flags|Keys.LButton,pos,true))f++;
        if(UpdateMouseButtonState(e,MouseButtons.Right,flags|Keys.RButton,pos,true))f++;
        if(UpdateMouseButtonState(e,MouseButtons.Middle,flags|Keys.MButton,pos,true))f++;
        if(f!=0)return;

        // MouseTracking "Release-Button Pressed"
        terminalMouseState&=~e.Button;
        if(terminalMouseState==0)
          if(UpdateMouseButtonState(e,0,flags,pos,false))
            return;
      }

    skip:
      base.OnMouseUp(e);
    }
#endif
		#endregion

		private bool ProcessAdvancedFeatureKey(Keys modifiers, Keys keybody) {
			if(_session.Terminal.TerminalMode==TerminalMode.Application) return false;

			if(_session.Terminal.IntelliSense.ProcessKey(modifiers, keybody))
				return true;
			else if(_session.Terminal.PopupStyleCommandResultRecognizer.ProcessKey(modifiers, keybody))
				return true;
			else
				return false;
		}

		private void SendBytes(byte[] data) {
			TerminalDocument doc = GetDocument();
			lock(doc) {
				//キーを押しっぱなしにしたときにキャレットがブリンクするのはちょっと見苦しいのでキー入力があるたびにタイマをリセット
				_caret.KeepActiveUntilNextTick();

				MakeCurrentLineVisible();
			}
			GetTerminalTransmission().Transmit(data);
		}

		private bool IsAcceptableUserInput() {
			//TODO: ModalTerminalTaskの存在が理由で拒否するときはステータスバーか何かに出すのがよいかも
			if(!this.EnabledEx || IsConnectionClosed() || _session.Terminal.CurrentModalTerminalTask!=null)
				return false;
			else
				return true;

		}

		private void MakeCurrentLineVisible() {
			TerminalDocument document = GetDocument();
			if(document.CurrentLineNumber-document.FirstLineNumber < _VScrollBar.Value) { //上に隠れた
				document.TopLineNumber = document.CurrentLineNumber;
				_session.Terminal.TransientScrollBarValues.Value = document.TopLineNumber-document.FirstLineNumber;
			}
			else if(_VScrollBar.Value + document.TerminalHeight <= document.CurrentLineNumber-document.FirstLineNumber) { //下に隠れた
				int n = document.CurrentLineNumber-document.FirstLineNumber - document.TerminalHeight + 1;
				if(n < 0) n = 0;
				GetTerminal().TransientScrollBarValues.Value = n;
				GetDocument().TopLineNumber = n + document.FirstLineNumber;
			}
		}

		protected override void OnResize(EventArgs args) {
			base.OnResize(args);

			//Debug.WriteLine(String.Format("TC RESIZE {0} {1} {2},{3}", _resizeCount++, DateTime.Now.ToString(), this.Size.Width, this.Size.Height));
			//Debug.WriteLine(new StackTrace(true).ToString());
			//最小化時にはなぜか自身の幅だけが０になってしまう
			if(this.DesignMode || this.FindForm()==null || this.FindForm().WindowState==FormWindowState.Minimized || _session == null) return;

			Size ts = CalcTerminalSize(GetRenderProfile());

			if(!IsConnectionClosed() && (ts.Width!=GetDocument().TerminalWidth || ts.Height!=GetDocument().TerminalHeight)) {
				ResizeTerminal(ts.Width, ts.Height);
				ShowSizeTip(ts.Width, ts.Height);
				CommitTransientScrollBar();
			}
		}

		private void OnHideSizeTip(object sender, EventArgs args) {
			Debug.Assert(!this.InvokeRequired);
			_sizeTip.Visible = false;
			_sizeTipTimer.Stop();
		}

		public override RenderProfile GetRenderProfile() {
			if(_session!=null) {
				ITerminalSettings ts = _session.TerminalSettings;
				if(ts.UsingDefaultRenderProfile)
					return GEnv.DefaultRenderProfile;
				else
					return ts.RenderProfile;
			}
			else
				return GEnv.DefaultRenderProfile;
		}

		protected override void CommitTransientScrollBar() {
			if(_session != null) {	// TerminalPaneを閉じるタイミングでこのメソッドが呼ばれたときにNullReferenceExceptionになるのを防ぐ
				_ignoreValueChangeEvent = true;
				GetTerminal().CommitScrollBar(_VScrollBar, true);	//!! ここ（スクロールバー）の処理は重い
				_ignoreValueChangeEvent = false;
			}
		}

		public override GLine GetTopLine() {
			//TODO Pane内のクラスチェンジができるようになったらここを改善
			return _session==null? base.GetTopLine() : GetDocument().TopLine;
		}

		protected override void AdjustCaret(Caret caret) {
			if(_session==null) return; 

			if(IsConnectionClosed() || !this.Focused || _inIMEComposition)
				caret.Enabled = false;
			else {
				TerminalDocument d = GetDocument();
				caret.X = d.CaretColumn;
				caret.Y = d.CurrentLineNumber - d.TopLineNumber;
				caret.Enabled = caret.Y>=0 && caret.Y<d.TerminalHeight;
			}
		}

		public Size CalcTerminalSize(RenderProfile prof) {
			SizeF charPitch = prof.Pitch;
			Win32.SystemMetrics sm = GEnv.SystemMetrics;
			int width = (int)Math.Floor((float)(this.ClientSize.Width - sm.ScrollBarWidth - CharacterDocumentViewer.BORDER * 2) / charPitch.Width);
			int height = (int)Math.Floor((float)(this.ClientSize.Height - CharacterDocumentViewer.BORDER * 2 + prof.LineSpacing) / (charPitch.Height + prof.LineSpacing));
			if (width <= 0) width = 1; //極端なリサイズをすると負の値になることがある
			if(height <= 0) height = 1;
			return new Size(width, height);
		}

		private void ShowSizeTip(int width, int height) {
			const int MARGIN = 8;
			//Form form = GEnv.Frame.AsForm();
			//if(form==null || !form.Visible) return; //起動時には表示しない
			if(!this.Visible) return;

			Point pt = new Point(this.Width-_VScrollBar.Width-_sizeTip.Width-MARGIN, this.Height-_sizeTip.Height-MARGIN);

			_sizeTip.Text = String.Format("{0} * {1}", width, height);
			_sizeTip.Location = pt;
			_sizeTip.Visible = true;

			_sizeTipTimer.Stop();
			_sizeTipTimer.Start();
		}

		//ピクセル単位のサイズを受け取り、チップを表示
		public void SplitterDragging(int width, int height) {
			SizeF charSize = GetRenderProfile().Pitch;
			Win32.SystemMetrics sm = GEnv.SystemMetrics;
			width  = (int)Math.Floor(((float)width - sm.ScrollBarWidth - sm.ControlBorderWidth*2) / charSize.Width);
			height = (int)Math.Floor((float)(height - sm.ControlBorderHeight*2) / charSize.Height);
			ShowSizeTip(width, height);
		}

		private void ResizeTerminal(int width, int height) {
			//Debug.WriteLine(String.Format("Resize {0} {1}", width, height));

			//Documentへ通知
			GetDocument().Resize(width, height);

			if(_session.Terminal.CurrentModalTerminalTask!=null) return; //別タスクが走っているときは無視
			GetDocument().SetScrollingRegion(0, height-1);//mwg: xterm は Application mode/Normal mode に拘らず ScrollingRegion をクリアしている。
			GetTerminal().Reset();
			if(_VScrollBar.Enabled) {
				bool scroll = IsAutoScrollMode();
				_VScrollBar.LargeChange = height;
				if(scroll)
					MakeCurrentLineVisible();
			}

			//接続先へ通知
			GetTerminalTransmission().Resize(width, height);
			InvalidateEx();
		}

		//現在行が見えるように自動的に追随していくべきかどうかの判定
		private bool IsAutoScrollMode() {
			TerminalDocument doc = GetDocument();
			return GetTerminal().TerminalMode==TerminalMode.Normal && 
				doc.CurrentLineNumber >= doc.TopLineNumber+doc.TerminalHeight-1 &&
				(!_VScrollBar.Enabled || _VScrollBar.Value+_VScrollBar.LargeChange>_VScrollBar.Maximum);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="prof">
		/// <ja>[mwg] 描画設定を保持する RenderProfile インスタンスを指定します。
		/// このインスタンスはこのメソッド呼び出し直後に破壊される可能性があります。
		/// </ja>
		/// </param>
		/// <remarks>
		/// <ja>[mwg] ApplyRenderProfile の実装に際して。
		/// 引数に渡される RenderProfile インスタンスは独自の寿命を持ちます。
		/// RenderProfile インスタンスを保持したい場合には、
		/// Clone メソッドでインスタンスをコピーして保持する様にして下さい。
		/// </ja>
		/// </remarks>
		public void ApplyRenderProfile(RenderProfile prof) {
			if(this.EnabledEx) {
				this.BackColor = prof.BackColor;
				Size ts = CalcTerminalSize(prof);
				if(!IsConnectionClosed() && (ts.Width!=GetDocument().TerminalWidth || ts.Height!=GetDocument().TerminalHeight)) {
					ResizeTerminal(ts.Width, ts.Height);
				}
				Invalidate();
			}
		}

		public void ApplyTerminalOptions(ITerminalEmulatorOptions opt) {
			if(this.EnabledEx) {
				if(GetTerminalSettings().UsingDefaultRenderProfile) {
					using(RenderProfile profile=opt.CreateRenderProfile())
						ApplyRenderProfile(profile);
				}
				_caret.Style = opt.CaretType;
				_caret.Blink = opt.CaretBlink;
				_caret.Color = opt.CaretColor;
				_caret.Reset();
			}
		}

		protected override void OnGotFocus(EventArgs args) {
			base.OnGotFocus(args);
			if(!this.EnabledEx) return;

			if(this.CharacterDocument!=null) { //初期化過程のときは無視

				//NOTE TerminalControlはSessionについては無知、という前提にしたほうがいいのかもしれない
				TerminalEmulatorPlugin.Instance.GetSessionManager().ActivateDocument(this.CharacterDocument, ActivateReason.ViewGotFocus);
			
			}
		}

		protected override void OnLostFocus(EventArgs args) {
			base.OnLostFocus(args);
			if(!this.EnabledEx) return;

			if(_inIMEComposition) ClearIMEComposition();
		}
		//Drag&Drop関係
		protected override void OnDragEnter(DragEventArgs args) {
			base.OnDragEnter(args);
			try {
				IWinFormsService wfs = TerminalEmulatorPlugin.Instance.GetWinFormsService();
				IPoderosaDocument document = (IPoderosaDocument)wfs.GetDraggingObject(args.Data, typeof(IPoderosaDocument));
				if(document!=null)
					args.Effect = DragDropEffects.Move;
				else
					wfs.BypassDragEnter(this, args);
			}
			catch(Exception ex) {
				RuntimeUtil.ReportException(ex);
			}
		}
		protected override void OnDragDrop(DragEventArgs args) {
			base.OnDragDrop(args);
			try {
				IWinFormsService wfs = TerminalEmulatorPlugin.Instance.GetWinFormsService();
				IPoderosaDocument document = (IPoderosaDocument)wfs.GetDraggingObject(args.Data, typeof(IPoderosaDocument));
				if(document!=null) {
					IPoderosaView view= (IPoderosaView)this.GetAdapter(typeof(IPoderosaView));
					TerminalEmulatorPlugin.Instance.GetSessionManager().AttachDocumentAndView(document, view);
					TerminalEmulatorPlugin.Instance.GetSessionManager().ActivateDocument(document, ActivateReason.DragDrop);
				}
				else
					wfs.BypassDragDrop(this, args);
			}
			catch(Exception ex) {
				RuntimeUtil.ReportException(ex);
			}
		}

		private void ProcessVScrollMessage(int cmd) {
			int newval = _VScrollBar.Value;
			switch(cmd) {
				case 0: //SB_LINEUP
					newval--;
					break;
				case 1: //SB_LINEDOWN
					newval++;
					break;
				case 2: //SB_PAGEUP
					newval -= GetDocument().TerminalHeight;
					break;
				case 3: //SB_PAGEDOWN
					newval += GetDocument().TerminalHeight;
					break;
			}

			if(newval<0) newval=0;
			if(newval>_VScrollBar.Maximum-_VScrollBar.LargeChange) newval=_VScrollBar.Maximum-_VScrollBar.LargeChange+1;
			_VScrollBar.Value = newval;
		}

		#region IME 関連
		//IMEの位置合わせなど。日本語入力開始時、現在のキャレット位置からIMEをスタートさせる。
		private void AdjustIMEComposition() {
			TerminalDocument document = GetDocument();
			IntPtr hIMC = Win32.ImmGetContext(this.Handle);
			RenderProfile prof = GetRenderProfile();

			//フォントのセットは１回やればよいのか？
			Win32.LOGFONT lf = new Win32.LOGFONT();
			prof.CalcFont(null,CharGroup.Zenkaku).ToLogFont(lf);
			Win32.ImmSetCompositionFont(hIMC, lf);

			Win32.COMPOSITIONFORM form = new Win32.COMPOSITIONFORM();
			form.dwStyle = Win32.CFS_POINT;
			Win32.SystemMetrics sm = GEnv.SystemMetrics;
			//Debug.WriteLine(String.Format("{0} {1} {2}", document.CaretColumn, charwidth, document.CurrentLine.CharPosToDisplayPos(document.CaretColumn)));
			form.ptCurrentPos.x = sm.ControlBorderWidth  + (int)(prof.Pitch.Width * (document.CaretColumn));
			form.ptCurrentPos.y = sm.ControlBorderHeight + (int)((prof.Pitch.Height + prof.LineSpacing) * (document.CurrentLineNumber - document.TopLineNumber));
			bool r = Win32.ImmSetCompositionWindow(hIMC, ref form);
			Debug.Assert(r);
			Win32.ImmReleaseContext(this.Handle, hIMC);
		}
		private void ClearIMEComposition() {
			IntPtr hIMC = Win32.ImmGetContext(this.Handle);
			Win32.ImmNotifyIME(hIMC, Win32.NI_COMPOSITIONSTR, Win32.CPS_CANCEL, 0);
			Win32.ImmReleaseContext(this.Handle, hIMC);
			_inIMEComposition = false;
		}
		/*
		 * この周辺で使いそうなデバッグ用のコード断片
		 private static bool _IMEFlag;
		 private static int _callnest;
		 
			_callnest++;
			if(_IMEFlag) {
				if(msg.Msg!=13 && msg.Msg!=14 && msg.Msg!=15 && msg.Msg!=0x14 && msg.Msg!=0x85 && msg.Msg!=0x20 && msg.Msg!=0x84) //うざいのはきる
					Debug.WriteLine(String.Format("{0} Msg {1:X} WP={2:X} LP={3:X}", _callnest, msg.Msg, msg.WParam.ToInt32(), msg.LParam.ToInt32()));
			}
			base.WndProc(ref msg);
			_callnest--;
		 */
		private bool _lastCompositionFlag;
		//IME関係を処理するためにかなりの苦労。なぜこうなのかについては別ドキュメント参照
		protected override void WndProc(ref Message msg) {
			if(_lastCompositionFlag) {
				LastCompositionWndProc(ref msg);
				return;
			}

			int m = msg.Msg;
			if(m==Win32.WM_IME_COMPOSITION) {
				if((msg.LParam.ToInt32() & 0xFF)==0) { //最終確定時の特殊処理へ迂回させるフラグを立てる
					_lastCompositionFlag = true;
					base.WndProc(ref msg); //この中で送られてくるWM_IME_CHARは無視
					_lastCompositionFlag = false;
					return;
				}
			}

			base.WndProc(ref msg); //通常時

			if(m==Win32.WM_IME_STARTCOMPOSITION) {
				_inIMEComposition = true; //_inIMECompositionはWM_IME_STARTCOMPOSITIONでしかセットしない
				AdjustIMEComposition();
			}
			else if(m==Win32.WM_IME_ENDCOMPOSITION) {
				_inIMEComposition = false;
			}
		}
		/// <summary>
		/// IME からの入力を受け付けます。
		/// </summary>
		/// <param name="msg"></param>
		private void LastCompositionWndProc(ref Message msg) {
			if(msg.Msg==Win32.WM_IME_CHAR) {
				char ch = (char)msg.WParam;
				SendChar(ch);
			}
			else
				base.WndProc(ref msg);
		}
		#endregion
	}

	internal class TerminalEmulatorMouseHandler : DefaultMouseHandler {
		private TerminalControl _control;

		public TerminalEmulatorMouseHandler(TerminalControl control)
			: base("terminal") {
			_control = control;
		}

		public override UIHandleResult OnMouseDown(MouseEventArgs args) {
			return UIHandleResult.Pass;
		}
		public override UIHandleResult OnMouseMove(MouseEventArgs args) {
			return UIHandleResult.Pass;
		}
		public override UIHandleResult OnMouseUp(MouseEventArgs args) {
			if(!_control.EnabledEx) return UIHandleResult.Pass;

			if(args.Button==MouseButtons.Right || args.Button==MouseButtons.Middle) {
				ITerminalEmulatorOptions opt = TerminalEmulatorPlugin.Instance.TerminalEmulatorOptions;
				MouseButtonAction act = args.Button==MouseButtons.Right? opt.RightButtonAction : opt.MiddleButtonAction;
				if(act!=MouseButtonAction.None) {
					if(Control.ModifierKeys==Keys.Shift ^ act==MouseButtonAction.ContextMenu) //シフトキーで動作反転
						ShowContextMenu(new Point(args.X, args.Y));
					else { //Paste
						IGeneralViewCommands vc = (IGeneralViewCommands)_control.GetAdapter(typeof(IGeneralViewCommands));
						TerminalEmulatorPlugin.Instance.GetCommandManager().Execute(vc.Paste, (ICommandTarget)vc.GetAdapter(typeof(ICommandTarget)));
						//ペースト後はフォーカス
						if(!_control.Focused) _control.Focus();
					}

					return UIHandleResult.Stop;
				}
			}

			return UIHandleResult.Pass;
		}

		public void ShowContextMenu(Point pt) {
			IPoderosaView view = (IPoderosaView)_control.GetAdapter(typeof(IPoderosaView));
			view.ParentForm.ShowContextMenu(TerminalEmulatorPlugin.Instance.ContextMenu, view, _control.PointToScreen(pt), ContextMenuFlags.None);
			//コマンド実行後自分にフォーカス
			if(!_control.Focused) _control.Focus();
		}
	}

	//描画パフォーマンス調査用クラス
	internal static class DrawingPerformance {
		private static int _receiveDataCount;
		private static long _lastReceivedTime;
		private static int _shortReceiveTimeCount;

		private static int _fullInvalidateCount;
		private static int _partialInvalidateCount;
		private static int _totalInvalidatedLineCount;
		private static int _invalidate1LineCount;

		public static void MarkReceiveData(InvalidatedRegion region) {
			_receiveDataCount++;
			long now = DateTime.Now.Ticks;
			if(_lastReceivedTime!=0) {
				if(now - _lastReceivedTime < 10*1000*100) _shortReceiveTimeCount++;
			}
			_lastReceivedTime = now;

			if(region.InvalidatedAll)
				_fullInvalidateCount++;
			else {
				_partialInvalidateCount++;
				_totalInvalidatedLineCount += region.LineIDEnd-region.LineIDStart+1;
				if(region.LineIDStart==region.LineIDEnd) _invalidate1LineCount++;
			}
		}

		public static void Output() {
			Debug.WriteLine(String.Format("ReceiveData:{0}  (short:{1})", _receiveDataCount, _shortReceiveTimeCount));
			Debug.WriteLine(String.Format("FullInvalidate:{0} PartialInvalidate:{1} 1-Line:{2} AvgLine:{3:F2}", _fullInvalidateCount, _partialInvalidateCount, _invalidate1LineCount, (double)_totalInvalidatedLineCount/_partialInvalidateCount));
		}

	}


}