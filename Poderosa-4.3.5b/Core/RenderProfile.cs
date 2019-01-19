/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: RenderProfile.cs,v 1.5 2011/04/04 13:08:26 kzmi Exp $
 */
//#define KM20121125_UseExtTextOut_impl3

using System;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Forms;

#if !MACRODOC
using Poderosa.Util;
using Poderosa.Document;
#endif

using Gdi=System.Drawing;

namespace Poderosa.View{
	
	/// <summary>
	/// <ja>背景画像の位置を指定します。</ja>
	/// <en>Specifies the position of the background image.</en>
	/// </summary>
	[EnumDesc(typeof(ImageStyle))]
	[System.Serializable]
	public enum ImageStyle {
		/// <summary>
		/// <ja>中央</ja>
		/// <en>Center</en>
		/// </summary>
		[EnumValue(Description="Enum.ImageStyle.Center")]
		Center,
		/// <summary>
		/// <ja>左上</ja>
		/// <en>Upper left corner</en>
		/// </summary>
		[EnumValue(Description="Enum.ImageStyle.TopLeft")]
		TopLeft,
		/// <summary>
		/// <ja>右上</ja>
		/// <en>Upper right corner</en>
		/// </summary>
		[EnumValue(Description="Enum.ImageStyle.TopRight")]
		TopRight,
		/// <summary>
		/// <ja>左下</ja>
		/// <en>Lower left corner</en>
		/// </summary>
		[EnumValue(Description="Enum.ImageStyle.BottomLeft")]
		BottomLeft,
		/// <summary>
		/// <ja>右下</ja>
		/// <en>Lower right corner</en>
		/// </summary>
		[EnumValue(Description="Enum.ImageStyle.BottomRight")]
		BottomRight,

		// 以降、DrawBackground_Scaled で処理 (順番が大事)
		/// <summary>
		/// <ja>伸縮して全体に表示</ja>
		/// <en>The image covers the whole area of the console by expansion</en>
		/// </summary>
		[EnumValue(Description="Enum.ImageStyle.Scaled")]
		Scaled,
		[EnumValue(Description="Enum.ImageStyle.HorizontalFit")]
		HorizontalFit,
		[EnumValue(Description="Enum.ImageStyle.VerticalFit")]
		VerticalFit,
		[EnumValue(Description="Enum.ImageStyle.MinimalFit")]
		MinimalFit,
		[EnumValue(Description="Enum.ImageStyle.MaximalFit")]
		MaximalFit,
	}
	namespace Utils{
		public static partial class ViewUtils{
			public static bool IsScaled(this ImageStyle self){
				return (int)ImageStyle.Scaled<=(int)self;
			}
		}
	}

	internal class FontHandle {
		private Font _font;
		private IntPtr _hFont;

		public FontHandle(Font f) {
			_font = f;
		}
		public Font Font {
			get {
				return _font;
			}
		}
		public IntPtr HFONT {
			get {
				if(_hFont==IntPtr.Zero) _hFont = _font.ToHfont();
				return _hFont;
			}
		}
		public void Dispose() {
			_hFont = IntPtr.Zero;
			_font.Dispose();
		}
	}

	/// <summary>
	/// <ja>コンソールの表示方法を指定するオブジェクトです。接続前にTerminalParamのRenderProfileプロパティにセットすることで、マクロから色・フォント・背景画像を指定できます。</ja>
	/// <en>Implements the parameters for displaying the console. By setting this object to the RenderProfile property of the TerminalParam object, the macro can control colors, fonts, and background images.</en>
	/// </summary>
	public class RenderProfile : ICloneable, System.IDisposable {
		private string _fontName;
		private string _cjkFontName;
		private float _fontSize;
		private bool _useClearType;
		private bool _enableBoldStyle;
		private bool _forceBoldStyle;
		private FontHandle _font;
		private FontHandle _boldfont;
		private FontHandle _underlinefont;
		private FontHandle _boldunderlinefont;
		private FontHandle _cjkFont;
		private FontHandle _cjkBoldfont;
		private FontHandle _cjkUnderlinefont;
		private FontHandle _cjkBoldUnderlinefont;
#if !MACRODOC
		private EscapesequenceColorSet _esColorSet;
#endif
		private Color _forecolor;
		private Color _bgcolor;

