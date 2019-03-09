/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: GLine.cs,v 1.6 2011/04/02 04:39:35 kzmi Exp $
 */
/*
* Copyright (c) 2005 Poderosa Project, All Rights Reserved.
* $Id: GLine.cs,v 1.6 2011/04/02 04:39:35 kzmi Exp $
*/
//#define KM20121125_UseExtTextOut_impl2
//#define KM20121125_UseExtTextOut_impl3

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Text;

#if UNITTEST
using NUnit.Framework;
#endif

using Poderosa.Util.Drawing;
using Poderosa.Forms;
using Poderosa.View;

namespace Poderosa.Document
{
	// GLine�̍\���v�f�B�P��GWord�͓����`�悪�Ȃ���A�V���O�������N���X�g�ɂȂ��Ă���B
	/// <summary>
	/// 
	/// </summary>
	/// <exclude/>
	public sealed class GWord {
		private TextDecoration _decoration; //�`����
		private int _offset;                //�R���e�i��GLine�̉������ڂ���n�܂��Ă��邩
		private CharGroup _charGroup;       //�����O���[�v
		private GWord _next;                //����GWord

		//���΂��ΎQ�Ƃ���̂ŃL���b�V������l�B
		internal int nextOffsetCache;     //_next==null�̂Ƃ��͂���GWord�ŕ`�悷�镶���̏I�[�ʒu�A_next!=null�̂Ƃ���_next.Offset
		internal int displayLengthCache;  //�`��ʒu�ł͉������ڂ�
		public const int NOT_CACHED = -1; //��L�̏����l

		/// �\���p�̑���
		internal TextDecoration Decoration {
			get {
				return _decoration;
			}
		}
		/// ��������GLine�̒��ŉ������ڂ���n�܂��Ă��邩
		public int Offset {
			get {
				return _offset;
			}
		}

		///����Word
		public GWord Next {
			get {
				return _next;
			}
			set {
				_next = value;
				nextOffsetCache = NOT_CACHED;
				displayLengthCache = NOT_CACHED;
			}
		}

		public CharGroup CharGroup {
			get {
				return _charGroup;
			}
			set {
				_charGroup = value;
			}
		}

		/// ������A�f�R���[�V�����A�I�t�Z�b�g���w�肷��R���X�g���N�^�B
		public GWord(TextDecoration d, int o, CharGroup chargroup) {
			Debug.Assert(d!=null);
			_offset = o;
			_decoration = d;
			_next = null;
			_charGroup = chargroup;
			nextOffsetCache = NOT_CACHED;
			displayLengthCache = NOT_CACHED;
		}

		//Next�̒l�ȊO���R�s�[����
		internal GWord StandAloneClone() {
			return new GWord(_decoration, _offset, _charGroup);
		}

		internal GWord DeepClone() {
			GWord w = new GWord(_decoration, _offset, _charGroup);
			if(_next!=null)
				w._next = _next.DeepClone();
			return w;
		}

	}


	/// �P�s�̃f�[�^
	/// GWord�ւ̕����͒x���]�������B���ׂ̍s�̓_�u�������N���X�g
	/// <exclude/>
	public sealed class GLine {
		static GLine() {}

		public const char WIDECHAR_PAD = '\uFFFF';

		private char[] _text; //�{�́F\0�͍s��������
		private EOLType _eolType;
		private int _id;
		private GWord _firstWord;
		private GLine _nextLine;
		private GLine _prevLine;

		public int Length {
			get {
				return _text.Length;
			}
		}

		public GWord FirstWord { 
			get {
				return _firstWord;
			}
		}
		public char[] Text {
			get {
				return _text;
			}
		}
		//ID, �אڍs�̐ݒ�@���̕ύX�͐T�d���K�v�I
		public int ID {
			get {
				return _id;
			}
			set {
				_id = value;
			}
		}
		public GLine NextLine {
			get {
				return _nextLine;
			}
			set {
				_nextLine = value;
			}
		}
		public GLine PrevLine {
			get {
				return _prevLine;
			}
			set {
				_prevLine = value;
			}
		}

		public EOLType EOLType {
			get {
				return _eolType;
			}
			set {
				_eolType = value;
			}
		}

		public GLine(int length) {
			Debug.Assert(length>0);
			_text = new char[length];
			_firstWord = new GWord(TextDecoration.Default, 0, CharGroup.Hankaku);
			_id = -1;
		}

		//GLineManipulator�Ȃǂ̂��߂̃R���X�g���N�^
		public GLine(char[] data, GWord firstWord) {
			_text = (char[])data.Clone();
			_firstWord = firstWord;
			_id = -1;
		}
		public GLine Clone() {
			GLine nl = new GLine(_text, _firstWord.DeepClone());
			nl._eolType = _eolType;
			nl._id = _id;
			return nl;
		}

		public void Clear() {
			Clear(null);
		}
		public void Clear(TextDecoration dec) {
			TextDecoration fillDec = (dec != null) ? dec.RetainBackColor() : TextDecoration.Default;
			char fill = fillDec.IsDefault ? '\0' : ' '; // �F�w��t���̂��Ƃ�����̂ŃX�y�[�X
			for(int i=0; i<_text.Length; i++)
				_text[i] = fill;
			_firstWord = new GWord(fillDec, 0, CharGroup.Hankaku);
		}

		public int DisplayLength {
			get {
				int i = 0;
				int m = _text.Length;
				//�S�p�\��������WIDECHAR_PAD�������Ă���̂ł���ł悢
				for(i=0; i<m; i++) {
					if(_text[i]=='\0') break;
				}
				return i;
			}
		}


		//�O��̒P���؂��������B�Ԃ��ʒu�́Apos��GetWordBreakGroup�̒l����v���钆�ŉ����n�_
		public int FindPrevWordBreak(int pos) {
			int v = ToCharGroupForWordBreak(_text[pos]);
			while(pos>=0) {
				if(v!=ToCharGroupForWordBreak(_text[pos])) return pos;
				pos--;
			}
			return -1;
		}
		public int FindNextWordBreak(int pos) {
			int v = ToCharGroupForWordBreak(_text[pos]);
			while(pos<_text.Length) {
				if(v!=ToCharGroupForWordBreak(_text[pos])) return pos;
				pos++;
			}
			return _text.Length;
		}
		private static int ToCharGroupForWordBreak(char ch) {
			if(ch<0x80)
				return ASCIIWordBreakTable.Default.GetAt(ch);
			else if(ch=='\u3000') //�S�p�X�y�[�X
				return ASCIIWordBreakTable.SPACE;
			else //����ɂ�����UnicodeCategory�����݂ēK���ɂ����炦�邱�Ƃ��ł��邪
				return ASCIIWordBreakTable.NOT_ASCII;
		}


