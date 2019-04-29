/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TerminalUtil.cs,v 1.1 2010/11/19 15:41:11 kzmi Exp $
 */
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

using Poderosa.ConnectionParam;
using Poderosa.Util;

namespace Poderosa.Terminal {

	/// <summary>
	/// 
	/// </summary>
	/// <exclude/>
	public enum TerminalMode { Normal, Application }


	/// <summary>
	/// 
	/// </summary>
	/// <exclude/>
	public class TerminalUtil {
		public static char[] NewLineChars(NewLine nl) {
			switch(nl) {
				case NewLine.CR:
					return new char[1] { '\r' };
				case NewLine.LF:
					return new char[1] { '\n' };
				case NewLine.CRLF:
					return new char[2] { '\r','\n' };
				default:
					throw new ArgumentException("Unknown NewLine "+nl);
			}
		}
		//TODO staticにしたほうがいい？ うっかり破壊が怖いが
		public static byte[] NewLineBytes(NewLine nl) {
			switch(nl) {
				case NewLine.CR:
					return new byte[1] { (byte)'\r' };
				case NewLine.LF:
					return new byte[1] { (byte)'\n' };
				case NewLine.CRLF:
					return new byte[2] { (byte)'\r', (byte)'\n' };
				default:
					throw new ArgumentException("Unknown NewLine "+nl);
			}
		}
		public static NewLine NextNewLineOption(NewLine nl) {
			switch(nl) {
				case NewLine.CR:
					return NewLine.LF;
				case NewLine.LF:
					return NewLine.CRLF;
				case NewLine.CRLF:
					return NewLine.CR;
				default:
					throw new ArgumentException("Unknown NewLine "+nl);
			}
		}


		//有効なボーレートのリスト
		public static string[] BaudRates {
			get {
				return new string[] {"110", "300", "600", "1200", "2400", "4800",
										"9600", "14400", "19200", "38400", "57600", "115200"};
			}
		}

		//秘密鍵ファイル選択
		public static string SelectPrivateKeyFileByDialog(Form parent) {
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.CheckFileExists = true;
			dlg.Multiselect = false;
			dlg.Title = "Select Private Key File";
			dlg.Filter = "Key Files(*.bin;*)|*.bin;*";
			if(dlg.ShowDialog(parent)==DialogResult.OK) {
				return dlg.FileName;
			}
			else
				return null;
		}

	}


	//これと同等の処理はToAscii APIを使ってもできるが、ちょっとやりづらいので逆引きマップをstaticに持っておく
	internal static class KeyboardInfo {
		static readonly char[] _defaultGroup= new char[256];
		static readonly char[] _shiftGroup= new char[256];
		static KeyboardInfo(){
			Init();
		}
		static void Init() {
			for(int i=32; i<128; i++) {
				short v = Win32.VkKeyScan((char)i);
				bool shift = (v & 0x0100)!=0;
				short body = (short)(v & 0x00FF);
				if(shift)
					_shiftGroup[body]  =(char)i;
				else
					_defaultGroup[body]=(char)i;
			}

			_defaultGroup[(char)Keys.OemBackslash]=(char)'\\'; // for Alt+\
			_shiftGroup[(char)Keys.OemBackslash]=(char)'_'; // for Alt+_

			_defaultGroup[(char)Keys.Divide]=(char)'/';   // KP/
			_defaultGroup[(char)Keys.Multiply]=(char)'*'; // KP*
			_defaultGroup[(char)Keys.Subtract]=(char)'-'; // KP-
			_defaultGroup[(char)Keys.Add]=(char)'+';      // KP+
			_defaultGroup[(char)Keys.Decimal]=(char)'.';  // KP.
			_shiftGroup[(char)Keys.Divide]=(char)'/';   // KP/
			_shiftGroup[(char)Keys.Multiply]=(char)'*'; // KP*
			_shiftGroup[(char)Keys.Subtract]=(char)'-'; // KP-
			_shiftGroup[(char)Keys.Add]=(char)'+';      // KP+
			_shiftGroup[(char)Keys.Decimal]=(char)'.';  // KP.

			_defaultGroup[(char)Keys.Tab]=(char)'\t';
			_defaultGroup[(char)Keys.Escape]=(char)'\x1b';
			_defaultGroup[(char)Keys.Enter]=(char)'\r';    // TODO: option 参照
			_defaultGroup[(char)Keys.LineFeed]=(char)'\n'; // TODO: option 参照
			//_defaultGroup[(char)Keys.Back]=(char)'\x08';   // TODO: option 参照
			//_defaultGroup[(char)Keys.Delete]=(char)'\x7F'; // TODO: option 参照 ← C-del 等を破壊
		}

		//public static char Scan(Keys body, bool shift) {
		//  //制御文字のうち単品のキーで送信できるもの
		//  if(body==Keys.Escape)
		//    return (char)0x1B;
		//  else if(body==Keys.Tab)
		//    return (char)0x09;
		//  else if(body==Keys.Back)
		//    return (char)0x08;
		//  else if(body==Keys.Delete)
		//    return (char)0x7F;
		//  if(shift)
		//    return (char)_shiftGroup[(int)body];
		//  else
		//    return (char)_defaultGroup[(int)body];
		//}

		/// <summary>
		/// <ja>記号・英数字、Keys.Back を文字に変換します。</ja>
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static byte ConvertToChar(Keys key){
			int k=(int)(key&Keys.KeyCode);
			if(k>0xFF)return (byte)0;
      
			if(0!=(key&Keys.Shift))
				return (byte)_shiftGroup[k];
			else
				return (byte)_defaultGroup[k];
		}

		public static bool IsKeyPressed(System.Windows.Forms.Keys key){
			return (Win32.GetKeyState((int)key)&0x8000)!=0;
		}
		public static Keys GetModifierState(){
			Keys flags=0;
			if(IsKeyPressed(Keys.ShiftKey))flags|=Keys.Shift;
			if(IsKeyPressed(Keys.Menu))flags|=Keys.Alt;
			if(IsKeyPressed(Keys.ControlKey))flags|=Keys.Control;
			return flags;
		}
	}
}