		private Brush _brush;
		private Brush _bgbrush;

		private string _backgroundImageFileName;
		private Image _backgroundImage;
		private bool  _imageLoadIsAttempted;
		private ImageStyle _imageStyle;

		private SizeF _pitch;
#if KM20121125_UseExtTextOut_impl3
    internal const int PITCH_DX_LEN=80;
    internal readonly int[] pitch_deltaXArray=new int[PITCH_DX_LEN];
#endif

		private int _lineSpacing;
		private float _chargap; //文字列を表示するときに左右につく余白
		private bool _usingIdenticalFont; //ASCII/CJKで同じフォントを使っているかどうか

		/// <summary>
		/// <ja>通常の文字を表示するためのフォント名です。</ja>
		/// <en>Gets or sets the font name for normal characters.</en>
		/// </summary>
		public string FontName {
			get {
				return _fontName;
			}
			set {
				_fontName = value;
				ClearFont();
			}
		}
		/// <summary>
		/// <ja>CJK文字を表示するためのフォント名です。</ja>
		/// <en>Gets or sets the font name for CJK characters.</en>
		/// </summary>
		public string CJKFontName {
			get {
				return _cjkFontName;
			}
			set {
				_cjkFontName = value;
				ClearFont();
			}
		}
		/// <summary>
		/// <ja>フォントサイズです。</ja>
		/// <en>Gets or sets the font size.</en>
		/// </summary>
		public float FontSize {
			get {
				return _fontSize;
			}
			set {
				_fontSize = value;
				ClearFont();
			}
		}
		/// <summary>
		/// <ja>trueにセットすると、フォントとOSでサポートされていれば、ClearTypeを使用して文字が描画されます。</ja>
		/// <en>If this property is true, the characters are drew by the ClearType when the font and the OS supports it.</en>
		/// </summary>
		public bool UseClearType {
			get {
				return _useClearType;
			}
			set {
				_useClearType = value;
			}
		}

		/// <summary>
		/// <ja>falseにするとエスケープシーケンスでボールドフォントが指定されていても通常フォントで描画します</ja>
		/// <en>If this property is false, bold fonts are replaced by normal fonts even if the escape sequence indicates bold.</en>
		/// </summary>
		public bool EnableBoldStyle {
			get {
				return _enableBoldStyle;
			}
			set {
				_enableBoldStyle = value;
			}
		}

		/// <summary>
		/// </summary>
		public bool ForceBoldStyle {
			get {
				return _forceBoldStyle;
			}
			set {
				_forceBoldStyle = value;
			}
		}

		/// <summary>
		/// <ja>文字色です。</ja>
		/// <en>Gets or sets the color of characters.</en>
		/// </summary>
		public Color ForeColor {
			get {
				return _forecolor;
			}
			set {
				_forecolor = value;
				ClearBrush();
			}
		}
		/// <summary>
		/// <ja>JScriptではColor構造体が使用できないので、ForeColorプロパティを設定するかわりにこのメソッドを使ってください。</ja>
		/// <en>Because JScript cannot handle the Color structure, please use this method instead of the ForeColor property.</en>
		/// </summary>
		public void SetForeColor(object value) {
			_forecolor = (Color)value;
			ClearBrush();
		}
		/// <summary>
		/// <ja>背景色です。</ja>
		/// <en>Gets or sets the background color.</en>
		/// </summary>
		public Color BackColor {
			get {
				return _bgcolor;
			}
			set {
				_bgcolor = value;
				ClearBrush();
			}
		}
		/// <summary>
		/// <ja>JScriptでは構造体が使用できないので、BackColorプロパティを設定するかわりにこのメソッドを使ってください。</ja>
		/// <en>Because JScript cannot handle the Color structure, please use this method instead of the BackColor property.</en>
		/// </summary>
		public void SetBackColor(object value) {
			_bgcolor = (Color)value;
			ClearBrush();
		}

		private void set_backgroundImage(Image value){
			if(_backgroundImage!=null)
				_backgroundImage.Dispose();
			_backgroundImage=value;
		}

		/// <summary>
		/// <ja>背景画像のファイル名です。</ja>
		/// <en>Gets or sets the file name of the background image.</en>
		/// </summary>
		public string BackgroundImageFileName {
			get {
				return _backgroundImageFileName;
			}
			set {
				_backgroundImageFileName = value;
				set_backgroundImage(null);
			}
		}

