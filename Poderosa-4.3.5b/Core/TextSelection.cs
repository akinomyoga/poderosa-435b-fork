/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TextSelection.cs,v 1.2 2010/11/27 12:44:08 kzmi Exp $
 */
using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

using Poderosa.Sessions;
using Poderosa.Document;
using Poderosa.Forms;
using Poderosa.Commands;

namespace Poderosa.View
{
  internal enum RangeType {
    Char,
    Word,
    Line
  }
  internal enum SelectionState {
    Empty,     //無選択
    Pivot,     //選択開始
    Expansion, //選択中
    Fixed      //選択領域確定
  }

  //CharacterDocumentの一部を選択するための機能
  internal class TextSelection : ITextSelection {
    
    //端点
    internal class TextPoint : ICloneable {
      private int _line;
      private int _column;

      public int Line {
        get {
          return _line;
        }
        set {
          _line = value;
        }
      }
      public int Column {
        get {
          return _column;
        }
        set {
          _column = value;
        }
      }

      public TextPoint() {
        Clear();
      }
      public TextPoint(int line, int column) {
        _line = line;
        _column = column;
      }


      public void Clear() {
        Line = -1;
        Column = 0;
      }

      public object Clone() {
        return MemberwiseClone();
      }
    }

    private SelectionState _state;

    private List<ISelectionListener> _listeners;

    private CharacterDocumentViewer _owner;
    //最初の選択点。単語や行を選択したときのために２つ(forward/backward)設ける。
    private TextPoint _forwardPivot;
    private TextPoint _backwardPivot;
    //選択の最終点
    private TextPoint _forwardDestination;
    private TextPoint _backwardDestination;

    //pivotの状態
    private RangeType _pivotType;

    //選択を開始したときのマウス座標
    private int _startX;
    private int _startY;

    //ちょっと汚いフラグ
    //private bool _disabledTemporary;

    public TextSelection(CharacterDocumentViewer owner) {
      _owner = owner;
      _forwardPivot = new TextPoint();
      _backwardPivot = new TextPoint();
      _forwardDestination = new TextPoint();
      _backwardDestination = new TextPoint();
      _listeners = new List<ISelectionListener>();
    }

    public SelectionState State {
      get {
        return _state;
      }
    }
    public RangeType PivotType {
      get {
        return _pivotType;
      }
    }

    //マウスを動かさなくてもクリックだけでMouseMoveイベントが発生してしまうので、位置のチェックのためにマウス座標記憶が必要
    public int StartX {
      get {
        return _startX;
      }
    }
    public int StartY {
      get {
        return _startY;
      }
    }


    public void Clear() {
      //if(_owner!=null)
      //  _owner.ExitTextSelection();
      _state = SelectionState.Empty;
      _forwardPivot.Clear();
      _backwardPivot.Clear();
      _forwardDestination.Clear();
      _backwardDestination.Clear();
      //_disabledTemporary = false;
    }

    /*
    public void DisableTemporary() {
      _disabledTemporary = true;
    }*/

    #region ISelection
    public IPoderosaView OwnerView {
      get {
        return (IPoderosaView)_owner.GetAdapter(typeof(IPoderosaView));
      }
    }
    public IPoderosaCommand TranslateCommand(IGeneralCommand command) {
      return null;
    }
    public IAdaptable GetAdapter(Type adapter) {
      return WindowManagerPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
    }
    #endregion

    //ドキュメントがDiscardされたときに呼ばれる。first_lineより前に選択領域が重なっていたらクリアする
    public void ClearIfOverlapped(int first_line) {
      if(_forwardPivot.Line!=-1 && _forwardPivot.Line<first_line) {
        _forwardPivot.Line = first_line;
        _forwardPivot.Column = 0;
        _backwardPivot.Line = first_line;
        _backwardPivot.Column = 0;
      }
      
      if(_forwardDestination.Line!=-1 && _forwardDestination.Line<first_line) {
        _forwardDestination.Line = first_line;
        _forwardDestination.Column = 0;
        _backwardDestination.Line = first_line;
        _backwardDestination.Column = 0;
      }
    }

    public bool IsEmpty {
      get {
        return _forwardPivot.Line==-1 || _backwardPivot.Line==-1 ||
          _forwardDestination.Line==-1 || _backwardDestination.Line==-1;
      }
    }
    