		public void ExpandBuffer(int length) {
			if(length<=_text.Length) return;

			char[] current = _text;
			_text = new char[length];
			Array.Copy(current, 0, _text, 0, current.Length);
		}
		internal void Append(GWord w) {
			if(_firstWord==null)
				_firstWord = w;
			else
				this.LastWord.Next = w;
		}
		internal GWord LastWord {
			get {
				GWord w = _firstWord;
				while(w.Next!=null)
					w = w.Next;
				return w;
			}
		}

    internal void Render(IntPtr hdc, RenderProfile prof, int x, int y) {
      this.Render2(hdc,prof,x,y);
    }

    #region Render2
    internal void Render2(IntPtr hdc, RenderProfile prof, int x, int y) {
      if(_text.Length==0 || _text[0]=='\0') return; //�����`���Ȃ��Ă悢�B����͂悭����P�[�X

      //2013-08-05, KM,
      //  �w�i�F�ɂ���Đ�ɕ`�悵�������̉E�[��������Ă��܂��̂ŁA
      //  �w�i�F��`�悵�Ă���ォ��(�w�i�Ȃ���)������`�悷��l�ɕύX�B
      this.Render2_DrawBackground(hdc,prof,x,y);
      this.Render2_DrawForeground(hdc,prof,x,y);
    }
    void Render2_DrawBackground(IntPtr hdc, RenderProfile prof, int x, int y) {
      float fx = (float)x;
      for(GWord word=_firstWord;word!=null;word=word.Next){
        int ix = (int)fx;
        TextDecoration dec = word.Decoration;
        int display_length = WordDisplayLength(word);

        if(dec!=null){
          Color bkcolor = prof.CalcBackColor(dec);
          if(bkcolor!=prof.BackColor){ // ��{�w�i�F�ƈقȂ鎞�����h��
            uint bkcolorref   = DrawUtil.ToCOLORREF(bkcolor);
            Win32.SetBkColor(hdc, bkcolorref);
            Win32.SetBkMode(hdc,2); //OPAQUE
            IntPtr bkbrush = Win32.CreateSolidBrush(bkcolorref);
            Render2_DrawBackRect(hdc,fx,y,display_length,bkbrush,prof);
            Win32.DeleteObject(bkbrush);
          }
        }

        fx += prof.Pitch.Width * display_length;
      }
    }
    void Render2_DrawForeground(IntPtr hdc, RenderProfile prof, int x, int y) {
      Win32.SetBkMode(hdc,1); //TRANSPARENT

      float fx = (float)x;
      for(GWord word=_firstWord;word!=null;word=word.Next){
        int ix = (int)fx;
        TextDecoration dec = word.Decoration;
        int display_length = WordDisplayLength(word);

        //Brush brush = prof.CalcTextBrush(dec);
        uint forecolorref = DrawUtil.ToCOLORREF(prof.CalcTextColor(dec));
        IntPtr hfont = prof.CalcHFONT_NoUnderline(dec, word.CharGroup);
        Win32.SelectObject(hdc, hfont);
        Win32.SetTextColor(hdc, forecolorref);
        this.Render2_DrawWordExt(prof,hdc,ix,y,word);

        if(dec.HasLine){
          //Underline�������Ƃ͂��܂�Ȃ����낤���疈��Pen�����B���ɂȂ肻���������炻�̂Ƃ��ɍl���悤
          IntPtr pen=Win32.CreatePen(0, 1, forecolorref);
          IntPtr prev=Win32.SelectObject(hdc, pen);
          int h=(int)prof.Pitch.Height-1;
          int w=(int)(prof.Pitch.Width * display_length);
          if(dec.Doubleline)
            Render2_DrawDoubleline(hdc, pen, ix, y+h, w);
          else if(dec.Underline)
            Render2_DrawUnderline(hdc, pen, ix, y+h, w);
          if(dec.Overline)
            Render2_DrawUnderline(hdc, pen, ix, y, w);
          if(dec.Throughline)
            Render2_DrawUnderline(hdc, pen, ix, y+h/2, w);
          Win32.SelectObject(hdc, prev);
          Win32.DeleteObject(pen);
        }

        fx += prof.Pitch.Width * display_length;
      }
    }

    const int RENDER2_DX_LEN=80;
    unsafe void Render2_DrawWordExt(RenderProfile prof,System.IntPtr hdc,int x,int y,GWord word){
      float pitch=prof.Pitch.Width;
      int ibase=word.Offset;
      int len=WordDisplayLength(word);

      if(word.CharGroup==CharGroup.Hankaku){
        // �s�b�`�z��̏�����
        int dxN=len>RENDER2_DX_LEN?RENDER2_DX_LEN:len;
        int* dx=stackalloc int[dxN];
        float fx=0;
        for(int i=0;i<dxN;i++){
          dx[i]=(int)(fx+pitch)-(int)fx;
          fx+=pitch;
        }

        fixed(char* p0 = &_text[0]){
          char* p=p0+ibase;
          while(len>RENDER2_DX_LEN){
            Win32.ExtTextOut(hdc,x,y,0,null,p,RENDER2_DX_LEN,dx);
            x+=(int)(pitch*RENDER2_DX_LEN);
            p+=RENDER2_DX_LEN;
            len-=RENDER2_DX_LEN;
          }
          Win32.ExtTextOut(hdc,x,y,0,null,p,len,dx);
          //Win32.ExtTextOut(hdc,x,y,Win32.ETO_CLIPPED,null,p,len,dx);
        }
      }else if(word.CharGroup==CharGroup.AcsSymbol){
        float fx=x;
        for(int i=0;i<len;i++){
          char ch=_text[ibase+i];
          if(ch=='\0') break;
          if(ch==GLine.WIDECHAR_PAD) continue;
          char gr=mwg.RosaTerm.AcsSymbolsDefinition.GetGriph(ch);
          Win32.TextOut(hdc, (int)fx, y, &gr, 1);
          fx += pitch * CalcDisplayLength(ch);
        }
      }else{
        // �S�p������Ȃ�
        int ich=0;
        int*  dx  =stackalloc int [RENDER2_DX_LEN];
        char* buff=stackalloc char[RENDER2_DX_LEN+1];

        float fx0=x,fx1=x,fx2;
        for(int i=0;i<len;){
          buff[ich]=_text[ibase+i];

          fx2=fx1+pitch;
          i++;
          while(i<len&&_text[ibase+i]==GLine.WIDECHAR_PAD){
            fx2+=pitch;
            i++;
          }

          dx[ich]=(int)fx2-(int)fx1;
          fx1=fx2;
          ich++;

          if(ich>=RENDER2_DX_LEN){
            buff[ich]='\0';
            Win32.ExtTextOut(hdc,(int)fx0,y,0,null,buff,ich,dx);
            ich=0;
            fx0=fx1;
          }
        }

        if(ich>0){
          buff[ich]='\0';
          Win32.ExtTextOut(hdc,(int)fx0,y,0,null,buff,ich,dx);
        }
      }

    }

