/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: CharacterDocumentViewer.cs,v 1.7 2011/01/22 11:11:25 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using Poderosa.Util;
using Poderosa.Document;
using Poderosa.Forms;
using Poderosa.UI;
using Poderosa.Sessions;
using Poderosa.Commands;

using Poderosa.View.Utils;
using mwg.RosaTerm.Utils;
using Gdi=System.Drawing;

namespace Poderosa.View
{
  /*
   * CharacterDocument�̕\�����s���R���g���[���B�@�\�Ƃ��Ă͎�������B
   * �@�c�����̂݃X�N���[���o�[���T�|�[�g
   * �@�ĕ`��̍œK��
   * �@�L�����b�g�̕\���B�������L�����b�g��K�؂Ɉړ�����@�\�͊܂܂�Ȃ�
   * 
   * �@���゠���Ă�������������Ȃ��@�\�́A�s�Ԃ�Padding(HTML�p���)�A�s�ԍ��\���Ƃ������Ƃ���
   */
  /// <summary>
  /// 
  /// </summary>
  /// <exclude/>
  public class CharacterDocumentViewer: Control, IPoderosaControl, ISelectionListener, SplitMarkSupport.ISite {
    
    public const int BORDER = 2; //�����̘g���̃T�C�Y
    internal const int TIMER_INTERVAL = 50; //�ĕ`��œK���ƃL�����b�g�������s���^�C�}�[�̊Ԋu

    private CharacterDocument _document;
    private bool _errorRaisedInDrawing;
    private List<GLine> _transientLines; //�ĕ`�悷��GLine���ꎞ�I�ɕۊǂ���
    private TextSelection _textSelection;
    private SplitMarkSupport _splitMark;
    private bool _enabled; //�h�L�������g���A�^�b�`����Ă��Ȃ��Ƃ������� �ύX����Ƃ���EnabledEx�v���p�e�B�ŁI
    private bool _drawbgImage = true;

    protected MouseHandlerManager _mouseHandlerManager;
    protected VScrollBar _VScrollBar;
    protected bool _enableAutoScrollBarAdjustment; //���T�C�Y���Ɏ����I��_VScrollBar�̒l�𒲐����邩�ǂ���
    protected Caret _caret;
    protected ITimerSite _timer;
    protected int _tickCount;

    public CharacterDocumentViewer() {
      _enableAutoScrollBarAdjustment = true;
      _transientLines = new List<GLine>();
      InitializeComponent();
      //SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.DoubleBuffer, true);
      this.DoubleBuffered = true;
      _caret = new Caret();

      _splitMark = new SplitMarkSupport(this, this);
      Pen p = new Pen(SystemColors.ControlDark);
      p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
      _splitMark.Pen = p;

      _textSelection = new TextSelection(this);
      _textSelection.AddSelectionListener(this);

      _mouseHandlerManager = new MouseHandlerManager();
      _mouseHandlerManager.AddLastHandler(new TextSelectionUIHandler(this));
      _mouseHandlerManager.AddLastHandler(new SplitMarkUIHandler(_splitMark));
      _mouseHandlerManager.AttachControl(this);
    
      SetStyle(ControlStyles.SupportsTransparentBackColor, true);
    }

    public CharacterDocument CharacterDocument {
      get {
        return _document;
      }
    }
    internal TextSelection TextSelection {
      get {
        return _textSelection;
      }
    }
    public ITextSelection ITextSelection {
      get {
        return _textSelection;
      }
    }
    internal MouseHandlerManager MouseHandlerManager {
      get {
        return _mouseHandlerManager;
      }
    }
    
    public Caret Caret {
      get {
        return _caret;
      }
    }

    public bool EnabledEx {
      get {
        return _enabled;
      }
      set {
        _enabled = value;
        _VScrollBar.Visible = value; //�X�N���[���o�[�Ƃ͘A��
        _splitMark.Pen.Color = value? SystemColors.ControlDark : SystemColors.Window; //����BackColor�Ƌt��
        this.Cursor = this.MouseCursor; //Splitter.ISite�����p
        this.BackColor = value? GetRenderProfile().BackColor : SystemColors.ControlDark;
        this.ImeMode = value? ImeMode.NoControl : ImeMode.Disable;
      }
    }
    public VScrollBar VScrollBar {
      get {
        return _VScrollBar;
      }
    }

    public void ShowVScrollBar() {
      _VScrollBar.Visible = true;
    }

    public void HideVScrollBar() {
      _VScrollBar.Visible = false;
    }


    #region IAdaptable
    public virtual IAdaptable GetAdapter(Type adapter) {
      return SessionManagerPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
    }
    #endregion