    public bool StartSelection(GLine line, int position, RangeType type, int x, int y) {
      Debug.Assert(position>=0);
      //日本語文字の右側からの選択は左側に修正
      line.ExpandBuffer(position+1);
      if(line.Text[position]==GLine.WIDECHAR_PAD) position--;

      //_disabledTemporary = false;
      _pivotType = type;
      _forwardPivot.Line = line.ID;
      _backwardPivot.Line = line.ID;
      _forwardDestination.Line = line.ID;
      _forwardDestination.Column = position;
      _backwardDestination.Line = line.ID;
      _backwardDestination.Column = position;
      switch(type) {
        case RangeType.Char:
          _forwardPivot.Column = position;
          _backwardPivot.Column = position;
          break;
        case RangeType.Word:
          _forwardPivot.Column = line.FindPrevWordBreak(position)+1;
          _backwardPivot.Column = line.FindNextWordBreak(position);
          break;
        case RangeType.Line:
          _forwardPivot.Column = 0;
          _backwardPivot.Column = line.DisplayLength;
          break;
      }
      _state = SelectionState.Pivot;
      _startX = x;
      _startY = y;
      FireSelectionStarted();
      return true;
    }

    public bool ExpandTo(GLine line, int position, RangeType type) {
      line.ExpandBuffer(position+1);
      //_disabledTemporary = false;
      _state = SelectionState.Expansion;

      _forwardDestination.Line = line.ID;
      _backwardDestination.Line = line.ID;
      //Debug.WriteLine(String.Format("ExpandTo Line{0} Position{1}", line.ID, position));
      switch(type) {
        case RangeType.Char:
          _forwardDestination.Column = position;
          _backwardDestination.Column = position;
          break;
        case RangeType.Word:
          _forwardDestination.Column = line.FindPrevWordBreak(position)+1;
          _backwardDestination.Column = line.FindNextWordBreak(position);
          break;
        case RangeType.Line:
          _forwardDestination.Column = 0;
          _backwardDestination.Column = line.DisplayLength;
          break;
      }

      return true;
    }

    public void SelectAll() {
      //_disabledTemporary = false;
      _forwardPivot.Line = _owner.CharacterDocument.FirstLine.ID;
      _forwardPivot.Column = 0;
      _backwardPivot = (TextPoint)_forwardPivot.Clone();
      _forwardDestination.Line = _owner.CharacterDocument.LastLine.ID;
      _forwardDestination.Column = _owner.CharacterDocument.LastLine.DisplayLength;
      _backwardDestination = (TextPoint)_forwardDestination.Clone();

      _pivotType = RangeType.Char;
      FixSelection();
    }

    //選択モードに応じて範囲を定める。マウスでドラッグすることもあるので、column<0のケースも存在する
    public TextPoint ConvertSelectionPosition(GLine line, int column) {
      TextPoint result = new TextPoint(line.ID, column);
      
      int line_length = line.DisplayLength;
      if(_pivotType==RangeType.Line) {
        //行選択のときは、選択開始点以前のであったらその行の先頭、そうでないならその行のラスト。
        //言い換えると(Pivot-Destination)を行頭・行末方向に拡大したものになるように
        if(result.Line<=_forwardPivot.Line)
          result.Column = 0;
        else
          result.Column = line.DisplayLength;
      }else{ //Word,Char選択
        if(result.Line<_forwardPivot.Line) { //開始点より前のときは
          if(result.Column<0)
            result.Column = 0; //行頭まで。
          else if(result.Column>=line_length) { //行の右端の右まで選択しているときは、次行の先頭まで
            result.Line++;
            result.Column = 0;
          }
        } else if(result.Line==_forwardPivot.Line) { //同一行内選択.その行におさまるように
          result.Column = RuntimeUtil.AdjustIntRange(result.Column, 0, line_length);
        } else { //開始点の後方への選択
          if(result.Column<0) {
            result.Line--;
            result.Column = line.PrevLine==null? 0 : line.PrevLine.DisplayLength;
          }else if(result.Column>=line_length){
            //result.Column = line_length;
            /* KM 2012/10/07 22:36:28
             *  矩形選択で文字のない場所を矩形の端点にする為コメントアウト。
             *  最終的に文字列をコピーする時に、再度同様のチェックを行うので問題は生じない様だ。
             */
          }
        }
      }

      return result;
    }
    
