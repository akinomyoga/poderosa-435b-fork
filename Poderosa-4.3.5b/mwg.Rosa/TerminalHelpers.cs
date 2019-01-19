using Gen=System.Collections.Generic;
using Forms=System.Windows.Forms;

namespace mwg.RosaTerm{

	#region class EscapeSequenceCutter
	/// <summary>
	/// 入力文字列からエスケープシーケンスを切り出す為に使用します。
	/// </summary>
	class EscapeSequenceCutter{
		const char CH_ESC='\x1b';
		const char CH_BEL='\a';

		const char CH_eST='\\';

		const char CH_IND=(char)0x84; // \eD
		const char CH_NEL=(char)0x85; // \eE
		const char CH_HTS=(char)0x88; // \eH
		const char CH_RI =(char)0x8D; // \eM
		const char CH_SS2=(char)0x8E; // \eN
		const char CH_SS3=(char)0x8F; // \eO
		const char CH_DCS=(char)0x90; // \eP
		const char CH_SPA=(char)0x96; // \eV
		const char CH_EPA=(char)0x97; // \eW
		const char CH_SOS=(char)0x98; // \eX
		const char CH_RTI=(char)0x9A; // \eZ
		const char CH_CSI=(char)0x9B; // \e[  EscSeq
		const char CH_ST =(char)0x9C; // \e\\
		const char CH_OSC=(char)0x9D; // \e]  EscSeq
		const char CH_PM =(char)0x9E; // \e^  EscSeq
		const char CH_APC=(char)0x9F; // \e_  EscSeq

		public enum EscapeState{
			Escape,
			Normal,
			Control,
			CSI,
			OSC,
			PM,
			APC,

			Intermediate,
			OSC_Pre,   // Intermediate
			OSC_Reach, // Intermediate
			PM_Reach,  // Intermediate
			APC_Reach, // Intermediate
		}

		public enum ControlChars{
			NUL     =0x00,
			SOH     =0x01,
			STX     =0x02,
			ETX     =0x03,
			EOT     =0x04,
			ENO     =0x05,
			ACK     =0x06,
			BEL     =0x07,
			BS      =0x08,
			HT      =0x09,
			LF      =0x0A,
			VT      =0x0B,
			FF      =0x0C,
			CR      =0x0D,
			SO      =0x0E,
			SI      =0x0F,

			DLO     =0x10,
			DC1     =0x11,
			DC2     =0x12,
			DC3     =0x13,
			DC4     =0x14,
			NAK     =0x15,
			SYN     =0x16,
			ETB     =0x17,
			CAN     =0x18,
			EM      =0x19,
			SUB     =0x1A,
			ESC     =0x1B,
			FS      =0x1C,
			GS      =0x1D,
			RS      =0x1E,
			US      =0x1F,

			SP      =0x20,
			DT      =0x7F,
			NSP     =0xA0,

			C1_80   =0x80,
			C1_81   =0x81,
			C1_82   =0x82,
			C1_83   =0x83,
			IND     =0x84,
			NEL     =0x85,
			SSA     =0x86,
			ESA     =0x87,
			HTS     =0x88,
			HTJ     =0x89,
			VTS     =0x8A,
			PLD     =0x8B,
			PLU     =0x8C,
			RI      =0x8D,
			SS2     =0x8E,
			SS3     =0x8F,

			DCS     =0x90,
			PU1     =0x91,
			PU2     =0x92,
			STS     =0x93,
			CRH     =0x94,
			MW      =0x95,
			SPA     =0x96,
			EPA     =0x97,
			SOS     =0x98,
			C1_99   =0X99,
			DECID   =0x9A,
			CSI     =0x9B,
			STC     =0x9C,
			OSC     =0x9D,
			PM      =0x9E,
			APC     =0x9F,
		}

