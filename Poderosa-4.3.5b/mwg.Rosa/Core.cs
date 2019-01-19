using Gdi=System.Drawing;

namespace Poderosa.Document{

	/// <summary>
	/// �����C���̐���������ׂ̐ݒ���s���܂��B
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
		/// �O�i�F���擾�E�ݒ肵�܂��B
		/// </summary>
		public Gdi::Color ForeColor{
			get{return this.fgcolor;}
			set{
				fgcolor=value;
				fgtype=value==Gdi::Color.Empty? ColorType.DefaultText : ColorType.Custom;
			}
		}
		/// <summary>
		/// �w�i�F���擾�E�ݒ肵�܂��B
		/// </summary>
		public Gdi::Color BackColor{
			get{return bgcolor;}
			set{
				bgcolor=value;
				bgtype=value==Gdi::Color.Empty? ColorType.DefaultBack : ColorType.Custom;
			}
		}
		/// <summary>
		/// �O�i�F������F�ł��邩�ǂ������擾���܂��B
		/// </summary>
		public ColorType TextColorType{
			get{return this.Inverted?bgtype:fgtype;}
			set{fgtype=value;}
		}
		/// <summary>
		/// �w�i�F������F�ł��邩�ǂ������擾���܂��B
		/// </summary>
		public ColorType BackColorType{
			get{return this.Inverted?fgtype:bgtype;}
			set{bgtype=value;}
		}
		//--------------------------------------------------------------------------
		/// <summary>
		/// ���邢�w�i�F�ɂ��邩�ǂ������擾�܂��͐ݒ肵�܂��B
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
		/// ���邢�O�i�F�ɂ��邩�ǂ������擾�܂��͐ݒ肵�܂��B
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
		/// ������s���ɂ��邩 (�O�i�F��w�i�F�Ɠ����ɂ���) ���ǂ������擾�܂��͐ݒ肵�܂��B
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
		/// �O�i�F�Ɣw�i�F���ꎞ�I�ɔ��]���邩�ۂ����擾�܂��͐ݒ肵�܂��B
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
		/// ���̂������ł��邩�ۂ����擾�E�ݒ肵�܂��B
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
		/// �������������ǂ������擾�E�ݒ肵�܂��B
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
		/// �������������ǂ������擾�E�ݒ肵�܂��B
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
		/// �������������ǂ������擾�E�ݒ肵�܂��B
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
		/// ������������ǂ������擾�E�ݒ肵�܂��B
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
		/// ��d�������������ǂ������擾�E�ݒ肵�܂��B
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
	/// ���������̃X�^�C���ɂ��Ă̏���ێ����܂��B
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