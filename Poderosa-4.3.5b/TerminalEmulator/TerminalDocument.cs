/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TerminalDocument.cs,v 1.4 2011/09/01 20:34:59 kzmi Exp $
 */
using System;
using System.Collections;
using System.Drawing;
using System.Diagnostics;

using Poderosa.Document;
using Poderosa.Commands;

namespace Poderosa.Terminal
{
	/// <summary>
	/// 
	/// </summary>
	/// <exclude/>
	public class TerminalDocument : CharacterDocument {
		private int _caretColumn;
		private int _scrollingTop;
		private int _scrollingBottom;
  		//�E�B���h�E�̕\���p�e�L�X�g
		private string _windowTitle; //�z�X�gOSC�V�[�P���X�Ŏw�肳�ꂽ�^�C�g��
		private GLine _topLine;
		private GLine _currentLine;

		//��ʂɌ����Ă��镝�ƍ���
		private int _width;
		private int _height;

		internal TerminalDocument(int width, int height) {
			Resize(width, height);
			Clear();
			_scrollingTop = -1;
			_scrollingBottom = -1;
		}

		public string WindowTitle {
			get {
				return _windowTitle;
			}
			set {
				_windowTitle = value;
			}
		}
		public int TerminalHeight {
			get {
				return _height;
			}
		}
		public int TerminalWidth {
			get {
				return _width;
			}
		}

		public override IPoderosaMenuGroup[] ContextMenu {
			get {
				return TerminalEmulatorPlugin.Instance.DocumentContextMenu;
			}
		}

		public void SetScrollingRegion(int top_offset, int bottom_offset) {
			_scrollingTop = TopLineNumber+top_offset;
			_scrollingBottom = TopLineNumber+bottom_offset;
			//GLine l = FindLine(_scrollingTop);
		}
		public void Clear() {
			_caretColumn = 0;
			_firstLine = null;
			_lastLine = null;
			_size = 0;
			AddLine(new GLine(_width));
		}
		public void Resize(int width, int height) {
			_width = width;
			_height = height;
		}

		public void ClearScrollingRegion() {
			_scrollingTop = -1;
			_scrollingBottom = -1;
		}
		public int CaretColumn {
			get {
				return _caretColumn;
			}
			set {
				_caretColumn = value;
			}
		}

		public GLine CurrentLine {
			get {
				return _currentLine;
			}
		}
   		public GLine TopLine {
			get {
				return _topLine;
			}
		}

		public int TopLineNumber {
			get {
				return _topLine.ID;
			}
			set {
				if(_topLine.ID!=value) _invalidatedRegion.InvalidatedAll = true;
				_topLine = FindLineOrEdge(value); //����̗��R��OrEdge�o�[�W�����ɕύX
			}
		}

		public void EnsureLine(int id) {
			while(id > _lastLine.ID) {
				AddLine(new GLine(_width));
			}
		}

		public int CurrentLineNumber {
			get {
				return _currentLine.ID;
			}
			set {
				if(value < _firstLine.ID) value = _firstLine.ID; //���T�C�Y���̔����ȃ^�C�~���O�ŕ��ɂȂ��Ă��܂����Ƃ��������悤��
				if(value > _lastLine.ID+100) value = _lastLine.ID+100; //�ɒ[�ɑ傫�Ȓl��H����Ď��ʂ��Ƃ��Ȃ��悤�ɂ���

				while(value > _lastLine.ID) {
					AddLine(new GLine(_width));
				}

				_currentLine = FindLineOrEdge(value); //�O������ςȒl���n���ꂽ��A���邢�͂ǂ����Ƀo�O�����邹���ł��̒��ŃN���b�V�����邱�Ƃ��܂�ɂ���悤���B�Ȃ̂�OrEdge�o�[�W�����ɂ��ăN���b�V���͉��
			}
		}

		public bool CurrentIsLast {
			get {
				return _currentLine.NextLine==null;
			}
		}
		#region part of IPoderosaDocument
		public override Image Icon {
			get {
				return _owner.Icon;
			}
		}
		//public override string Caption {
		//  get {
		//    return _owner.Caption;
		//  }
		//}
		#endregion

		public int ScrollingTop {
			get {
				return _scrollingTop;
			}
		}
		public int ScrollingBottom {
			get {
				return _scrollingBottom;
			}
		}
		internal void LineFeed() {
			if(_scrollingTop!=-1 && _currentLine.ID >= _scrollingBottom) { //���b�N����Ă��ĉ��܂ōs���Ă���
				ScrollDown(); 
			}
			else {
				if(_height>1) { //�ɒ[�ɍ������Ȃ��Ƃ��͂���ŕςȒl�ɂȂ��Ă��܂��̂ŃX�L�b�v
					if(_currentLine.ID >= _topLine.ID + _height - 1)
						this.TopLineNumber = _currentLine.ID - _height + 2; //����Ŏ���CurrentLineNumber++�ƍ��킹�čs����ɂȂ�
				}
				this.CurrentLineNumber++; //����Ńv���p�e�B�Z�b�g���Ȃ���A�K�v�Ȃ�s�̒ǉ��������B
			}
			
			//Debug.WriteLine(String.Format("c={0} t={1} f={2} l={3}", _currentLine.ID, _topLine.ID, _firstLine.ID, _lastLine.ID));
		}