		//==========================================================================
		//	抽出結果
		//--------------------------------------------------------------------------
		public string Content{
			get{return this.seq;}
		}
		public IntArguments CSIArguments{
			get{return this.csiargs;}
		}
		/// <summary>
		/// CSI シーケンスの引数を解析し提供します。
		/// #;#;#;# ... の形式の入力から int 配列を構築します。
		/// </summary>
		public class IntArguments:mwg.RosaTerm.IReadOnlyIndexer<int>{
			int index=-1;
			readonly Gen::List<int> args;
			internal IntArguments(){
				this.args=new Gen::List<int>();
			}
			//========================================================================
			//	引数内容アクセス
			//------------------------------------------------------------------------
			public int Length{
				get{
					int c=this.args.Count;
					return c==0?1:c;
				}
			}
			public int this[int index]{
				get{
					if(index<0||this.args.Count<=index)return 0;
					return this.args[index];
				}
			}
			public int GetOrDefault(int index,int defValue){
				if(index<0||this.args.Count<=index)return defValue;
				int r=this.args[index];
				return r==0?defValue:r;
			}
			//========================================================================
			//	引数構築
			//------------------------------------------------------------------------
			internal void Clear(){
				this.args.Clear();
				this.index=-1;
			}
			internal bool AddChar(char c){
				if('0'<=c&&c<='9'){
					if(index<0){
						this.args.Add(c-'0');
						index++;
					}else
						this.args[index]=this.args[index]*10+(c-'0');
					return true;
				}else if(c==';'){
					this.index++;
					this.args.Add(0);
					return true;
				}else{
					return false;
				}
			}
			/// <summary>
			/// 引数列の文字列表現を取得します。
			/// </summary>
			/// <returns>引数列を文字列にして返します。</returns>
			public override string ToString() {
				if(args.Count==0)return "";
				if(args.Count==1)return args[0].ToString();

				System.Text.StringBuilder build=new System.Text.StringBuilder();
				build.Append(args[0]);
				for(int i=1,iN=args.Count;i<iN;i++){
					build.Append(';');
					build.Append(args[i]);
				}
				return build.ToString();
			}

      #region IReadOnlyIndexer<int> メンバ

      int IReadOnlyIndexer<int>.Count {
        get { return this.Length; }
      }

      #endregion
    }