    static void Render2_DrawBackRect(IntPtr hdc,float fx,float fy,int nchar,IntPtr bkbrush,RenderProfile prof){
      if(bkbrush==IntPtr.Zero)return;

      Win32.RECT rect = new Win32.RECT();
      rect.left = (int)fx;
      rect.top  = (int)fy;
      rect.right = (int)(fx + prof.Pitch.Width*nchar);
      rect.bottom = (int)(fy + prof.Pitch.Height);
      Win32.FillRect(hdc, ref rect, bkbrush);
    }
    static void Render2_DrawUnderline(IntPtr hdc, IntPtr pen, int x, int y, int length) {
      Win32.MoveToEx(hdc, x, y, IntPtr.Zero);
      Win32.LineTo(hdc, x+length, y);
    }
    static void Render2_DrawDoubleline(IntPtr hdc, IntPtr pen, int x, int y, int length) {
      Win32.MoveToEx(hdc, x, y-1, IntPtr.Zero);
      Win32.LineTo(hdc, x+length, y-1);
      Win32.MoveToEx(hdc, x, y+1, IntPtr.Zero);
      Win32.LineTo(hdc, x+length, y+1);
    }
    #endregion

    #region Render1
    internal void Render1(IntPtr hdc, RenderProfile prof, int x, int y) {
      if(_text.Length==0 || _text[0]=='\0') return; //�����`���Ȃ��Ă悢�B����͂悭����P�[�X
      float fx = (float)x;

      GWord word = _firstWord;
      while(word != null) {
        int ix = (int)fx;
        TextDecoration dec = word.Decoration;

        //Brush brush = prof.CalcTextBrush(dec);
        uint forecolorref = DrawUtil.ToCOLORREF(prof.CalcTextColor(dec));
        Color bkcolor = prof.CalcBackColor(dec);
        uint bkcolorref   = DrawUtil.ToCOLORREF(bkcolor);
        IntPtr hfont = prof.CalcHFONT_NoUnderline(dec, word.CharGroup);
        Win32.SelectObject(hdc, hfont);
        Win32.SetTextColor(hdc, forecolorref);
        Win32.SetBkColor(hdc, bkcolorref);
        Win32.SetBkMode(hdc, bkcolor==prof.BackColor? 1 : 2); //��{�w�i�F�ƈꏏ�Ȃ�TRANSPARENT, �قȂ��OPAQUE
        IntPtr bkbrush = bkcolor==prof.BackColor? IntPtr.Zero : Win32.CreateSolidBrush(bkcolorref);

        int display_length = WordDisplayLength(word);
        if(dec==null) { //�����Ȃ�
          //g.DrawString(WordText(word), font, brush, rect);
          //mwg: �����ɓ����Ă��鎖�͂���̂�?
          DrawWord(hdc, ix, y, word);
        }else{
          if(prof.CalcBold(dec) || word.CharGroup!=CharGroup.Hankaku) //�����t�H���g�w��ł����{�ꂪ���p�̂Q�{�łȂ��ꍇ����B�p�t�H�[�}���X���̓N���A�������̂Ŋm���ɂP�������`��
            DrawStringByOneChar2(hdc, word, display_length, bkbrush, fx, y, prof);
          else
            //DrawWordExt(hdc, ix, y, word); //���܂�A�z�ȕ`��G���W���̖�肩��͉�����ꂽ�I
            DrawWordExt(prof,hdc,ix, y, word);
        }

        if(dec.HasLine){
          //Underline�������Ƃ͂��܂�Ȃ����낤���疈��Pen�����B���ɂȂ肻���������炻�̂Ƃ��ɍl���悤
          IntPtr pen=Win32.CreatePen(0, 1, forecolorref);
          IntPtr prev=Win32.SelectObject(hdc, pen);
          int h=(int)prof.Pitch.Height-1;
          int w=(int)(prof.Pitch.Width * display_length);
          if(dec.Doubleline)
            Render2_DrawDoubleline(hdc, pen, ix, y+h, w);
          else if(dec.Underline)
            Render2_DrawUnderline(hdc, pen, ix, y+h, w);
          if(dec.Overline)
            Render2_DrawUnderline(hdc, pen, ix, y, w);
          if(dec.Throughline)
            Render2_DrawUnderline(hdc, pen, ix, y+h/2, w);
          Win32.SelectObject(hdc, prev);
          Win32.DeleteObject(pen);
        }

        fx += prof.Pitch.Width * display_length;
        word = word.Next;
        if(bkbrush!=IntPtr.Zero) Win32.DeleteObject(bkbrush);
      }
    }

#if KM20121125_UseExtTextOut_impl2
    const int DX_LEN=80;
    [System.ThreadStatic]
    static readonly int[] DX_LIST=new int[DX_LEN];
    [System.ThreadStatic]
    static float DX_PITCH=0;
    static unsafe void UpdatePitchArray(float pitch){
      if(DX_PITCH!=pitch){
        fixed(int* dx=&DX_LIST[0]){
          float fx=0;
          for(int i=0;i<DX_LEN;i++){
            DX_LIST[i]=(int)(fx+pitch)-(int)fx;
            fx+=pitch;
          }
          DX_PITCH=pitch;
        }
      }
    }
#elif KM20121125_UseExtTextOut_impl3
    const int DX_LEN=RenderProfile.PITCH_DX_LEN;
#else
    const int DX_LEN=80;
#endif

    void DrawWordExt(RenderProfile prof,System.IntPtr hdc,int x,int y,GWord word){
      float pitch=prof.Pitch.Width;
      if(word.CharGroup==CharGroup.Hankaku){
        int len = WordNextOffset(word) - word.Offset;

        unsafe{
#if KM20121125_UseExtTextOut_impl2
          UpdatePitchArray(pitch);
          fixed(int* dx=&DX_LIST[0])
#elif KM20121125_UseExtTextOut_impl3
          fixed(int* dx=&prof.pitch_deltaXArray[0])
#else
          // �s�b�`�z��̏�����
          int dxN=len>DX_LEN?DX_LEN:len;
          int* dx=stackalloc int[dxN];
          float fx=0;
          for(int i=0;i<dxN;i++){
            dx[i]=(int)(fx+pitch)-(int)fx;
            fx+=pitch;
          }
#endif
          fixed(char* p0 = &_text[0]){
            char* p=p0+word.Offset;
            while(len>DX_LEN){
              Win32.ExtTextOut(hdc,x,y,0,null,p,DX_LEN,dx);
              x+=(int)(pitch*DX_LEN);
              p+=DX_LEN;
              len-=DX_LEN;
            }
            Win32.ExtTextOut(hdc,x,y,0,null,p,len,dx);
            //Win32.ExtTextOut(hdc,x,y,Win32.ETO_CLIPPED,null,p,len,dx);
          }
        }
      }else{
        DrawWord(hdc,x,y,word);
      }
    }

