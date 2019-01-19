using BitVector32=System.Collections.Specialized.BitVector32;
using TerminalType=Poderosa.ConnectionParam.TerminalType;

namespace mwg.RosaTerm{

	public partial class TerminalState{
		static TerminalState(){
			InitializeBitsMapping();
		}

		readonly RosaTerminal term;
		internal TerminalState(RosaTerminal term){
			this.term=term;
      this.bits=new System.Collections.BitArray((int)DecsetBitIndex.COUNT);

      this.emuterm=TerminalType.Cygwin;
			this.SmIrm=false;
			this.DecAwm=true;
			this.DecNkm=false;
			this.DecTcem=true;
			this.DecOm=false;
		}
		//--------------------------------------------------------------------------
		TerminalType emuterm;
		public TerminalType EmulationType{
			get{return this.emuterm;}
			set{this.emuterm=value;}
		}
		// eat_newline_glitch
		public bool TerminfoXenlCap{
			get{return emuterm==TerminalType.VT100||emuterm==TerminalType.XTerm||emuterm==TerminalType.KTerm;}
		}
		// back_color_erase
		public bool TerminfoBceCap{
			get{return emuterm==TerminalType.XTerm;}
		}
		//--------------------------------------------------------------------------
		//  オプションに対応する時の手順
		//  1. 既定値
		//     TerminalState の初期化子に既定値を設定する。
		//  2. 処理
		//     RosaTerminal に、そのオプションを使用した処理を書く。
		//  3. 変化の通知
		//     値が変化した瞬間に、何らかの処理をする必要があるオプションについては、
		//     変化の通知を行う。具体的には、
		//     1. 先ず、Terminal 側に変化の通知を受け取る為の関数 (internal) を定義し、
		//     2. 次に TerminalState のプロパティに、その関数の呼出を追加する。
		//     3. DEC なら RestoreDec にも値の変更を明示的に書く。
		//--------------------------------------------------------------------------
		private TerminalState(TerminalState state){
			this.term=state.term;

      //this.vdec1=state.vdec1;
      //this.vdecX=state.vdecX;
      this.bits=(System.Collections.BitArray)state.bits.Clone();
		}
		private TerminalState Clone(){
			return new TerminalState(this);
		}

		TerminalState dec_store=null;
		public void SaveDec(){
			this.dec_store=this.Clone();
		}
		public void RestoreDec(){
			// Invoke OnChanged Functions
			this.DecCkm=dec_store.DecCkm;

			// Copy Values
      //this.vdec1=dec_store.vdec1;
      //this.vdecX=dec_store.vdecX;
      this.bits=dec_store.bits;
		}
		//==========================================================================
		BitVector32 values1;
		const int MODIFY_FUNCTION_KEYS  =0;
		const int MODIFY_CURSOR_KEYS    =1;
		const int MODIFY_OTHER_KEYS     =2;
		const int MODE_VT52             =3;

		public bool ModifyFunctionKeys{
			get{return this.values1[MODIFY_FUNCTION_KEYS];}
			set{this.values1[MODIFY_FUNCTION_KEYS]=value;}
		}
		public bool ModifyCursorKeys{
			get{return this.values1[MODIFY_CURSOR_KEYS];}
			set{this.values1[MODIFY_CURSOR_KEYS]=value;}
		}
		public bool ModifyOtherKeys{
			get{return this.values1[MODIFY_OTHER_KEYS];}
			set{this.values1[MODIFY_OTHER_KEYS]=value;}
		}
		public bool ModeVt52{
			get{return this.values1[MODE_VT52];}
			set{this.values1[MODE_VT52]=value;}
		}

		//==========================================================================
		//	SM Switches
		//--------------------------------------------------------------------------
		BitVector32 vsm;
		enum SmIndex{
			SM_AM         ,
			SM_CRM        ,
			SM_IRM        ,
			SM_SRM        ,
			SM_LMN        ,
			SM_BIG_CURSOR ,
		}
		public bool SmAm{
			get{return this.vsm[(int)SmIndex.SM_AM];}
			set{this.vsm[(int)SmIndex.SM_AM]=value;}
		}
		public bool SmCrm{
			get{return this.vsm[(int)SmIndex.SM_CRM];}
			set{this.vsm[(int)SmIndex.SM_CRM]=value;}
		}
		// _insertMode に相当
		public bool SmIrm{
			get{return this.vsm[(int)SmIndex.SM_IRM];}
			set{this.vsm[(int)SmIndex.SM_IRM]=value;}
		}
		public bool SmSrm{
			get{return this.vsm[(int)SmIndex.SM_SRM];}
			set{
				if(this.vsm[(int)SmIndex.SM_SRM]==value)return;
				this.vsm[(int)SmIndex.SM_SRM]=value;
				term.OnStateSmSrmChanged(value);
			}
		}
		public bool SmLmn{
			get{return this.vsm[(int)SmIndex.SM_LMN];}
			set{this.vsm[(int)SmIndex.SM_LMN]=value;}
		}
		public bool SmBigCursor{
			get{return this.vsm[(int)SmIndex.SM_BIG_CURSOR];}
			set{this.vsm[(int)SmIndex.SM_BIG_CURSOR]=value;}
		}
		//==========================================================================
		//	DEC Switches
		//--------------------------------------------------------------------------
    //Dec1Flags vdec1;
    //DecXFlags vdecX;
    System.Collections.BitArray bits;
	}
}