// -*- mode:csharp -*-
using Gen=System.Collections.Generic;

namespace mwg.RosaTerm{
  partial class TerminalState{
    //**************************************************************************

    enum DecsetBitIndex{
      IDX_DEC_CKM,
      IDX_DEC_ANM,
      IDX_DEC_COLM,
      IDX_DEC_SCLM,
      IDX_DEC_SCNM,
      IDX_DEC_OM,
      IDX_DEC_AWM,
      IDX_DEC_ARM,
      IDX_DEC_INLM,
      IDX_DEC_SHOW_TOOLBAR,
      IDX_DEC_BLINK_CURSOR,
      IDX_DEC_PFF,
      IDX_DEC_PEX,
      IDX_DEC_TCEM,
      IDX_DEC_SHOW_SCROLLBAR,
      IDX_DEC_FONT_SHIFTING,
      IDX_DEC_TEK,
      IDX_DEC_ALLOW_80_132,
      IDX_DEC_FIX_FOR_MORE,
      IDX_DEC_NRCM,
      IDX_DEC_MARGIN_BELL,
      IDX_DEC_RWM,
      IDX_DEC_LOGGING,
      IDX_DEC_ALT_SCR_BUFF,
      IDX_DEC_NKM,
      IDX_DEC_BKM,

      IDX_DEC_RLM,
      IDX_DEC_HEBM,
      IDX_DEC_HEM,
      IDX_DEC_NAKB,
      IDX_DEC_IPEM,
      IDX_DEC_HCCM,
      IDX_DEC_VCCM,
      IDX_DEC_PCCM,
      IDX_DEC_KBUM,
      IDX_DEC_VSSM,
      IDX_DEC_XRLM,
      IDX_DEC_KPM,
      IDX_DEC_NCSM,
      IDX_DEC_RLCM,
      IDX_DEC_RTSM,
      IDX_DEC_ARSM,
      IDX_DEC_MCM,
      IDX_DEC_AAM,
      IDX_DEC_CANSM,
      IDX_DEC_NULM,
      IDX_DEC_HDPXM,
      IDX_DEC_ESKM,
      IDX_DEC_OSCNM,
      IDX_DEC_NUMLK,
      IDX_DEC_CAPSLK,
      IDX_DEC_KLHIM,

      IDX_XTERM_SendMouse,
      IDX_XTERM_MMHilite,
      IDX_XTERM_MMCell,
      IDX_XTERM_MMAll,
      IDX_XTERM_SendFocus,
      IDX_XTERM_ExMouseUtf8,
      IDX_XTERM_ExMouseSgr,
      IDX_XTERM_ExMouseUrxvt,
      IDX_XTERM_ScrDnTTY,
      IDX_XTERM_ScrDnKey,
      IDX_XTERM_Meta8th,
      IDX_XTERM_AltMods,
      IDX_XTERM_MetaESC,
      IDX_XTERM_KpDelDEL,
      IDX_XTERM_AltESC,
      IDX_XTERM_KeepSelection,
      IDX_XTERM_Select2Clipboard,
      IDX_XTERM_BellUrgent,
      IDX_XTERM_BellPop,
      IDX_XTERM_AltScrBuff,
      IDX_XTERM_DECSC,
      IDX_XTERM_AltScrBuffSC,
      IDX_XTERM_FnTerminfo,
      IDX_XTERM_FnSun,
      IDX_XTERM_FnHp,
      IDX_XTERM_FnSco,
      IDX_XTERM_KeyboardX11R6,
      IDX_XTERM_KeyboardVT220,
      IDX_XTERM_BracketPaste,
      COUNT
    }