		private void DrawWord(IntPtr hdc, int x, int y, GWord word) {
			unsafe {
				int len;

				if(word.CharGroup==CharGroup.Hankaku) {
					fixed(char* p = &_text[0]) {
						len = WordNextOffset(word) - word.Offset;
						Win32.TextOut(hdc, x, y, p+word.Offset, len);
						//Win32.ExtTextOut(hdc, x, y, 4, null, p+word.Offset, len, null);
					}
				}
				else {
					string t = WordText(word);
					fixed(char* p = t) {
						len = t.Length;
						Win32.TextOut(hdc, x, y, p, len);
						//Win32.ExtTextOut(hdc, x, y, 4, null, p, len, null);
					}
				}
			
			}
		}

		private void DrawStringByOneChar2(IntPtr hdc, GWord word, int display_length, IntPtr bkbrush, float fx, int y, RenderProfile prof) {
			float pitch = prof.Pitch.Width;
			int nextoffset = WordNextOffset(word);
			if(bkbrush!=IntPtr.Zero) { //���ꂪ�Ȃ��Ɠ��{�ꕶ���s�b�`���������Ƃ��I�����̂����܂��ł���ꍇ������
				Win32.RECT rect = new Win32.RECT();
				rect.left = (int)fx;
				rect.top  = y;
				rect.right = (int)(fx + pitch*display_length);
				rect.bottom = y + (int)prof.Pitch.Height;
				Win32.FillRect(hdc, ref rect, bkbrush);
			}

			if(word.CharGroup==CharGroup.AcsSymbol){
				for(int i=word.Offset; i<nextoffset; i++) {
					char ch = _text[i];
					if(ch=='\0') break;
					if(ch==GLine.WIDECHAR_PAD) continue;
					char gr=mwg.RosaTerm.AcsSymbolsDefinition.GetGriph(ch);
					unsafe {
						Win32.TextOut(hdc, (int)fx, y, &gr, 1);
					}
					fx += pitch * CalcDisplayLength(ch);
				}
			}else{
				for(int i=word.Offset; i<nextoffset; i++) {
					char ch = _text[i];
					if(ch=='\0') break;
					if(ch==GLine.WIDECHAR_PAD) continue;
					unsafe {
						Win32.TextOut(hdc, (int)fx, y, &ch, 1);
					}
					fx += pitch * CalcDisplayLength(ch);
				}
			}
		}
    #endregion

		private string WordText(GWord word) {
			int nextoffset = WordNextOffset(word);
			if(nextoffset==0)
				return "";
			else {
				Debug.Assert(nextoffset-word.Offset >= 0);
				if(word.CharGroup==CharGroup.Hankaku)
					return new string(_text, word.Offset, nextoffset-word.Offset);
				else {
					StringBuilder bld = new StringBuilder();
					int o = word.Offset;
					while(o < nextoffset) {
						char ch = _text[o];
						if(ch!=GLine.WIDECHAR_PAD)
							bld.Append(ch);
						o++;
					}
					return bld.ToString();
				}
			}
		}
		private int WordDisplayLength(GWord word) {
			//�����͌Ă΂�邱�Ƃ��ƂĂ������̂ŃL���b�V����݂���
			int cache = word.displayLengthCache;
			if(cache==GWord.NOT_CACHED) {
				int nextoffset = WordNextOffset(word);
				int l = nextoffset - word.Offset;
				word.displayLengthCache = l;
				return l;
			}
			else
				return cache;
		}

		internal int WordNextOffset(GWord word) {
			//�����͌Ă΂�邱�Ƃ��ƂĂ������̂ŃL���b�V����݂���
			int cache = word.nextOffsetCache;
			if(cache==GWord.NOT_CACHED) {
				if(word.Next==null) {
					int i = _text.Length-1;
					while(i>=0 && _text[i]=='\0')
						i--;
					word.nextOffsetCache = i+1;
					return i+1;
				}
				else {
					word.nextOffsetCache = word.Next.Offset;
					return word.Next.Offset;
				}
			}
			else
				return cache;
		}


		//index�̈ʒu�̕\���𔽓]������B�ȑO�͐V����GLine��Ԃ��Ă��������͔j��I�ɏ�ԕω�������
		//inverse��false���ƁAGWord�̕����͂��邪Decoration�̔��]�͂��Ȃ��B�q�N�q�N���̑Ώ��Ƃ��Ď����B
		internal void InverseCharacter(int index, bool inverse, Color color) {
			//��Ƀf�[�^�̂���Ƃ������̈ʒu���w�肳�ꂽ��o�b�t�@���L���Ă���
			if(index >= this.DisplayLength) {
				int old_length = this.DisplayLength;
				ExpandBuffer(index+1);
				for(int i = old_length; i<index+1; i++)
					_text[i] = ' ';
				this.LastWord.Next = new GWord(TextDecoration.Default, old_length, CharGroup.Hankaku);
			}
			if(_text[index]==WIDECHAR_PAD) index--;

			GWord prev = null;
			GWord word = _firstWord;
			int nextoffset = 0;
			while(word!=null) {
				nextoffset = WordNextOffset(word);
				if(word.Offset<=index && index<nextoffset) {
					GWord next = word.Next;

					//�L�����b�g�̔��]
					TextDecoration inv_dec = word.Decoration;
					if(inverse)
						inv_dec = inv_dec.GetInvertedCopyForCaret(color);

					//GWord�͍ő�R��(head:index�̑O�Amiddle:index�Atail:index�̎�)�ɕ��������
					GWord head = word.Offset<index? new GWord(word.Decoration, word.Offset, word.CharGroup) : null;
					GWord mid  = new GWord(inv_dec, index, word.CharGroup);
					GWord tail = index+CalcDisplayLength(_text[index]) < nextoffset?
						new GWord(word.Decoration, index+CalcDisplayLength(_text[index]), word.CharGroup) : null;

					//�A�� head,tail��null�̂��Ƃ�����̂ł�₱����
					List<GWord> list = new List<GWord>(3);
					if(head!=null) {
						list.Add(head);
						head.Next = mid;
					}

					list.Add(mid);
					mid.Next = tail==null? next : tail;
					
					if(tail!=null) list.Add(tail);

					//�O��Ƃ̘A��
					if(prev==null)
						_firstWord = list[0];
					else
						prev.Next = list[0];

					list[list.Count-1].Next = next;

					break;
				}

				prev = word;
				word = word.Next;
			}
		}


