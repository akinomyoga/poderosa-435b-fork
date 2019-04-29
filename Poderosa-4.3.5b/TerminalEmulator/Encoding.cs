/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: Encoding.cs,v 1.3 2010/11/24 16:04:10 kzmi Exp $
 */
using System;
using System.Text;

using Poderosa.ConnectionParam;
using Poderosa.Util;

namespace Poderosa.Terminal
{
	//encoding�֌W
	internal abstract class EncodingProfile {
		
		private Encoding _encoding;
		private EncodingType _type;
		private byte[] _buffer;
		private int _cursor;
		private int _byte_len;
		private char[] _tempOneCharArray;

		protected EncodingProfile(EncodingType t, Encoding enc) {
			_type = t;
			_encoding = enc;
			_buffer = new byte[3]; //���͂P�����͍ő�R�o�C�g
			_cursor = 0;
			_tempOneCharArray = new char[1]; //API�̓s���Œ����P��char[]���K�v�ȂƂ��g��
		}

		public abstract bool IsLeadByte(byte b);

		//�擪�o�C�g����A���������o�C�g�ō\������Ă��邩��Ԃ�
		public abstract int  GetCharLength(byte b);

		//UTF��BOM�ȂǁA�f�R�[�h�̌��ʏo�Ă��Ă��������ׂ��������𔻒�
		public abstract bool IsIgnoreableChar(char c);

		public Encoding Encoding {
			get {
				return _encoding;
			}
		}
		public EncodingType Type {
			get {
				return _type;
			}
		}
		internal byte[] Buffer {
			get {
				return _buffer;
			}
		}

		internal byte[] GetBytes(char[] chars) {
			return _encoding.GetBytes(chars);
		}
        //NOTE ���ݓI�ɂ�_tempOneCharArray�̎g�p�Ń}���`�X���b�h�ł̊댯������B
		internal byte[] GetBytes(char ch) {
			_tempOneCharArray[0] = ch;
			return _encoding.GetBytes(_tempOneCharArray);
		}

		//==========================================================================
		// mwg �ǉ�
		/// <summary>
		/// <ja>�w�肵����������w�肵���o�C�g�z��� Encode ���܂��B</ja>
		/// <en>Encodes a character and store the result to a byte array.</en>
		/// </summary>
		/// <param name="ch">Encode ���镶�����w�肵�܂��B</param>
		/// <param name="buff">Encode ���ʂ��i�[����z����w�肵�܂��B</param>
		/// <returns>Encode ���ʂ̃o�C�g����Ԃ��܂��B</returns>
		internal int GetBytes(char ch,byte[] buff){
			return _encoding.GetBytes(_tempOneCharArray,0,1,buff,0);
		}
		//==========================================================================

		internal bool IsInterestingByte(byte b) {
            //"b>=33"�̂Ƃ���͂���������Ƃ܂��߂ɔ��肷��ׂ��B
            //�����̊ԂɃG�X�P�[�v�V�[�P���X������P�[�X�ւ̑Ή��B
			return _cursor==0? IsLeadByte(b) : b>=33; 
		}

        /* REMOVE
		internal int Decode(byte[] data, char[] result) {
			return _encoding.GetChars(data, 0, data.Length, result, 0);
		}
         */

		internal void Reset() {
			_cursor = 0;
			_byte_len = 0;
		}

		//�P�o�C�g��ǉ�����B��������������΃f�R�[�h���Ă��̕�����Ԃ��B�܂������̃o�C�g���K�v�Ȃ�\0��Ԃ�
		internal char PutByte(byte b) {
			if(_cursor==0)
				_byte_len = GetCharLength(b);
			_buffer[_cursor++] = b;
			if(_cursor==_byte_len) {
				_encoding.GetChars(_buffer, 0, _byte_len, _tempOneCharArray, 0);
				_cursor = 0;
                if(IsIgnoreableChar(_tempOneCharArray[0]))
                    return '\0';
                else
    				return _tempOneCharArray[0];
			}
			return '\0';
		}