    //�h���^�ł��邱�Ƃ��������邱�ƂȂǂ̂��߂�override���邱�Ƃ�����
    public virtual void SetContent(CharacterDocument doc) {
      RenderProfile prof = GetRenderProfile();
      this.BackColor = prof.BackColor;
      _document = doc;
      this.EnabledEx = doc!=null;

      if(_timer!=null) _timer.Close();
      if(this.EnabledEx) {
        _timer = WindowManagerPlugin.Instance.CreateTimer(TIMER_INTERVAL, new TimerDelegate(OnWindowManagerTimer));
        _tickCount = 0;
      }

      if(_enableAutoScrollBarAdjustment) AdjustScrollBar();
    }
    //�^�C�}�[�̎�M
    private void CaretTick() {
      if(_enabled && _caret.Blink) {
        _caret.Tick();
        _document.InvalidatedRegion.InvalidateLine(GetTopLine().ID+_caret.Y);
        InvalidateEx();
      }
    }
    protected virtual void OnWindowManagerTimer() {
      //�^�C�}�[��TIMER_INTERVAL���ƂɃJ�E���g�����̂ŁB
      int q = WindowManagerPlugin.Instance.WindowPreference.OriginalPreference.CaretInterval / TIMER_INTERVAL;
      if (q == 0)
        q = 1;
      if(++_tickCount % q ==0)
        CaretTick();
    }


    //���ȃT�C�Y����ScrollBar��K�؂ɂ�����
    public void AdjustScrollBar() {
      if(_document==null) return;
      RenderProfile prof = GetRenderProfile();
      float ch = prof.Pitch.Height + prof.LineSpacing;
      int largechange = (int)Math.Floor((this.ClientSize.Height - BORDER * 2 + prof.LineSpacing) / ch); //������ƕ\���ł���s����LargeChange�ɃZ�b�g
      int current = GetTopLine().ID - _document.FirstLineNumber;
      int size = Math.Max(_document.Size, current + largechange);
      if(size <= largechange) {
        _VScrollBar.Enabled = false;
      }
      else {
        _VScrollBar.Enabled = true;
        _VScrollBar.LargeChange = largechange;
        _VScrollBar.Maximum = size - 1; //����-1���K�v�Ȃ̂����Ȏd�l��
      }
    }

    //���̂�����̏��u��܂��Ă��Ȃ�
    private RenderProfile _privateRenderProfile = null;
    public void SetPrivateRenderProfile(RenderProfile prof) {
      _privateRenderProfile = prof;
    }

    //override���ĕʂ̕��@��RenderProfile���擾���邱�Ƃ�����
    public virtual RenderProfile GetRenderProfile() {
      return _privateRenderProfile;
    }

    protected virtual void CommitTransientScrollBar() {
      //Viewer��UI�ɂ���Ă����؂���Ȃ����炱���ł͉������Ȃ��Ă���
    }

    //�s���ŕ\���\�ȍ�����Ԃ�
    protected virtual int GetHeightInLines() {
      RenderProfile prof = GetRenderProfile();
      float ch = prof.Pitch.Height + prof.LineSpacing;
      int height = (int)Math.Floor((this.ClientSize.Height - BORDER * 2 + prof.LineSpacing) / ch);
      return (height > 0) ? height : 0;
    }

    //_document�̂����ǂ��擪(1�s��)�Ƃ��ĕ\�����邩��Ԃ�
    public virtual GLine GetTopLine() {
      return _document.FindLine(_document.FirstLine.ID + _VScrollBar.Value);
    }

    //_VScrollBar.ValueChanged�C�x���g
    protected virtual void VScrollBarValueChanged() {
      if(_enableAutoScrollBarAdjustment)
        Invalidate();
    }

    //�L�����b�g�̍��W�ݒ�A�\���̉ۂ�ݒ�
    protected virtual void AdjustCaret(Caret caret) {
    }

    //_document�̍X�V�󋵂����ēK�؂ȗ̈��Control.Invalidate()���ĂԁB
    //�܂��A�R���g���[�������L���Ă��Ȃ��X���b�h����Ă�ł�OK�Ȃ悤�ɂȂ��Ă���B
    protected void InvalidateEx() {
      if(this.IsDisposed) return;
      bool full_invalidate = true;
      Rectangle r = new Rectangle();

      if (_document != null) {
        if (_document.InvalidatedRegion.IsEmpty)
          return;
        InvalidatedRegion rgn = _document.InvalidatedRegion.GetCopyAndReset();
        if (rgn.IsEmpty)
          return;
        if (!rgn.InvalidatedAll) {
          full_invalidate = false;
          r.X = 0;
          r.Width = this.ClientSize.Width;
          int topLine = GetTopLine().ID;
          int y1 = rgn.LineIDStart - topLine;
          int y2 = rgn.LineIDEnd + 1 - topLine;
          RenderProfile prof = GetRenderProfile();
          r.Y = BORDER + (int)(y1 * (prof.Pitch.Height + prof.LineSpacing));
          r.Height = (int)((y2 - y1) * (prof.Pitch.Height + prof.LineSpacing)) + 1;
        }
      }

      if(this.InvokeRequired) {
        if(full_invalidate)
          this.BeginInvoke((MethodInvoker)delegate(){ Invalidate(); });
        else {
          this.BeginInvoke((MethodInvoker)delegate(){ Invalidate(r); });
        }
      }
      else {
        if(full_invalidate)
          Invalidate();
        else
          Invalidate(r);
      }
    }