    // DECSET Number ‚©‚ç bit index ‚Ö‚Ì‘Î‰ž
    static readonly Gen::Dictionary<int,int> decbits=new Gen::Dictionary<int,int>();
    static void InitializeBitsMapping(){
      decbits[   1]=(int)DecsetBitIndex.IDX_DEC_CKM;
      decbits[   2]=(int)DecsetBitIndex.IDX_DEC_ANM;
      decbits[   3]=(int)DecsetBitIndex.IDX_DEC_COLM;
      decbits[   4]=(int)DecsetBitIndex.IDX_DEC_SCLM;
      decbits[   5]=(int)DecsetBitIndex.IDX_DEC_SCNM;
      decbits[   6]=(int)DecsetBitIndex.IDX_DEC_OM;
      decbits[   7]=(int)DecsetBitIndex.IDX_DEC_AWM;
      decbits[   8]=(int)DecsetBitIndex.IDX_DEC_ARM;
      decbits[   9]=(int)DecsetBitIndex.IDX_DEC_INLM;
      decbits[  10]=(int)DecsetBitIndex.IDX_DEC_SHOW_TOOLBAR;
      decbits[  12]=(int)DecsetBitIndex.IDX_DEC_BLINK_CURSOR;
      decbits[  18]=(int)DecsetBitIndex.IDX_DEC_PFF;
      decbits[  19]=(int)DecsetBitIndex.IDX_DEC_PEX;
      decbits[  25]=(int)DecsetBitIndex.IDX_DEC_TCEM;
      decbits[  30]=(int)DecsetBitIndex.IDX_DEC_SHOW_SCROLLBAR;
      decbits[  35]=(int)DecsetBitIndex.IDX_DEC_FONT_SHIFTING;
      decbits[  38]=(int)DecsetBitIndex.IDX_DEC_TEK;
      decbits[  40]=(int)DecsetBitIndex.IDX_DEC_ALLOW_80_132;
      decbits[  41]=(int)DecsetBitIndex.IDX_DEC_FIX_FOR_MORE;
      decbits[  42]=(int)DecsetBitIndex.IDX_DEC_NRCM;
      decbits[  44]=(int)DecsetBitIndex.IDX_DEC_MARGIN_BELL;
      decbits[  45]=(int)DecsetBitIndex.IDX_DEC_RWM;
      decbits[  46]=(int)DecsetBitIndex.IDX_DEC_LOGGING;
      decbits[  47]=(int)DecsetBitIndex.IDX_DEC_ALT_SCR_BUFF;
      decbits[  66]=(int)DecsetBitIndex.IDX_DEC_NKM;
      decbits[  67]=(int)DecsetBitIndex.IDX_DEC_BKM;

      decbits[  34]=(int)DecsetBitIndex.IDX_DEC_RLM;
      decbits[  35]=(int)DecsetBitIndex.IDX_DEC_HEBM;
      decbits[  36]=(int)DecsetBitIndex.IDX_DEC_HEM;
      decbits[  57]=(int)DecsetBitIndex.IDX_DEC_NAKB;
      decbits[  58]=(int)DecsetBitIndex.IDX_DEC_IPEM;
      decbits[  60]=(int)DecsetBitIndex.IDX_DEC_HCCM;
      decbits[  61]=(int)DecsetBitIndex.IDX_DEC_VCCM;
      decbits[  64]=(int)DecsetBitIndex.IDX_DEC_PCCM;
      decbits[  68]=(int)DecsetBitIndex.IDX_DEC_KBUM;
      decbits[  69]=(int)DecsetBitIndex.IDX_DEC_VSSM;
      decbits[  73]=(int)DecsetBitIndex.IDX_DEC_XRLM;
      decbits[  81]=(int)DecsetBitIndex.IDX_DEC_KPM;
      decbits[  95]=(int)DecsetBitIndex.IDX_DEC_NCSM;
      decbits[  96]=(int)DecsetBitIndex.IDX_DEC_RLCM;
      decbits[  97]=(int)DecsetBitIndex.IDX_DEC_RTSM;
      decbits[  98]=(int)DecsetBitIndex.IDX_DEC_ARSM;
      decbits[  99]=(int)DecsetBitIndex.IDX_DEC_MCM;
      decbits[ 100]=(int)DecsetBitIndex.IDX_DEC_AAM;
      decbits[ 101]=(int)DecsetBitIndex.IDX_DEC_CANSM;
      decbits[ 102]=(int)DecsetBitIndex.IDX_DEC_NULM;
      decbits[ 103]=(int)DecsetBitIndex.IDX_DEC_HDPXM;
      decbits[ 104]=(int)DecsetBitIndex.IDX_DEC_ESKM;
      decbits[ 106]=(int)DecsetBitIndex.IDX_DEC_OSCNM;
      decbits[ 108]=(int)DecsetBitIndex.IDX_DEC_NUMLK;
      decbits[ 109]=(int)DecsetBitIndex.IDX_DEC_CAPSLK;
      decbits[ 110]=(int)DecsetBitIndex.IDX_DEC_KLHIM;

      decbits[1000]=(int)DecsetBitIndex.IDX_XTERM_SendMouse;
      decbits[1001]=(int)DecsetBitIndex.IDX_XTERM_MMHilite;
      decbits[1002]=(int)DecsetBitIndex.IDX_XTERM_MMCell;
      decbits[1003]=(int)DecsetBitIndex.IDX_XTERM_MMAll;
      decbits[1004]=(int)DecsetBitIndex.IDX_XTERM_SendFocus;
      decbits[1005]=(int)DecsetBitIndex.IDX_XTERM_ExMouseUtf8;
      decbits[1006]=(int)DecsetBitIndex.IDX_XTERM_ExMouseSgr;
      decbits[1015]=(int)DecsetBitIndex.IDX_XTERM_ExMouseUrxvt;
      decbits[1010]=(int)DecsetBitIndex.IDX_XTERM_ScrDnTTY;
      decbits[1011]=(int)DecsetBitIndex.IDX_XTERM_ScrDnKey;
      decbits[1034]=(int)DecsetBitIndex.IDX_XTERM_Meta8th;
      decbits[1035]=(int)DecsetBitIndex.IDX_XTERM_AltMods;
      decbits[1036]=(int)DecsetBitIndex.IDX_XTERM_MetaESC;
      decbits[1037]=(int)DecsetBitIndex.IDX_XTERM_KpDelDEL;
      decbits[1039]=(int)DecsetBitIndex.IDX_XTERM_AltESC;
      decbits[1040]=(int)DecsetBitIndex.IDX_XTERM_KeepSelection;
      decbits[1041]=(int)DecsetBitIndex.IDX_XTERM_Select2Clipboard;
      decbits[1042]=(int)DecsetBitIndex.IDX_XTERM_BellUrgent;
      decbits[1043]=(int)DecsetBitIndex.IDX_XTERM_BellPop;
      decbits[1047]=(int)DecsetBitIndex.IDX_XTERM_AltScrBuff;
      decbits[1048]=(int)DecsetBitIndex.IDX_XTERM_DECSC;
      decbits[1049]=(int)DecsetBitIndex.IDX_XTERM_AltScrBuffSC;
      decbits[1050]=(int)DecsetBitIndex.IDX_XTERM_FnTerminfo;
      decbits[1051]=(int)DecsetBitIndex.IDX_XTERM_FnSun;
      decbits[1052]=(int)DecsetBitIndex.IDX_XTERM_FnHp;
      decbits[1053]=(int)DecsetBitIndex.IDX_XTERM_FnSco;
      decbits[1060]=(int)DecsetBitIndex.IDX_XTERM_KeyboardX11R6;
      decbits[1061]=(int)DecsetBitIndex.IDX_XTERM_KeyboardVT220;
      decbits[2004]=(int)DecsetBitIndex.IDX_XTERM_BracketPaste;
    }