		//�X�N���[���͈͂̍ł������P�s�����A�ł���ɂP�s�ǉ��B���ݍs�͂��̐V�K�s�ɂȂ�B
		internal void ScrollUp() {
			if(_scrollingTop!=-1 && _scrollingBottom!=-1)
				ScrollUp(_scrollingTop, _scrollingBottom);
			else
				ScrollUp(TopLineNumber, TopLineNumber + _height - 1);
		}
		
		internal void ScrollUp(int from, int to) {
			GLine removePoint = FindLineOrEdge(from);
			GLine insertPoint = FindLineOrEdge(to);
			if(removePoint==null || insertPoint==null) return; //�G���[�n���h�����O��FindLine�̒��ŁB�����ł̓N���b�V������������s��
			int bottom_id = insertPoint.ID;
			int topline_id = _topLine.ID;
			GLine nextbottom = insertPoint.NextLine;

      // 2015-03-11 KM bugfix
      //   if(from==to) �Ŕ�r���Ă����̂� if(removePoint==insertPoint) �ɕύX�B
      //   from==to ���� FindLineOrEdge �ł���� removePoint==insertPoint �ɂȂ�������
      //   �ʖڂȏ�ԂɂȂ�B
      if(removePoint==insertPoint){
				_currentLine = insertPoint;
				_currentLine.Clear();
			}	else {
				Remove(insertPoint);
				_currentLine = new GLine(_width);

				InsertBefore(removePoint, _currentLine);
				GLine c = _currentLine;
				do {
					c.ID = from++;
					c = c.NextLine;
				} while(c!=nextbottom);
				Debug.Assert(nextbottom==null || nextbottom.ID==from);
			}
			/*
			//id maintainance
			GLine c = newbottom;
			GLine end = _currentLine.PrevLine;
			while(c != end) {
				c.ID = bottom_id--;
				c = c.PrevLine;
			}
			*/

			//!!���̂Q�s��xterm������Ă���Ԃɔ������ďC���B VT100�ł͉����̕K�v�������Ă����Ȃ����͂��Ȃ̂Ō�Œ��ׂ邱��
			//if(_scrollingTop<=_topLine.ID && _topLine.ID<=_scrollingBottom)
			//	_topLine = _currentLine;
			while(topline_id<_topLine.ID)
				_topLine = _topLine.PrevLine;
			
			
			_invalidatedRegion.InvalidatedAll = true;
		}

		//�X�N���[���͈͂̍ł�����P�s�����A�ł����ɂP�s�ǉ��B���ݍs�͂��̐V�K�s�ɂȂ�B
		internal void ScrollDown() {
			if(_scrollingTop!=-1 && _scrollingBottom!=-1)
				ScrollDown(_scrollingTop, _scrollingBottom);
			else
				ScrollDown(TopLineNumber, TopLineNumber + _height - 1);
		}

		internal void ScrollDown(int from, int to) {
			GLine removePoint = FindLineOrEdge(from);
			GLine insertPoint = FindLineOrEdge(to);
			int top_id = removePoint.ID;
			GLine newtop = removePoint.NextLine;

      // 2015-03-11 KM bugfix
      //   if(from==to) �Ŕ�r���Ă����̂� if(removePoint==insertPoint) �ɕύX�B
      //   from==to ���� FindLineOrEdge �ł���� removePoint==insertPoint �ɂȂ�������
      //   �ʖڂȏ�ԂɂȂ�B
			if(removePoint==insertPoint) {
				_currentLine = removePoint;
				_currentLine.Clear();
			} else {
				bool removed=Remove(removePoint); //_topLine�̒����͕K�v�Ȃ炱���ōs����
				_currentLine = new GLine(_width);
				InsertAfter(insertPoint, _currentLine);

				//id maintainance
				if(removed){
					GLine c = newtop;
					GLine end = _currentLine.NextLine;
					while(c != end) {
						c.ID = top_id++;
						c = c.NextLine;
					}
				}else{
					GLine line=_currentLine;
					GLine lineM=_lastLine;
					for(;;){
						line.ID=line.PrevLine.ID+1;
						if(line==lineM)break;
						line=line.NextLine;
					}
					_scrollingTop++;
					_scrollingBottom++;
				}
			}
			
			_invalidatedRegion.InvalidatedAll = true;
		}

		//�����C���f�N�X���猩����@CurrentLine���炻�������Ȃ��ʒu���낤�Ƃ����������
		public override GLine FindLine(int index) {
			//current��top�̋߂������珇�ɂ݂Ă���
			int d1 = Math.Abs(index - _currentLine.ID);
			int d2 = Math.Abs(index - _topLine.ID);
			if(d1<d2)
				return FindLineByHint(index, _currentLine);
			else
				return FindLineByHint(index, _topLine);
		}