    private void InitializeComponent() {
      this.SuspendLayout();
       this._VScrollBar = new System.Windows.Forms.VScrollBar();
      // 
      // _VScrollBar
      // 
      this._VScrollBar.Enabled = false;
      //this._VScrollBar.Dock = DockStyle.Right;
      this._VScrollBar.Anchor = AnchorStyles.Right|AnchorStyles.Top|AnchorStyles.Bottom;
      this._VScrollBar.LargeChange = 1;
      this._VScrollBar.Minimum = 0;
      this._VScrollBar.Value = 0;
      this._VScrollBar.Maximum = 2;
      this._VScrollBar.Name = "_VScrollBar";
      this._VScrollBar.TabIndex = 0;
      this._VScrollBar.TabStop = false;
      this._VScrollBar.Cursor = Cursors.Default;
      this._VScrollBar.Visible = false;
      this._VScrollBar.ValueChanged += delegate(object sender, EventArgs args) { VScrollBarValueChanged(); };
      this.Controls.Add(_VScrollBar);

      this.ImeMode = ImeMode.NoControl;
      //this.BorderStyle = BorderStyle.Fixed3D; //IMEPROBLEM
      AdjustScrollBarPosition();
      this.ResumeLayout();
    }

    protected override void Dispose(bool disposing) {
      if(_privateRenderProfile!=null){
        _privateRenderProfile.Dispose();
        _privateRenderProfile=null;
      }

      base.Dispose(disposing);
      if(disposing) {
        _caret.Dispose();
        if(_timer!=null) _timer.Close();
        _splitMark.Pen.Dispose();
      }

      bgdrawer.Dispose();
    }

    protected override void OnResize(EventArgs e) {
      base.OnResize(e);
      if(_VScrollBar.Visible) AdjustScrollBarPosition();
      if(_enableAutoScrollBarAdjustment && _enabled) AdjustScrollBar();

      Invalidate();
    }

    //NOTE ������Dock��Top��Left�̂Ƃ��A�X�N���[���o�[�̈ʒu���ǐ����Ă���Ȃ��݂���
    private void AdjustScrollBarPosition() {
      _VScrollBar.Height = this.ClientSize.Height;
      _VScrollBar.Left = this.ClientSize.Width - _VScrollBar.Width;
    }

    //�`��̖{��
    protected override sealed void OnPaint(PaintEventArgs e) {
      base.OnPaint(e);
      try {
        if (_document != null)
          ShowVScrollBar();
        else
          HideVScrollBar();

        if(_enabled && !this.DesignMode) {
          Rectangle clip = e.ClipRectangle;
          Graphics g = e.Graphics;
          RenderProfile profile = GetRenderProfile();
          int paneheight = GetHeightInLines();

          if (_document.BackColor != profile.BackColor) {
            if (_document.IsApplicationMode) {
              if (this.BackColor != _document.BackColor) {
                this.BackColor = _document.BackColor;
                _drawbgImage = false;
              }
            }
            else {
              this.BackColor = profile.BackColor;
              _document.BackColor = profile.BackColor;
              _drawbgImage = true;
            }
          }
          if (_drawbgImage) {
            Image img = profile.GetImage();
            if (img != null) {
              DrawBackgroundImage(g, img, profile.ImageStyle, clip);
            }
          }

          //�`��p�Ƀe���|������GLine�����A�`�撆��document�����b�N���Ȃ��悤�ɂ���
          //!!�����͎��s�p�x�������̂�new�𖈉񂷂�͔̂��������Ƃ��낾
          RenderParameter param = new RenderParameter();
          _caret.Enabled = _caret.Enabled && this.Focused; //TODO �����IME�N�����̓L�����b�g��\�����Ȃ��悤��. TerminalControl��������AdjustCaret��IME���݂Ă�̂Ŗ��͂Ȃ�
          lock(_document) {
            CommitTransientScrollBar();
            BuildTransientDocument(e, param);
          }

          DrawLines(g, param);

          if(_caret.Enabled && (!_caret.Blink || _caret.IsActiveTick)) { //�_�ł��Ȃ����Enabled�ɂ���Ă̂݌��܂�
            if(_caret.Style==CaretType.Line)
              DrawBarCaret(g, param, _caret.X, _caret.Y);
            else if(_caret.Style==CaretType.Underline)
              DrawUnderLineCaret(g, param, _caret.X, _caret.Y);
            else if(_caret.Style==CaretType.BoldUnderline)
              DrawBoldUnderlineCaret(g,param,_caret.X,_caret.Y);
          }
        }
        //�}�[�N�̕`��
        _splitMark.OnPaint(e);
      }
      catch(Exception ex) {
        if(!_errorRaisedInDrawing) { //���̒��ň�x��O����������ƌJ��Ԃ��N�����Ă��܂����Ƃ��܂܂���B�Ȃ̂ŏ���̂ݕ\�����ĂƂ肠�����؂蔲����
          _errorRaisedInDrawing = true;
          RuntimeUtil.ReportException(ex);
        }
      }
    }

