/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TextDecoration.cs,v 1.3 2011/04/02 04:39:35 kzmi Exp $
 */
using System;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Diagnostics;
using System.Text;

//using Poderosa.Util;

namespace Poderosa.Document
{
	//TextDecorationで色を指定するのか、外部で定義された色を使うのかの区別につかう。ColorのAプロパティの値で代用すればちょっと効率は上がりそうだが...
	/// <exclude/>
	public enum ColorType {
		DefaultBack,
		DefaultText,
		Custom,

		ColorBlack=0x8,
		ColorR    =0x9,
		ColorG    =0xA,
		ColorY    =0xB,
		ColorB    =0xC,
		ColorM    =0xD,
		ColorC    =0xE,
		ColorWhite=0xF,
	}

	//テキストの描画情報.
	//標準背景色を使うときは_bgColorがColor.Empty, 標準テキスト色を使うときは_textColorがColor.Emptyになることに注意
	/// <summary>
	/// Text decoration.
	/// </summary>
	/// <remarks>
	/// The instance is immutable.
	/// </remarks>
	/// <exclude/>
	[System.Serializable]
	public sealed class TextDecoration {
		private readonly ColorType _bgColorType;
		private readonly Color _bgColor;
		private readonly ColorType _textColorType;
		private readonly Color _textColor;
		private readonly TextDecorationStyle _style;

		private static readonly TextDecoration _default =
			new TextDecoration(ColorType.DefaultBack, Color.Empty, ColorType.DefaultText, Color.Empty, false, false);

		/// <summary>
		/// Get a default decoration.
		/// "default decoration" means that text is displayed
		/// with default text color, default background color,
		/// no underline, and no bold.
		/// </summary>
		public static TextDecoration Default {
			get {
				return _default;
			}
		}

		private TextDecoration(ColorType ctbg, Color bg, ColorType cttxt, Color txt, bool underline, bool bold) {
			_bgColor = bg;
			_bgColorType = ctbg;
			_textColor = txt;
			_textColorType = cttxt;

			_style=0;
			if(underline)
				_style|=TextDecorationStyle.Underline;
			if(bold)
				_style|=TextDecorationStyle.Bold;
		}

		public Color TextColor {
			get {return _textColor;}
		}
		public Color BackColor {
			get {return _bgColor;}
		}
		public ColorType TextColorType {
			get {return _textColorType;}
		}
		public ColorType BackColorType {
			get {return _bgColorType;}
		}
		/// <summary>
		/// 字体が太字であるか否かを取得・設定します。
		/// </summary>
		public bool Bold {
			get {return (_style&TextDecorationStyle.Bold)!=0;}
		}
		/// <summary>
		/// 下線を引くかどうかを取得・設定します。
		/// </summary>
		public bool Underline {
			get {return (_style&TextDecorationStyle.Underline)!=0;}
		}

		public bool IsDefault {
			get {
				return _style==0 && _bgColorType==ColorType.DefaultBack && _textColorType==ColorType.DefaultText;
			}
		}

		#region RosaExtension
		public TextDecorationStyle TextStyle{
			get{return this._style;}
		}
		public bool Invisible{
			get{return 0!=(_style&TextDecorationStyle.Invisible);}
		}
		public bool Inverted{
			get{return 0!=(_style&TextDecorationStyle.Inverted);}
		}
		public bool BrightBack{
			get{return 0!=(_style&TextDecorationStyle.BrightBack);}
		}
		public bool BrightText{
			get{return 0!=(_style&TextDecorationStyle.BrightText);}
		}
		//--------------------------------------------------------------------------
		/// <summary>
		/// <en>Retrieves if the throughline is enabled or not.</en>
		/// <ja>打ち消し線を引くかどうかを取得します。</ja>
		/// </summary>
		public bool Throughline{
			get{return 0!=(_style&TextDecorationStyle.Throughline);}
		}
		/// <summary>
		/// イタリック書体で描画するかどうかを取得・設定します。
		/// </summary>
		public bool Italic{
			get{return 0!=(_style&TextDecorationStyle.Italic);}
		}
		/// <summary>
		/// 上線を引くかどうかを取得・設定します。
		/// </summary>
		public bool Overline{
			get{return 0!=(_style&TextDecorationStyle.Overline);}
		}
		/// <summary>
		/// 二重下線を引くかどうかを取得・設定します。
		/// </summary>
		public bool Doubleline{
			get{return 0!=(_style&TextDecorationStyle.Doubleline);}
		}
		//--------------------------------------------------------------------------
		/// <summary>
		/// 文字スタイルが線の類を持っているかどうかを取得します。
		/// </summary>
		public bool HasLine{
			get{
				const TextDecorationStyle STYLE_LINE=TextDecorationStyle.Doubleline|TextDecorationStyle.Overline|TextDecorationStyle.Throughline|TextDecorationStyle.Underline;
				return 0!=(_style&STYLE_LINE);
			}
		}
		//--------------------------------------------------------------------------
		internal TextDecoration(ColorType ctbg, Color bg, ColorType cttxt, Color txt,TextDecorationStyle style) {
			_bgColor = bg;
			_bgColorType = ctbg;
			_textColor = txt;
			_textColorType = cttxt;
			_style=style;
		}
		#endregion