    public void FixSelection() {
      _state = SelectionState.Fixed;
      FireSelectionFixed();
    }

    public string GetSelectedText(TextFormatOption opt){
      System.Text.StringBuilder b=new StringBuilder();
      TextPoint head=HeadPoint;
      TextPoint tail=TailPoint;

      GLine l=_owner.CharacterDocument.FindLineOrEdge(head.Line);
      int p0=head.Column;
      if(l.Text[p0]==GLine.WIDECHAR_PAD)p0--;
      if(p0<0)return "";

      bool isrect=(opt&TextFormatOption.Rectangle)!=0;
      int p1=0;
      if(isrect){
        p1=tail.Column;
        GLine tl=_owner.CharacterDocument.FindLineOrEdge(tail.Line);
        if(tl.Text[p1]==GLine.WIDECHAR_PAD)p1--;

        if(p0==p1)return "";

        if(p0>p1){
          l=tl;
          int p3=p0;p0=p1;p1=p3;
        }
      }

      int start=p0;
      for(;l!=null&&l.ID<=tail.Line;l=l.NextLine){
        //note: 本来 l==null はないはずだがクラッシュレポートのため回避

        bool fCRLF=
          (opt&(TextFormatOption.AsLook|TextFormatOption.Rectangle))!=0||
          l.EOLType!=EOLType.Continue;

        char[] text=l.Text;

        int end;
        if(isrect){
          start=p0;
          if(text[start]==GLine.WIDECHAR_PAD)start--;
          end=p1<l.Length?p1:l.Length;
        }else{
          if(l.ID==tail.Line){ //最終行
            end=tail.Column;
            fCRLF=fCRLF&&_pivotType==RangeType.Line;
          }else{ //最終以外の行
            end=l.Length;

            //nl=eol_required&&b.Length>0; //b.Length>0は行単位選択で余計な改行が入るのを避けるための処置
            //↑KM: 意図して先頭の改行も含む様に囲まなければ、これらの行は入らないのでは?
          }
        }

        if(end>text.Length)end=text.Length;
        if(fCRLF&&(opt&TextFormatOption.TrimEol)!=0&&l.ID!=tail.Line){
          // TrimEol
          for(;end>start;end--)
            if(!char.IsWhiteSpace(text[end-1])&&text[end-1]!=GLine.WIDECHAR_PAD&&text[end-1]!='\0')
              break;
        }

        for(int i=start;i<end;i++){
          char ch=text[i];
          if(ch!=GLine.WIDECHAR_PAD&&ch!='\0')
            b.Append(ch);
          //末尾にNULL文字が入るケースがあるようだ
        }

        //note: LFのみをクリップボードに持っていっても他のアプリの混乱があるだけなのでやめておく
        if(fCRLF)b.Append("\r\n");

        start=0;
      }

      return b.ToString();
    }


    internal TextPoint HeadPoint {
      get {
        return Min(_forwardPivot, _forwardDestination);
      }
    }
    internal TextPoint TailPoint {
      get {
        return Max(_backwardPivot, _backwardDestination);
      }
    }
    private static TextPoint Min(TextPoint p1, TextPoint p2) {
      int id1 = p1.Line;
      int id2 = p2.Line;
      if(id1==id2) {
        int pos1 = p1.Column;
        int pos2 = p2.Column;
        if(pos1==pos2)
          return p1;
        else
          return pos1<pos2? p1 : p2;
      }
      else
        return id1<id2? p1 : p2;
        
    }
    private static TextPoint Max(TextPoint p1, TextPoint p2) {
      int id1 = p1.Line;
      int id2 = p2.Line;
      if(id1==id2) {
        int pos1 = p1.Column;
        int pos2 = p2.Column;
        if(pos1==pos2)
          return p1;
        else
          return pos1>pos2? p1 : p2;
      }
      else
        return id1>id2? p1 : p2;
        
    }

    //Listener系
    public void AddSelectionListener(ISelectionListener listener) {
      _listeners.Add(listener);
    }
    public void RemoveSelectionListener(ISelectionListener listener) {
      _listeners.Remove(listener);
    }

    void FireSelectionStarted() {
      foreach(ISelectionListener listener in _listeners) listener.OnSelectionStarted();
    }
    void FireSelectionFixed() {
      foreach(ISelectionListener listener in _listeners) listener.OnSelectionFixed();
    }

  }
}