		internal GLine InverseRange(int from, int to) {
			ExpandBuffer(Math.Max(from+1,to)); //���������T�C�Y�����Ƃ��Ȃǂɂ��̏������������Ȃ����Ƃ�����
			Debug.Assert(from>=0 && from<_text.Length);
			if(from<_text.Length && _text[from]==WIDECHAR_PAD) from--;
			if(0<to && to-1<_text.Length && _text[to-1]==WIDECHAR_PAD) to--;

			GLine ret = new GLine(_text, null);
			ret.ID = _id;
			ret.EOLType = _eolType;
			//�����̔z����Z�b�g
			TextDecoration[] dec = new TextDecoration[_text.Length];
			GWord w = _firstWord;
			while(w!=null) {
				Debug.Assert(w.Decoration!=null);
				dec[w.Offset] = w.Decoration;
				w = w.Next;
			}

			//���]�J�n�_
			TextDecoration original = null;
			for(int i=from; i>=0; i--) {
				if(dec[i]!=null) {
					original = dec[i];
					break;
				}
			}
			Debug.Assert(original!=null);

      TextDecoration inverted=original.GetInvertedCopy();
      dec[from] = inverted;

      //�͈͂ɘi���Ĕ��]���
      for(int i=from+1,iN=to<dec.Length?to:dec.Length; i<iN; i++){
        if(dec[i]==null)continue;

        if(dec[i]!=original){
          original=dec[i];
          inverted=original.GetInvertedCopy();
        }

        dec[i]=inverted;
        //KM:
        // dec[i]==original �̏ꍇ�ł��ACharGroup ���قȂ鎖�� GWord ����������鎖������̂ŁA
        // null �������� (= ��� GWord ���������鎖�ɂȂ�) ���͏o���Ȃ��B
      }

      if(to<_text.Length&&_text[to]==WIDECHAR_PAD)to++;
      if(to<dec.Length&&dec[to]==null) dec[to] = original;
      
			//����ɏ]����GWord�����
			w = null;
			for(int i=dec.Length-1; i>=0; i--) {
				char ch = _text[i];
				if(dec[i]!=null && ch!='\0') {
					int j = i;
					if(ch==WIDECHAR_PAD) j++;
					GWord ww = new GWord(dec[i], j, CalcCharGroup(ch));
					ww.Next = w;
					w = ww;
				}
			}
			ret.Append(w);

			return ret;
		}

    public static int CalcDisplayLength(char ch) {
      if(mwg.RosaTerm.AcsSymbolsDefinition.IsAcsChar(ch))
        return 1;
      else
        return mwg.RosaTerm.UnicodeCharacterSet.EmacsCharWidth(ch);
    }

    //ASCII�����{�ꕶ���� �t�H���g�̑I���Ɏg��
    public static CharGroup CalcCharGroup(char ch) {
      if(mwg.RosaTerm.AcsSymbolsDefinition.IsAcsChar(ch))
        return CharGroup.AcsSymbol;
      else
        return mwg.RosaTerm.UnicodeCharacterSet.EmacsCharWidth(ch)==1?CharGroup.Hankaku:CharGroup.Zenkaku;
      //if(ch < 0x80)
      //  return CharGroup.Hankaku;
      //else if(ch < 0x100)
      //  return mwg.RosaTerm.UnicodeCharacterSet.EmacsCharWidth(ch)==1?CharGroup.Hankaku:CharGroup.Zenkaku;
      //else {
      //  if(0x2500 <= ch && ch <= 0x25FF) //�r���͓��{��t�H���g�͎g��Ȃ�
      //    return mwg.RosaTerm.UnicodeCharacterSet.EmacsCharWidth(ch)==1?CharGroup.Hankaku:CharGroup.Zenkaku; 
      //  else if(mwg.RosaTerm.AcsSymbolsDefinition.IsAcsChar(ch))
      //    return CharGroup.AcsSymbol;
      //  else
      //    return CharGroup.Zenkaku;
      //}
    }

		public static char[] ToCharArray(string src) {
			int len = 0;
			for(int i=0; i<src.Length; i++) len += (src[i] < 0x100)? 1 : 2;
			char[] r = new char[len];
			int c = 0;
			for(int i=0; i<src.Length; i++) {
				r[c++] = src[i];
				if(src[i] >= 0x100) r[c++] = GLine.WIDECHAR_PAD;
			}
			return r;
		}

		public string ToNormalString() {
			StringBuilder bld = new StringBuilder();
			for(int i=0; i<_text.Length; i++) {
				char ch = _text[i];
				if(ch=='\0') break;
				if(ch!=GLine.WIDECHAR_PAD) bld.Append(ch);
			}
			return bld.ToString();
		}
	}

	/// <summary>
	/// <ja>
	/// <seealso cref="GLine">GLine</seealso>�ɑ΂��āA�����̒ǉ��^�폜�Ȃǂ𑀍삵�܂��B
	/// </ja>
	/// <en>
	/// Addition/deletion of the character etc. are operated for <seealso cref="GLine">GLine</seealso>. 
	/// </en>
	/// </summary>
	/// <remarks>
	/// <ja>
	/// ���̃N���X�́A���Ƃ��΃^�[�~�i�����h�L�������g�̓����Gline��u��������ꍇ�ȂǂɎg���܂��B
	/// </ja>
	/// <en>
	/// When the terminal replaces specific Gline of the document for instance, this class uses it. 
	/// </en>
	/// </remarks>
	/// <exclude/>
	public class GLineManipulator {

		private char[] _text;
		private TextDecoration[] _decorations;
		private int _caretColumn;
		private TextDecoration _defaultDecoration;
		private EOLType _eolType;

		/// <summary>
		/// <ja>
		/// �o�b�t�@�T�C�Y�ł��B
		/// </ja>
		/// <en>
		/// Buffer size.
		/// </en>
		/// </summary>
		public int BufferSize {
			get {
				return _text.Length;
			}
		}

		/// <summary>
		/// <ja>
		/// �L�����b�g�ʒu���擾�^�ݒ肵�܂��B
		/// </ja>
		/// <en>
		/// Get / set the position of the caret.
		/// </en>
		/// </summary>
		public int CaretColumn {
			get {
				return _caretColumn;
			}
			set {
				Debug.Assert(value>=0 && value<=_text.Length);
				_caretColumn = value;
				value--;
				while(value>=0 && _text[value]=='\0')
					_text[value--] = ' ';
			}
		}

		/// <summary>
		/// <ja>
		/// �L�����b�W���^�[����}�����܂��B
		/// </ja>
		/// <en>
		/// Insert the carriage return.
		/// </en>
		/// </summary>
		public void CarriageReturn() {
			_caretColumn = 0;
			_eolType = EOLType.CR;
		}

		/// <summary>
		/// <ja>
		/// ���e���󂩂ǂ����������܂��Btrue�ł���΋�Afalse�Ȃ牽�炩�̕����������Ă��܂��B
		/// </ja>
		/// <en>
		/// It is shown whether the content is empty. Return false if here are some characters in it. True retuens if it is empty.
		/// </en>
		/// </summary>
		public bool IsEmpty {
			get {
				//_text��S������K�v�͂Ȃ����낤
				return _caretColumn==0 && _text[0]=='\0';
			}
		}
		/// <summary>
		/// <ja>
		/// �e�L�X�g�̕`������擾�^�ݒ肵�܂��B
		/// </ja>
		/// <en>
		/// Drawing information in the text is get/set. 
		/// </en>
		/// </summary>
		public TextDecoration DefaultDecoration {
			get {
				return _defaultDecoration;
			}
			set {
				_defaultDecoration = value;
			}
		}

