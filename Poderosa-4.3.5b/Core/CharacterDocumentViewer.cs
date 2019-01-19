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
   * CharacterDocumentの表示を行うコントロール。機能としては次がある。
   * 　縦方向のみスクロールバーをサポート
   * 　再描画の最適化
   * 　キャレットの表示。ただしキャレットを適切に移動する機能は含まれない
   * 
   * 　今後あってもいいかもしれない機能は、行間やPadding(HTML用語の)、行番号表示といったところ
   */
  /// <summary>
  /// 
  /// </summary>
  /// <exclude/>
  public class CharacterDocumentViewer: Control, IPoderosaControl, ISelectionListener, SplitMarkSupport.ISite {
    
    public const int BORDER = 2; //内側の枠線のサイズ
    internal const int TIMER_INTERVAL = 50; //再描画最適化とキャレット処理を行うタイマーの間隔

    private CharacterDocument _document;
    private bool _errorRaisedInDrawing;
    private List<GLine> _transientLines; //再描画するGLineを一時的に保管する
    private TextSelection _textSelection;
    private SplitMarkSupport _splitMark;
    private bool _enabled; //ドキュメントがアタッチされていないときを示す 変更するときはEnabledExプロパティで！
    private bool _drawbgImage = true;

    protected MouseHandlerManager _mouseHandlerManager;
    protected VScrollBar _VScrollBar;
    protected bool _enableAutoScrollBarAdjustment; //リサイズ時に自動的に_VScrollBarの値を調整するかどうか
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
        _VScrollBar.Visible = value; //スクロールバーとは連動
        _splitMark.Pen.Color = value? SystemColors.ControlDark : SystemColors.Window; //このBackColorと逆で
        this.Cursor = this.MouseCursor; //Splitter.ISiteを援用
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

    //派生型であることを強制することなどのためにoverrideすることを許す
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
    //タイマーの受信
    private void CaretTick() {
      if(_enabled && _caret.Blink) {
        _caret.Tick();
        _document.InvalidatedRegion.InvalidateLine(GetTopLine().ID+_caret.Y);
        InvalidateEx();
      }
    }
    protected virtual void OnWindowManagerTimer() {
      //タイマーはTIMER_INTERVALごとにカウントされるので。
      int q = WindowManagerPlugin.Instance.WindowPreference.OriginalPreference.CaretInterval / TIMER_INTERVAL;
      if (q == 0)
        q = 1;
      if(++_tickCount % q ==0)
        CaretTick();
    }


    //自己サイズからScrollBarを適切にいじる
    public void AdjustScrollBar() {
      if(_document==null) return;
      RenderProfile prof = GetRenderProfile();
      float ch = prof.Pitch.Height + prof.LineSpacing;
      int largechange = (int)Math.Floor((this.ClientSize.Height - BORDER * 2 + prof.LineSpacing) / ch); //きちんと表示できる行数をLargeChangeにセット
      int current = GetTopLine().ID - _document.FirstLineNumber;
      int size = Math.Max(_document.Size, current + largechange);
      if(size <= largechange) {
        _VScrollBar.Enabled = false;
      }
      else {
        _VScrollBar.Enabled = true;
        _VScrollBar.LargeChange = largechange;
        _VScrollBar.Maximum = size - 1; //この-1が必要なのが妙な仕様だ
      }
    }

    //このあたりの処置定まっていない
    private RenderProfile _privateRenderProfile = null;
    public void SetPrivateRenderProfile(RenderProfile prof) {
      _privateRenderProfile = prof;
    }

    //overrideして別の方法でRenderProfileを取得することもある
    public virtual RenderProfile GetRenderProfile() {
      return _privateRenderProfile;
    }

    protected virtual void CommitTransientScrollBar() {
      //ViewerはUIによってしか切り取れないからここでは何もしなくていい
    }

    //行数で表示可能な高さを返す
    protected virtual int GetHeightInLines() {
      RenderProfile prof = GetRenderProfile();
      float ch = prof.Pitch.Height + prof.LineSpacing;
      int height = (int)Math.Floor((this.ClientSize.Height - BORDER * 2 + prof.LineSpacing) / ch);
      return (height > 0) ? height : 0;
    }

    //_documentのうちどれを先頭(1行目)として表示するかを返す
    public virtual GLine GetTopLine() {
      return _document.FindLine(_document.FirstLine.ID + _VScrollBar.Value);
    }

    //_VScrollBar.ValueChangedイベント
    protected virtual void VScrollBarValueChanged() {
      if(_enableAutoScrollBarAdjustment)
        Invalidate();
    }

    //キャレットの座標設定、表示の可否を設定
    protected virtual void AdjustCaret(Caret caret) {
    }

    //_documentの更新状況を見て適切な領域のControl.Invalidate()を呼ぶ。
    //また、コントロールを所有していないスレッドから呼んでもOKなようになっている。
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

    //NOTE 自分のDockがTopかLeftのとき、スクロールバーの位置が追随してくれないみたい
    private void AdjustScrollBarPosition() {
      _VScrollBar.Height = this.ClientSize.Height;
      _VScrollBar.Left = this.ClientSize.Width - _VScrollBar.Width;
    }

    //描画の本体
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

          //描画用にテンポラリのGLineを作り、描画中にdocumentをロックしないようにする
          //!!ここは実行頻度が高いのでnewを毎回するのは避けたいところだ
          RenderParameter param = new RenderParameter();
          _caret.Enabled = _caret.Enabled && this.Focused; //TODO さらにIME起動中はキャレットを表示しないように. TerminalControlだったらAdjustCaretでIMEをみてるので問題はない
          lock(_document) {
            CommitTransientScrollBar();
            BuildTransientDocument(e, param);
          }

          DrawLines(g, param);

          if(_caret.Enabled && (!_caret.Blink || _caret.IsActiveTick)) { //点滅しなければEnabledによってのみ決まる
            if(_caret.Style==CaretType.Line)
              DrawBarCaret(g, param, _caret.X, _caret.Y);
            else if(_caret.Style==CaretType.Underline)
              DrawUnderLineCaret(g, param, _caret.X, _caret.Y);
            else if(_caret.Style==CaretType.BoldUnderline)
              DrawBoldUnderlineCaret(g,param,_caret.X,_caret.Y);
          }
        }
        //マークの描画
        _splitMark.OnPaint(e);
      }
      catch(Exception ex) {
        if(!_errorRaisedInDrawing) { //この中で一度例外が発生すると繰り返し起こってしまうことがままある。なので初回のみ表示してとりあえず切り抜ける
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
      //  this.Width - _VScrollBar.Width - sm.ControlBorderWidth + 8, //この８がない値が正当だが、.NETの文字サイズ丸め問題のため行の最終文字が表示されないことがある。これを回避するためにちょっと増やす
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
          _transientLines.Add(l.Clone()); //TODO クローンはきついよなあ　だが描画の方が時間かかるので、その間ロックをしないためには仕方ない点もある
          l = l.NextLine;
          if(l==null) break;
        }
      }

      //以下、_transientLinesにはparam.LineFromから示される値が入っていることに注意

      //選択領域の描画
      if(!_textSelection.IsEmpty) {
        TextSelection.TextPoint from = _textSelection.HeadPoint;
        TextSelection.TextPoint to   = _textSelection.TailPoint;
        l = _document.FindLineOrNull(from.Line);
        GLine t = _document.FindLineOrNull(to.Line);

        bool isrect=(Control.ModifierKeys&Keys.Alt)!=0;

        if(l!=null && t!=null) { //本当はlがnullではいけないはずだが、それを示唆するバグレポートがあったので念のため
          t = t.NextLine;
          int p0=from.Column;
          //TODO: 全角文字調整?

          int p1=0;
          if(isrect){
            p1=to.Column;
            //TODO: 全角文字調整?
            if(p0>p1){
              int p2=p0;p0=p1;p1=p2;
            }
          }

          int start = from.Column; //たとえば左端を越えてドラッグしたときの選択範囲は前行末になるので pos==TerminalWidthとなるケースがある。
          do{
            int index = l.ID-(topline_id+param.LineFrom);
            if(0<=start && start<l.DisplayLength && 0<=index && index<_transientLines.Count) {

              int end=0;
              if(isrect){
                start=p0;
                end=p1;
                //TODO: 全角文字調整?
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

            start = 0; //２行目からの選択は行頭から
            l = l.NextLine;
          }while(l!=t);
        }
      }

      AdjustCaret(_caret);
      _caret.Enabled = _caret.Enabled && (param.LineFrom <= _caret.Y && _caret.Y < param.LineFrom+param.LineCount);

      //Caret画面外にあるなら処理はしなくてよい。２番目の条件は、Attach-ResizeTerminalの流れの中でこのOnPaintを実行した場合にTerminalHeight>lines.Countになるケースがあるのを防止するため
      if(_caret.Enabled) { 
        //ヒクヒク問題のため、キャレットを表示しないときでもこの操作は省けない
        if(_caret.Style==CaretType.Box) {
          int y = _caret.Y-param.LineFrom;
          if(y>=0 && y<_transientLines.Count)
            _transientLines[y].InverseCharacter(_caret.X, _caret.IsActiveTick, _caret.Color);
        }
      }
    }


    #region 描画
    private void DrawLines(Graphics g, RenderParameter param) {
      RenderProfile prof = GetRenderProfile();
      //Rendering Core
      if(param.LineFrom <= _document.LastLineNumber) {
        IntPtr hdc = g.GetHdc();
        IntPtr gdipgraphics = IntPtr.Zero;
        try {
          if(prof.UseClearType) {
            Win32.GdipCreateFromHDC(hdc, ref gdipgraphics);
            Win32.GdipSetTextRenderingHint(gdipgraphics, 5); //5はTextRenderingHintClearTypeGridFit
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
        // 色反転
        IntPtr hdc = g.GetHdc();
        try{
          const uint DSTINVERT = 0x550009; // 反転
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

      //画像内のコピー開始座標
      bgdrawer.UpdateImage(img,1,1,offset_x,offset_y,this.Size);
      bgdrawer.DrawImageClipped(g,clip);
    }

    //------------------------------------------------------------------------
    // 位置計算
    /// <summary>
    /// <ja>指定した点が、何行・何列目に対応するかを計算します。</ja>
    /// </summary>
    /// <param name="clientPoint"><ja>コントロールのクライアント座標で点を指定します。</ja></param>
    /// <returns><ja>指定した点が、何行何列目に対応するかを返します。一番左上の点が (0,0) です。</ja></returns>
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
    /// <ja>指定した点が、何行目・何列目の文字境界に対応するかを計算します。</ja>
    /// </summary>
    /// <param name="clientPoint"><ja>コントロールのクライアント座標で点を指定します。</ja></param>
    /// <returns><ja>指定した点が、何行何列目の文字境界に最も近いかを返します。
    /// 最も左上の境界が (0,0) に対応します。</ja></returns>
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

    //マウスホイールでのスクロール
    protected override void OnMouseWheel(MouseEventArgs e) {
      if(!this.EnabledEx) return;

      int d = e.Delta / 120; //開発環境だとDeltaに120。これで1か-1が入るはず
      d *= 3; //可変にしてもいいかも

      int newval = _VScrollBar.Value - d;
      if(newval<0) newval=0;
      if(newval>_VScrollBar.Maximum-_VScrollBar.LargeChange) newval=_VScrollBar.Maximum-_VScrollBar.LargeChange+1;
      _VScrollBar.Value = newval;
    }


    //SplitMark関係
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
          // ■bug: Keys.Control と Keys.Shift は 単語単位・行単位の選択として既に使われている 
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
   * 何行目から何行目までを描画すべきかの情報を収録
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

  //テキスト選択のハンドラ
  internal class TextSelectionUIHandler : DefaultMouseHandler {
    private CharacterDocumentViewer _viewer;
    public TextSelectionUIHandler(CharacterDocumentViewer v)
      : base("textselection") {
      _viewer = v;
    }

    public override UIHandleResult OnMouseDown(MouseEventArgs args) {
      if(args.Button!=MouseButtons.Left || !_viewer.EnabledEx) return UIHandleResult.Pass;

      //テキスト選択ではないのでちょっと柄悪いが。UserControl->Controlの置き換えに伴う
      if(!_viewer.Focused) _viewer.Focus();


      CharacterDocument document = _viewer.CharacterDocument;
      lock(document) {
        int col, row;
        MousePosToTextPos(args.X, args.Y, out col, out row);
        int target_id = _viewer.GetTopLine().ID+row;
        TextSelection sel = _viewer.TextSelection;
        if(sel.State==SelectionState.Fixed) sel.Clear(); //変なところでMouseDownしたとしてもClearだけはする
        if(target_id <= document.LastLineNumber) {
          //if(InFreeSelectionMode) ExitFreeSelectionMode();
          //if(InAutoSelectionMode) ExitAutoSelectionMode();
          RangeType rt;
          //Debug.WriteLine(String.Format("MouseDown {0} {1}", sel.State, sel.PivotType));

          //同じ場所でポチポチと押すとChar->Word->Line->Charとモード変化する
          if(sel.StartX!=args.X || sel.StartY!=args.Y)
            rt = RangeType.Char;
          else
            rt = sel.PivotType==RangeType.Char? RangeType.Word : sel.PivotType==RangeType.Word? RangeType.Line : RangeType.Char;

          //マウスを動かしていなくても、MouseDownとともにMouseMoveが来てしまうようだ
          GLine tl = document.FindLine(target_id);
          sel.StartSelection(tl, col, rt, args.X, args.Y);
        }
      }
      _viewer.Invalidate(); //NOTE 選択状態に変化のあった行のみ更新すればなおよし
      return UIHandleResult.Capture;
    }
    public override UIHandleResult OnMouseMove(MouseEventArgs args) {
      if(args.Button!=MouseButtons.Left) return UIHandleResult.Pass;
      TextSelection sel = _viewer.TextSelection;
      if(sel.State==SelectionState.Fixed || sel.State==SelectionState.Empty) return UIHandleResult.Pass;
      //クリックだけでもなぜかMouseDownの直後にMouseMoveイベントが来るのでこのようにしてガード。でないと単発クリックでも選択状態になってしまう
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

        if(_viewer.VScrollBar.Enabled) { //スクロール可能なときは
          VScrollBar vsc = _viewer.VScrollBar;
          if(target_id < topline_id) //前方スクロール
            vsc.Value = point.Line - document.FirstLineNumber; 
          else if(point.Line >= topline_id+vsc.LargeChange) { //後方スクロール
            int newval = point.Line - document.FirstLineNumber - vsc.LargeChange + 1;
            if(newval<0) newval = 0;
            if(newval>vsc.Maximum-vsc.LargeChange) newval = vsc.Maximum-vsc.LargeChange+1;
            vsc.Value = newval;
          }
        }
        else { //スクロール不可能なときは見えている範囲で
          point.Line = RuntimeUtil.AdjustIntRange(point.Line, topline_id, topline_id+viewheight-1);
        } //ここさぼっている
        //Debug.WriteLine(String.Format("MouseMove {0} {1} {2}", sel.State, sel.PivotType, args.X));
        RangeType rt = sel.PivotType;
        if((Control.ModifierKeys & Keys.Control)!=Keys.None)
          rt = RangeType.Word;
        else if((Control.ModifierKeys & Keys.Shift)!=Keys.None)
          rt = RangeType.Line;

        GLine tl = document.FindLine(point.Line);
        sel.ExpandTo(tl, point.Column, rt);
      }
      _viewer.Invalidate(); //TODO 選択状態に変化のあった行のみ更新するようにすればなおよし
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

  //スプリットマークのハンドラ
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
      //直前にキャプチャーしていたらEndCapture
      return _splitMark.IsSplitMarkVisible? UIHandleResult.Capture : v? UIHandleResult.EndCapture : UIHandleResult.Pass;
    }
    public override UIHandleResult OnMouseUp(MouseEventArgs args) {
      bool visible = _splitMark.IsSplitMarkVisible;
      if(visible) {
        //例えば、マーク表示位置から選択したいような場合を考慮し、マーク上で右クリックすると選択が消えるようにする。
        _splitMark.OnMouseUp(args);
        return UIHandleResult.EndCapture;
      }
      else
        return UIHandleResult.Pass;
    }
  }


}