		public EscapeSequenceCutter(){
			this.stat=EscapeState.Normal;
			this.build=new System.Text.StringBuilder();
			this.csiargs=new IntArguments();
		}
		//==========================================================================
		//	抽出
		//--------------------------------------------------------------------------
		EscapeState stat;
		readonly System.Text.StringBuilder build;
		readonly IntArguments  csiargs;
		string seq=null;
		/// <summary>
		/// 状態をクリアします。
		/// </summary>
		public void Clear(){
			this.stat=EscapeState.Normal;
			this.seq="";
			this.csiargs.Clear();
			this.build.Length=0;
		}
		/// <summary>
		/// 受信した文字をエスケープシーケンスの一部か判定し処理します。
		/// </summary>
		/// <param name="c">入力する文字を指定します。</param>
		/// <returns>
		/// 通常の文字と判定された場合には Normal を返します。
		/// エスケープシーケンスの一部として処理した場合に Escape を返します。
		/// エスケープシーケンスが完成し、処理を要する場合には、シーケンスの種類に応じた値を返します。
		/// </returns>
		public EscapeState ProcessChar(char c){
			if(this.stat==EscapeState.Normal){
				if(c<0x20)switch(c){
					case CH_ESC:
						this.stat=EscapeState.Escape;
						this.build.Length=0;
						return EscapeState.Intermediate;
					default:
						return EscapeState.Control;
				}else if(c>=0x80&&c<=0x9F)switch(c){
					case CH_CSI:
						this.stat=EscapeState.CSI;
						this.csiargs.Clear();
						return EscapeState.Intermediate;
					case CH_OSC:
						this.stat=EscapeState.OSC_Pre;
						this.csiargs.Clear();
						return EscapeState.Intermediate;
					case CH_PM:
						this.stat=EscapeState.PM;
						return EscapeState.Intermediate;
					case CH_APC:
						this.stat=EscapeState.APC;
						return EscapeState.Intermediate;
					default:
						return EscapeState.Control;
				}
				return EscapeState.Normal;
			}else{
				// NULL 文字は無視
				if(c=='\0')return EscapeState.Intermediate;
				switch(this.stat){
					case EscapeState.Escape:
						switch(c){
							case '[':
								//this.stat=EscapeState.Normal;
								//return ProcessChar(CH_CSI);
								this.stat=EscapeState.CSI;
								this.csiargs.Clear();
								return EscapeState.Intermediate;
							case ']':
								//this.stat=EscapeState.Normal;
								//return ProcessChar(CH_OSC);
								this.stat=EscapeState.OSC_Pre;
								this.csiargs.Clear();
								return EscapeState.Intermediate;
							case '^':
								//this.stat=EscapeState.Normal;
								//return ProcessChar(CH_PM);
								this.stat=EscapeState.PM;
								return EscapeState.Intermediate;
							case '_':
								//this.stat=EscapeState.Normal;
								//return ProcessChar(CH_APC);
								this.stat=EscapeState.APC;
								return EscapeState.Intermediate;
							//TODO: TerminalBase.cs:524/EscapeSequenceTerminal は
							//      \e@0 \e@1 に対する処理も実行している。
						}
						if('0'<=c&&c<='~'){
							// DONE: Escape Sequence Completed
							this.build.Append(c);
							goto EscapeCompleted;
						}
						break;
					case EscapeState.CSI:
						if(c>='@'){
							// DONE: Escape Sequence Completed
							build.Append(c);
							goto EscapeCompleted;
						}

						if(!this.csiargs.AddChar(c))
							this.build.Append(c);
						break;
					//------------------------------------------------
					case EscapeState.OSC_Pre:
						if(c==';'){
							this.stat=EscapeState.OSC;
							break;
						}
						if(!this.csiargs.AddChar(c)){
							this.stat=EscapeState.OSC;
							goto case EscapeState.OSC;
						}
						break;
					case EscapeState.OSC: // 本来 BEL で終わる
        OSC:
						if(c==CH_BEL||c==CH_ST){
							goto EscapeCompleted;
						}else if(c==CH_ESC){
							this.stat=EscapeState.OSC_Reach;
						}else{
							this.build.Append(c);
						}
						break;
					case EscapeState.OSC_Reach:
						this.stat=EscapeState.OSC;
						if(c==CH_eST){
							goto EscapeCompleted;
						}else{
              this.build.Append(CH_ESC);
              goto OSC;
						}
					//------------------------------------------------
					case EscapeState.APC: // 本来 ST で終わる
						if(c==CH_BEL||c==CH_ST){
							goto EscapeCompleted;
						}else if(c==CH_ESC){
							this.stat=EscapeState.APC_Reach;
						}else{
							this.build.Append(c);
						}
						break;
					case EscapeState.APC_Reach:
						this.stat=EscapeState.APC;
						if(c==CH_eST){
							goto EscapeCompleted;
						}else{
							build.Append(c);
						}
						break;
					//------------------------------------------------
					case EscapeState.PM: // 本来 ST で終わる
						if(c==CH_BEL||c==CH_ST){
							goto EscapeCompleted;
						}else if(c==CH_ESC){
							this.stat=EscapeState.PM_Reach;
						}else{
							this.build.Append(c);
						}
						break;
					case EscapeState.PM_Reach:
						this.stat=EscapeState.PM;
						if(c==CH_eST){
							goto EscapeCompleted;
						}else{
							build.Append(c);
						}
						break;
					//------------------------------------------------
					default:
						throw new System.InvalidProgramException("Fatal:this.state: 不定状態。");
					EscapeCompleted:{
						this.seq=build.ToString();
						EscapeState r=this.stat;
						this.stat=EscapeState.Normal;
						return r;
					}
				}
				return EscapeState.Intermediate;
			}
		}
		
	}
	#endregion

