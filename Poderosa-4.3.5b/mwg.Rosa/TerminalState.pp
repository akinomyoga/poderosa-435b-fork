// -*- mode:csharp -*-
using Gen=System.Collections.Generic;

namespace mwg.RosaTerm{
  partial class TerminalState{
    //**************************************************************************
#pragma%m ppDecsetFlags (
#pragma%%x line .r/%decnum%/   1/.r/%NAME%/DEC_CKM/                .r/%Name%/DecCkm/           .r/%Event%/1/
#pragma%%x line .r/%decnum%/   2/.r/%NAME%/DEC_ANM/                .r/%Name%/DecAnm/           .r/%Event%/0/
#pragma%%x line .r/%decnum%/   3/.r/%NAME%/DEC_COLM/               .r/%Name%/DecColm/          .r/%Event%/0/
#pragma%%x line .r/%decnum%/   4/.r/%NAME%/DEC_SCLM/               .r/%Name%/DecSclm/          .r/%Event%/0/
#pragma%%x line .r/%decnum%/   5/.r/%NAME%/DEC_SCNM/               .r/%Name%/DecScnm/          .r/%Event%/0/
#pragma%%x line .r/%decnum%/   6/.r/%NAME%/DEC_OM/                 .r/%Name%/DecOm/            .r/%Event%/0/
#pragma%%x line .r/%decnum%/   7/.r/%NAME%/DEC_AWM/                .r/%Name%/DecAwm/           .r/%Event%/0/
#pragma%%x line .r/%decnum%/   8/.r/%NAME%/DEC_ARM/                .r/%Name%/DecArm/           .r/%Event%/0/
#pragma%%x line .r/%decnum%/   9/.r/%NAME%/DEC_INLM/               .r/%Name%/DecInlm/          .r/%Event%/0/
#pragma%%x line .r/%decnum%/  10/.r/%NAME%/DEC_SHOW_TOOLBAR/       .r/%Name%/DecShowToolbar/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  12/.r/%NAME%/DEC_BLINK_CURSOR/       .r/%Name%/DecBlinkCursor/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  18/.r/%NAME%/DEC_PFF/                .r/%Name%/DecPff/           .r/%Event%/0/
#pragma%%x line .r/%decnum%/  19/.r/%NAME%/DEC_PEX/                .r/%Name%/DecPex/           .r/%Event%/0/
#pragma%%x line .r/%decnum%/  25/.r/%NAME%/DEC_TCEM/               .r/%Name%/DecTcem/          .r/%Event%/0/
#pragma%%x line .r/%decnum%/  30/.r/%NAME%/DEC_SHOW_SCROLLBAR/     .r/%Name%/DecShowScrollbar/ .r/%Event%/0/
#pragma%%x line .r/%decnum%/  35/.r/%NAME%/DEC_FONT_SHIFTING/      .r/%Name%/DecFontShifting/  .r/%Event%/0/
#pragma%%x line .r/%decnum%/  38/.r/%NAME%/DEC_TEK/                .r/%Name%/DecTek/           .r/%Event%/0/
#pragma%%x line .r/%decnum%/  40/.r/%NAME%/DEC_ALLOW_80_132/       .r/%Name%/DecAllow80_132/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  41/.r/%NAME%/DEC_FIX_FOR_MORE/       .r/%Name%/DecFixForMore/    .r/%Event%/0/
#pragma%%x line .r/%decnum%/  42/.r/%NAME%/DEC_NRCM/               .r/%Name%/DecNrcm/          .r/%Event%/0/
#pragma%%x line .r/%decnum%/  44/.r/%NAME%/DEC_MARGIN_BELL/        .r/%Name%/DecMarginBell/    .r/%Event%/0/
#pragma%%x line .r/%decnum%/  45/.r/%NAME%/DEC_RWM/                .r/%Name%/DecRwm/           .r/%Event%/0/
#pragma%%x line .r/%decnum%/  46/.r/%NAME%/DEC_LOGGING/            .r/%Name%/DecLogging/       .r/%Event%/0/
#pragma%%x line .r/%decnum%/  47/.r/%NAME%/DEC_ALT_SCR_BUFF/       .r/%Name%/DecAltScrBuff/    .r/%Event%/1/
#pragma%%x line .r/%decnum%/  66/.r/%NAME%/DEC_NKM/                .r/%Name%/DecNkm/           .r/%Event%/1/
#pragma%%x line .r/%decnum%/  67/.r/%NAME%/DEC_BKM/                .r/%Name%/DecBkm/           .r/%Event%/0/

#pragma%%x line .r/%decnum%/  34/.r/%NAME%/DEC_RLM/                .r/%Name%/Rlm/    .r/%Event%/0/
#pragma%(
    // Å´ DEC_HEBM: î‘çÜÇ™ Font Shifting Ç∆èdï°
#pragma%)
#pragma%%x line .r/%decnum%/  35/.r/%NAME%/DEC_HEBM/               .r/%Name%/Hebm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  36/.r/%NAME%/DEC_HEM/                .r/%Name%/Hem/    .r/%Event%/0/
#pragma%%x line .r/%decnum%/  57/.r/%NAME%/DEC_NAKB/               .r/%Name%/Nakb/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  58/.r/%NAME%/DEC_IPEM/               .r/%Name%/Ipem/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  60/.r/%NAME%/DEC_HCCM/               .r/%Name%/Hccm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  61/.r/%NAME%/DEC_VCCM/               .r/%Name%/Vccm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  64/.r/%NAME%/DEC_PCCM/               .r/%Name%/Pccm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  68/.r/%NAME%/DEC_KBUM/               .r/%Name%/Kbum/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  69/.r/%NAME%/DEC_VSSM/               .r/%Name%/Vssm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  73/.r/%NAME%/DEC_XRLM/               .r/%Name%/Xrlm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  81/.r/%NAME%/DEC_KPM/                .r/%Name%/Kpm/    .r/%Event%/0/
#pragma%%x line .r/%decnum%/  95/.r/%NAME%/DEC_NCSM/               .r/%Name%/Ncsm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  96/.r/%NAME%/DEC_RLCM/               .r/%Name%/Rlcm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  97/.r/%NAME%/DEC_RTSM/               .r/%Name%/Rtsm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  98/.r/%NAME%/DEC_ARSM/               .r/%Name%/Arsm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/  99/.r/%NAME%/DEC_MCM/                .r/%Name%/Mcm/    .r/%Event%/0/
#pragma%%x line .r/%decnum%/ 100/.r/%NAME%/DEC_AAM/                .r/%Name%/Aam/    .r/%Event%/0/
#pragma%%x line .r/%decnum%/ 101/.r/%NAME%/DEC_CANSM/              .r/%Name%/Cansm/  .r/%Event%/0/
#pragma%%x line .r/%decnum%/ 102/.r/%NAME%/DEC_NULM/               .r/%Name%/Nulm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/ 103/.r/%NAME%/DEC_HDPXM/              .r/%Name%/Hdpxm/  .r/%Event%/0/
#pragma%%x line .r/%decnum%/ 104/.r/%NAME%/DEC_ESKM/               .r/%Name%/Eskm/   .r/%Event%/0/
#pragma%%x line .r/%decnum%/ 106/.r/%NAME%/DEC_OSCNM/              .r/%Name%/Oscnm/  .r/%Event%/0/
#pragma%%x line .r/%decnum%/ 108/.r/%NAME%/DEC_NUMLK/              .r/%Name%/Numlk/  .r/%Event%/0/
#pragma%%x line .r/%decnum%/ 109/.r/%NAME%/DEC_CAPSLK/             .r/%Name%/Capslk/ .r/%Event%/0/
#pragma%%x line .r/%decnum%/ 110/.r/%NAME%/DEC_KLHIM/              .r/%Name%/Klhim/  .r/%Event%/0/

#pragma%%x line .r/%decnum%/1000/.r/%NAME%/XTERM_SendMouse/        .r/%Name%/XtermSendMouse/        .r/%Event%/0/
#pragma%%x line .r/%decnum%/1001/.r/%NAME%/XTERM_MMHilite/         .r/%Name%/XtermMMHilite/         .r/%Event%/0/
#pragma%%x line .r/%decnum%/1002/.r/%NAME%/XTERM_MMCell/           .r/%Name%/XtermMMCell/           .r/%Event%/0/
#pragma%%x line .r/%decnum%/1003/.r/%NAME%/XTERM_MMAll/            .r/%Name%/XtermMMAll/            .r/%Event%/0/
#pragma%%x line .r/%decnum%/1004/.r/%NAME%/XTERM_SendFocus/        .r/%Name%/XtermSendFocus/        .r/%Event%/0/
#pragma%%x line .r/%decnum%/1005/.r/%NAME%/XTERM_ExMouseUtf8/      .r/%Name%/XtermExMouseUtf8/      .r/%Event%/0/
#pragma%%x line .r/%decnum%/1006/.r/%NAME%/XTERM_ExMouseSgr/       .r/%Name%/XtermExMouseSgr/       .r/%Event%/0/
#pragma%%x line .r/%decnum%/1015/.r/%NAME%/XTERM_ExMouseUrxvt/     .r/%Name%/XtermExMouseUrxvt/     .r/%Event%/0/
#pragma%%x line .r/%decnum%/1010/.r/%NAME%/XTERM_ScrDnTTY/         .r/%Name%/XtermScrDnTTY/         .r/%Event%/0/
#pragma%%x line .r/%decnum%/1011/.r/%NAME%/XTERM_ScrDnKey/         .r/%Name%/XtermScrDnKey/         .r/%Event%/0/
#pragma%%x line .r/%decnum%/1034/.r/%NAME%/XTERM_Meta8th/          .r/%Name%/XtermMeta8th/          .r/%Event%/0/
#pragma%%x line .r/%decnum%/1035/.r/%NAME%/XTERM_AltMods/          .r/%Name%/XtermAltMods/          .r/%Event%/0/
#pragma%%x line .r/%decnum%/1036/.r/%NAME%/XTERM_MetaESC/          .r/%Name%/XtermMetaESC/          .r/%Event%/0/
#pragma%%x line .r/%decnum%/1037/.r/%NAME%/XTERM_KpDelDEL/         .r/%Name%/XtermKpDelDEL/         .r/%Event%/0/
#pragma%%x line .r/%decnum%/1039/.r/%NAME%/XTERM_AltESC/           .r/%Name%/XtermAltESC/           .r/%Event%/0/
#pragma%%x line .r/%decnum%/1040/.r/%NAME%/XTERM_KeepSelection/    .r/%Name%/XtermKeepSelection/    .r/%Event%/0/
#pragma%%x line .r/%decnum%/1041/.r/%NAME%/XTERM_Select2Clipboard/ .r/%Name%/XtermSelect2Clipboard/ .r/%Event%/0/
#pragma%%x line .r/%decnum%/1042/.r/%NAME%/XTERM_BellUrgent/       .r/%Name%/XtermBellUrgent/       .r/%Event%/0/
#pragma%%x line .r/%decnum%/1043/.r/%NAME%/XTERM_BellPop/          .r/%Name%/XtermBellPop/          .r/%Event%/0/
#pragma%%x line .r/%decnum%/1047/.r/%NAME%/XTERM_AltScrBuff/       .r/%Name%/XtermAltScrBuff/       .r/%Event%/1/
#pragma%%x line .r/%decnum%/1048/.r/%NAME%/XTERM_DECSC/            .r/%Name%/XtermDECSC/            .r/%Event%/1/
#pragma%%x line .r/%decnum%/1049/.r/%NAME%/XTERM_AltScrBuffSC/     .r/%Name%/XtermAltScrBuffSC/     .r/%Event%/1/
#pragma%%x line .r/%decnum%/1050/.r/%NAME%/XTERM_FnTerminfo/       .r/%Name%/XtermFnTerminfo/       .r/%Event%/0/
#pragma%%x line .r/%decnum%/1051/.r/%NAME%/XTERM_FnSun/            .r/%Name%/XtermFnSun/            .r/%Event%/0/
#pragma%%x line .r/%decnum%/1052/.r/%NAME%/XTERM_FnHp/             .r/%Name%/XtermFnHp/             .r/%Event%/0/
#pragma%%x line .r/%decnum%/1053/.r/%NAME%/XTERM_FnSco/            .r/%Name%/XtermFnSco/            .r/%Event%/0/
#pragma%%x line .r/%decnum%/1060/.r/%NAME%/XTERM_KeyboardX11R6/    .r/%Name%/XtermKeyboardX11R6/    .r/%Event%/0/
#pragma%%x line .r/%decnum%/1061/.r/%NAME%/XTERM_KeyboardVT220/    .r/%Name%/XtermKeyboardVT220/    .r/%Event%/0/
#pragma%%x line .r/%decnum%/2004/.r/%NAME%/XTERM_BracketPaste/     .r/%Name%/XtermBracketPaste/     .r/%Event%/0/
#pragma%)