		#region ForPoderosa_435b_VT100
		/// <summary>
		/// Get a new copy whose text color and background color were swapped.
		/// </summary>
		/// <returns>new instance</returns>
		public TextDecoration GetInvertedCopy() {
			//return new TextDecoration(_bgColorType,_bgColor,_textColorType,_textColor,_style^TextDecorationStyle.Inverted);
			return new TextDecoration(_textColorType,_textColor,_bgColorType,_bgColor,_style);
		}

		/// <summary>
		/// Get a new copy whose text color and background color were swapped.
		/// If a specified bgColor was not empty, it is used as a background color.
		/// </summary>
		/// <returns>new instance</returns>
		public TextDecoration GetInvertedCopyForCaret(Color bgColor) {
			ColorType newBgColorType;
			Color newBgColor;
			if(bgColor.IsEmpty){
				newBgColorType = _textColorType;
				newBgColor = _textColor;
			}else{
				newBgColorType = ColorType.Custom;
				newBgColor = bgColor;
			}

			return new TextDecoration(newBgColorType, newBgColor, _bgColorType, _bgColor, this._style);
		}

		/// <summary>
		/// Get a new copy whose text color was set to the default text color.
		/// </summary>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithDefaultTextColor() {
			return GetCopyWithTextColor(Color.Empty);
		}

		/// <summary>
		/// Get a new copy whose text color was set.
		/// </summary>
		/// <param name="textColor">new text color</param>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithTextColor(Color textColor) {
			ColorType textColorType = textColor.IsEmpty ? ColorType.DefaultText : ColorType.Custom;
			return new TextDecoration(_bgColorType, _bgColor, textColorType, textColor, this.Underline, this.Bold);
		}

		/// <summary>
		/// Get a new copy whose background color was set to the default backgeound color.
		/// </summary>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithDefaultBackColor() {
			return GetCopyWithBackColor(Color.Empty);
		}

		/// <summary>
		/// Get a new copy whose background color was set.
		/// </summary>
		/// <param name="bgColor">new background color</param>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithBackColor(Color bgColor) {
			ColorType bgColorType = bgColor.IsEmpty ? ColorType.DefaultBack : ColorType.Custom;
			return new TextDecoration(bgColorType, bgColor, _textColorType, _textColor, this.Underline, this.Bold);
		}

		/// <summary>
		/// Get a new copy whose underline status was set.
		/// </summary>
		/// <param name="underline">new underline status</param>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithUnderline(bool underline) {
			return new TextDecoration(_bgColorType, _bgColor, _textColorType, _textColor, underline, this.Bold);
		}

		/// <summary>
		/// Get a new copy whose bold status was set.
		/// </summary>
		/// <param name="bold">new bold status</param>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithBold(bool bold) {
			return new TextDecoration(_bgColorType, _bgColor, _textColorType, _textColor, this.Underline, bold);
		}

		/// <summary>
		/// Get a new instance whose attributes except BackColor were reset to the default.
		/// </summary>
		/// <returns></returns>
		public TextDecoration RetainBackColor() {
			return new TextDecoration(
				_bgColorType, _bgColor,
				_default._textColorType, _default._textColor, _default.Underline, _default.Bold);
		}
		#endregion

		public override string ToString() {
			StringBuilder b = new StringBuilder();
			b.Append(_bgColor.ToString()); //これでまっとうな文字列が出るのか?
			b.Append('/');
			b.Append(_textColor.ToString());
			b.Append('/');
			if(this.Bold) b.Append('B');
			return b.ToString();
		}

	}

}