    private void BuildTransientDocument(PaintEventArgs e, RenderParameter param) {
      Rectangle clip = e.ClipRectangle;
      RenderProfile profile = GetRenderProfile();
      _transientLines.Clear();

      //Win32.SystemMetrics sm = GEnv.SystemMetrics;
      //param.TargetRect = new Rectangle(sm.ControlBorderWidth+1, sm.ControlBorderHeight,
      //  this.Width - _VScrollBar.Width - sm.ControlBorderWidth + 8, //���̂W���Ȃ��l�����������A.NET�̕����T�C�Y�ۂߖ��̂��ߍs�̍ŏI�������\������Ȃ����Ƃ�����B�����������邽�߂ɂ�����Ƒ��₷
      //  this.Height - sm.ControlBorderHeight);
      param.TargetRect = this.ClientRectangle;

      int offset1 = (int)Math.Floor((clip.Top - BORDER) / (profile.Pitch.Height + profile.LineSpacing));
      if (offset1 < 0)
        offset1 = 0;
      param.LineFrom = offset1;
      int offset2 = (int)Math.Floor((clip.Bottom - BORDER) / (profile.Pitch.Height + profile.LineSpacing));
      if (offset2 < 0)
        offset2 = 0;

      param.LineCount = offset2 - offset1 + 1;
      //Debug.WriteLine(String.Format("{0} {1} ", param.LineFrom, param.LineCount));

      int topline_id = GetTopLine().ID;
      GLine l = _document.FindLineOrNull(topline_id + param.LineFrom);
      if(l!=null) {
        for(int i=param.LineFrom; i<param.LineFrom+param.LineCount; i++) {
          _transientLines.Add(l.Clone()); //TODO �N���[���͂�����Ȃ��@�����`��̕������Ԃ�����̂ŁA���̊ԃ��b�N�����Ȃ����߂ɂ͎d���Ȃ��_������
          l = l.NextLine;
          if(l==null) break;
        }
      }

      //�ȉ��A_transientLines�ɂ�param.LineFrom���玦�����l�������Ă��邱�Ƃɒ���

      //�I��̈�̕`��
      if(!_textSelection.IsEmpty) {
        TextSelection.TextPoint from = _textSelection.HeadPoint;
        TextSelection.TextPoint to   = _textSelection.TailPoint;
        l = _document.FindLineOrNull(from.Line);
        GLine t = _document.FindLineOrNull(to.Line);

        bool isrect=(Control.ModifierKeys&Keys.Alt)!=0;

        if(l!=null && t!=null) { //�{����l��null�ł͂����Ȃ��͂������A�������������o�O���|�[�g���������̂ŔO�̂���
          t = t.NextLine;
          int p0=from.Column;
          //TODO: �S�p��������?

          int p1=0;
          if(isrect){
            p1=to.Column;
            //TODO: �S�p��������?
            if(p0>p1){
              int p2=p0;p0=p1;p1=p2;
            }
          }

          int start = from.Column; //���Ƃ��΍��[���z���ăh���b�O�����Ƃ��̑I��͈͂͑O�s���ɂȂ�̂� pos==TerminalWidth�ƂȂ�P�[�X������B
          do{
            int index = l.ID-(topline_id+param.LineFrom);
            if(0<=start && start<l.DisplayLength && 0<=index && index<_transientLines.Count) {

              int end=0;
              if(isrect){
                start=p0;
                end=p1;
                //TODO: �S�p��������?
              }else{
                if(l.ID==to.Line){
                  if(start!=to.Column)
                    end=to.Column;
                }else
                  end=l.DisplayLength;
              }

              if(start<end){
                GLine r = _transientLines[index].InverseRange(start, end);
                if(r!=null)
                  _transientLines[index] = r;
              }
            }

            start = 0; //�Q�s�ڂ���̑I���͍s������
            l = l.NextLine;
          }while(l!=t);
        }
      }

      AdjustCaret(_caret);
      _caret.Enabled = _caret.Enabled && (param.LineFrom <= _caret.Y && _caret.Y < param.LineFrom+param.LineCount);

      //Caret��ʊO�ɂ���Ȃ珈���͂��Ȃ��Ă悢�B�Q�Ԗڂ̏����́AAttach-ResizeTerminal�̗���̒��ł���OnPaint�����s�����ꍇ��TerminalHeight>lines.Count�ɂȂ�P�[�X������̂�h�~���邽��
      if(_caret.Enabled) { 
        //�q�N�q�N���̂��߁A�L�����b�g��\�����Ȃ��Ƃ��ł����̑���͏Ȃ��Ȃ�
        if(_caret.Style==CaretType.Box) {
          int y = _caret.Y-param.LineFrom;
          if(y>=0 && y<_transientLines.Count)
            _transientLines[y].InverseCharacter(_caret.X, _caret.IsActiveTick, _caret.Color);
        }
      }
    }


    #region �`��
    private void DrawLines(Graphics g, RenderParameter param) {
      RenderProfile prof = GetRenderProfile();
      //Rendering Core
      if(param.LineFrom <= _document.LastLineNumber) {
        IntPtr hdc = g.GetHdc();
        IntPtr gdipgraphics = IntPtr.Zero;
        try {
          if(prof.UseClearType) {
            Win32.GdipCreateFromHDC(hdc, ref gdipgraphics);
            Win32.GdipSetTextRenderingHint(gdipgraphics, 5); //5��TextRenderingHintClearTypeGridFit
          }

          float y = (prof.Pitch.Height + prof.LineSpacing) * param.LineFrom + BORDER;
          for (int i = 0; i < _transientLines.Count; i++) {
            GLine line = _transientLines[i];
            line.Render(hdc, prof, BORDER, (int)y);
            y += prof.Pitch.Height + prof.LineSpacing;
          }
       
        }
        finally {
          if(gdipgraphics!=IntPtr.Zero) Win32.GdipDeleteGraphics(gdipgraphics);
          g.ReleaseHdc(hdc);
        }
      }
    }