    enum DecsetBitIndex{
#pragma%m line (
      IDX_%NAME%,
#pragma%)
#pragma%x ppDecsetFlags
      COUNT
    }

    // DECSET Number Ç©ÇÁ bit index Ç÷ÇÃëŒâû
    static readonly Gen::Dictionary<int,int> decbits=new Gen::Dictionary<int,int>();
    static void InitializeBitsMapping(){
#pragma%m line (
      decbits[%decnum%]=(int)DecsetBitIndex.IDX_%NAME%;
#pragma%)
#pragma%x ppDecsetFlags
    }

#pragma%m line (
    public bool %Name%{
      get{return bits[(int)DecsetBitIndex.IDX_%NAME%];}
      set{
        if(this.%Name%==value)return;
        bits[(int)DecsetBitIndex.IDX_%NAME%]=value;
#pragma%%if %Event% (
        term.OnChanged%Name%(value);
#pragma%%)
      }
    }
#pragma%)
#pragma%x ppDecsetFlags

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
#pragma%m line (
#pragma%%if %Event% (
        case %decnum%: /* %NAME% */
          this.%Name%=true;
          return true;
#pragma%%)
#pragma%)
#pragma%x ppDecsetFlags
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
#pragma%m line (
#pragma%%if %Event% (
        case %decnum%: /* %NAME% */
          this.%Name%=false;
          return true;
#pragma%%)
#pragma%)
#pragma%x ppDecsetFlags
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
#pragma%m line (
#pragma%%if %Event% (
        case %decnum%: /* %NAME% */
          this.%Name%=!this.%Name%;
          return true;
#pragma%%)
#pragma%)
#pragma%x ppDecsetFlags
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