		// �S���e��j������
		/// <summary>
		/// <ja>
		/// �ێ����Ă���e�L�X�g���N���A���܂��B
		/// </ja>
		/// <en>
		/// Clear the held text.
		/// </en>
		/// </summary>
		/// <param name="length"><ja>�N���A���钷��</ja><en>Length to clear</en></param>
		public void Clear(int length) {
			if(_text==null || length!=_text.Length) {
				_decorations = new TextDecoration[length];
				_text = new char[length];
			}
			else {
				for(int i=0; i<_decorations.Length; i++) _decorations[i] = null;
				for(int i=0; i<_text.Length;        i++) _text[i] = '\0';
			}
			_caretColumn = 0;
			_eolType = EOLType.Continue;
		}

		/// �����Ɠ������e�ŏ���������Bline�̓��e�͔j�󂳂�Ȃ��B
		/// ������null�̂Ƃ��͈����Ȃ��̃R���X�g���N�^�Ɠ������ʂɂȂ�B
		/// <summary>
		/// <ja>
		/// �����Ɠ������e�ŏ��������܂��B
		/// </ja>
		/// <en>
		/// Initialize same as argument.
		/// </en>
		/// </summary>
		/// <param name="cc">
		/// <ja>
		/// �ݒ肷��L�����b�g�ʒu
		/// </ja>
		/// <en>
		/// The caret position to set.
		/// </en>
		/// </param>
		/// <param name="line">
		/// <ja>�R�s�[���ƂȂ�GLine�I�u�W�F�N�g</ja>
		/// <en>GLine object that becomes copy origin</en>
		/// </param>
		/// <remarks>
		/// <ja>
		/// <paramref name="line"/>��null�̂Ƃ��ɂ́A�����Ȃ��̃R���X�g���N�^�Ɠ������ʂɂȂ�܂��B
		/// </ja>
		/// <en>
		/// The same results with the constructor who doesn't have the argument when <paramref name="line"/> is null. 
		/// </en>
		/// </remarks>
		public void Load(GLine line, int cc) {
			if(line==null) { //���ꂪnull�ɂȂ��Ă���Ƃ����v���Ȃ��N���b�V�����|�[�g���������B�{���͂Ȃ��͂��Ȃ񂾂�...
				Clear(80);
				return;
			}

			Clear(line.Length);
			GWord w = line.FirstWord;
			Debug.Assert(line.Text.Length==_text.Length);
			Array.Copy(line.Text, 0, _text, 0, _text.Length);
			
			int n = 0;
			while(w != null) {
				int nextoffset = line.WordNextOffset(w);
				while(n < nextoffset)
					_decorations[n++] = w.Decoration;
				w = w.Next;
			}

			_eolType = line.EOLType;
			ExpandBuffer(cc+1);
			this.CaretColumn = cc; //' '�Ŗ��߂邱�Ƃ�����̂Ńv���p�e�B�Z�b�g���g��
		}
#if UNITTEST
		public void Load(char[] text, int cc) {
			_text = text;
			_decorations = new TextDecoration[text.Length];
			_eolType = EOLType.Continue;
			_caretColumn = cc;
		}
		public char[] InternalBuffer {
			get {
				return _text;
			}
		}
#endif

		/// <summary>
		/// <ja>
		/// �o�b�t�@���g�����܂��B
		/// </ja>
		/// <en>
		/// Expand the buffer.
		/// </en>
		/// </summary>
		/// <param name="length">Expanded buffer size.</param>
		public void ExpandBuffer(int length) {
			if(length<=_text.Length) return;

			char[] current = _text;
			_text = new char[length];
			Array.Copy(current, 0, _text, 0, current.Length);
			TextDecoration[] current2 = _decorations;
			_decorations = new TextDecoration[length];
			Array.Copy(current2, 0, _decorations, 0, current2.Length);
		}

		/// <summary>
		/// <ja>
		/// �w��ʒu��1�����������݂܂��B
		/// </ja>
		/// <en>
		/// Write one character to specified position.
		/// </en>
		/// </summary>
		/// <param name="ch"><ja>�������ޕ���</ja><en>Character to write.</en></param>
		/// <param name="dec"><ja>�e�L�X�g�������w�肷��TextDecoration�I�u�W�F�N�g</ja>
		/// <en>TextDecoration object that specifies text format
		/// </en></param>
		public void PutChar(char ch, TextDecoration dec) {
			Debug.Assert(dec!=null);
			Debug.Assert(_caretColumn>=0);
			Debug.Assert(_caretColumn<_text.Length);

			//�ȉ��킩��ɂ������A�v�͏ꍇ�����B
			bool onZenkakuRight = (_text[_caretColumn] == GLine.WIDECHAR_PAD);
			bool onZenkaku = onZenkakuRight || (_text.Length>_caretColumn+1 && _text[_caretColumn+1] == GLine.WIDECHAR_PAD);

			if(onZenkaku) {
				//�S�p�̏�ɏ���
				if(!onZenkakuRight) {
					_text[_caretColumn] = ch;
					_decorations[_caretColumn] = dec;
					if(GLine.CalcDisplayLength(ch)==1) {
						//�S�p�̏�ɔ��p���������ꍇ�A�ׂɃX�y�[�X�����Ȃ��ƕ\���������
						if(_caretColumn+1<_text.Length) _text[_caretColumn+1] = ' ';
						_caretColumn++;
					}
					else {
						_decorations[_caretColumn+1] = dec;
						_caretColumn+=2;
					}
				}
				else {
					_text[_caretColumn-1] = ' ';
					_text[_caretColumn]   = ch;
					_decorations[_caretColumn] = dec;
					if(GLine.CalcDisplayLength(ch)==2) {
						if(GLine.CalcDisplayLength(_text[_caretColumn+1])==2)
							if(_caretColumn+2<_text.Length) _text[_caretColumn+2] = ' ';
						_text[_caretColumn+1] = GLine.WIDECHAR_PAD;
						_decorations[_caretColumn+1] = _decorations[_caretColumn];
						_caretColumn += 2;
					}
					else
						_caretColumn++;
				}
			}
			else { //���p�̏�ɏ���
				_text[_caretColumn] = ch;
				_decorations[_caretColumn] = dec;
				if(GLine.CalcDisplayLength(ch)==2) {
					if(GLine.CalcDisplayLength(_text[_caretColumn+1])==2) //���p�A�S�p�ƂȂ��Ă���Ƃ���ɑS�p����������
						if(_caretColumn+2<_text.Length) _text[_caretColumn+2] = ' ';
					_text[_caretColumn+1] = GLine.WIDECHAR_PAD;
					_decorations[_caretColumn+1] = _decorations[_caretColumn];
					_caretColumn += 2;
				}
				else
					_caretColumn++; //���ꂪ�ł�common�ȃP�[�X����
			}
		}

		/// <summary>
		/// <ja>
		/// �e�L�X�g�������w�肷��TextDecoration�I�u�W�F�N�g��ݒ肵�܂��B
		/// </ja>
		/// <en>
		/// Set the TextDecoration object that specifies the text format.
		/// </en>
		/// </summary>
		/// <param name="dec"><ja>�ݒ肷��TextDecoration�I�u�W�F�N�g</ja><en>Set TextDecoration object</en></param>
		public void SetDecoration(TextDecoration dec) {
			if(_caretColumn<_decorations.Length)
				_decorations[_caretColumn] = dec;
		}
		
