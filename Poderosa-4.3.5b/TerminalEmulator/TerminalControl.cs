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
	/// �^�[�~�i���������R���g���[���ł��B
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

		private bool _inIMEComposition; //IME�ɂ�镶�����͂̍Œ��ł����true�ɂȂ�
		private bool _ignoreValueChangeEvent;

		private bool _escForVI;

		//�ĕ`��̏�ԊǗ�
		private int _drawOptimizingState = 0; //���̏�ԊǗ���OnWindowManagerTimer(), SmartInvalidate()�Q��
	 
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
		/// �K�v�ȃf�U�C�i�ϐ��ł��B
		/// </summary>
		private System.ComponentModel.Container components = null;

		public TerminalControl() {
			_instanceID = _instanceCount++;
			_enableAutoScrollBarAdjustment = false;
			_escForVI = false;
			this.EnabledEx = false;

			// ���̌Ăяo���́AWindows.Forms �t�H�[�� �f�U�C�i�ŕK�v�ł��B
			InitializeComponent();

			_terminalEmulatorMouseHandler = new TerminalEmulatorMouseHandler(this);
			_mouseHandlerManager.AddLastHandler(_terminalEmulatorMouseHandler);
			//TODO �^�C�}�[�͋��p���H
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

			//KeepAlive�^�C�}�N���͍ł��x�点���ꍇ�ŃR�R
			TerminalEmulatorPlugin.Instance.KeepAlive.Refresh(opt.KeepAliveInterval);

			//ASCIIWordBreakTable : ���͋��L�ݒ肾���ASession�ŗL�Ƀf�[�^�����悤�ɂ��邩������Ȃ��܂݂��������āB
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

					//TODO �l�S�J�n�O�͂�����}��������
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
		/// �g�p����Ă��郊�\�[�X�Ɍ㏈�������s���܂��B
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
		 * ��  ��M�X���b�h�ɂ����s�̃G���A
		 */ 
		
		public void DataArrived() {
			//�悭�݂�ƁA���������s���Ă���Ƃ���document�����b�N���Ȃ̂ŁA��̃p�^�[���̂悤��SendMessage���g���ƃf�b�h���b�N�̊댯������
			InternalDataArrived();
		}

		private void InternalDataArrived() {
			if(_session == null) return;	// �y�C������鎞�� _tag �� null �ɂȂ��Ă��邱�Ƃ�����

			TerminalDocument document = GetDocument();
			if(!this.ITextSelection.IsEmpty) {
				document.InvalidatedRegion.InvalidatedAll = true; //�ʓ|����
				this.ITextSelection.Clear();
			}
			//Debug.WriteLine(String.Format("v={0} l={1} m={2}", _VScrollBar.Value, _VScrollBar.LargeChange, _VScrollBar.Maximum));
			if(DebugOpt.DrawingPerformance) DrawingPerformance.MarkReceiveData(GetDocument().InvalidatedRegion);
			SmartInvalidate();

			//�����ϊ����ł������Ƃ��̂��߂̒���
			if(_inIMEComposition) {
				if (this.InvokeRequired)
					this.Invoke(new AdjustIMECompositionDelegate(AdjustIMEComposition));
				else
					AdjustIMEComposition();
			}
		}

		private void SmartInvalidate() {
			//������DrawOptimizeState��������B�ߐڂ��ē�������f�[�^�ɂ��ߏ�ȍĕ`���������A�^�C�}�[�ň�莞�Ԍ�ɂ͊m���ɕ`�悳���悤�ɂ���B
			//��ԑJ�ڂ́A�f�[�^�����ƃ^�C�}�[���g���K�Ƃ���R��Ԃ̊ȒP�ȃI�[�g�}�g���ł���B
			switch (_drawOptimizingState) {
				case 0:
					_drawOptimizingState = 1;
					InvalidateEx();
					break;
				case 1:
					if (_session.TerminalConnection.Socket.Available)
						Interlocked.Exchange(ref _drawOptimizingState, 2); //�Ԉ������[�h��
					else
						InvalidateEx();
					break;
				case 2:
					break; //do nothing
			}
		}

		/*
		 * ��  ��M�X���b�h�ɂ����s�̃G���A
		 * -------------------------------
		 * ��  UI�X���b�h�ɂ����s�̃G���A
		 */

		protected override void OnWindowManagerTimer() {
			base.OnWindowManagerTimer();

			switch (_drawOptimizingState) {
				case 0:
					break; //do nothing
				case 1:
					Interlocked.CompareExchange(ref _drawOptimizingState, 0, 1);
					break;
				case 2: //�Z�����Ă���ɂ͕`��
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

		/* �L�[�{�[�h�����n�ɂ���
		 * �@���M�͍ŏI�I�ɂ�SendChar/String�֍s���B
		 * 
		 * �@�����Ɏ���ߒ��ł́A
		 *  ProcessCmdKey: Alt�L�[�̐ݒ莟��ŁA�x�[�X�N���X�ɓn���i���R�}���h�N�������݂�j���ǂ������߂�
		 *  ProcessDialogKey: �����L�[�ȊO�͊�{�I�ɂ����ŏ����B
		 *  OnKeyPress: �����̑��M
		 */
		private byte[] _sendCharBuffer = new byte[1];
		public void SendChar(char ch) { //IS����̃R�[���o�b�N����̂�
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
		//    �[���ւ̓��͂��o�C�g��ɂ��đ��M
		//--------------------------------------------------------------------------
		//CHK: �ꕶ����32�����ȏ�ɃG���R�[�h���� Encoding �͖����Ƃ�������
		private byte[] mwgSendCharBuff=new byte[32];
		private void mwgSendChar(char ch){
			if(ch<0x80){
				// Unicode �� 0x80<=ch<0x100 �́A�����R�[�h�ɂ���ĈقȂ�
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
		/// mwgSendKey �� AltKeyAction ���w��o����łł��B
		/// key �� Keys.Alt ���܂�ł���Ƒz�肵�܂��B
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
		/// <ja>Alt �C���q�t�̃L�[�{�[�h���͂��󂯕t���܂��BA-k / A-x / A-Up ��</ja>
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="key">�����ꂽ�L�[�Ɋւ�������w�肵�܂��B</param>
		/// <returns>�L�[���͂��������ꂽ���� true ��Ԃ��܂��B</returns>
		protected override bool ProcessCmdKey(ref Message msg, Keys key) {
			if(IsAcceptableUserInput()){
				if((key&Keys.Alt)!=0){
					if(GEnv.Options.LeftAltKey!=AltKeyAction.Menu&&(Win32.GetKeyState(Win32.VK_LMENU)&0x8000)!=0){
						mwgSendAltKey(key,GEnv.Options.LeftAltKey);
						return true; //mwg: �P�Ȃ� Alt �ł��ǂݎ̂�
					}else if(GEnv.Options.RightAltKey!=AltKeyAction.Menu&&(Win32.GetKeyState(Win32.VK_RMENU)&0x8000)!=0){
						mwgSendAltKey(key,GEnv.Options.RightAltKey);
						return true; //mwg: �P�Ȃ� Alt �ł��ǂݎ̂�
					}else
						return base.ProcessCmdKey(ref msg, key);
				}
				
				// Control �������Ă���ꍇ�� OnKeyPress ��҂����ɁB
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
			
			//����܂łŏ����ł��Ȃ���Ώ�ʂ֓n��
			return base.ProcessCmdKey(ref msg, key);
		}

		/// <summary>
		/// <ja>���ʂȃL�[�̓��͂��󂯕t���܂��B
		/// Enter Up Dn Right Left Del Ins Home End PgUp PgDn, C-�L��, etc.</ja>
		/// <en>Processes special key inputs such as
		/// Enter, Up, Dn, Right, Left, Del, Ins, Home, End, PgUp, PgDn, C-symbols, etc.</en>
		/// </summary>
		/// <param name="key">�����ꂽ�L�[�Ɋւ�������w�肵�܂��B</param>
		/// <returns>�L�[���͂��������ꂽ�ꍇ�� true ��Ԃ��܂��B</returns>
		protected override bool ProcessDialogKey(Keys key) {
			Keys modifiers = key & Keys.Modifiers;
			Keys keybody = key & Keys.KeyCode;

			//�ڑ����łȂ��Ƃ��߂ȃL�[
			if(IsAcceptableUserInput()) {
				//TODO Enter,Space,SequenceKey�n���J�X�^���L�[�ɓ���Ă��܂�����
				char[] custom = TerminalEmulatorPlugin.Instance.CustomKeySettings.Scan(key); //�J�X�^���L�[
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

			//��ɑ����L�[
			if(keybody==Keys.Apps) { //�R���e�L�X�g���j���[
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
		/// <ja>�ʏ�̃L�[�{�[�h���͂��󂯕t���܂��Ba / S-a ���B</ja>
		/// <en>Processes ordinary keyboard inputs such as a, S-a, etc.</en>
		/// </summary>
		/// <param name="e">�L�[���͂Ɋ֘A��������w�肵�܂��B</param>
		protected override void OnKeyPress(KeyPressEventArgs e) {
			base.OnKeyPress(e);
			if (e.KeyChar == '\x001b') {
				_escForVI = true;
			}
			if(!IsAcceptableUserInput()) return;
			/* �����̏����ɂ���
			 * �@IME�œ��͕������m�肷��Ɓi�����m��ł͂Ȃ��j�AWM_IME_CHAR�AWM_ENDCOMPOSITION�AWM_CHAR�̏��Ń��b�Z�[�W�������Ă���B
			 * �@Control�͂��̗�����KeyPress�C�x���g�𔭐�������̂ŁAIME�̓��͂��Q�񑗐M����Ă��܂��B
			 * �@��������m��̂Ƃ���WM_IME_CHAR�݂̂ł���B
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
			//base.OnMouseWheel(e); //CharacaterDocumentViewer�͖������Ă悢
			if(!this.EnabledEx)return;
			
#if MWG_MOUSE
			if(this.IsAcceptableUserInput()){
				Keys flags=KeyboardInfo.GetModifierState();
				//if((flags&Keys.Shift)==0){ // shift �� poderosa �ŏ����B
				if(Control.IsKeyLocked(Keys.Scroll)){ // shift �� poderosa �ŏ����B
					// �֋X�� [��]�� XButton1 �� [��]�� XButton2 �Ɋ��蓖�Ă�B
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

			//�A�v���P�[�V�������[�h�ł͒ʏ폈������߂ăJ�[�\���㉺�Ɠ����̏����ɂ���
			if(_session!=null && !GEnv.Options.AllowsScrollInAppMode && GetTerminal().TerminalMode==TerminalMode.Application) {
				int m = GEnv.Options.WheelAmount;
				for(int i=0; i<m; i++)
					mwgSendKey(Keys.None|(e.Delta>0? Keys.Up : Keys.Down));
				return;
			}

			if(_VScrollBar.Enabled) {
				int d = e.Delta / 120; //�J��������Delta��120�B�����1��-1������͂�
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

      // ��ԕω����Ȃ���Ζ���
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

        // �ʒu�E�{�^�����
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
				//�L�[���������ςȂ��ɂ����Ƃ��ɃL�����b�g���u�����N����̂͂�����ƌ��ꂵ���̂ŃL�[���͂����邽�тɃ^�C�}�����Z�b�g
				_caret.KeepActiveUntilNextTick();

				MakeCurrentLineVisible();
			}
			GetTerminalTransmission().Transmit(data);
		}

		private bool IsAcceptableUserInput() {
			//TODO: ModalTerminalTask�̑��݂����R�ŋ��ۂ���Ƃ��̓X�e�[�^�X�o�[�������ɏo���̂��悢����
			if(!this.EnabledEx || IsConnectionClosed() || _session.Terminal.CurrentModalTerminalTask!=null)
				return false;
			else
				return true;

		}

		private void MakeCurrentLineVisible() {
			TerminalDocument document = GetDocument();
			if(document.CurrentLineNumber-document.FirstLineNumber < _VScrollBar.Value) { //��ɉB�ꂽ
				document.TopLineNumber = document.CurrentLineNumber;
				_session.Terminal.TransientScrollBarValues.Value = document.TopLineNumber-document.FirstLineNumber;
			}
			else if(_VScrollBar.Value + document.TerminalHeight <= document.CurrentLineNumber-document.FirstLineNumber) { //���ɉB�ꂽ
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
			//�ŏ������ɂ͂Ȃ������g�̕��������O�ɂȂ��Ă��܂�
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
			if(_session != null) {	// TerminalPane�����^�C�~���O�ł��̃��\�b�h���Ă΂ꂽ�Ƃ���NullReferenceException�ɂȂ�̂�h��
				_ignoreValueChangeEvent = true;
				GetTerminal().CommitScrollBar(_VScrollBar, true);	//!! �����i�X�N���[���o�[�j�̏����͏d��
				_ignoreValueChangeEvent = false;
			}
		}

		public override GLine GetTopLine() {
			//TODO Pane���̃N���X�`�F���W���ł���悤�ɂȂ����炱�������P
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
			if (width <= 0) width = 1; //�ɒ[�ȃ��T�C�Y������ƕ��̒l�ɂȂ邱�Ƃ�����
			if(height <= 0) height = 1;
			return new Size(width, height);
		}

		private void ShowSizeTip(int width, int height) {
			const int MARGIN = 8;
			//Form form = GEnv.Frame.AsForm();
			//if(form==null || !form.Visible) return; //�N�����ɂ͕\�����Ȃ�
			if(!this.Visible) return;

			Point pt = new Point(this.Width-_VScrollBar.Width-_sizeTip.Width-MARGIN, this.Height-_sizeTip.Height-MARGIN);

			_sizeTip.Text = String.Format("{0} * {1}", width, height);
			_sizeTip.Location = pt;
			_sizeTip.Visible = true;

			_sizeTipTimer.Stop();
			_sizeTipTimer.Start();
		}

		//�s�N�Z���P�ʂ̃T�C�Y���󂯎��A�`�b�v��\��
		public void SplitterDragging(int width, int height) {
			SizeF charSize = GetRenderProfile().Pitch;
			Win32.SystemMetrics sm = GEnv.SystemMetrics;
			width  = (int)Math.Floor(((float)width - sm.ScrollBarWidth - sm.ControlBorderWidth*2) / charSize.Width);
			height = (int)Math.Floor((float)(height - sm.ControlBorderHeight*2) / charSize.Height);
			ShowSizeTip(width, height);
		}

		private void ResizeTerminal(int width, int height) {
			//Debug.WriteLine(String.Format("Resize {0} {1}", width, height));

			//Document�֒ʒm
			GetDocument().Resize(width, height);

			if(_session.Terminal.CurrentModalTerminalTask!=null) return; //�ʃ^�X�N�������Ă���Ƃ��͖���
			GetDocument().SetScrollingRegion(0, height-1);//mwg: xterm �� Application mode/Normal mode �ɍS�炸 ScrollingRegion ���N���A���Ă���B
			GetTerminal().Reset();
			if(_VScrollBar.Enabled) {
				bool scroll = IsAutoScrollMode();
				_VScrollBar.LargeChange = height;
				if(scroll)
					MakeCurrentLineVisible();
			}

			//�ڑ���֒ʒm
			GetTerminalTransmission().Resize(width, height);
			InvalidateEx();
		}

		//���ݍs��������悤�Ɏ����I�ɒǐ����Ă����ׂ����ǂ����̔���
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
		/// <ja>[mwg] �`��ݒ��ێ����� RenderProfile �C���X�^���X���w�肵�܂��B
		/// ���̃C���X�^���X�͂��̃��\�b�h�Ăяo������ɔj�󂳂��\��������܂��B
		/// </ja>
		/// </param>
		/// <remarks>
		/// <ja>[mwg] ApplyRenderProfile �̎����ɍۂ��āB
		/// �����ɓn����� RenderProfile �C���X�^���X�͓Ǝ��̎����������܂��B
		/// RenderProfile �C���X�^���X��ێ��������ꍇ�ɂ́A
		/// Clone ���\�b�h�ŃC���X�^���X���R�s�[���ĕێ�����l�ɂ��ĉ������B
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

			if(this.CharacterDocument!=null) { //�������ߒ��̂Ƃ��͖���

				//NOTE TerminalControl��Session�ɂ��Ă͖��m�A�Ƃ����O��ɂ����ق��������̂�������Ȃ�
				TerminalEmulatorPlugin.Instance.GetSessionManager().ActivateDocument(this.CharacterDocument, ActivateReason.ViewGotFocus);
			
			}
		}

		protected override void OnLostFocus(EventArgs args) {
			base.OnLostFocus(args);
			if(!this.EnabledEx) return;

			if(_inIMEComposition) ClearIMEComposition();
		}
		//Drag&Drop�֌W
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

		#region IME �֘A
		//IME�̈ʒu���킹�ȂǁB���{����͊J�n���A���݂̃L�����b�g�ʒu����IME���X�^�[�g������B
		private void AdjustIMEComposition() {
			TerminalDocument document = GetDocument();
			IntPtr hIMC = Win32.ImmGetContext(this.Handle);
			RenderProfile prof = GetRenderProfile();

			//�t�H���g�̃Z�b�g�͂P����΂悢�̂��H
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
		 * ���̎��ӂŎg�������ȃf�o�b�O�p�̃R�[�h�f��
		 private static bool _IMEFlag;
		 private static int _callnest;
		 
			_callnest++;
			if(_IMEFlag) {
				if(msg.Msg!=13 && msg.Msg!=14 && msg.Msg!=15 && msg.Msg!=0x14 && msg.Msg!=0x85 && msg.Msg!=0x20 && msg.Msg!=0x84) //�������̂͂���
					Debug.WriteLine(String.Format("{0} Msg {1:X} WP={2:X} LP={3:X}", _callnest, msg.Msg, msg.WParam.ToInt32(), msg.LParam.ToInt32()));
			}
			base.WndProc(ref msg);
			_callnest--;
		 */
		private bool _lastCompositionFlag;
		//IME�֌W���������邽�߂ɂ��Ȃ�̋�J�B�Ȃ������Ȃ̂��ɂ��Ă͕ʃh�L�������g�Q��
		protected override void WndProc(ref Message msg) {
			if(_lastCompositionFlag) {
				LastCompositionWndProc(ref msg);
				return;
			}

			int m = msg.Msg;
			if(m==Win32.WM_IME_COMPOSITION) {
				if((msg.LParam.ToInt32() & 0xFF)==0) { //�ŏI�m�莞�̓��ꏈ���։I�񂳂���t���O�𗧂Ă�
					_lastCompositionFlag = true;
					base.WndProc(ref msg); //���̒��ő����Ă���WM_IME_CHAR�͖���
					_lastCompositionFlag = false;
					return;
				}
			}

			base.WndProc(ref msg); //�ʏ펞

			if(m==Win32.WM_IME_STARTCOMPOSITION) {
				_inIMEComposition = true; //_inIMEComposition��WM_IME_STARTCOMPOSITION�ł����Z�b�g���Ȃ�
				AdjustIMEComposition();
			}
			else if(m==Win32.WM_IME_ENDCOMPOSITION) {
				_inIMEComposition = false;
			}
		}
		/// <summary>
		/// IME ����̓��͂��󂯕t���܂��B
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
					if(Control.ModifierKeys==Keys.Shift ^ act==MouseButtonAction.ContextMenu) //�V�t�g�L�[�œ��씽�]
						ShowContextMenu(new Point(args.X, args.Y));
					else { //Paste
						IGeneralViewCommands vc = (IGeneralViewCommands)_control.GetAdapter(typeof(IGeneralViewCommands));
						TerminalEmulatorPlugin.Instance.GetCommandManager().Execute(vc.Paste, (ICommandTarget)vc.GetAdapter(typeof(ICommandTarget)));
						//�y�[�X�g��̓t�H�[�J�X
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
			//�R�}���h���s�㎩���Ƀt�H�[�J�X
			if(!_control.Focused) _control.Focus();
		}
	}

	//�`��p�t�H�[�}���X�����p�N���X
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