		/// <summary>
		/// <ja>背景画像を取得します。</ja>
		/// <en>Gets the image for background.</en>
		/// </summary>
		/// <returns>
		/// <ja>背景画像を表す Image オブジェクトを返します。</ja>
		/// <en>Returns the background image.</en>
		/// </returns>
		public Image GetImage() {
			try {
				if(!_imageLoadIsAttempted) {
					_imageLoadIsAttempted = true;
					this.set_backgroundImage(null);
					if(_backgroundImageFileName!=null&&_backgroundImageFileName.Length>0) {
						try{
							this.set_backgroundImage(Image.FromFile(_backgroundImageFileName));
						}catch (Exception) {
							MessageBox.Show("Can't find the background image!", "Poderosa error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
						}
					}
				}

				return _backgroundImage;
			}
			catch(Exception ex) {
				RuntimeUtil.ReportException(ex);
				return null;
			}
		}

		/// <summary>
		/// <ja>背景画像の位置です。</ja>
		/// <en>Gets or sets the position of the background image.</en>
		/// </summary>
		public ImageStyle ImageStyle {
			get {
				return _imageStyle;
			}
			set {
				_imageStyle = value;
			}
		}

#if !MACRODOC

		/// <summary>
		/// 
		/// </summary>
		/// <exclude/>
		public EscapesequenceColorSet ESColorSet {
			get {
				return _esColorSet;
			}
			set {
				Debug.Assert(value!=null);
				_esColorSet = value;
			}
		}
#endif
		/// <summary>
		/// <ja>コピーして作成します。</ja>
		/// <en>Initializes with another instance.</en>
		/// </summary>
		public RenderProfile(RenderProfile src) {
			_fontName = src._fontName;
			_cjkFontName = src._cjkFontName;
			_fontSize = src._fontSize;
			_lineSpacing = src._lineSpacing;
			_useClearType = src._useClearType;
			_enableBoldStyle = src._enableBoldStyle;
			_forceBoldStyle = src._forceBoldStyle;
			_cjkFont = _font = null;

			_forecolor = src._forecolor;
			_bgcolor = src._bgcolor;
#if !MACRODOC
			_esColorSet = (EscapesequenceColorSet)src._esColorSet.Clone();
#endif
			_bgbrush = _brush = null;

			_backgroundImageFileName = src._backgroundImageFileName;
			_imageLoadIsAttempted = false;
			_imageStyle = src.ImageStyle;
		}
		public RenderProfile() {
			//do nothing. properties must be filled
			_backgroundImageFileName = "";
#if !MACRODOC
			_esColorSet = new EscapesequenceColorSet();
#endif
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		/// <exclude/>
		public object Clone() {
			return new RenderProfile(this);
		}

		public void Dispose(){
			// フォント・ブラシ
			this.ClearFont();
			this.ClearBrush();

			// 背景画像
			this.set_backgroundImage(null);
			this._imageLoadIsAttempted=false;
		}

		private void ClearFont() {
			DisposeFontHandle(ref _font);
			DisposeFontHandle(ref _boldfont);
			DisposeFontHandle(ref _underlinefont);
			DisposeFontHandle(ref _boldunderlinefont);
			DisposeFontHandle(ref _cjkFont);
			DisposeFontHandle(ref _cjkBoldfont);
			DisposeFontHandle(ref _cjkUnderlinefont);
			DisposeFontHandle(ref _cjkBoldUnderlinefont);
		}
		private void DisposeFontHandle(ref FontHandle f) {
			if(f!=null) {
				f.Dispose();
				f = null;
			}
		}
		private void ClearBrush() {
			if(_brush!=null) _brush.Dispose();
			if(_bgbrush!=null) _bgbrush.Dispose();
			_brush = null;
			_bgbrush = null;
		}

#if !MACRODOC
		private void CreateFonts() {
			_font = new FontHandle(RuntimeUtil.CreateFont(_fontName, _fontSize));
			FontStyle fs = _font.Font.Style;
			_boldfont = new FontHandle(new Font(_font.Font, fs | FontStyle.Bold));
			_underlinefont = new FontHandle(new Font(_font.Font, fs | FontStyle.Underline));
			_boldunderlinefont = new FontHandle(new Font(_font.Font, fs | FontStyle.Underline | FontStyle.Bold));
			
			_cjkFont = new FontHandle(new Font(_cjkFontName, _fontSize));
			fs = _cjkFont.Font.Style;
			_cjkBoldfont = new FontHandle(new Font(_cjkFont.Font, fs | FontStyle.Bold));
			_cjkUnderlinefont = new FontHandle(new Font(_cjkFont.Font, fs | FontStyle.Underline));
			_cjkBoldUnderlinefont = new FontHandle(new Font(_cjkFont.Font, fs | FontStyle.Underline | FontStyle.Bold));

			_usingIdenticalFont = (_font.Font.Name==_cjkFont.Font.Name);
			
			//通常版
			Graphics g = Graphics.FromHwnd(Win32.GetDesktopWindow());
			IntPtr hdc = g.GetHdc();
			Win32.SelectObject(hdc, _font.HFONT);
			Win32.SIZE charsize1, charsize2;
			Win32.GetTextExtentPoint32(hdc, "A", 1, out charsize1);
			Win32.GetTextExtentPoint32(hdc, "AAA", 3, out charsize2);

			_pitch = new SizeF((charsize2.width-charsize1.width)/2, charsize1.height);
#if KM20121125_UseExtTextOut_impl3
      {
        float pw=_pitch.Width;
        float fx=0f;
        for(int i=0;i<PITCH_DX_LEN;i++,fx+=pw)
          pitch_deltaXArray[i]=(int)(fx+pw)-(int)fx;
      }
#endif

			_chargap = (charsize1.width-_pitch.Width)/2;
			g.ReleaseHdc(hdc);
			g.Dispose();
		}
		private void CreateBrushes() {
			_brush = new SolidBrush(_forecolor);
			_bgbrush = new SolidBrush(_bgcolor);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <exclude/>
		public Brush Brush {
			get {
				if(_brush==null) CreateBrushes();
				return _brush;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <exclude/>
		public Brush BgBrush {
			get {
				if(_bgbrush==null) CreateBrushes();
				return _bgbrush;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <exclude/>
		public SizeF Pitch {
			get {
				if(_font==null) CreateFonts();
				return _pitch;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <exclude/>
		public int LineSpacing {
			get {
				return _lineSpacing;
			}
			set {
				_lineSpacing = value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <exclude/>
		public Font DefaultFont {
			get {
				if(_font==null) CreateFonts();
				return _font.Font;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <exclude/>
		public float CharGap {
			get {
				if(_font==null) CreateFonts();
				return _chargap;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <exclude/>
		public bool UsingIdenticalFont {
			get {
				return _usingIdenticalFont;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dec"></param>
		/// <returns></returns>
		/// <exclude/>
		public Color CalcTextColor(TextDecoration dec) {
			// inverted xor invisible
			if(dec.Inverted!=dec.Invisible)
				return CalcBackColor_Impl(dec);
			else
				return CalcTextColor_Impl(dec);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dec"></param>
		/// <returns></returns>
		/// <exclude/>
		public Color CalcBackColor(TextDecoration dec){
			if(dec.Inverted)
				return CalcTextColor_Impl(dec);
			else
				return CalcBackColor_Impl(dec);
		}

		private Color CalcTextColor_Impl(TextDecoration dec){
			if(_brush==null) CreateBrushes();
			if(dec==null) return _forecolor;

			ColorType coltype=dec.TextColorType;
			if(8<=(int)coltype&&(int)coltype<16){
				if(dec.BrightText){
					return this.ESColorSet[(int)coltype];
				}else{
					return this.ESColorSet[(int)coltype-(int)ColorType.ColorBlack];
				}
			}else switch(coltype){
				case ColorType.Custom:
					return dec.TextColor;
				case ColorType.DefaultBack:
					return _bgcolor;
				case ColorType.DefaultText:
					return _forecolor;
				default:
					throw new Exception("Unexpected decoration object");
			}
		}
		private Color CalcBackColor_Impl(TextDecoration dec){
			if(_brush==null) CreateBrushes();
			if(dec==null) return _bgcolor;

			ColorType coltype=dec.BackColorType;

			if(8<=(int)coltype&&(int)coltype<16){
				if(dec.BrightBack){
					return this.ESColorSet[(int)coltype];
				}else{
					return this.ESColorSet[(int)coltype-(int)ColorType.ColorBlack];
				}
			}else switch(coltype){
				case ColorType.Custom:
					return dec.BackColor;
				case ColorType.DefaultBack:
					return _bgcolor;
				case ColorType.DefaultText:
					return _forecolor;
				default:
					throw new Exception("Unexpected decoration object");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dec"></param>
		/// <returns></returns>
		/// <exclude/>
		public bool CalcBold(TextDecoration dec) {
			if (_forceBoldStyle)
				return true;

			if (_enableBoldStyle)
				return dec.Bold;
			else
				return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dec"></param>
		/// <param name="cg"></param>
		/// <returns></returns>
		/// <exclude/>
		public Font CalcFont(TextDecoration dec, CharGroup cg) {
			return CalcFontInternal(dec, cg, false).Font;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dec"></param>
		/// <param name="cg"></param>
		/// <returns></returns>
		/// <exclude/>
		public IntPtr CalcHFONT_NoUnderline(TextDecoration dec, CharGroup cg) {
			return CalcFontInternal(dec, cg, true).HFONT;
		}

		private FontHandle CalcFontInternal(TextDecoration dec, CharGroup cg, bool ignore_underline) {
			if(_font==null) CreateFonts();

			if(cg!=CharGroup.Hankaku) {
				if(dec==null) return _cjkFont;

				if(CalcBold(dec)) {
					if(!ignore_underline && dec.Underline)
						return _cjkBoldUnderlinefont;
					else
						return _cjkBoldfont;
				}
				else if(!ignore_underline && dec.Underline)
					return _cjkUnderlinefont;
				else
					return _cjkFont;
			}
			else {
				if(dec==null) return _font;

				if(CalcBold(dec)) {
					if(!ignore_underline && dec.Underline)
						return _boldunderlinefont;
					else
						return _boldfont;
				}
				else if(!ignore_underline && dec.Underline)
					return _underlinefont;
				else
					return _font;
			}
		}
#endif

	}

#if !MACRODOC
	//Escape sequence color
	/// <summary>
	/// 
	/// </summary>
	/// <exclude/>
	public class EscapesequenceColorSet : ICloneable {

		private bool _isDefault;
		private Color[] _colors;

		public EscapesequenceColorSet() {
			_colors = new Color[256];
			SetDefault();
		}

		public bool IsDefault {
			get {
				return _isDefault;
			}
		}

		public object Clone() {
			EscapesequenceColorSet newval = new EscapesequenceColorSet();
			for(int i=0; i<_colors.Length; i++)
				newval._colors[i] = _colors[i];
			newval._isDefault = _isDefault;
			return newval;
		}

		public Color this[int index] {
			get {
				return _colors[index];
			}
			set {
				_colors[index] = value;
				if(_isDefault) _isDefault = GetDefaultColor(index)==value;
			}
		}

		public void SetDefault() {
			for(int i=0; i<_colors.Length; i++) {
				_colors[i] = GetDefaultColor(i);
			}
			_isDefault = true;
		}
		public string Format() {
			if(_isDefault) return "";
			StringBuilder bld = new StringBuilder();
			for(int i=0; i<_colors.Length; i++) {
				if(i>0) bld.Append(',');
				bld.Append(_colors[i].Name);
			}
			return bld.ToString();
		}
		public void Load(string value) {
			if(value==null)
				SetDefault();
			else {
				string[] cols = value.Split(',');

				int n=Math.Min(cols.Length,_colors.Length);
				int i;
				for(i=0;i<n;i++){
					Color c=ParseUtil.ParseColor(cols[i],Color.Empty);
					if(!c.IsEmpty){
						_colors[i]=c;
						_isDefault=true;
					}else
						_colors[i]=GetDefaultColor(i);
				}
				for(;i<_colors.Length;i++)
					_colors[i]=GetDefaultColor(i);
			}
		}
		public static EscapesequenceColorSet Parse(string s) {
			EscapesequenceColorSet r = new EscapesequenceColorSet();
			r.Load(s);
			return r;
		}

		public static Color GetDefaultColor(int index){
			if(index<16)
				return mwg.RosaTerm.TerminalColors.GetRosa16Color(index);
			else
				return mwg.RosaTerm.TerminalColors.GetXterm256Color(index);
		}
	}
#endif
}