		public static EncodingProfile Get(EncodingType et) {
			EncodingProfile p = null;
			switch(et) {
				case EncodingType.ISO8859_1:
					p = new ISO8859_1Profile();
					break;
				case EncodingType.EUC_JP:
					p = new EUCJPProfile();
					break;
				case EncodingType.SHIFT_JIS:
					p = new ShiftJISProfile();
					break;
				case EncodingType.UTF8:
					p = new UTF8Profile();
					break;
				case EncodingType.GB2312:
					p = new GB2312Profile();
					break;
				case EncodingType.BIG5:
					p = new Big5Profile();
					break;
				case EncodingType.EUC_CN:
					p = new EUCCNProfile();
					break;
				case EncodingType.EUC_KR:
					p = new EUCKRProfile();
					break;
			}
			return p;
		}

        //NOTE �����̓��\�b�h��override�łȂ�delegate�ł܂킵���ق��������͎኱�悢�̂���
		class ISO8859_1Profile : EncodingProfile {
			public ISO8859_1Profile() : base(EncodingType.ISO8859_1, Encoding.GetEncoding("iso-8859-1")) {
			}
			public override int GetCharLength(byte b) {
				return 1;
			}
			public override bool IsLeadByte(byte b) {
				return b>=0xA0 && b<=0xFF;
			}
            public override bool IsIgnoreableChar(char c) {
                return false;
            }
		}
		class ShiftJISProfile : EncodingProfile {
			public ShiftJISProfile() : base(EncodingType.SHIFT_JIS, Encoding.GetEncoding("shift_jis")) {
			}
			public override int GetCharLength(byte b) {
				return (b>=0xA1 && b<=0xDF)? 1 : 2;
			}
			public override bool IsLeadByte(byte b) {
				return b>=0x81 && b<=0xFC;
			}
            public override bool IsIgnoreableChar(char c) {
                return false;
            }
        }
		class EUCJPProfile : EncodingProfile {
			public EUCJPProfile() : base(EncodingType.EUC_JP, Encoding.GetEncoding("euc-jp")) {
			}
			public override int GetCharLength(byte b) {
				return b==0x8F? 3 : b>=0x8E? 2 : 1;
			}
			public override bool IsLeadByte(byte b) {
				return b>=0x8E && b<=0xFE;
			}
            public override bool IsIgnoreableChar(char c) {
                return false;
            }
        }
		class UTF8Profile : EncodingProfile {
			public UTF8Profile() : base(EncodingType.UTF8, Encoding.UTF8) {
			}
			public override int GetCharLength(byte b) {
				return b>=0xE0? 3 : b>=0x80? 2 : 1;
			}
			public override bool IsLeadByte(byte b) {
				return b>=0x80;
			}
            public override bool IsIgnoreableChar(char c) {
                return c=='\uFFFE' || c=='\uFEFF';
            }
        }
		private class GB2312Profile : EncodingProfile {
			public GB2312Profile()
				: base(EncodingType.GB2312, Encoding.GetEncoding("gb2312")) {
			}
			public override int GetCharLength(byte b) {
				return 2;
			}
			public override bool IsLeadByte(byte b) {
				return b >= 0xA1 && b <= 0xF7;
			}
			public override bool IsIgnoreableChar(char c) {
				return false;
			}
		}
		private class Big5Profile : EncodingProfile {
			public Big5Profile()
				: base(EncodingType.BIG5, Encoding.GetEncoding("big5")) {
			}
			public override int GetCharLength(byte b) {
				return 2;
			}
			public override bool IsLeadByte(byte b) {
				return b >= 0x81 && b <= 0xFE;
			}
			public override bool IsIgnoreableChar(char c) {
				return false;
			}
		}
		private class EUCCNProfile : EncodingProfile {
			public EUCCNProfile()
				: base(EncodingType.EUC_CN, Encoding.GetEncoding("euc-cn")) {
			}
			public override int GetCharLength(byte b) {
				return 2;
			}
			public override bool IsLeadByte(byte b) {
				return b >= 0xA1 && b <= 0xF7;
			}
			public override bool IsIgnoreableChar(char c) {
				return false;
			}
		}
		private class EUCKRProfile : EncodingProfile {
			public EUCKRProfile()
				: base(EncodingType.EUC_KR, Encoding.GetEncoding("euc-kr")) {
			}
			public override int GetCharLength(byte b) {
				return 2;
			}
			public override bool IsLeadByte(byte b) {
				return b >= 0xA1 && b <= 0xFE;
			}
			public override bool IsIgnoreableChar(char c) {
				return false;
			}
		}
	}
}