    private void DrawBarCaret(Graphics g, RenderParameter param, int x, int y) {
      RenderProfile profile = GetRenderProfile();
      float xchar=BORDER+profile.Pitch.Width*x;
      float ychar=BORDER+(profile.Pitch.Height+profile.LineSpacing)*y;
      //this.DrawCaret_Box(g,xchar,ychar+2,2.0f,profile.Pitch.Height-1);
      this.DrawCaret_Box(g,xchar,ychar+1,2.0f,profile.Pitch.Height-1);
    }
    private void DrawUnderLineCaret(Graphics g, RenderParameter param, int x, int y) {
      RenderProfile profile = GetRenderProfile();
      float xchar=BORDER+profile.Pitch.Width*x;
      float ychar=BORDER+(profile.Pitch.Height+profile.LineSpacing)*y;
      //this.DrawCaret_Box(g,xchar,ychar+profile.Pitch.Height,profile.Pitch.Width-1,2.0f);
      //this.DrawCaret_Box(g,xchar,ychar+profile.Pitch.Height-1,profile.Pitch.Width-1,2.0f);
      this.DrawCaret_Box(g,xchar,ychar+profile.Pitch.Height,profile.Pitch.Width-1,1.0f);
    }
    private void DrawBoldUnderlineCaret(Graphics g, RenderParameter param, int x, int y) {
      RenderProfile profile = GetRenderProfile();
      float xchar=BORDER+profile.Pitch.Width*x;
      float ychar=BORDER+(profile.Pitch.Height+profile.LineSpacing)*y;
      this.DrawCaret_Box(g,xchar,ychar+profile.Pitch.Height-3,profile.Pitch.Width,3.0f);
    }
    private void DrawCaret_Box(Graphics g,float bx,float by,float bw,float bh){
      if(_caret.Color.IsEmpty){
        // �F���]
        IntPtr hdc = g.GetHdc();
        try{
          const uint DSTINVERT = 0x550009; // ���]
          Win32.PatBlt(hdc,(int)(bx+0.5f),(int)(by+0.5f),(int)(bw+0.5f),(int)(bh+0.5),DSTINVERT);
        }finally{
          g.ReleaseHdc(hdc);
        }
      }else{
        g.FillRectangle(_caret.ToBrush(this.GetRenderProfile()),bx,by,bw,bh);
        //PointF pt1=new PointF(bx,by);
        //PointF pt2=new PointF(bx+bw-1,by);
        //Pen p=_caret.ToPen(this.GetRenderProfile());
        //for(int i=0;i<bh;i++){
        //  g.DrawLine(p,pt1,pt2);
        //  pt1.Y++;
        //  pt2.Y++;
        //}
      }
    }

    readonly mwg.RosaTerm.View.BackgroundDrawer bgdrawer=new mwg.RosaTerm.View.BackgroundDrawer();
    private void DrawBackgroundImage(Graphics g, Image img, ImageStyle style, Rectangle clip) {
      if(style.IsScaled()){
        DrawBackgroundImage_Scaled(g, img, style, clip);
      }else{
        DrawBackgroundImage_Normal(g, img, style, clip);
      }
    }
    private void DrawBackgroundImage_Scaled(Graphics g, Image img, ImageStyle style, Rectangle clip) {
      Size clientSize = this.ClientSize;

      Size size=this.ClientSize;
      size.Width-=_VScrollBar.Width;
      if(size.Width<1)size.Width=1;
      if(size.Height<1)size.Height=1;

      float sw=(float)size.Width/img.Width;
      float sh=(float)size.Height/img.Height;

      switch(style){
        case ImageStyle.Scaled:       break;
        case ImageStyle.HorizontalFit:sh=sw;break;
        case ImageStyle.VerticalFit:  sw=sh;break;
        case ImageStyle.MinimalFit:   if(sw<sh)sh=sw;else sw=sh;break;
        case ImageStyle.MaximalFit:   if(sw>sh)sh=sw;else sw=sh;break;
      }

      bgdrawer.UpdateImage(img,sw,sh,(size.Width-img.Width*sw)/2,(size.Height-img.Height*sh)/2,size);
      bgdrawer.DrawImageClipped(g,clip);
    }

    private void DrawBackgroundImage_Normal(Graphics g, Image img, ImageStyle style, Rectangle clip) {
      int offset_x, offset_y;
      if(style==ImageStyle.Center) {
        offset_x = (this.Width-_VScrollBar.Width - img.Width) / 2;
        offset_y = (this.Height - img.Height) / 2;
      }
      else {
        offset_x = (style==ImageStyle.TopLeft || style==ImageStyle.BottomLeft) ? 0 : (this.ClientSize.Width - _VScrollBar.Width - img.Width);
        offset_y = (style == ImageStyle.TopLeft || style == ImageStyle.TopRight) ? 0 : (this.ClientSize.Height - img.Height);
      }
      //if(offset_x < BORDER) offset_x = BORDER;
      //if(offset_y < BORDER) offset_y = BORDER;

      //�摜���̃R�s�[�J�n���W
      bgdrawer.UpdateImage(img,1,1,offset_x,offset_y,this.Size);
      bgdrawer.DrawImageClipped(g,clip);
    }