		/// <summary>
		/// <ja>
		/// �w��ʒu�ɂ��镶����Ԃ��܂��B
		/// </ja>
		/// <en>
		/// Return the character at a specified position.
		/// </en>
		/// </summary>
		/// <param name="index"><ja>�擾�����������ʒu</ja><en>Character position that wants to be got</en></param>
		/// <returns><ja>���̈ʒu�Ɏw�肳��Ă��镶��</ja><en>Character specified for the position</en></returns>
		public char CharAt(int index) {
			return _text[index];
		}

		/// <summary>
		/// <ja>
		/// �L�����b�g���ЂƂ�O�ɖ߂��܂��B
		/// </ja>
		/// <en>
		/// Move the caret to the left of one character. 
		/// </en>
		/// </summary>
		/// <remarks>
		/// <ja>�L�����b�g�����łɍō��[�ɂ���Ƃ��ɂ́A�������܂���B</ja>
		/// <en>Nothing is done when there is already a caret at the high order end. </en>
		/// </remarks>
		public void BackCaret() {
			if(_caretColumn>0) { //�ō��[�ɂ���Ƃ��͉������Ȃ�
				_caretColumn--;
			}
		}

		/// <summary>
		/// <ja>�L�����b�g�̌�ɂ��镶����S�č폜���܂��B</ja>
		/// <en>
		/// Delete all characters after the current caret position.
		/// </en>
		/// </summary>
		public void RemoveAfterCaret(){
			for(int i=_caretColumn; i<_text.Length; i++) {
				_text[i] = '\0';
				_decorations[i] = null;
			}
		}
		/// <summary>
		/// <ja>�w��͈͂��󔒂Ŗ��߂܂��B�s�̖��[�̏ꍇ�́A�P���ɕ������폜���܂��B</ja>
		/// <en>Fill the specified range with spaces,
		/// or just delete characters if the range is adjacent to the end of line.</en>
		/// </summary>
		/// <param name="from"><ja>�͈͂̊J�n�ʒu���w�肵�܂��B</ja><en>Specify the beginning of the range.</en></param>
		/// <param name="to"><ja>�͈̖͂��[���w�肵�܂��B</ja><en>Specify the end of the range (exclusive).</en></param>
		/// <param name="dec"><ja>�w�i�F���w�肷��I�u�W�F�N�g���w�肵�܂��B</ja><en>Specify the object to determine background color.</en></param>
		public void FillBlank(int from,int to,TextDecoration dec){
			if(from<0)from=0;
			if(to>_text.Length)to=_text.Length;

			if(dec!=null){
				dec=dec.RetainBackColor();
				if(dec.IsDefault)dec=null;
			}

			char ch=dec==null&&to==_text.Length?'\0':' ';
			//char ch=to==_text.Length?'\0':' ';
			for(int i=from;i<to;i++){
				_text[i]=ch;
				_decorations[i]=dec;
			}
		}
		/// <summary>
		/// <ja>
		/// �w��͈͂𔼊p�X�y�[�X�Ŗ��߂܂��B
		/// </ja>
		/// <en>
		/// Fill the range of specification with space. 
		/// </en>
		/// </summary>
		/// <param name="from"><ja>���߂�J�n�ʒu�i���̈ʒu���܂݂܂��j</ja><en>Start position(include this position)</en></param>
		/// <param name="to"><ja>���߂�I���ʒu�i���̈ʒu�͊܂݂܂���j</ja><en>End position(exclude this position)</en></param>
		/// <param name="dec"><ja>�e�L�X�g�������w�肷��TextDecoration�I�u�W�F�N�g</ja><en>TextDecoration object that specifies text format
		/// </en></param>
		public void FillSpace(int from, int to, TextDecoration dec) {
			if (to>_text.Length)
				to = _text.Length;
			TextDecoration fillDec = dec;
			if (fillDec != null) {
				fillDec = fillDec.RetainBackColor();
				if (fillDec.IsDefault)
					fillDec = null;
			}
			for(int i=from; i<to; i++) {
				_text[i] = ' ';
				_decorations[i] = fillDec;
			}
		}
		//start����count�������������ċl�߂�B�E�[�ɂ�null������
		/// <summary>
		/// <ja>
		/// �w�肳�ꂽ�ꏊ����w�肳�ꂽ���������폜���A���̌����l�߂܂��B
		/// </ja>
		/// <en>
		/// The number of characters specified from the specified place is deleted, and the furnace is packed afterwards. 
		/// </en>
		/// </summary>
		/// <param name="start"><ja>�폜����J�n�ʒu</ja><en>Start position</en></param>
		/// <param name="count"><ja>�폜���镶����</ja><en>Count to delete</en></param>
		/// <param name="dec"><ja>�����̐V�����󔒗̈�̃e�L�X�g����</ja><en>text decoration for the new empty spaces at the tail of the line</en></param>
		public void DeleteChars(int start, int count, TextDecoration dec) {
			char fillChar;
			TextDecoration fillDec = dec;
			if (fillDec != null) {
				fillDec = fillDec.RetainBackColor();
				if (fillDec.IsDefault) {
					fillDec = null;
					fillChar = '\0';
				} else {
					fillChar = ' ';
				}
			} else {
				fillChar = '\0';
			}

      bool preceding_widechar_pad = true;
			for(int i = start; i<_text.Length; i++) {
				int j = i + count;
				if (j < _text.Length) {
          char src_ch = _text[j];
          TextDecoration src_dec = _decorations[j];
          if (preceding_widechar_pad) {
            if (src_ch == GLine.WIDECHAR_PAD) {
              src_ch = ' ';
              src_dec = fillDec;
            } else {
              preceding_widechar_pad = false;
            }
          }
          _text[i] = src_ch;
					_decorations[i] = src_dec;
				}
				else {
					_text[i] = fillChar;
					_decorations[i] = fillDec;
				}
			}
		}

		/// <summary>
		/// <ja>�w��ʒu�Ɏw�肳�ꂽ�������̔��p�X�y�[�X��}�����܂��B</ja>
		/// <en>The half angle space only of the number specified for a specified position is inserted. </en>
		/// </summary>
		/// <param name="start"><ja>�폜����J�n�ʒu</ja><en>Start position</en></param>
		/// <param name="count"><ja>�}�����锼�p�X�y�[�X�̐�</ja><en>Count space to insert</en></param>
		/// <param name="dec"><ja>�󔒗̈�̃e�L�X�g����</ja><en>text decoration for the new empty spaces</en></param>
		public void InsertBlanks(int start, int count, TextDecoration dec) {
			TextDecoration fillDec = dec;
			if (fillDec != null) {
				fillDec = fillDec.RetainBackColor();
				if (fillDec.IsDefault)
					fillDec = null;
			}
			for(int i=_text.Length-1; i>=_caretColumn; i--) {
				int j = i - count;
				if(j >= _caretColumn) {
					_text[i] = _text[j];
					_decorations[i] = _decorations[j];
				}
				else {
					_text[i] = ' ';
					_decorations[i] = fillDec;
				}
			}
		}

