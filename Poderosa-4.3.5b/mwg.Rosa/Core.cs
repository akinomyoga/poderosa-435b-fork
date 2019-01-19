using Gdi=System.Drawing;

namespace Poderosa.Document{

	/// <summary>
	/// 文字修飾の生成をする為の設定を行います。
	/// </summary>
	[System.Serializable]
	public struct TextDecorationConstructor{
		private ColorType bgtype;
		private Gdi::Color bgcolor;
		private ColorType fgtype;
		private Gdi::Color fgcolor;
		private TextDecorationStyle style;

		public TextDecorationConstructor(TextDecoration b){
			this.bgtype=b.BackColorType;
			this.bgcolor=b.BackColor;
			this.fgtype=b.TextColorType;
			this.fgcolor=b.TextColor;
			this.style=b.TextStyle;
		}

		public void Clear(){
			this.bgtype=ColorType.DefaultBack;
			this.bgcolor=Gdi::Color.Empty;
			this.fgtype=ColorType.DefaultText;
			this.fgcolor=Gdi::Color.Empty;
			this.style=TextDecorationStyle.None;
		}

		public TextDecoration CreateTextDecoration(){
			return new TextDecoration(bgtype,bgcolor,fgtype,fgcolor,style);
		}

		//==========================================================================
		//	Font Style Properties
		//--------------------------------------------------------------------------
		/// <summary>
		/// 前景色を取得・設定します。
		/// </summary>
		public Gdi::Color ForeColor{
			get{return this.fgcolor;}
			set{
				fgcolor=value;
				fgtype=value==Gdi::Color.Empty? ColorType.DefaultText : ColorType.Custom;
			}
		}
		/// <summary>
		/// 背景色を取得・設定します。
		/// </summary>
		public Gdi::Color BackColor{
			get{return bgcolor;}
			set{
				bgcolor=value;
				bgtype=value==Gdi::Color.Empty? ColorType.DefaultBack : ColorType.Custom;
			}
		}
		/// <summary>
		/// 前景色が既定色であるかどうかを取得します。
		/// </summary>
		public ColorType TextColorType{
			get{return this.Inverted?bgtype:fgtype;}
			set{fgtype=value;}
		}
		/// <summary>
		/// 背景色が既定色であるかどうかを取得します。
		/// </summary>
		public ColorType BackColorType{
			get{return this.Inverted?fgtype:bgtype;}
			set{bgtype=value;}
		}
		//--------------------------------------------------------------------------
		/// <summary>
		/// 明るい背景色にするかどうかを取得または設定します。
		/// </summary>
		public bool BrightBack{
			get{return 0!=(style&TextDecorationStyle.BrightBack);}
			set{
				if(value){
					style|=TextDecorationStyle.BrightBack;
				}else{
					style&=~TextDecorationStyle.BrightBack;
				}
			}
		}
		/// <summary>
		/// 明るい前景色にするかどうかを取得または設定します。
		/// </summary>
		public bool BrightText{
			get{return 0!=(style&TextDecorationStyle.BrightText);}
			set{
				if(value){
					style|=TextDecorationStyle.BrightText;
				}else{
					style&=~TextDecorationStyle.BrightText;
				}
			}
		}
		//--------------------------------------------------------------------------
		/// <summary>
		/// 文字を不可視にするか (前景色を背景色と同じにする) かどうかを取得または設定します。
		/// </summary>
		public bool Invisible{
			get{return 0!=(style&TextDecorationStyle.Invisible);}
			set{
				if(value){
					style|=TextDecorationStyle.Invisible;
				}else{
					style&=~TextDecorationStyle.Invisible;
				}
			}
		}
		/// <summary>
		/// 前景色と背景色を一時的に反転するか否かを取得または設定します。
		/// </summary>
		public bool Inverted{
			get{return 0!=(style&TextDecorationStyle.Inverted);}
			set{
				if(value){
					style|=TextDecorationStyle.Inverted;
				}else{
					style&=~TextDecorationStyle.Inverted;
				}
			}
		}
		//--------------------------------------------------------------------------
		/// <summary>
		/// 字体が太字であるか否かを取得・設定します。
		/// </summary>
		public bool Bold{
			get{return 0!=(style&TextDecorationStyle.Bold);}
			set{
				if(value){
					style|=TextDecorationStyle.Bold;
				}else{
					style&=~TextDecorationStyle.Bold;
				}
			}
		}
		/// <summary>
		/// 下線を引くかどうかを取得・設定します。
		/// </summary>
		public bool Underline{
			get{return 0!=(style&TextDecorationStyle.Underline);}
			set{
				if(value){
					style|=TextDecorationStyle.Underline;
				}else{
					style&=~TextDecorationStyle.Underline;
				}
			}
		}
		/// <summary>
		/// 下線を引くかどうかを取得・設定します。
		/// </summary>
		public bool Throughline{
			get{return 0!=(style&TextDecorationStyle.Throughline);}
			set{
				if(value){
					style|=TextDecorationStyle.Throughline;
				}else{
					style&=~TextDecorationStyle.Throughline;
				}
			}
		}
		/// <summary>
		/// 下線を引くかどうかを取得・設定します。
		/// </summary>
		public bool Italic{
			get{return 0!=(style&TextDecorationStyle.Italic);}
			set{
				if(value){
					style|=TextDecorationStyle.Italic;
				}else{
					style&=~TextDecorationStyle.Italic;
				}
			}
		}
		/// <summary>
		/// 上線を引くかどうかを取得・設定します。
		/// </summary>
		public bool Overline{
			get{return 0!=(style&TextDecorationStyle.Overline);}
			set{
				if(value){
					style|=TextDecorationStyle.Overline;
				}else{
					style&=~TextDecorationStyle.Overline;
				}
			}
		}
		/// <summary>
		/// 二重下線を引くかどうかを取得・設定します。
		/// </summary>
		public bool Doubleline{
			get{return 0!=(style&TextDecorationStyle.Doubleline);}
			set{
				if(value){
					style|=TextDecorationStyle.Doubleline;
				}else{
					style&=~TextDecorationStyle.Doubleline;
				}
			}
		}
	}

	/// <summary>
	/// 文字装飾のスタイルについての情報を保持します。
	/// </summary>
	[System.Flags]
	[System.Serializable]
	public enum TextDecorationStyle{
		None         =0,
		Bold         =0x01,
		Italic       =0x02,

		Underline    =0x10,
		Throughline  =0x20,
		Overline     =0x40,
		Doubleline   =0x80,

		BrightBack   =0x100,
		BrightText   =0x200,
		Invisible    =0x400,
		Inverted     =0x800,
	}
}