    public bool DecCkm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_CKM];}
      set{
        if(this.DecCkm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_CKM]=value;
        term.OnChangedDecCkm(value);
      }
    }
    public bool DecAnm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_ANM];}
      set{
        if(this.DecAnm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_ANM]=value;
      }
    }
    public bool DecColm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_COLM];}
      set{
        if(this.DecColm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_COLM]=value;
      }
    }
    public bool DecSclm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_SCLM];}
      set{
        if(this.DecSclm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_SCLM]=value;
      }
    }
    public bool DecScnm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_SCNM];}
      set{
        if(this.DecScnm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_SCNM]=value;
      }
    }
    public bool DecOm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_OM];}
      set{
        if(this.DecOm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_OM]=value;
      }
    }
    public bool DecAwm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_AWM];}
      set{
        if(this.DecAwm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_AWM]=value;
      }
    }
    public bool DecArm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_ARM];}
      set{
        if(this.DecArm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_ARM]=value;
      }
    }
    public bool DecInlm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_INLM];}
      set{
        if(this.DecInlm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_INLM]=value;
      }
    }
    public bool DecShowToolbar{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_SHOW_TOOLBAR];}
      set{
        if(this.DecShowToolbar==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_SHOW_TOOLBAR]=value;
      }
    }
    public bool DecBlinkCursor{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_BLINK_CURSOR];}
      set{
        if(this.DecBlinkCursor==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_BLINK_CURSOR]=value;
      }
    }
    public bool DecPff{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_PFF];}
      set{
        if(this.DecPff==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_PFF]=value;
      }
    }
    public bool DecPex{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_PEX];}
      set{
        if(this.DecPex==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_PEX]=value;
      }
    }
    public bool DecTcem{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_TCEM];}
      set{
        if(this.DecTcem==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_TCEM]=value;
      }
    }
    public bool DecShowScrollbar{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_SHOW_SCROLLBAR];}
      set{
        if(this.DecShowScrollbar==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_SHOW_SCROLLBAR]=value;
      }
    }
    public bool DecFontShifting{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_FONT_SHIFTING];}
      set{
        if(this.DecFontShifting==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_FONT_SHIFTING]=value;
      }
    }
    public bool DecTek{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_TEK];}
      set{
        if(this.DecTek==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_TEK]=value;
      }
    }
    public bool DecAllow80_132{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_ALLOW_80_132];}
      set{
        if(this.DecAllow80_132==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_ALLOW_80_132]=value;
      }
    }
    public bool DecFixForMore{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_FIX_FOR_MORE];}
      set{
        if(this.DecFixForMore==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_FIX_FOR_MORE]=value;
      }
    }
    public bool DecNrcm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_NRCM];}
      set{
        if(this.DecNrcm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_NRCM]=value;
      }
    }
    public bool DecMarginBell{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_MARGIN_BELL];}
      set{
        if(this.DecMarginBell==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_MARGIN_BELL]=value;
      }
    }
    public bool DecRwm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_RWM];}
      set{
        if(this.DecRwm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_RWM]=value;
      }
    }
    public bool DecLogging{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_LOGGING];}
      set{
        if(this.DecLogging==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_LOGGING]=value;
      }
    }
    public bool DecAltScrBuff{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_ALT_SCR_BUFF];}
      set{
        if(this.DecAltScrBuff==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_ALT_SCR_BUFF]=value;
        term.OnChangedDecAltScrBuff(value);
      }
    }
    public bool DecNkm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_NKM];}
      set{
        if(this.DecNkm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_NKM]=value;
        term.OnChangedDecNkm(value);
      }
    }
    public bool DecBkm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_BKM];}
      set{
        if(this.DecBkm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_BKM]=value;
      }
    }

    public bool Rlm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_RLM];}
      set{
        if(this.Rlm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_RLM]=value;
      }
    }
    public bool Hebm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_HEBM];}
      set{
        if(this.Hebm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_HEBM]=value;
      }
    }
    public bool Hem{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_HEM];}
      set{
        if(this.Hem==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_HEM]=value;
      }
    }
    public bool Nakb{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_NAKB];}
      set{
        if(this.Nakb==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_NAKB]=value;
      }
    }
    public bool Ipem{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_IPEM];}
      set{
        if(this.Ipem==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_IPEM]=value;
      }
    }
    public bool Hccm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_HCCM];}
      set{
        if(this.Hccm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_HCCM]=value;
      }
    }
    public bool Vccm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_VCCM];}
      set{
        if(this.Vccm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_VCCM]=value;
      }
    }
    public bool Pccm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_PCCM];}
      set{
        if(this.Pccm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_PCCM]=value;
      }
    }
    public bool Kbum{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_KBUM];}
      set{
        if(this.Kbum==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_KBUM]=value;
      }
    }
    public bool Vssm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_VSSM];}
      set{
        if(this.Vssm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_VSSM]=value;
      }
    }
    public bool Xrlm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_XRLM];}
      set{
        if(this.Xrlm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_XRLM]=value;
      }
    }
    public bool Kpm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_KPM];}
      set{
        if(this.Kpm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_KPM]=value;
      }
    }
    public bool Ncsm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_NCSM];}
      set{
        if(this.Ncsm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_NCSM]=value;
      }
    }
    public bool Rlcm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_RLCM];}
      set{
        if(this.Rlcm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_RLCM]=value;
      }
    }
    public bool Rtsm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_RTSM];}
      set{
        if(this.Rtsm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_RTSM]=value;
      }
    }
    public bool Arsm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_ARSM];}
      set{
        if(this.Arsm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_ARSM]=value;
      }
    }
    public bool Mcm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_MCM];}
      set{
        if(this.Mcm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_MCM]=value;
      }
    }
    public bool Aam{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_AAM];}
      set{
        if(this.Aam==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_AAM]=value;
      }
    }
    public bool Cansm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_CANSM];}
      set{
        if(this.Cansm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_CANSM]=value;
      }
    }
    public bool Nulm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_NULM];}
      set{
        if(this.Nulm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_NULM]=value;
      }
    }
    public bool Hdpxm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_HDPXM];}
      set{
        if(this.Hdpxm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_HDPXM]=value;
      }
    }
    public bool Eskm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_ESKM];}
      set{
        if(this.Eskm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_ESKM]=value;
      }
    }
    public bool Oscnm{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_OSCNM];}
      set{
        if(this.Oscnm==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_OSCNM]=value;
      }
    }
    public bool Numlk{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_NUMLK];}
      set{
        if(this.Numlk==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_NUMLK]=value;
      }
    }
    public bool Capslk{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_CAPSLK];}
      set{
        if(this.Capslk==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_CAPSLK]=value;
      }
    }
    public bool Klhim{
      get{return bits[(int)DecsetBitIndex.IDX_DEC_KLHIM];}
      set{
        if(this.Klhim==value)return;
        bits[(int)DecsetBitIndex.IDX_DEC_KLHIM]=value;
      }
    }

    public bool XtermSendMouse{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_SendMouse];}
      set{
        if(this.XtermSendMouse==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_SendMouse]=value;
      }
    }
    public bool XtermMMHilite{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_MMHilite];}
      set{
        if(this.XtermMMHilite==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_MMHilite]=value;
      }
    }
    public bool XtermMMCell{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_MMCell];}
      set{
        if(this.XtermMMCell==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_MMCell]=value;
      }
    }
    public bool XtermMMAll{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_MMAll];}
      set{
        if(this.XtermMMAll==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_MMAll]=value;
      }
    }
    public bool XtermSendFocus{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_SendFocus];}
      set{
        if(this.XtermSendFocus==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_SendFocus]=value;
      }
    }
    public bool XtermExMouseUtf8{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_ExMouseUtf8];}
      set{
        if(this.XtermExMouseUtf8==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_ExMouseUtf8]=value;
      }
    }
    public bool XtermExMouseSgr{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_ExMouseSgr];}
      set{
        if(this.XtermExMouseSgr==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_ExMouseSgr]=value;
      }
    }
    public bool XtermExMouseUrxvt{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_ExMouseUrxvt];}
      set{
        if(this.XtermExMouseUrxvt==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_ExMouseUrxvt]=value;
      }
    }
    public bool XtermScrDnTTY{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_ScrDnTTY];}
      set{
        if(this.XtermScrDnTTY==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_ScrDnTTY]=value;
      }
    }
    public bool XtermScrDnKey{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_ScrDnKey];}
      set{
        if(this.XtermScrDnKey==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_ScrDnKey]=value;
      }
    }
    public bool XtermMeta8th{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_Meta8th];}
      set{
        if(this.XtermMeta8th==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_Meta8th]=value;
      }
    }
    public bool XtermAltMods{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_AltMods];}
      set{
        if(this.XtermAltMods==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_AltMods]=value;
      }
    }
    public bool XtermMetaESC{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_MetaESC];}
      set{
        if(this.XtermMetaESC==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_MetaESC]=value;
      }
    }
    public bool XtermKpDelDEL{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_KpDelDEL];}
      set{
        if(this.XtermKpDelDEL==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_KpDelDEL]=value;
      }
    }
    public bool XtermAltESC{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_AltESC];}
      set{
        if(this.XtermAltESC==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_AltESC]=value;
      }
    }
    public bool XtermKeepSelection{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_KeepSelection];}
      set{
        if(this.XtermKeepSelection==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_KeepSelection]=value;
      }
    }
    public bool XtermSelect2Clipboard{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_Select2Clipboard];}
      set{
        if(this.XtermSelect2Clipboard==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_Select2Clipboard]=value;
      }
    }
    public bool XtermBellUrgent{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_BellUrgent];}
      set{
        if(this.XtermBellUrgent==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_BellUrgent]=value;
      }
    }
    public bool XtermBellPop{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_BellPop];}
      set{
        if(this.XtermBellPop==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_BellPop]=value;
      }
    }
    public bool XtermAltScrBuff{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_AltScrBuff];}
      set{
        if(this.XtermAltScrBuff==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_AltScrBuff]=value;
        term.OnChangedXtermAltScrBuff(value);
      }
    }
    public bool XtermDECSC{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_DECSC];}
      set{
        if(this.XtermDECSC==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_DECSC]=value;
        term.OnChangedXtermDECSC(value);
      }
    }
    public bool XtermAltScrBuffSC{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_AltScrBuffSC];}
      set{
        if(this.XtermAltScrBuffSC==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_AltScrBuffSC]=value;
        term.OnChangedXtermAltScrBuffSC(value);
      }
    }
    public bool XtermFnTerminfo{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_FnTerminfo];}
      set{
        if(this.XtermFnTerminfo==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_FnTerminfo]=value;
      }
    }
    public bool XtermFnSun{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_FnSun];}
      set{
        if(this.XtermFnSun==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_FnSun]=value;
      }
    }
    public bool XtermFnHp{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_FnHp];}
      set{
        if(this.XtermFnHp==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_FnHp]=value;
      }
    }
    public bool XtermFnSco{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_FnSco];}
      set{
        if(this.XtermFnSco==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_FnSco]=value;
      }
    }
    public bool XtermKeyboardX11R6{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_KeyboardX11R6];}
      set{
        if(this.XtermKeyboardX11R6==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_KeyboardX11R6]=value;
      }
    }
    public bool XtermKeyboardVT220{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_KeyboardVT220];}
      set{
        if(this.XtermKeyboardVT220==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_KeyboardVT220]=value;
      }
    }
    public bool XtermBracketPaste{
      get{return bits[(int)DecsetBitIndex.IDX_XTERM_BracketPaste];}
      set{
        if(this.XtermBracketPaste==value)return;
        bits[(int)DecsetBitIndex.IDX_XTERM_BracketPaste]=value;
      }
    }

    //**************************************************************************

    static DecsetBitIndex Code2DecsetBitIndex(int code){
      int s;
      if(decbits.TryGetValue(code,out s)){
        return (DecsetBitIndex)s;
      }else{
        return (DecsetBitIndex)0;
      }
    }
    //**************************************************************************
    //  DEC Set/Reset/Toggle
    //==========================================================================
    public bool DecSet(int code){
      switch(code){
        case    1: /* DEC_CKM */
          this.DecCkm=true;
          return true;
        case   47: /* DEC_ALT_SCR_BUFF */
          this.DecAltScrBuff=true;
          return true;
        case   66: /* DEC_NKM */
          this.DecNkm=true;
          return true;


        case 1047: /* XTERM_AltScrBuff */
          this.XtermAltScrBuff=true;
          return true;
        case 1048: /* XTERM_DECSC */
          this.XtermDECSC=true;
          return true;
        case 1049: /* XTERM_AltScrBuffSC */
          this.XtermAltScrBuffSC=true;
          return true;
        default:{
          int index=(int)Code2DecsetBitIndex(code);
          if(index!=0){
            this.bits[index]=true;
            return true;
          }
          return false;
        }
      }
    }
    public bool DecReset(int code){
      switch(code){
        case    1: /* DEC_CKM */
          this.DecCkm=false;
          return true;
        case   47: /* DEC_ALT_SCR_BUFF */
          this.DecAltScrBuff=false;
          return true;
        case   66: /* DEC_NKM */
          this.DecNkm=false;
          return true;


        case 1047: /* XTERM_AltScrBuff */
          this.XtermAltScrBuff=false;
          return true;
        case 1048: /* XTERM_DECSC */
          this.XtermDECSC=false;
          return true;
        case 1049: /* XTERM_AltScrBuffSC */
          this.XtermAltScrBuffSC=false;
          return true;
        default:{
          int index=(int)Code2DecsetBitIndex(code);
          if(index!=0){
            this.bits[index]=false;
            return true;
          }
          return false;
        }
      }
    }
    public bool DecToggle(int code){
      switch(code){
        case    1: /* DEC_CKM */
          this.DecCkm=!this.DecCkm;
          return true;
        case   47: /* DEC_ALT_SCR_BUFF */
          this.DecAltScrBuff=!this.DecAltScrBuff;
          return true;
        case   66: /* DEC_NKM */
          this.DecNkm=!this.DecNkm;
          return true;


        case 1047: /* XTERM_AltScrBuff */
          this.XtermAltScrBuff=!this.XtermAltScrBuff;
          return true;
        case 1048: /* XTERM_DECSC */
          this.XtermDECSC=!this.XtermDECSC;
          return true;
        case 1049: /* XTERM_AltScrBuffSC */
          this.XtermAltScrBuffSC=!this.XtermAltScrBuffSC;
          return true;
        default:{
          int index=(int)Code2DecsetBitIndex(code);
          if(index!=0){
            this.bits[index]=!this.bits[index];
            return true;
          }
          return false;
        }
      }
    }

  }
}