  internal static class KeyModifiers{
    public const Forms::Keys Shift=Forms::Keys.Shift;     // 0x010000
    public const Forms::Keys Ctrl =Forms::Keys.Control;   // 0x020000
    public const Forms::Keys MetaE=Forms::Keys.Alt;       // 0x040000
    public const Forms::Keys Meta =(Forms::Keys)0x080000; // 0x080000
    //public const Forms::Keys ModAlter=(Forms::Keys)0x100000; // 0x100000
    //public const Forms::Keys ModSuper=(Forms::Keys)0x200000; // 0x200000
    //public const Forms::Keys ModHyper=(Forms::Keys)0x400000; // 0x400000
  }

	internal class InputSequence{
		public readonly byte[] data=new byte[30];// mouse button * 3
		public int length=0;

		public void Clear(){
			this.length=0;
		}
		public void Add(byte val){
			data[length++]=val;
		}
		public void Add(char val){
			data[length++]=(byte)val;
		}
    public void WriteUtf8Char(int code){
      if(code<0)return;

      if(code<0x80){
        this.Add((byte)(code&0x7F));
      }else if(code<0x0800){
        this.Add((byte)(0xC0|code>>6 &0x1F));
        this.Add((byte)(0x80|code    &0x3F));
      }else if(code<0x00010000){
        this.Add((byte)(0xE0|code>>12&0x0F));
        this.Add((byte)(0x80|code>>6 &0x3F));
        this.Add((byte)(0x80|code    &0x3F));
      }else if(code<0x00200000){
        this.Add((byte)(0xF0|code>>18&0x07));
        this.Add((byte)(0x80|code>>12&0x3F));
        this.Add((byte)(0x80|code>>6 &0x3F));
        this.Add((byte)(0x80|code    &0x3F));
      }else if(code<0x04000000){
        this.Add((byte)(0xF8|code>>24&0x03));
        this.Add((byte)(0x80|code>>18&0x3F));
        this.Add((byte)(0x80|code>>12&0x3F));
        this.Add((byte)(0x80|code>>6 &0x3F));
        this.Add((byte)(0x80|code    &0x3F));
      }else{ // if(c<0x80000000) 常に成立
        this.Add((byte)(0xF8|code>>30&0x01));
        this.Add((byte)(0x80|code>>24&0x3F));
        this.Add((byte)(0x80|code>>18&0x3F));
        this.Add((byte)(0x80|code>>12&0x3F));
        this.Add((byte)(0x80|code>>6 &0x3F));
        this.Add((byte)(0x80|code    &0x3F));
      }
    }
		//==========================================================================

    private const byte ESC=(byte)'\x1b';
		private void write_modifier(Forms::Keys mods){
			int i=1;
			if(0!=(mods&Forms::Keys.Shift))  i+=1;
			if(0!=(mods&Forms::Keys.Alt))    i+=2;
			if(0!=(mods&Forms::Keys.Control))i+=4;
			if(i!=1){
				this.Add(';');
				this.Add((char)('0'+i));
			}
			// http://msdn.microsoft.com/ja-jp/library/system.windows.input.keyboard.iskeyup.aspx
			//Keys.IMEConvert; super
			//Keys.IMENonconvert; hyper
		}
    public void WriteNumber(uint index){
      int m=1;
      while(m*10<=index)m*=10;
      for(;m>0;m/=10)
        this.Add((byte)('0'+index/m%10));
    }

    public void v2WriteChar(RosaTerminal.EncodeMetaType meta,byte b) {
      switch(meta){
        case RosaTerminal.EncodeMetaType.Escape:
          this.Add(ESC);
          break;
        case RosaTerminal.EncodeMetaType.Meta:
          b|=0x80;
          break;
      }
      this.Add(b);
    }