    //------------------------------------------------------------------------
    // �ʒu�v�Z
    /// <summary>
    /// <ja>�w�肵���_���A���s�E����ڂɑΉ����邩���v�Z���܂��B</ja>
    /// </summary>
    /// <param name="clientPoint"><ja>�R���g���[���̃N���C�A���g���W�œ_���w�肵�܂��B</ja></param>
    /// <returns><ja>�w�肵���_���A���s����ڂɑΉ����邩��Ԃ��܂��B��ԍ���̓_�� (0,0) �ł��B</ja></returns>
    protected Gdi::Point ClientPointToTextPosition(Gdi::Point clientPoint) {
      RenderProfile prof=this.GetRenderProfile();
      SizeF pitch=prof.Pitch;
      float x=(clientPoint.X-BORDER)/pitch.Width;
      float y=(clientPoint.Y-BORDER)/(pitch.Height+prof.LineSpacing);
      return new Gdi::Point(
        ((int)x).Clamp(0,Int32.MaxValue),
        ((int)y).Clamp(0,Int32.MaxValue));
    }
    /// <summary>
    /// <ja>�w�肵���_���A���s�ځE����ڂ̕������E�ɑΉ����邩���v�Z���܂��B</ja>
    /// </summary>
    /// <param name="clientPoint"><ja>�R���g���[���̃N���C�A���g���W�œ_���w�肵�܂��B</ja></param>
    /// <returns><ja>�w�肵���_���A���s����ڂ̕������E�ɍł��߂�����Ԃ��܂��B
    /// �ł�����̋��E�� (0,0) �ɑΉ����܂��B</ja></returns>
    protected Gdi::Point ClientPointToCharBorder(Gdi::Point clientPoint){
      RenderProfile prof=this.GetRenderProfile();
      SizeF pitch=prof.Pitch;
      float x=(clientPoint.X-BORDER)/pitch.Width;
      float y=(clientPoint.Y-BORDER)/(pitch.Height+prof.LineSpacing);
      return new Gdi::Point(
        ((int)(x+0.5f)).Clamp(0,Int32.MaxValue),
        ((int)y).Clamp(0,Int32.MaxValue));
    }
    #endregion

    //IPoderosaControl
    public Control AsControl() {
      return this;
    }

    //�}�E�X�z�C�[���ł̃X�N���[��
    protected override void OnMouseWheel(MouseEventArgs e) {
      if(!this.EnabledEx) return;

      int d = e.Delta / 120; //�J��������Delta��120�B�����1��-1������͂�
      d *= 3; //�ςɂ��Ă���������

      int newval = _VScrollBar.Value - d;
      if(newval<0) newval=0;
      if(newval>_VScrollBar.Maximum-_VScrollBar.LargeChange) newval=_VScrollBar.Maximum-_VScrollBar.LargeChange+1;
      _VScrollBar.Value = newval;
    }


    //SplitMark�֌W
    #region SplitMark.ISite
    protected override void OnMouseLeave(EventArgs e) {
      base.OnMouseLeave(e);
      if(_splitMark.IsSplitMarkVisible) _mouseHandlerManager.EndCapture();
      _splitMark.ClearMark();
    }

    public bool CanSplit {
      get {
        IContentReplaceableView v = AsControlReplaceableView();
        return v==null? false : GetSplittableViewManager().CanSplit(v);
      }
    }
    public int SplitClientWidth {
      get {
        return this.ClientSize.Width - (_enabled? _VScrollBar.Width : 0);
      }
    }
    public int SplitClientHeight {
      get {
        return this.ClientSize.Height;
      }
    }
    public Cursor MouseCursor {
      get {
        return _enabled? Cursors.IBeam : Cursors.Default;
      }
    }

    public void SplitVertically() {
      GetSplittableViewManager().SplitVertical(AsControlReplaceableView(), null);
    }
    public void SplitHorizontally() {
      GetSplittableViewManager().SplitHorizontal(AsControlReplaceableView(), null); 
    }

    public SplitMarkSupport SplitMark {
      get {
        return _splitMark;
      }
    }

    #endregion

    private ISplittableViewManager GetSplittableViewManager() {
      IContentReplaceableView v = AsControlReplaceableView();
      if(v==null)
        return null;
      else
        return (ISplittableViewManager)v.ViewManager.GetAdapter(typeof(ISplittableViewManager));
    }
    private IContentReplaceableView AsControlReplaceableView() {
      IContentReplaceableViewSite site = (IContentReplaceableViewSite)this.GetAdapter(typeof(IContentReplaceableViewSite));
      return site==null? null : site.CurrentContentReplaceableView;
    }