		public void Replace(GLine target, GLine newline) {
			newline.NextLine = target.NextLine;
			newline.PrevLine = target.PrevLine;
			if(target.NextLine!=null) target.NextLine.PrevLine = newline;
			if(target.PrevLine!=null) target.PrevLine.NextLine = newline;

			if(target==_firstLine) _firstLine = newline;
			if(target==_lastLine)  _lastLine = newline;
			if(target==_topLine)  _topLine  = newline;
			if(target==_currentLine) _currentLine = newline;
			
			newline.ID = target.ID;
			_invalidatedRegion.InvalidateLine(newline.ID);
		}

		//�����ɒǉ�����
		public override void AddLine(GLine line) {
			base.AddLine(line);
			if(_size==1) {
				_currentLine = line;
				_topLine = line;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		/// <returns>���ۂɍ폜�����s���ꂽ�ꍇ�� true ��Ԃ��܂��B
		/// false ���Ԃ��ꂽ�ꍇ�́A�\���̈�̊O�ɑ���ꂽ����\���܂��B
		/// </returns>
		private bool Remove(GLine line) {
			bool ret=false;
			if(line==_topLine){
				//mwg: ���֑���
				_topLine=line.NextLine;
			}else{
				//mwg: �폜
				if(line.PrevLine!=null)
					line.PrevLine.NextLine=line.NextLine;
				if(line.NextLine!=null)
					line.NextLine.PrevLine=line.PrevLine;
				if(line==_firstLine)
					_firstLine=line.NextLine;
				if(line==_lastLine)
					_lastLine =line.PrevLine;
				_size--;
				ret=true;
			}
			if(line==_currentLine) {
				_currentLine=line.NextLine;
				if(_currentLine==null)
					_currentLine=_lastLine;
			}

			//TODO:mwg: �S�� invalidate ����K�v�͂���̂�??
			_invalidatedRegion.InvalidatedAll=true;
			return ret;
		}

		/// �Ō��remain�s�ȑO���폜����
		public int DiscardOldLines(int remain) {
			int delete_count = _size - remain;
			if(delete_count <= 0) return 0;

			GLine newfirst = _firstLine;
			for(int i=0; i<delete_count; i++)
				newfirst = newfirst.NextLine;

			//�V�����擪�����߂�
			_firstLine = newfirst;
			newfirst.PrevLine.NextLine = null;
			newfirst.PrevLine = null;
			_size -= delete_count;
			Debug.Assert(_size==remain);


			if(_topLine.ID<_firstLine.ID) _topLine=_firstLine;
			if(_currentLine.ID<_firstLine.ID) {
				_currentLine = _firstLine;
				_caretColumn = 0;
			}

			return delete_count;
		}

		public void RemoveAfter(int from) {
			GLine delete = FindLineOrNullClipTop(from);
			if(delete==null) return;

			GLine remain = delete.PrevLine;
			delete.PrevLine = null;
			if(remain==null) {
				Clear();
			}
			else {
				remain.NextLine = null;
				_lastLine = remain;

				while(delete!=null) {
					_size--;
					if(delete==_topLine) _topLine = remain;
					if(delete==_currentLine) _currentLine = remain;
					delete = delete.NextLine;
				}
			}

			_invalidatedRegion.InvalidatedAll = true;
		}

		public void ClearAfter(int from, TextDecoration dec) {
			GLine l = FindLineOrNullClipTop(from);
			if(l==null) return;

			while(l!=null) {
				l.Clear(dec);
				l = l.NextLine;
			}

			_invalidatedRegion.InvalidatedAll = true;
		}

		public void ClearRange(int from, int to, TextDecoration dec) {
			GLine l = FindLineOrNullClipTop(from);
			if(l==null) return;

			while(l != null && l.ID < to) {
				l.Clear(dec);
				_invalidatedRegion.InvalidateLine(l.ID);
				l = l.NextLine;
			}
		}

		//�Đڑ��p�Ɍ��݃h�L�������g�̑O�ɑ}��
		public void InsertBefore(TerminalDocument olddoc, int paneheight) {
			lock(this) {
				GLine c = olddoc.LastLine;
				int offset = _currentLine.ID - _topLine.ID;
				bool flag = false;
				while(c!=null) {
					if(flag || c.Text[0]!='\0') {
						flag = true;
						GLine nl = c.Clone();
						nl.ID = _firstLine.ID-1;
						InsertBefore(_firstLine, nl); //�ŏ��ɋ�łȂ��s������Έȍ~�͑S���}��
						offset++;
					}
					c = c.PrevLine;
				}

				//ID�����ɂȂ�̂͂�����ƕ|���̂ŏC��
				if(_firstLine.ID<0) {
					int t = -_firstLine.ID;
					c = _firstLine;
					while(c!=null) {
						c.ID += t;
						c = c.NextLine;
					}
				}

				_topLine = FindLineOrEdge(_currentLine.ID - Math.Min(offset, paneheight));
				//Dump("insert doc");
			}
		}

		public void ReplaceCurrentLine(GLine line) {
#if DEBUG
			Replace(_currentLine, line);
#else
			if(_currentLine!=null) //�N���b�V�����|�[�g���݂�ƁA�����̔��q��null�ɂȂ��Ă����Ƃ����v���Ȃ�
				Replace(_currentLine, line);
#endif
		}
	}

}