    public void v2WriteCsiChar(RosaTerminal.EncodeMetaType meta,byte ch,System.Windows.Forms.Keys mods) {
      if(meta!=RosaTerminal.EncodeMetaType.None)this.Add(ESC);
      this.Add(ESC);
      this.Add('[');

      //this.WriteNumber((uint)ch);
      //this.write_modifier(mods);
      //this.Add('^');

      this.Add('2');
      this.Add('7');
      this.write_modifier(mods);
      this.Add(';');
      this.WriteNumber((uint)ch);
      this.Add('~');
    }
    public void v2WriteCsiSequence(RosaTerminal.EncodeMetaType meta,byte b,System.Windows.Forms.Keys mods,char c) {
      if(meta!=RosaTerminal.EncodeMetaType.None)this.Add(ESC);
      this.Add(ESC);
      this.Add('[');
      this.WriteNumber(b);
      this.write_modifier(mods);
      this.Add(c);
    }
    public void v2WriteCsiSequence(RosaTerminal.EncodeMetaType meta,System.Windows.Forms.Keys mods,char c){
      if(meta!=RosaTerminal.EncodeMetaType.None)this.Add(ESC);
      this.Add(ESC);
      this.Add('[');
      if(mods!=0){
        this.Add('1');
        this.write_modifier(mods);
      }
      this.Add(c);
    }
    public void v2WriteSs3Sequence(RosaTerminal.EncodeMetaType meta,System.Windows.Forms.Keys mods,char c){
      if(meta!=RosaTerminal.EncodeMetaType.None)this.Add(ESC);
      this.Add(ESC);
      if(mods!=0){
        this.Add('[');
        this.Add('1');
        this.write_modifier(mods);
      }else{
        this.Add('O');
      }
      this.Add(c);
    }
  }

	static class AcsSymbolsDefinition{
		// Unicode に勝手に埋め込む対象
		static char unicode_begin='\u26DB';
		static char unicode_axis ='\u26E0';
		static char unicode_end  ='\u26FF';
		public static bool IsAcsChar(char c){
			return unicode_begin<=c&&c<unicode_end;
		}

		public static char Decode(char c){
			if('`'<=c&&c<='~'){
				return (char)(unicode_axis+(c-'`'));
			}else if(c=='+'){
				return (char)(unicode_axis-5);
			}else if(c==','){
				return (char)(unicode_axis-4);
			}else if(c=='-'){
				return (char)(unicode_axis-3);
			}else if(c=='.'){
				return (char)(unicode_axis-2);
			}else if(c=='0'){
				return (char)(unicode_axis-1);
			}else{
				return c;
			}
		}

		static char[] dict={
			'\uFFEB','\uFFE9','\uFFEA','\uFFEC','\u25AF',
			'\u2666','\u2593','H','F',           // diamond checker HT FF
			'C','L','\uFF9F','+',                // CR LF degree pm
			'\u2588','\u263C','\u255D','\u2557', // board lantern ┘ ┐
			'\u2554','\u255A','\u256C','\u00AF',      // ┌ └ ┼ -
			'\u00AF','-','_','_',                     // - - - -
			'\u2560','\u2563','\u2569','\u2566', // ├ ┤ ┴ ┬
			'\u2551','\u2264','\u2265','\u2293', // ｜ ≦ ≧ π
			'!','\u00A3','\u25E6',                   // ≠ ￡ ・
		};
		public static char GetGriph(char c){
			if(IsAcsChar(c))
				return dict[c-unicode_begin];
			else
				return ' ';
		}
	}

  static class UnicodeCharacterSet {

    #region from Poderosa\Core\GLine.cs
    //この領域の文字の幅は本当はフォント依存だが、多くの日本語環境では全角として扱われる模様。BSで消すと２個来たりする
    static byte[] SimpleCharWidth_0x0080=new byte[]{
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,2,2,1,1,1,1,1,1,1,2,2,1,1,2,1,2,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1,1,
    };
    //全角半角混在ゾーン
    static byte[] SimpleCharWidth_0x2500=new byte[]{
      2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 2, //2500-0F
      2, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1, 2, 2, 2, 1, 1, //2510-1F
      2, 1, 1, 2, 2, 2, 1, 1, 2, 1, 1, 2, 2, 1, 1, 2, //2520-2F
      2, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1, 2, //2530-3F
      1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, //2540-4F
      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //2550-5F
      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //2560-6F
      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //2570-7F
      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //2580-8F
      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //2590-9F
      2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //25A0-AF
      1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 1, 1, //25B0-BF
      1, 1, 1, 1, 1, 1, 2, 2, 1, 1, 1, 2, 1, 1, 2, 2, //25C0-CF
      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //25D0-DF
      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, //25E0-EF
      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1  //25F0-FF
    };