    #region ISelectionListener
    public void OnSelectionStarted() {
    }
    public void OnSelectionFixed() {
      if(WindowManagerPlugin.Instance.WindowPreference.OriginalPreference.AutoCopyByLeftButton) {
        ICommandTarget ct = (ICommandTarget)this.GetAdapter(typeof(ICommandTarget));
        if(ct!=null) {
          CommandManagerPlugin cm = CommandManagerPlugin.Instance;
          Keys mods=Control.ModifierKeys;
          //KM: (CharacterDocumentViewer.OnMouseMove):
          // ��bug: Keys.Control �� Keys.Shift �� �P��P�ʁE�s�P�ʂ̑I���Ƃ��Ċ��Ɏg���Ă��� 
          if(mods==Keys.Shift){
            //Debug.WriteLine("NormalCopy");
            IGeneralViewCommands gv = (IGeneralViewCommands)GetAdapter(typeof(IGeneralViewCommands));
            if(gv!=null) cm.Execute(gv.Copy, ct);
          }else if(mods==0){
            //Debug.WriteLine("CopyAsLook");
            cm.Execute(cm.Find("org.poderosa.terminalemulator.copyaslook"), ct);
          }else{
            TextFormatOption option=TextFormatOption.AsLook|TextFormatOption.TrimEol;
            if((mods&Keys.Alt)!=0)
              option|=TextFormatOption.Rectangle;
            if((mods&Keys.Shift)!=0)
              option&=~TextFormatOption.AsLook;
            if((mods&Keys.Control)!=0)
              option&=~TextFormatOption.TrimEol;
            cm.Execute(new Poderosa.Commands.SelectedTextCopyCommand(option),ct);
          }
        }
      }

    }
    #endregion

  }

  /*
   * ���s�ڂ��牽�s�ڂ܂ł�`�悷�ׂ����̏������^
   */
  internal class RenderParameter {
    private int _linefrom;
    private int _linecount;
    private Rectangle _targetRect;

    public int LineFrom {
      get {
        return _linefrom;
      }
      set {
        _linefrom=value;
      }
    }

    public int LineCount {
      get {
        return _linecount;
      }
      set {
        _linecount=value;
      }
    }
    public Rectangle TargetRect {
      get {
        return _targetRect;
      }
      set {
        _targetRect=value;
      }
    }
  }

  //�e�L�X�g�I���̃n���h��
  internal class TextSelectionUIHandler : DefaultMouseHandler {
    private CharacterDocumentViewer _viewer;
    public TextSelectionUIHandler(CharacterDocumentViewer v)
      : base("textselection") {
      _viewer = v;
    }