		/// <summary>
		/// <ja>
		/// �f�[�^���G�N�X�|�[�g���܂��B
		/// </ja>
		/// <en>
		/// Export the data.
		/// </en>
		/// </summary>
		/// <returns><ja>�G�N�X�|�[�g���ꂽGLine�I�u�W�F�N�g</ja><en>Exported GLine object</en></returns>
		public GLine Export() {
			GWord w = new GWord(_decorations[0] == null ? TextDecoration.Default : _decorations[0], 0, GLine.CalcCharGroup(_text[0]));

			GLine line = new GLine(_text, w);
			line.EOLType = _eolType;
			int m = _text.Length;
			for(int offset=1; offset<m; offset++) {
				char ch = _text[offset];
				if(ch=='\0') break;
				else if(ch==GLine.WIDECHAR_PAD) continue;

				TextDecoration dec = _decorations[offset];
				if(_decorations[offset-1]!=dec || w.CharGroup!=GLine.CalcCharGroup(ch)) {
					if(dec==null) dec = TextDecoration.Default;
					GWord ww = new GWord(dec, offset, GLine.CalcCharGroup(ch));
					w.Next = ww;
					w = ww;
				}
			}
			return line;
		}

		/// <summary>
		/// <ja>
		/// ������Ƃ��Ď擾���܂��B
		/// </ja>
		/// <en>
		/// Get as character string
		/// </en>
		/// </summary>
		/// <returns><ja>���̃I�u�W�F�N�g���ێ����Ă���e�L�X�g������</ja><en>Character string that this object holds.</en></returns>
		public override string ToString() {
			StringBuilder b = new StringBuilder();
			b.Append(_text);
			//�A�g���r���[�g�܂��̕\���͂܂����Ă��Ȃ�
			return b.ToString();
		}
	}

#if UNITTEST
	[TestFixture]
	public class GLineManipulatorTests {

		[Test]
		public void PutChar1() {
			Assert.AreEqual("��aaz", TestPutChar("aaaaz", 0, '��'));
		}
		[Test]
		public void PutChar2() {
			Assert.AreEqual("�� az", TestPutChar("a��az", 0, '��'));
		}
		[Test]
		public void PutChar3() {
			Assert.AreEqual("b ��z", TestPutChar("����z", 0, 'b'));
		}
		[Test]
		public void PutChar4() {
			Assert.AreEqual("����z", TestPutChar("����z", 0, '��'));
		}
		[Test]
		public void PutChar5() {
			Assert.AreEqual(" b��z", TestPutChar("����z", 1, 'b'));
		}
		[Test]
		public void PutChar6() {
			Assert.AreEqual(" ��az", TestPutChar("��aaz", 1, '��'));
		}
		[Test]
		public void PutChar7() {
			Assert.AreEqual(" �� z", TestPutChar("����z", 1, '��'));
		}

		private static string TestPutChar(string initial, int col, char ch) {
			GLineManipulator m = new GLineManipulator();
			m.Load(GLine.ToCharArray(initial), col);
			//Debug.WriteLine(String.Format("Test{0}  [{1}] col={2} char={3}", num, SafeString(m._text), m.CaretColumn, ch));
			m.PutChar(ch, TextDecoration.ClonedDefault());
			//Debug.WriteLine(String.Format("Result [{0}] col={1}", SafeString(m._text), m.CaretColumn));
			return SafeString(m.InternalBuffer);
		}
	}
#endif

	/// <summary>
	/// <ja>
	/// ���s�R�[�h�̎�ނ������܂��B
	/// </ja>
	/// <en>
	/// Kind of Line feed code
	/// </en>
	/// </summary>
	public enum EOLType {
		/// <summary>
		/// <ja>���s�����Ɍp�����܂��B</ja><en>It continues without changing line.</en>
		/// </summary>
		Continue,
		/// <summary>
		/// <ja>CRLF�ŉ��s���܂��B</ja><en>It changes line with CRLF. </en>
		/// </summary>
		CRLF,
		/// <summary>
		/// <ja>CR�ŉ��s���܂��B</ja><en>It changes line with CR. </en>
		/// </summary>
		CR,
		/// <summary>
		/// <ja>LF�ŉ��s���܂��B</ja><en>It changes line with LF. </en>
		/// </summary>
		LF
	}

	/// <summary>
	/// <ja>�����̎�ނ������܂��B</ja><en>Kind of character</en>
	/// </summary>
	public enum CharGroup {
		/// <summary>
		/// <ja>���p�����BUnicode��0x100�����̕����ł��B</ja><en>One-byte character. It is Unicode and a character of less than 0x100.</en>
		/// </summary>
		Hankaku, //unicode��0x100�����̕���
		/// <summary>
		/// <ja>�S�p�����BUnicode��0x100�ȏ�̕����ł��B</ja><en>Two-byte character. It is Unicode and a character of 0x100 or more. </en>
		/// </summary>
		Zenkaku, //0x100�ȏ�̕���
		/// <summary>
		/// <ja>Alternate Character Set �̕����ł��B</ja>
		/// <en>Character in the alternate character set.</en>
		/// </summary>
		AcsSymbol, // �ڍׂ� mwg.RosaTerm.AcsSymbols �Œ�`
	}

	//�P���؂�ݒ�B�܂�Preference�ɂ���Ԃł��Ȃ����낤
	/// <exclude/>
	public class ASCIIWordBreakTable {
		public const int LETTER = 0;
		public const int SYMBOL = 1;
		public const int SPACE = 2;
		public const int NOT_ASCII = 3;

		private int[] _data;

		public ASCIIWordBreakTable() {
			_data = new int[0x80];
			Reset();
		}

		public void Reset() { //�ʏ�ݒ�ɂ���
			//���䕶���p�[�g
			for(int i=0; i<=0x20; i++) _data[i] = SPACE;
			_data[0x7F] = SPACE; //DEL

			//�ʏ핶���p�[�g
			for(int i=0x21; i<=0x7E; i++) {
				char c = (char)i;
				if(('0'<=c && c<='9') || ('a'<=c && c<='z') || ('A'<=c && c<='Z') || c=='_')
					_data[i] = LETTER;
				else
					_data[i] = SYMBOL;
			}
		}

		public int GetAt(char ch) {
			Debug.Assert(ch < 0x80);
			return _data[(int)ch];
		}

		//�ꕶ���ݒ�
		public void Set(char ch, int type) {
			Debug.Assert(ch < 0x80);
			_data[(int)ch] = type;
		}

		//�C���X�^���X
		private static ASCIIWordBreakTable _instance;

		public static ASCIIWordBreakTable Default {
			get {
				if(_instance==null) _instance = new ASCIIWordBreakTable();
				return _instance;
			}
		}
	}

}