    //文字によって描画幅を決める
    public static int SimpleCharWidth(char ch) {
      if (ch >= 0x100) {
        if (0x2500 <= ch && ch <= 0x25FF) //罫線等特殊記号
          return SimpleCharWidth_0x2500[ch - 0x2500];
        else if (0xFF61 <= ch && ch <= 0xFFDC) // FF61-FF64:Halfwidth CJK punctuation FF65-FF9F:Halfwidth Katakana FFA0-FFDC:Halfwidth Hangul
          return 1;
        else if (0xFFE8 <= ch && ch <= 0xFFEE) // Halfwidth Symbol
          return 1;
        else if(mwg.RosaTerm.AcsSymbolsDefinition.IsAcsChar(ch))
          return 1;
        else
          return 2;
      }
      else if (ch >= 0x80) {
        return SimpleCharWidth_0x0080[ch - 0x80];
      }
      else
        return 1; //本当はtabなどあるかもしれないのでもう少し真面目に計算すべき
    }
    #endregion

    static byte[] EmacsCharWidth_data=new byte[]{
      // [0x0000] \u00??
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,2,2,1,1,1,2,2,1,1,1,2,1,1,1,2,2,1,1,2,1,2,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1,1,

      // [0x0100] \u20??
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,2,2,2,1,2,2,1,1,2,2,1,1,
      2,2,1,1,1,2,2,1,1,1,1,1,1,1,1,1,2,1,2,2,1,1,1,1,1,1,1,2,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      // [0x0200]
      1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1,1,1,
      1,2,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,2,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      // [0x0300]
      2,1,2,2,1,1,1,2,2,1,1,2,1,1,1,1,1,2,2,1,1,1,1,1,1,1,2,1,1,2,2,2,
      2,1,1,1,1,2,1,2,2,2,2,2,2,1,2,1,1,1,1,1,2,2,1,1,1,1,1,1,1,2,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,
      2,2,1,1,1,1,2,2,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,2,2,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      // [0x0400]
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      // [0x0500]
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      // [0x0600]
      2,2,2,2,1,1,1,1,1,1,1,1,2,1,1,2,2,1,1,2,2,1,1,2,2,1,1,2,2,2,1,1,
      2,1,1,2,2,2,1,1,2,1,1,2,2,1,1,2,2,1,1,2,2,1,1,2,2,1,1,2,2,1,1,2,
      1,1,2,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,1,1,1,1,1,1,1,1,2,2,1,1,
      1,1,1,1,1,1,2,2,1,1,1,2,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      // [0x0700]
      1,1,1,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      2,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,2,1,1,2,1,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
      1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
    };
    public static int EmacsCharWidth(char ch){
      if(ch<'\xA0')
        return 1; // ASCII, 高頻度
      else if('\u3100'<=ch&&ch<'\uA4D0'||'\uAC00'<=ch&&ch<'\uD7A4')
        return 2; // CJK
      else if('\u2000'<=ch&&ch<'\u2700')
        return EmacsCharWidth_data[0x0100+(int)ch-0x2000]; // 記号

      int al=(int)ch&0xFF;
      int ah=(int)ch/256;
      switch(ah){
        case 0x00:return EmacsCharWidth_data[al];
        case 0x03:{uint t=(uint)((al-0x91)&~0x20);return t<25&&t!=17?2:1;}
        case 0x04:return al==1||0x10<=al&&al<=0x50||al==0x51?2:1;
        case 0x11:return al<0x60?2:1;
        // 20-26 記号
        case 0x2e:return al>=0x80?2:1;
        case 0x2f:return 2;
        case 0x30:return al!=0x3f?2:1;
        // 3100-a4cf CJK
        // AC00-D7A4 CJK
        case 0xf9:
        case 0xfa:return 2;
        case 0xfe:return 0x30<=al&&al<0x70?2:1;
        case 0xff:return 0x01<=al&&al<0x61||0xE0<=al&&al<=0xE7?2:1;
        default:return 1;
      }
    }
  }
}