    public override UIHandleResult OnMouseDown(MouseEventArgs args) {
      if(args.Button!=MouseButtons.Left || !_viewer.EnabledEx) return UIHandleResult.Pass;

      //�e�L�X�g�I���ł͂Ȃ��̂ł�����ƕ��������BUserControl->Control�̒u�������ɔ���
      if(!_viewer.Focused) _viewer.Focus();


      CharacterDocument document = _viewer.CharacterDocument;
      lock(document) {
        int col, row;
        MousePosToTextPos(args.X, args.Y, out col, out row);
        int target_id = _viewer.GetTopLine().ID+row;
        TextSelection sel = _viewer.TextSelection;
        if(sel.State==SelectionState.Fixed) sel.Clear(); //�ςȂƂ����MouseDown�����Ƃ��Ă�Clear�����͂���
        if(target_id <= document.LastLineNumber) {
          //if(InFreeSelectionMode) ExitFreeSelectionMode();
          //if(InAutoSelectionMode) ExitAutoSelectionMode();
          RangeType rt;
          //Debug.WriteLine(String.Format("MouseDown {0} {1}", sel.State, sel.PivotType));

          //�����ꏊ�Ń|�`�|�`�Ɖ�����Char->Word->Line->Char�ƃ��[�h�ω�����
          if(sel.StartX!=args.X || sel.StartY!=args.Y)
            rt = RangeType.Char;
          else
            rt = sel.PivotType==RangeType.Char? RangeType.Word : sel.PivotType==RangeType.Word? RangeType.Line : RangeType.Char;

          //�}�E�X�𓮂����Ă��Ȃ��Ă��AMouseDown�ƂƂ���MouseMove�����Ă��܂��悤��
          GLine tl = document.FindLine(target_id);
          sel.StartSelection(tl, col, rt, args.X, args.Y);
        }
      }
      _viewer.Invalidate(); //NOTE �I����Ԃɕω��̂������s�̂ݍX�V����΂Ȃ��悵
      return UIHandleResult.Capture;
    }
    public override UIHandleResult OnMouseMove(MouseEventArgs args) {
      if(args.Button!=MouseButtons.Left) return UIHandleResult.Pass;
      TextSelection sel = _viewer.TextSelection;
      if(sel.State==SelectionState.Fixed || sel.State==SelectionState.Empty) return UIHandleResult.Pass;
      //�N���b�N�����ł��Ȃ���MouseDown�̒����MouseMove�C�x���g������̂ł��̂悤�ɂ��ăK�[�h�B�łȂ��ƒP���N���b�N�ł��I����ԂɂȂ��Ă��܂�
      if(sel.StartX==args.X && sel.StartY==args.Y) return UIHandleResult.Capture;

      CharacterDocument document = _viewer.CharacterDocument;
      lock(document) {
        int topline_id = _viewer.GetTopLine().ID;
        SizeF pitch = _viewer.GetRenderProfile().Pitch;
        int row, col;
        MousePosToTextPos_AllowNegative(args.X, args.Y, out col, out row);
        int viewheight = (int)Math.Floor(_viewer.ClientSize.Height / pitch.Width);
        int target_id = topline_id+row;

        GLine target_line = document.FindLineOrEdge(target_id);
        TextSelection.TextPoint point = sel.ConvertSelectionPosition(target_line, col);

        point.Line = RuntimeUtil.AdjustIntRange(point.Line, document.FirstLineNumber, document.LastLineNumber);

        if(_viewer.VScrollBar.Enabled) { //�X�N���[���\�ȂƂ���
          VScrollBar vsc = _viewer.VScrollBar;
          if(target_id < topline_id) //�O���X�N���[��
            vsc.Value = point.Line - document.FirstLineNumber; 
          else if(point.Line >= topline_id+vsc.LargeChange) { //����X�N���[��
            int newval = point.Line - document.FirstLineNumber - vsc.LargeChange + 1;
            if(newval<0) newval = 0;
            if(newval>vsc.Maximum-vsc.LargeChange) newval = vsc.Maximum-vsc.LargeChange+1;
            vsc.Value = newval;
          }
        }
        else { //�X�N���[���s�\�ȂƂ��͌����Ă���͈͂�
          point.Line = RuntimeUtil.AdjustIntRange(point.Line, topline_id, topline_id+viewheight-1);
        } //�������ڂ��Ă���
        //Debug.WriteLine(String.Format("MouseMove {0} {1} {2}", sel.State, sel.PivotType, args.X));
        RangeType rt = sel.PivotType;
        if((Control.ModifierKeys & Keys.Control)!=Keys.None)
          rt = RangeType.Word;
        else if((Control.ModifierKeys & Keys.Shift)!=Keys.None)
          rt = RangeType.Line;

        GLine tl = document.FindLine(point.Line);
        sel.ExpandTo(tl, point.Column, rt);
      }
      _viewer.Invalidate(); //TODO �I����Ԃɕω��̂������s�̂ݍX�V����悤�ɂ���΂Ȃ��悵
      return UIHandleResult.Capture;

    }
    public override UIHandleResult OnMouseUp(MouseEventArgs args) {
      TextSelection sel = _viewer.TextSelection;
      if(args.Button==MouseButtons.Left) {
        if(sel.State==SelectionState.Expansion || sel.State==SelectionState.Pivot)
          sel.FixSelection();
        else
          sel.Clear();
      }
      return _viewer.MouseHandlerManager.CapturingHandler==this? UIHandleResult.EndCapture : UIHandleResult.Pass;
               
    }

    private void MousePosToTextPos(int mouseX, int mouseY, out int textX, out int textY) {
      MousePosToTextPos_AllowNegative(mouseX,mouseY,out textX,out textY);
      textX=textX.Clamp(0,int.MaxValue);
      textY=textY.Clamp(0,int.MaxValue);
    }
    private void MousePosToTextPos_AllowNegative(int mouseX, int mouseY, out int textX, out int textY) {
      SizeF pitch = _viewer.GetRenderProfile().Pitch;
      float x=(mouseX-CharacterDocumentViewer.BORDER)/pitch.Width;
      float y=(mouseY-CharacterDocumentViewer.BORDER)/(pitch.Height+_viewer.GetRenderProfile().LineSpacing);
      textX=(int)Math.Floor(x+0.5);
      textY=(int)Math.Floor(y);
    }
  }

  //�X�v���b�g�}�[�N�̃n���h��
  internal class SplitMarkUIHandler : DefaultMouseHandler {
    private SplitMarkSupport _splitMark;
    public SplitMarkUIHandler(SplitMarkSupport split)
      : base("splitmark") {
      _splitMark = split;
    }

    public override UIHandleResult OnMouseDown(MouseEventArgs args) {
      return UIHandleResult.Pass;
    }
    public override UIHandleResult OnMouseMove(MouseEventArgs args) {
      bool v = _splitMark.IsSplitMarkVisible;
      if(v || WindowManagerPlugin.Instance.WindowPreference.OriginalPreference.ViewSplitModifier==Control.ModifierKeys)
        _splitMark.OnMouseMove(args);
      //���O�ɃL���v�`���[���Ă�����EndCapture
      return _splitMark.IsSplitMarkVisible? UIHandleResult.Capture : v? UIHandleResult.EndCapture : UIHandleResult.Pass;
    }
    public override UIHandleResult OnMouseUp(MouseEventArgs args) {
      bool visible = _splitMark.IsSplitMarkVisible;
      if(visible) {
        //�Ⴆ�΁A�}�[�N�\���ʒu����I���������悤�ȏꍇ���l�����A�}�[�N��ŉE�N���b�N����ƑI����������悤�ɂ���B
        _splitMark.OnMouseUp(args);
        return UIHandleResult.EndCapture;
      }
      else
        return UIHandleResult.Pass;
    }
  }


}
