using Gen=System.Collections.Generic;
using Gdi=System.Drawing;
using BitVector32=System.Collections.Specialized.BitVector32;

using Poderosa.Terminal;
using Poderosa.Document;
using LineFeedRule=Poderosa.ConnectionParam.LineFeedRule;

using GUtil=Poderosa.GUtil;
using Keys=System.Windows.Forms.Keys;
using mwg.RosaTerm.Utils;

namespace mwg.RosaTerm{

  class RosaTerminal:Poderosa.Terminal.AbstractTerminal{
    readonly TerminalState tstat;
    readonly EscapeSequenceCutter cutter=new EscapeSequenceCutter();

    #region ModalTask って何?
    IModalCharacterTask currentCharacterTask;
    public override void StartModalTerminalTask(IModalTerminalTask task) {
      base.StartModalTerminalTask(task);
      this.currentCharacterTask=(IModalCharacterTask)task.GetAdapter(typeof(IModalCharacterTask));
    }
    public override void EndModalTerminalTask() {
      base.EndModalTerminalTask();
      this.currentCharacterTask=null;
    }
    #endregion

    protected override void ResetInternal(){
      this.cutter.Clear();
      // TODO: 他にクリアする事?
    }
    
    // abstract: protected override void ChangeMode(Poderosa.Terminal.TerminalMode tm);

    public RosaTerminal(Poderosa.Terminal.TerminalInitializeInfo info):base(info){
      this.tstat=new TerminalState(this);
      this.tstat.EmulationType=info.Session.TerminalSettings.TerminalType;
      this.tabstops.InitTabs(8,System.Math.Max(this.GetDocument().TerminalWidth,400));

    }
    static RosaTerminal(){
      InitializeCSIHandlers();
    }

    //==========================================================================
    //  プログラム→端末
    //--------------------------------------------------------------------------
    public override void ProcessChar(char ch){
      switch(cutter.ProcessChar(ch)){
        case EscapeSequenceCutter.EscapeState.Normal:
          if(currentCharacterTask!=null)
            currentCharacterTask.ProcessChar(ch);
          this.LogService.XmlLogger.Write(ch);
          ProcessNormalChar(ch);
          break;
        case EscapeSequenceCutter.EscapeState.Control:
          if(currentCharacterTask!=null)
            currentCharacterTask.ProcessChar(ch);
          this.LogService.XmlLogger.Write(ch);
          ProcessControlChar(ch);
          break;
        case EscapeSequenceCutter.EscapeState.CSI:
          ProcessCSISequence();
          break;
        case EscapeSequenceCutter.EscapeState.Escape:
          ProcessEscSequence();
          break;
        case EscapeSequenceCutter.EscapeState.OSC:
          ProcessOSCSequence();
          break;
        case EscapeSequenceCutter.EscapeState.PM:
          ProcessPMSequence();
          break;
        case EscapeSequenceCutter.EscapeState.APC:
          ProcessAPCSequence();
          break;
      }
    }

    protected void ProcessOSCSequence(){
      if(cutter.CSIArguments.Length==1){
        switch(cutter.CSIArguments[0]){
          case 0:
          case 1:
          case 2:
            GetDocument().Caption=cutter.Content;
            return;
          default:
            this.ReportEscapeSequenceError(string.Format("Unknown OSC code {0}",cutter.CSIArguments[0]));
            return;
        }
      }
      this.ReportEscapeSequenceError(string.Format(
        "Unknown OSC sequence {0};{1}",
        cutter.CSIArguments,cutter.Content));
    }
    protected void ProcessPMSequence(){}
    protected void ProcessAPCSequence(){}

    // エスケープシーケンスが来るたびに try-catch を張るよりも、
    // エラーになった時にだけ報告する様にした方が速い…筈。
    // 抑もエスケープシーケンスはそんなに来ないと思うが。
    protected void ReportEscapeSequenceError(string msg){
      CharDecodeError(Poderosa.GEnv.Strings.GetString("Message.EscapesequenceTerminal.UnsupportedSequence")+msg);
      Poderosa.RuntimeUtil.SilentReportException(new UnknownEscapeSequenceException(msg));
    }

    //**************************************************************************
    //  ESC Sequences
    //==========================================================================
    protected void ProcessEscSequence(){
      switch(cutter.Content){
        case "=":ProcDECKPAM(this);break;
        case ">":ProcDECKPNM(this);break;
        case "7":ProcDECSC(this);break;
        case "8":ProcDECRC(this);break;
        case "c":ProcRIS(this);break;
        case "F":ProcHpLowerLeftBug(this);break;

        case " F": // ToDo: S7C1T C1 制御文字を 7bit 文字で送る (CSI を ESC [ と送信するなど) 
        case " G": // ToDo: S8C1T C1 制御文字を 8bit 文字で送る (CSI を直接送る)
          // 備考 受信は 7/8ビットコントロールは常に両方をサポートしている。
        case " L": // 無視? [cf VT100は最初からOK]
          break;

        // control character (see also ProcessControlChar)
        case "H":ProcHTS(this);break;
        case "D":ProcIND(this);break;
        case "M":ProcRI(this);break;
        case "E":ProcNEL(this);break;
        default:
          ReportEscapeSequenceError(string.Format("Unknown ESC seq '{0}'",cutter.Content));
          break;
      }
      /*
       * VT100
       * \eE
       */
    }
    //--------------------------------------------------------------------------
    // ESC 7: Save Cursor
    int ProcDECSC_line=0;
    int ProcDECSC_col=0;
    static void ProcDECSC(RosaTerminal t){
      TerminalDocument doc=t.GetDocument();
      t.ProcDECSC_line=doc.CurrentLineNumber-doc.TopLineNumber;
      t.ProcDECSC_col =t._manipulator.CaretColumn;
    }
    // ESC 8: Restore Cursor
    static void ProcDECRC(RosaTerminal t){
      TerminalDocument doc=t.GetDocument();
      doc.ReplaceCurrentLine(t._manipulator.Export());
      doc.CurrentLineNumber=doc.TopLineNumber+t.ProcDECSC_line;
      t._manipulator.Load(doc.CurrentLine,t.ProcDECSC_col);
    }
    //--------------------------------------------------------------------------
    // ESC F: Hp Lower Left Bug
    static void ProcHpLowerLeftBug(RosaTerminal t){
      t.MoveCursorTo(t.GetDocument().TerminalHeight,1);
    }
    // ESC c: Full Reset
    static void ProcRIS(RosaTerminal t){
      t.FullReset();
    }
    //--------------------------------------------------------------------------
    #region 突貫工事
    //**************************************************************************
    protected void ProcessNormalChar(char c){
      //TODO: リファクタリング (Xterm overrided functions も考慮)

      TerminalDocument doc=GetDocument();
      int tw = doc.TerminalWidth;

      if(!this.tstat.DecAwm&&_manipulator.CaretColumn+1>=tw)return;

      // 挿入モードの時は、予め余白を作っておく
      // TODO: 挿入によって右端からはみ出た部分はどうなるのか?
      if(this.tstat.SmIrm)
        _manipulator.InsertBlanks(_manipulator.CaretColumn,GLine.CalcDisplayLength(c),_currentdecoration);

      //既に画面右端にキャレットがあるのに文字が来たら改行をする
      if(tstat.DecAwm&&_manipulator.CaretColumn+GLine.CalcDisplayLength(c)>tw)
        ProcessNormalChar_Wrap();

      //画面のリサイズがあったときは、_manipulatorのバッファサイズが不足の可能性がある
      if(tw > _manipulator.BufferSize)
        _manipulator.ExpandBuffer(tw);

      //通常文字の処理
      _manipulator.PutChar(c, _currentdecoration);

      // 一番右端にいたら、新しい文字が来なくても、下の行へ移動する
      // これがないと、cygwin emacs/vi で表示が崩れる (行がずれる)
      // cf terminfo の xenl capability
      if(!tstat.TerminfoXenlCap&&tstat.DecAwm&&_manipulator.CaretColumn>=tw)
          ProcessNormalChar_Wrap();
    }
    void ProcessNormalChar_Wrap(){
      TerminalDocument doc=GetDocument();
      GLine l = _manipulator.Export();
      l.EOLType = EOLType.Continue;
      this.LogService.TextLogger.WriteLine(l); //ログに行をcommit
      doc.ReplaceCurrentLine(l);
      doc.LineFeed();
      _manipulator.Load(doc.CurrentLine, 0);
    }
    //**************************************************************************
    protected void ProcessControlChar(char c){
      // TODO:
      switch((EscapeSequenceCutter.ControlChars)c){
        case EscapeSequenceCutter.ControlChars.NUL:
          // 無視
          break;
        case EscapeSequenceCutter.ControlChars.BEL:
          this.IndicateBell();
          break;
        case EscapeSequenceCutter.ControlChars.CR:{
          LineFeedRule rule = GetTerminalSettings().LineFeedRule;
          switch(rule){
            case LineFeedRule.Normal:
              ProcCR(this);
              break;
            case LineFeedRule.CROnly:
              ProcCR(this);
              ProcLF(this);
              break;
          }
          break;
        }
        case EscapeSequenceCutter.ControlChars.BS:
          ProcBS(this);
          break;
        case EscapeSequenceCutter.ControlChars.HT:
          ProcHT(this);
          break;
        case EscapeSequenceCutter.ControlChars.LF:
        case EscapeSequenceCutter.ControlChars.VT:
        case EscapeSequenceCutter.ControlChars.FF:{
          LineFeedRule rule = GetTerminalSettings().LineFeedRule;
          switch(rule){
            case LineFeedRule.Normal:
              ProcLF(this);
              break;
            case LineFeedRule.CROnly:
              ProcCR(this);
              ProcLF(this);
              break;
          }
          break;
        }
        case EscapeSequenceCutter.ControlChars.HTS:ProcHTS(this);break; // \eH
        case EscapeSequenceCutter.ControlChars.IND:ProcIND(this);break; // \eD
        case EscapeSequenceCutter.ControlChars.RI: ProcRI(this); break; // \eM
        case EscapeSequenceCutter.ControlChars.NEL:ProcNEL(this);break; // \eE
      }
    }

    static void ProcLF(RosaTerminal t){
      //TODO: リファクタリング
      GLine nl = t._manipulator.Export();
      nl.EOLType = (nl.EOLType==EOLType.CR || nl.EOLType==EOLType.CRLF)? EOLType.CRLF : EOLType.LF;
      t.LogService.TextLogger.WriteLine(nl); //ログに行をcommit
      t.GetDocument().ReplaceCurrentLine(nl);
      t.GetDocument().LineFeed();
        
      //カラム保持は必要。サンプル:linuxconf.log
      int col = t._manipulator.CaretColumn;
      t._manipulator.Load(t.GetDocument().CurrentLine, col);
    }
    static void ProcCR(RosaTerminal t){
      t._manipulator.CarriageReturn();
    }
    static void ProcBS(RosaTerminal t){
      //TODO: リファクタリング
      if(t._manipulator.CaretColumn==0) {
        TerminalDocument doc = t.GetDocument();
        int line = doc.CurrentLineNumber-1;
        if(line>=0 && doc.FindLineOrEdge(line).EOLType==EOLType.Continue) {
          doc.InvalidatedRegion.InvalidateLine(doc.CurrentLineNumber);
          doc.CurrentLineNumber = line;
          if(doc.CurrentLine==null)
            t._manipulator.Clear(doc.TerminalWidth);
          else
            t._manipulator.Load(doc.CurrentLine, doc.CurrentLine.DisplayLength-1); //NOTE ここはCharLengthだったが同じだと思って改名した
          doc.InvalidatedRegion.InvalidateLine(doc.CurrentLineNumber);
        }
      }
      else
        t._manipulator.BackCaret();
    }
    static void ProcHT(RosaTerminal t){
      int c=t.tabstops.GetForwardTabStop(t._manipulator.CaretColumn,1);
      t._manipulator.CaretColumn=c;
    }
    #endregion

    #region CSI Handlers
    //**************************************************************************
    //  CSI Sequences
    //==========================================================================
    public delegate void CSIHandler(RosaTerminal t);

    static readonly Gen::Dictionary<string,CSIHandler> csiHandlers
      =new Gen::Dictionary<string,CSIHandler>();
    static void InitializeCSIHandlers(){
      csiHandlers["c" ]=ProcPDA;
      csiHandlers[">c"]=ProcSDA;
      csiHandlers["h" ]=ProcSM;
      csiHandlers["l" ]=ProcRM;
      csiHandlers["?h"]=ProcDECSET;
      csiHandlers["?l"]=ProcDECRST;
      csiHandlers["?t"]=ProcDECToggle;
      csiHandlers["?s"]=delegate(RosaTerminal t){t.tstat.SaveDec();};
      csiHandlers["?r"]=delegate(RosaTerminal t){t.tstat.RestoreDec();};

      csiHandlers["r" ]=ProcDECSTBM;
      csiHandlers["n" ]=ProcDSR;

      csiHandlers["A" ]=ProcCUU;
      csiHandlers["e" ]=ProcCUU;
      csiHandlers["B" ]=ProcCUD;
      csiHandlers["C" ]=ProcCUF;
      csiHandlers["a" ]=ProcCUF;
      csiHandlers["D" ]=ProcCUB;
      csiHandlers["E" ]=ProcCNL;
      csiHandlers["F" ]=ProcCPL;
      csiHandlers["G" ]=ProcHPA;
      //csiHandlers["'" ]=ProcHPA; // ' は終端文字でない。 (rxvt のマニュアルが違う?)
      csiHandlers["`" ]=ProcHPA;
      csiHandlers["d" ]=ProcVPA;
      csiHandlers["H" ]=ProcCUP;
      csiHandlers["f" ]=ProcCUP;

      csiHandlers["@" ]=ProcICH;
      csiHandlers["X" ]=ProcECH;
      csiHandlers["P" ]=ProcDCH;
      csiHandlers["J" ]=ProcED;
      csiHandlers["K" ]=ProcEL;
      csiHandlers["L" ]=ProcIL;
      csiHandlers["M" ]=ProcDL;

      // tabs
      csiHandlers["Z" ]=ProcCBT;
      csiHandlers["I" ]=ProcCHT;
      csiHandlers["g" ]=ProcTBC;
      csiHandlers["?W"]=ProcDECST8C;
      csiHandlers["W" ]=ProcTabSet;

      csiHandlers["S" ]=ProcSU;
      csiHandlers["T" ]=ProcSD;

      csiHandlers["s" ]=ProcSCOSC;
      csiHandlers["u" ]=ProcSCORC;

      csiHandlers["t" ]=ProcDECSLPP;

      // VT100
      /*
       * XTerm
       * 
        case 'p':
          return SoftTerminalReset(param);
        case 't':
          //!!パラメータによって無視してよい場合と、応答を返すべき場合がある。応答の返し方がよくわからないので保留中
          return ProcessCharResult.Processed;
        case 'U': //これはSFUでしか確認できてない
          base.ProcessCursorPosition(GetDocument().TerminalHeight, 1);
          return ProcessCharResult.Processed;
        case 'u': //SFUでのみ確認。特にbは続く文字を繰り返すらしいが、意味のある動作になっているところを見ていない
        case 'b':
       * 
       * Xterm Manual
       * 
        case "\"q":
        case "?J":
        case "?K":

        case ">T":
        case "i":
        case "?i":
        case ">m":
        case ">n":
        case "?n":
        case ">p":
        case "!p":
        case "$p":
        case "?$p":
        case "\"$p":
        case "q":
        case " q":
        case "$r":
        case "$t":
        case ">t":
        case " t":
        case " u":
        case "$v":
        case "'w":
        case "x":
        case "$x":
        case "'z":
        case "$z":
        case "'{":
        case "${":
        case "'|":
       * 
       * rxvt
       * 
        case "r":
       * 
      */
    }

    protected void ProcessCSISequence(){
      CSIHandler handler=null;

      // 頻出シーケンス
      if(cutter.Content=="m"){
        ProcSGR(this);

      // その他のシーケンス
      }else if(csiHandlers.TryGetValue(cutter.Content,out handler)){
        handler(this);

      // 未対応シーケンス
      }else{
        ReportEscapeSequenceError(string.Format("Unknown CSI Sequence: CSI {0} {1}",cutter.CSIArguments,cutter.Content));
      }
    }
    //==========================================================================
    //  問い合わせ
    //--------------------------------------------------------------------------
    // CSI c: PrimaryDA
    /// <summary>
    /// Process `Send Device Attributes'
    /// </summary>
    static readonly byte[] ProcPDA_reply=System.Text.Encoding.ASCII.GetBytes("\x1b[?1;2c");
    static void ProcPDA(RosaTerminal t){
      int code=t.cutter.CSIArguments[0];
      switch(code){
        case 0:
          // TODO: 今は暫定的に \e[?1;2c (VT100+AdvancedVideoOption) を返答
          t.Transmit(ProcPDA_reply);
          break;
        default:
          t.ReportEscapeSequenceError(string.Format("Unknown Primary DA code: {0}",code));
          break;
      }
    }
    // CSI > c: SecondaryDA
    static readonly byte[] ProcSDA_reply=System.Text.Encoding.ASCII.GetBytes("\x1b[>1;95;0c");
    static void ProcSDA(RosaTerminal t){
      int code=t.cutter.CSIArguments[0];
      switch(code){
        case 0:
          // TODO: 今は暫定的に \e[>1;95;0c (VT220/XFree86 Patch0) を返答
          t.Transmit(ProcSDA_reply);
          break;
        default:
          t.ReportEscapeSequenceError(string.Format("Unknown Secondary DA code: {0}",code));
          break;
      }
    }
    //--------------------------------------------------------------------------
    // CSI n: DSR (Device Status Report)
    static readonly byte[] ProcDSR_ResponseOK=System.Text.Encoding.ASCII.GetBytes("\x1b[0n");
    static void ProcDSR(RosaTerminal t){
      int code=t.cutter.CSIArguments[0];
      switch(code){
        case 5: // Status
          t.Transmit(ProcDSR_ResponseOK);
          break;
        case 6:{ // CursorPosition CPR
          TerminalDocument doc=t.GetDocument();
          t.Transmit(System.Text.Encoding.ASCII.GetBytes(string.Format(
            "\x1b[{0};{1}n",
            doc.CurrentLineNumber-doc.TopLineNumber+1,
            t._manipulator.CaretColumn+1
          )));
          break;
        }
        default:
          t.ReportEscapeSequenceError(string.Format("Unknown DSR code {0}",code));
          break;
      }
    }
    string[] saved_captions=new string[3];
    // "CSI P1; P2; P3 t": DECSLPP (Set Lines Per Page)
    static void ProcDECSLPP(RosaTerminal t){
      int code=t.cutter.CSIArguments[0];
      switch(code){
        //case 1: // 表示(最小化でない)
        //case 2: // 最小化
        //case 3: // ウィンドウを (P2, P3) に移動
        //case 4: // ウィンドウサイズを (P2, P3) に設定
        //case 5: // ウィンドウを前面に移動
        //case 6: // ウィンドウを背面に移動
        //case 7: // ウィンドウを再描画
        //case 8: // 端末サイズを (P2行, P3列) に設定
        //case 9: // P2 = 0 非最大化, P3=1 最大化
        case 22: // ウィンドウタイトルを保存
          {
            int index=t.cutter.CSIArguments[1];
            if(0<=index&&index<=2){
              t.saved_captions[index]=t.GetDocument().Caption;
              return;
            }
          }
          break;
        case 23: // ウィンドウタイトルを復元
          {
            int index=t.cutter.CSIArguments[1];
            if(0<=index&&index<=2){
              string caption=t.saved_captions[index];
              if(caption!=null)
                t.GetDocument().Caption=caption;
              return;
            }
          }
          break;
      }
      t.ReportEscapeSequenceError(string.Format("Unrecognized DECSLPP code {0}",code));
    }
    //==========================================================================
    //  状態設定
    //--------------------------------------------------------------------------
    // CSI h / CSI l: DECSM/DECRM
    static void ProcSM(RosaTerminal t){
      for(int i=0,iM=t.cutter.CSIArguments.Length;i<iM;i++){
        int code=t.cutter.CSIArguments[i];
        switch(code){
          case 2:t.tstat.SmAm=true;break;
          case 4:t.tstat.SmIrm=true;break;
          case 3:t.tstat.SmCrm=true;break;
          case 12:t.tstat.SmSrm=false;break;
          case 20:t.tstat.SmLmn=true;break;
          case 34:t.tstat.SmBigCursor=true;break;
          default:
            t.ReportEscapeSequenceError(string.Format("Unknown SM code {0}",code));
            break;
        }
      }
    }
    static void ProcRM(RosaTerminal t){
      for(int i=0,iM=t.cutter.CSIArguments.Length;i<iM;i++){
        int code=t.cutter.CSIArguments[i];
        switch(code){
          case 2:t.tstat.SmAm=false;break;
          case 3:t.tstat.SmCrm=false;break;
          case 4:t.tstat.SmIrm=false;break;
          case 12:t.tstat.SmSrm=false;break;
          case 20:t.tstat.SmLmn=false;break;
          case 34:t.tstat.SmBigCursor=false;break;
          default:
            t.ReportEscapeSequenceError(string.Format("Unknown SM code {0}",code));
            break;
        }
      }
    }
    internal void OnStateSmSrmChanged(bool value){
      _afterExitLockActions.Add(delegate(){
        ITerminalSettings settings=this.GetTerminalSettings();
        settings.BeginUpdate();
        settings.LocalEcho=!value;
        settings.EndUpdate();
      });
    }
    //--------------------------------------------------------------------------
    // CSI ? h / CSI ? l: DECSET/DECRST
    static void ProcDECSET(RosaTerminal t){
      int code=t.cutter.CSIArguments[0];
      if(!t.tstat.DecSet(code)){
        t.ReportEscapeSequenceError(string.Format("Unknown DECSET code {0}",code));
      }
    }
    static void ProcDECRST(RosaTerminal t){
      int code=t.cutter.CSIArguments[0];
      if(!t.tstat.DecReset(code)){
        t.ReportEscapeSequenceError(string.Format("Unknown DECSET code {0}",code));
      }
    }
    static void ProcDECToggle(RosaTerminal t){
      int code=t.cutter.CSIArguments[0];
      if(!t.tstat.DecToggle(code)){
        t.ReportEscapeSequenceError(string.Format("Unknown DECSET code {0}",code));
      }
    }
    internal void OnChangedDecCkm(bool value){
      ChangeCursorKeyMode(value?TerminalMode.Application:TerminalMode.Normal);
    }

    Gen::List<GLine> OnChangedDECAltScrBuff_abuff=null;
    internal void OnChangedDecAltScrBuff(bool value){
      TerminalDocument doc=this.GetDocument();
      if(value){
        OnChangedDECAltScrBuff_abuff=new Gen::List<GLine>();
        GLine l=doc.TopLine;
        int m=l.ID+doc.TerminalHeight;
        while(l!=null&&l.ID<m){
          OnChangedDECAltScrBuff_abuff.Add(l.Clone());
          l=l.NextLine;
        }
      }else{
        if(OnChangedDECAltScrBuff_abuff==null)return;
        int w = doc.TerminalWidth;
        int m = doc.TerminalHeight;
        GLine t = doc.TopLine;
        foreach(GLine l in OnChangedDECAltScrBuff_abuff) {
          l.ExpandBuffer(w);
          if(t==null)
            doc.AddLine(l);
          else {
            doc.Replace(t, l);
            t = l.NextLine;
          }
          if(--m==0) break;
        }

        OnChangedDECAltScrBuff_abuff=null;
      }
    }
    //--------------------------------------------------------------------------
    internal void OnChangedXtermAltScrBuff(bool value){
      this.OnChangedDecAltScrBuff(value);
    }
    internal void OnChangedXtermDECSC(bool value){
      if(value)
        ProcDECSC(this);
      else
        ProcDECRC(this);
    }
    internal void OnChangedXtermAltScrBuffSC(bool value){
      this.OnChangedDecAltScrBuff(value);
      this.OnChangedXtermDECSC(value);
    }
    //--------------------------------------------------------------------------
    // ESC =: DECKPAM/SMKX: Application Keypad
    static void ProcDECKPAM(RosaTerminal t){
      t.tstat.DecNkm=true;
    }
    // ESC >: DECKPNM/RMKX: Numeric/Normal Keypad
    static void ProcDECKPNM(RosaTerminal t){
      t.tstat.DecNkm=false;
    }
    // CSI 66 hl: Application/Numeric Keypad
    internal void OnChangedDecNkm(bool value){
      // ? ApplicationKeypad と Application/Normal モードは関係ない様な気がする。
      //   が、Poderosa.Terminal.Xterm がこうしているので。
      this.ChangeMode(value?TerminalMode.Application:TerminalMode.Normal);
    }
    //==========================================================================
    //  領域設定
    //--------------------------------------------------------------------------
    // CSI r: DECSTBM (Set Scrolling Region)
    static void ProcDECSTBM(RosaTerminal t){
      int height=t.GetDocument().TerminalHeight;
      int x=t.cutter.CSIArguments[0].Clamp(1,height);
      int y=t.cutter.CSIArguments[1].Clamp(1,height);
      if(x>y){int v=x;x=y;y=v;}

      //@ 指定は1-originだが処理は0-origin
      t.GetDocument().SetScrollingRegion(x-1,y-1);
    }
    //==========================================================================
    //  消去・挿入
    //--------------------------------------------------------------------------
    // CSI @: ICH
    static void ProcICH(RosaTerminal t){
      int len=t.cutter.CSIArguments[0];
      if(len<1)len=1;
      t._manipulator.InsertBlanks(t._manipulator.CaretColumn,len,t._currentdecoration);
    }
    // CSI X: ECH
    static void ProcECH(RosaTerminal t){
      int n=t.cutter.CSIArguments.GetOrDefault(0,1);
      int oc=t._manipulator.CaretColumn;

      int cM=oc+n;
      if(cM>t._manipulator.BufferSize)cM=t._manipulator.BufferSize;
      for(int c=oc;c<cM;c++)
        t._manipulator.PutChar(' ',t._currentdecoration);

      t._manipulator.CaretColumn=oc;
    }
    // CSI P: DCH
    static void ProcDCH(RosaTerminal t){
      t._manipulator.DeleteChars(
        t._manipulator.CaretColumn,
        t.cutter.CSIArguments.GetOrDefault(0,1),
        t._currentdecoration
        );
    }
    //--------------------------------------------------------------------------
    // CSI J
    static void ProcED(RosaTerminal t){
      TerminalDocument doc=t.GetDocument();
      int c=t._manipulator.CaretColumn;

      TextDecoration dec=t.tstat.TerminfoBceCap?t._currentdecoration:TextDecoration.Default;
      int code=t.cutter.CSIArguments[0];
      switch(code){
        case 0:
          t._manipulator.FillBlank(t._manipulator.CaretColumn,t._manipulator.BufferSize,dec);
          doc.ReplaceCurrentLine(t._manipulator.Export());
          doc.RemoveAfter(doc.TopLineNumber+doc.TerminalHeight);

          doc.ClearAfter(doc.CurrentLineNumber+1,dec);
          t._manipulator.Load(doc.CurrentLine,c);
          break;
        case 1:
          t._manipulator.FillBlank(0,t._manipulator.CaretColumn,dec);
          doc.ReplaceCurrentLine(t._manipulator.Export());

          doc.ClearRange(doc.TopLineNumber,doc.CurrentLineNumber,dec);
          t._manipulator.Load(doc.CurrentLine,c);
          break;
        case 2:
          doc.ReplaceCurrentLine(t._manipulator.Export());
          doc.ClearAfter(doc.TopLineNumber,dec);
          t._manipulator.Load(doc.CurrentLine,c);
          break;
        default:
          t.ReportEscapeSequenceError(string.Format("Unknown ED code {0}",code));
          break;
      }
    }
    // CSI K
    static void ProcEL(RosaTerminal t){
      int c=t._manipulator.CaretColumn;

      TextDecoration dec=t.tstat.TerminfoBceCap?t._currentdecoration:TextDecoration.Default;
      int code=t.cutter.CSIArguments[0];
      switch(code){
        case 0: //erase right
          //t._manipulator.FillSpace(t._manipulator.CaretColumn,t.GetDocument().TerminalWidth,t._currentdecoration);
          t._manipulator.FillBlank(t._manipulator.CaretColumn,t._manipulator.BufferSize,dec);
          break;
        case 1: //erase left
          t._manipulator.FillBlank(0, t._manipulator.CaretColumn,dec);
          break;
        case 2: //erase all
          t._manipulator.Clear(t.GetDocument().TerminalWidth);
          break;
        default:
          t.ReportEscapeSequenceError(string.Format("Unknown ED code {0}",code));
          break;
      }
    }
    //--------------------------------------------------------------------------
    // CSI L
    static void ProcIL(RosaTerminal t){
      int d=t.cutter.CSIArguments[0];
      if(d<1)d=1;
      TerminalDocument doc=t.GetDocument();

      int col=t._manipulator.CaretColumn;
      int offset=doc.CurrentLineNumber-doc.TopLineNumber;
      doc.ReplaceCurrentLine(t._manipulator.Export());
      if(doc.ScrollingBottom==-1)
        doc.SetScrollingRegion(0,doc.TerminalHeight-1);

      for(int i=0;i<d;i++){
        doc.ScrollUp(doc.CurrentLineNumber,doc.ScrollingBottom);
        doc.CurrentLineNumber=doc.TopLineNumber+offset;
      }
      t._manipulator.Load(doc.CurrentLine, col);
    }
    // CSI M
    static void ProcDL(RosaTerminal t){
      int d=t.cutter.CSIArguments[0];
      if(d<1)d=1;
      TerminalDocument doc=t.GetDocument();

      int col=t._manipulator.CaretColumn;
      int offset=doc.CurrentLineNumber-doc.TopLineNumber;
      doc.ReplaceCurrentLine(t._manipulator.Export());
      if(doc.ScrollingBottom==-1)
        doc.SetScrollingRegion(0, doc.TerminalHeight-1);

      for(int i=0;i<d;i++) {
        doc.ScrollDown(doc.CurrentLineNumber,doc.ScrollingBottom);
        doc.CurrentLineNumber=doc.TopLineNumber+offset;
      }
      t._manipulator.Load(doc.CurrentLine,col);
    }
    //--------------------------------------------------------------------------
    // CSI S : ScrollUp (中身を上へ移動/窓を下へ移動)
    //   画面の下端に新しい行を挿入
    //   画面の上端から行を削除
    static void ProcSU(RosaTerminal t){
      TerminalDocument doc=t.GetDocument();
      int c=t._manipulator.CaretColumn;
      int l=doc.CurrentLineNumber;
      doc.ReplaceCurrentLine(t._manipulator.Export());

      int d=t.cutter.CSIArguments[0];if(d<1)d=1;
      if(doc.ScrollingBottom==-1)
        doc.SetScrollingRegion(0, doc.TerminalHeight-1);
      for(int i=0;i<d;i++){
        doc.ScrollDown(doc.TopLineNumber,doc.ScrollingBottom);
      }

      doc.CurrentLineNumber=l;
      t._manipulator.Load(doc.CurrentLine,c);
    }
    // CSI T : ScrollDn (中身を下へ移動/窓を上へ移動)
    //   画面の先頭に新しい行を挿入
    //   画面の下端から行を削除
    static void ProcSD(RosaTerminal t){
      TerminalDocument doc=t.GetDocument();
      int c=t._manipulator.CaretColumn;
      int l=doc.CurrentLineNumber;
      doc.ReplaceCurrentLine(t._manipulator.Export());

      int d=t.cutter.CSIArguments[0];if(d<1)d=1;
      if(doc.ScrollingBottom==-1)
        doc.SetScrollingRegion(0, doc.TerminalHeight-1);
      for(int i=0;i<d;i++){
        doc.ScrollUp(doc.TopLineNumber,doc.ScrollingBottom);
      }

      doc.CurrentLineNumber=l;
      t._manipulator.Load(doc.CurrentLine,c);
    }
    // IND
    static void ProcIND(RosaTerminal t){
      TerminalDocument doc=t.GetDocument();
      doc.ReplaceCurrentLine(t._manipulator.Export());

      int iline=doc.CurrentLineNumber;
      if(iline==doc.TopLineNumber+doc.TerminalHeight-1 || iline==doc.ScrollingBottom)
        doc.ScrollDown();
      else
        doc.CurrentLineNumber=iline+1;

      t._manipulator.Load(doc.CurrentLine, t._manipulator.CaretColumn);
    }
    // RI
    static void ProcRI(RosaTerminal t){
      TerminalDocument doc=t.GetDocument();
      doc.ReplaceCurrentLine(t._manipulator.Export());

      int iline=doc.CurrentLineNumber;
      if(iline==doc.TopLineNumber||iline==doc.ScrollingTop)
        doc.ScrollUp();
      else
        doc.CurrentLineNumber=iline-1;

      t._manipulator.Load(doc.CurrentLine, t._manipulator.CaretColumn);
    }
    //==========================================================================
    //  Tab Stops
    //--------------------------------------------------------------------------
    //class TabStopArray{
    //  Gen::List<bool> tabstop=new Gen::List<bool>(100);
    //  public TabStopArray(){}

    //  public bool this[int index]{
    //    get{
    //      if(index<0||tabstop.Count<=index)return false;
    //      return tabstop[index];
    //    }
    //    set{
    //      if(index<0)return;
    //      if(index<tabstop.Count)
    //        tabstop[index]=value;
    //      else if(value){
    //        if(tabstop.Capacity<index)
    //          tabstop.Capacity=(int)(index+1);
    //        while(tabstop.Count<index)
    //          tabstop.Add(false);
    //        tabstop.Add(true);
    //      }
    //    }
    //  }

    //}
    class TabStopArray{
      readonly Gen::SortedList<int,int> data; // 二つ目の型はダミー

      public TabStopArray(){
        this.data=new Gen::SortedList<int,int>();
        this.Clear();
      }
      //------------------------------------------------------------------------
      public void SetTabStop(int col){
        if(!this.data.ContainsKey(col)){
          this.data.Add(col,0);
        }
      }
      public void ResetTabStop(int col){
        if(this.data.ContainsKey(col)){
          this.data.Remove(col);
        }
      }
      public void Clear(){
        this.data.Clear();
        this.data.Add(0,0);
      }
      public void InitTabs(int interval,int width){
        this.data.Clear();
        for(int i=0;i<width;i+=interval)
          this.data.Add(i,0);
      }
      //------------------------------------------------------------------------
      public int GetBackwardTabStop(int col,int tabcount){
        if(tabcount<0)return GetForwardTabStop(col,-tabcount);
        if(tabcount==0)return col;

        int idx=BinarySearchArgInf(col);
        if(idx<0)return col;
        if(data.Keys[idx]!=col)tabcount--;
        
        idx-=tabcount;
        if(idx<0)idx=0;
        return data.Keys[idx];
      }
      int BinarySearchArgInf(int col){
        Gen::IList<int> keys=data.Keys;
        if(keys.Count==0)return -1;
        if(col<keys[0])return -1;
        if(col>=keys[keys.Count-1])return keys.Count-1;

        int l=0,u=keys.Count; // keys[l] <= col < keys[u]
        while(l<u-1){
          int m=(l+u)/2;
          if(keys[m]<=col)
            l=m;
          else
            u=m;
        }

        return l;
      }
      //------------------------------------------------------------------------
      public int GetForwardTabStop(int col,int tabcount){
        if(tabcount<0)return GetBackwardTabStop(col,-tabcount);
        if(tabcount==0)return col;

        int idx=BinarySearchArgSup(col);
        if(idx<0)return col;
        if(data.Keys[idx]!=col)tabcount--;

        idx+=tabcount;
        if(data.Count<=idx)idx=data.Count-1;
        return data.Keys[idx];
      }
      int BinarySearchArgSup(int col){
        Gen::IList<int> keys=data.Keys;
        if(keys.Count==0)return -1;
        if(col<=keys[0])return 0;
        if(col>keys[keys.Count-1])return -1;

        int l=0,u=keys.Count; // keys[l] < col <= keys[u]
        while(l<u-1){
          int m=(l+u)/2;
          if(keys[m]<col)
            l=m;
          else
            u=m;
        }

        return u;
      }
    }
    //--------------------------------------------------------------------------
#if CHECK
    public static void testTabStops(){
      TabStopArray arr=new TabStopArray();
      arr.SetTabStop(5);
      arr.SetTabStop(20);
      arr.SetTabStop(10);
      System.Console.WriteLine("Hello");
      System.Console.WriteLine(
        "Backwards: {0} {1} {2} {3} | {4} {5} {6} {7}",
        arr.GetBackwardTabStop(0,1)==0,
        arr.GetBackwardTabStop(1,1)==0,
        arr.GetBackwardTabStop(5,1)==0,
        arr.GetBackwardTabStop(7,1)==5,
        arr.GetBackwardTabStop(10,1)==5,
        arr.GetBackwardTabStop(12,1)==10,
        arr.GetBackwardTabStop(20,1)==10,
        arr.GetBackwardTabStop(25,1)==20
        );
      System.Console.WriteLine(
        "Backwards: {0} {1} {2} {3} | {4} {5} {6} {7}",
        arr.GetBackwardTabStop(0,2)==0,
        arr.GetBackwardTabStop(1,2)==0,
        arr.GetBackwardTabStop(5,2)==0,
        arr.GetBackwardTabStop(7,2)==0,
        arr.GetBackwardTabStop(10,2)==0,
        arr.GetBackwardTabStop(12,2)==5,
        arr.GetBackwardTabStop(20,2)==5,
        arr.GetBackwardTabStop(25,2)==10
        );
      System.Console.WriteLine(
        "Forwards: {0} {1} {2} {3} | {4} {5} {6} {7}",
        arr.GetForwardTabStop(0,1)==5,
        arr.GetForwardTabStop(1,1)==5,
        arr.GetForwardTabStop(5,1)==10,
        arr.GetForwardTabStop(7,1)==10,
        arr.GetForwardTabStop(10,1)==20,
        arr.GetForwardTabStop(12,1)==20,
        arr.GetForwardTabStop(20,1)==20,
        arr.GetForwardTabStop(25,1)==25
        );
      System.Console.WriteLine(
        "Forwards: {0} {1} {2} {3} | {4} {5} {6} {7}",
        arr.GetForwardTabStop(0,2)==10,
        arr.GetForwardTabStop(1,2)==10,
        arr.GetForwardTabStop(5,2)==20,
        arr.GetForwardTabStop(7,2)==20,
        arr.GetForwardTabStop(10,2)==20,
        arr.GetForwardTabStop(12,2)==20,
        arr.GetForwardTabStop(20,2)==20,
        arr.GetForwardTabStop(25,2)==25
        );
      arr.ResetTabStop(0);
      System.Console.WriteLine(
        "Backwards: {0} {1} {2} {3} | {4} {5} {6} {7}",
        arr.GetBackwardTabStop(0,1)==0,
        arr.GetBackwardTabStop(1,1)==1,
        arr.GetBackwardTabStop(5,1)==5,
        arr.GetBackwardTabStop(7,1)==5,
        arr.GetBackwardTabStop(10,1)==5,
        arr.GetBackwardTabStop(12,1)==10,
        arr.GetBackwardTabStop(20,1)==10,
        arr.GetBackwardTabStop(25,1)==20
        );
      System.Console.WriteLine(
        "Backwards: {0} {1} {2} {3} | {4} {5} {6} {7}",
        arr.GetBackwardTabStop(0,2)==0,
        arr.GetBackwardTabStop(1,2)==1,
        arr.GetBackwardTabStop(5,2)==5,
        arr.GetBackwardTabStop(7,2)==5,
        arr.GetBackwardTabStop(10,2)==5,
        arr.GetBackwardTabStop(12,2)==5,
        arr.GetBackwardTabStop(20,2)==5,
        arr.GetBackwardTabStop(25,2)==10
        );
    }
#endif
    //--------------------------------------------------------------------------
    readonly TabStopArray tabstops=new TabStopArray();
    // CSI Z
    static void ProcCBT(RosaTerminal t){
      int n=t.cutter.CSIArguments[0];
      if(n==0)n=1;

      int c=t._manipulator.CaretColumn;
      t._manipulator.CaretColumn=t.tabstops.GetBackwardTabStop(c,n);
    }
    // CSI I
    static void ProcCHT(RosaTerminal t){
      int n=t.cutter.CSIArguments[0];
      if(n==0)n=1;

      int c=t._manipulator.CaretColumn;
      t._manipulator.CaretColumn=t.tabstops.GetForwardTabStop(c,n);
    }
    // CSI g
    static void ProcTBC(RosaTerminal t){
      int code=t.cutter.CSIArguments[0];
      switch(code){
        case 0:t.tabstops.ResetTabStop(t._manipulator.CaretColumn);break;
        case 3:t.tabstops.Clear();break;
        default:
          t.ReportEscapeSequenceError(string.Format("Unknown TBC code {0}",code));
          break;
      }
    }
    // HTS
    static void ProcHTS(RosaTerminal t){
      t.tabstops.SetTabStop(t._manipulator.CaretColumn);
    }
    // CSI ? W
    static void ProcDECST8C(RosaTerminal t){
      int code=t.cutter.CSIArguments[0];
      switch(code){
        case 5:t.tabstops.InitTabs(8,t.GetDocument().TerminalWidth);break;
        default:
          t.ReportEscapeSequenceError(string.Format("Unknown DECST8C code {0}",code));
          break;
      }
    }
    // CSI W
    static void ProcTabSet(RosaTerminal t){
      int code=t.cutter.CSIArguments[0];
      switch(code){
        case 0:t.tabstops.SetTabStop(t._manipulator.CaretColumn);break;
        case 2:t.tabstops.ResetTabStop(t._manipulator.CaretColumn);break;
        case 5:t.tabstops.Clear();break;
        default:
          t.ReportEscapeSequenceError(string.Format("Unknown TabSet code {0}",code));
          break;
      }
    }
    //==========================================================================
    //  カーソル移動
    //--------------------------------------------------------------------------
    // CSI A
    static void ProcCUU(RosaTerminal t){
      int q=t.cutter.CSIArguments[0];
      if(q==0)q=1;

      TerminalDocument doc=t.GetDocument();

      int column=t._manipulator.CaretColumn;
      doc.ReplaceCurrentLine(t._manipulator.Export());
      doc.CurrentLineNumber-=q;
      t._manipulator.Load(doc.CurrentLine,column);
    }
    // CSI B
    static void ProcCUD(RosaTerminal t){
      int q=t.cutter.CSIArguments[0];
      if(q==0)q=1;

      TerminalDocument doc=t.GetDocument();

      int column=t._manipulator.CaretColumn;
      doc.ReplaceCurrentLine(t._manipulator.Export());
      doc.CurrentLineNumber+=q;
      t._manipulator.Load(doc.CurrentLine,column);
    }
    // ESC E
    static void ProcNEL(RosaTerminal t){
      TerminalDocument doc=t.GetDocument();

      doc.ReplaceCurrentLine(t._manipulator.Export());
      doc.CurrentLineNumber=doc.CurrentLineNumber+1;
      t._manipulator.Load(doc.CurrentLine,0);
    }
    //-------------------------------------------------------------------------
    // CSI C
    static void ProcCUF(RosaTerminal t){
      int q=t.cutter.CSIArguments[0];
      if(q==0)q=1;

      TerminalDocument doc=t.GetDocument();

      int column=t._manipulator.CaretColumn+q;
      if(column>doc.TerminalWidth-1)column=doc.TerminalWidth-1;
      t._manipulator.ExpandBuffer(column);
      t._manipulator.CaretColumn=column;
    }
    // CSI D
    static void ProcCUB(RosaTerminal t){
      int q=t.cutter.CSIArguments[0];
      if(q==0)q=1;

      TerminalDocument doc=t.GetDocument();

      int column=t._manipulator.CaretColumn-q;
      if(column<0)column=0;
      t._manipulator.CaretColumn=column;
    }
    //-------------------------------------------------------------------------
    // CSI E
    static void ProcCNL(RosaTerminal t){
      int q=t.cutter.CSIArguments[0];
      if(q==0)q=1;

      TerminalDocument doc=t.GetDocument();

      doc.ReplaceCurrentLine(t._manipulator.Export());
      doc.CurrentLineNumber-=q;
      t._manipulator.Load(doc.CurrentLine,0);
    }
    // CSI F
    static void ProcCPL(RosaTerminal t){
      int q=t.cutter.CSIArguments[0];
      if(q==0)q=1;

      TerminalDocument doc=t.GetDocument();

      doc.ReplaceCurrentLine(t._manipulator.Export());
      doc.CurrentLineNumber+=q;
      t._manipulator.Load(doc.CurrentLine,0);
    }
    //-------------------------------------------------------------------------
    // CSI d
    static void ProcVPA(RosaTerminal t){
      TerminalDocument doc=t.GetDocument();
      int y=t.cutter.CSIArguments[0].Clamp(1,doc.TerminalHeight);
      int c=t._manipulator.CaretColumn;

      doc.ReplaceCurrentLine(t._manipulator.Export());
      doc.CurrentLineNumber=doc.TopLineNumber+y-1;
      t._manipulator.Load(doc.CurrentLine,c);
    }
    // CSI G
    static void ProcHPA(RosaTerminal t){
      TerminalDocument doc=t.GetDocument();
      int x=t.cutter.CSIArguments[0].Clamp(1,doc.TerminalWidth);
      t._manipulator.ExpandBuffer(x-1);
      t._manipulator.CaretColumn=x-1;
    }
    //-------------------------------------------------------------------------
    // CSI H
    static void ProcCUP(RosaTerminal t){
      TerminalDocument doc=t.GetDocument();
      int y=t.cutter.CSIArguments[0];
      int x=t.cutter.CSIArguments[1];
      if(t.tstat.DecOm&&doc.ScrollingTop!=-1)y+=doc.ScrollingTop;
      y=y.Clamp(1,doc.TerminalHeight);
      x=x.Clamp(1,doc.TerminalWidth);
      
      t.MoveCursorTo(y,x);
    }
    /// <summary>
    /// 指定した位置にカーソルを移動します。
    /// </summary>
    /// <param name="row1">移動先の行番号を指定します。上橋が 1 です。</param>
    /// <param name="col1">移動先の列番号を指定します。左端が 1 です。</param>
    protected void MoveCursorTo(int row1,int col1){
      TerminalDocument doc=GetDocument();
      doc.ReplaceCurrentLine(_manipulator.Export());
      doc.CurrentLineNumber=doc.TopLineNumber+row1-1;
      _manipulator.Load(doc.CurrentLine,col1-1);
    }
    //--------------------------------------------------------------------------
    // CSI s: SCOSC
    static void ProcSCOSC(RosaTerminal t){
      ProcDECSC(t);
    }
    // CSI u: SCORC
    static void ProcSCORC(RosaTerminal t){
      ProcDECRC(t);
    }
    //==========================================================================
    //  カーソル設定
    //-------------------------------------------------------------------------
    // CSI m: SGR (Set Cursor Attributes)
    /// <summary>
    /// SGR (Set Cursor Graphics Attributes) シーケンスの処理を行います。
    /// </summary>
    static void ProcSGR(RosaTerminal t){
      TextDecorationConstructor dec=new TextDecorationConstructor(t._currentdecoration);
      for(int i=0,iM=t.cutter.CSIArguments.Length;i<iM;i++){
        int code=t.cutter.CSIArguments[i];

        if(code>=30&&code<=37){
          dec.TextColorType=(ColorType)(code-30+ColorType.ColorBlack);
        }else if(code>=40&&code<=47){
          dec.BackColorType=(ColorType)(code-40+ColorType.ColorBlack);
        }else if(code>=90&&code<=97){
          dec.TextColorType=(ColorType)(code-90+ColorType.ColorBlack);
          dec.BrightText=true;
        }else if(code>=100&&code<=107){
          dec.BackColorType=(ColorType)(code-100+ColorType.ColorBlack);
          dec.BrightBack=true;
        }else{
          switch(code){
            case 0:
              dec.Clear();
              break;
            //------------------------------------------------------------------
            case  1:dec.BrightText=true;  break;
            case  2:dec.BrightText=false; break;
            case  5:dec.BrightBack=true;  break;
            case 25:dec.BrightBack=false; break;
            case  6:dec.Bold=true;        break;
            case 26:dec.Bold=false;       break;
            case  7:dec.Inverted=true;    break;
            case 27:dec.Inverted=false;   break;
            case  8:dec.Invisible=true;   break;
            case 28:dec.Invisible=false;  break;
            case  3:dec.Italic=true;      break;
            case 23:dec.Italic=false;     break;
            case  9:dec.Throughline=true; break;
            case 29:dec.Throughline=false;break;
            case 53:dec.Overline=true;    break;
            case 55:dec.Overline=false;   break;
            case  4:dec.Underline=true;   break;
            case 21:dec.Doubleline=true;  break;
            case 24:
              dec.Underline=false;
              dec.Doubleline=false;
              break;
            //------------------------------------------------------------------
            case 22:
              dec.ForeColor=Gdi::Color.Empty;
              dec.BackColor=Gdi::Color.Empty;
              break;
            case 39:
              dec.ForeColor=Gdi::Color.Empty;
              break;
            case 49:
              dec.BackColor=Gdi::Color.Empty;
              break;
            case 38:
              {
                Gdi::Color color;
                int ret=TerminalColors.GetISO8613_6Colors(out color,t.cutter.CSIArguments,i);
                if(ret!=0){
                  dec.ForeColor=color;
                  i+=ret-1;
                }
              }
              break;
            case 48:
              {
                Gdi::Color color;
                int ret=TerminalColors.GetISO8613_6Colors(out color,t.cutter.CSIArguments,i);
                if(ret!=0){
                  dec.BackColor=color;
                  i+=ret-1;
                }
              }
              break;
            //------------------------------------------------------------------
            case 10: // 元の文字コードに戻す
            case 11: // 一時的に sjis にする (enter_pc_charset_mode , smpch)
            case 12:
              break;
            default:
              t.ReportEscapeSequenceError(string.Format("Unknown SGR code {0}",code));
              break;
          }
        }
      }

      t._currentdecoration=dec.CreateTextDecoration();
      //_manipulator.SetDecoration(dec);
    }
    #endregion

    //==========================================================================
    //  端末→プログラム
    //--------------------------------------------------------------------------
    //internal override byte[] SequenceKeyData(System.Windows.Forms.Keys modifier,System.Windows.Forms.Keys body) {
    //  throw new System.NotImplementedException();
    //}

    #region キーボード入力
    readonly InputSequence inputseq=new InputSequence();

    internal override int EncodeInputKey2(Keys key,out byte[] data){
      EncodeInputKey_v2(key);
      data=inputseq.data;
      return inputseq.length;
    }
    internal enum EncodeMetaType{None,Meta,Escape}
    void EncodeInputKey_v2(Keys key){
      inputseq.Clear();

      // decompose argument
      EncodeMetaType meta=EncodeMetaType.None;
      if((key&KeyModifiers.MetaE)!=0){
        //bool send_esc_as_meta=true;
        // GEnv 或いは、terminalState から取得
        meta=EncodeMetaType.Escape;
      }else if((key&KeyModifiers.Meta)!=0){
        meta=EncodeMetaType.Meta;
      }
      Keys code=key&Keys.KeyCode;
      Keys mods=key&Keys.Modifiers&~(KeyModifiers.Meta|KeyModifiers.MetaE);

      if((int)Keys.A<=(int)code&&(int)code<=(int)Keys.Z){
        if((mods&~KeyModifiers.Shift)==0){
          if(System.Windows.Forms.Control.IsKeyLocked(Keys.CapsLock))
            mods^=KeyModifiers.Shift;

          byte b=KeyboardInfo.ConvertToChar(key);
          if(b==0)return;
          inputseq.v2WriteChar(meta,b);
        }else if(mods==KeyModifiers.Ctrl){
          byte b=(byte)('a'-(int)Keys.A+(int)code);
          inputseq.v2WriteChar(meta,(byte)(0x1f&b));
        }else{
          // [a-z] Shiftがあっても小文字の儘で Shift 修飾を送る。
          byte b=(byte)('a'-(int)Keys.A+(int)code);
          inputseq.v2WriteCsiChar(meta,b,mods);
        }
        return;
      }else if((int)Keys.D0<=(int)code&&(int)code<=(int)Keys.D9){
        byte b=KeyboardInfo.ConvertToChar(code|mods&KeyModifiers.Shift);
        if(b==0)return;
        mods&=~KeyModifiers.Shift;
        EncodeInputKey_v2char(meta,b,mods);
        return;
      }else if((int)Keys.NumPad0<=(int)code&&(int)code<=(int)Keys.NumPad9){
        bool numlocked=System.Windows.Forms.Control.IsKeyLocked(Keys.NumLock);
        bool shifted=(mods&KeyModifiers.Shift)!=0;
        mods&=~KeyModifiers.Shift;
        if(numlocked!=shifted){
          // NumPad (数字)
          byte b=(byte)('0'-(int)Keys.NumPad0+(int)code);
          EncodeInputKey_v2char(meta,b,mods);
          return;
        }else{
          // KeyPad (操作)
          switch(code){
            case Keys.NumPad2:inputseq.v2WriteCsiSequence(meta,mods,'B');return;//KPDown
            case Keys.NumPad4:inputseq.v2WriteCsiSequence(meta,mods,'D');return;//KPLeft
            case Keys.NumPad6:inputseq.v2WriteCsiSequence(meta,mods,'C');return;//KPRight
            case Keys.NumPad8:inputseq.v2WriteCsiSequence(meta,mods,'A');return;//KPUp
            case Keys.NumPad0:code=Keys.Insert;break;
            case Keys.NumPad7:code=Keys.Home;break;
            case Keys.NumPad1:code=Keys.End;break;
            case Keys.NumPad9:code=Keys.PageUp;break;
            case Keys.NumPad3:code=Keys.PageDown;break;
            case Keys.NumPad5:inputseq.v2WriteCsiSequence(meta,mods,'E');return;//Begin (普通来ない)
          }
        }
      }else if((int)Keys.F1<=(int)code&&(int)code<=(int)Keys.F24){
        int number=(int)code-(int)Keys.F1;
        if(tstat.EmulationType==Poderosa.ConnectionParam.TerminalType.XTerm
          ||tstat.EmulationType==Poderosa.ConnectionParam.TerminalType.KTerm){
          if(number<4)
            inputseq.v2WriteSs3Sequence(meta,mods,(char)('P'+number));
          else
            inputseq.v2WriteCsiSequence(meta,EncodeInputKey_v2fmap[number],mods,number>20?'$':'~');
        }else{
          if((mods&KeyModifiers.Shift)!=0&&number<12){
            mods&=~KeyModifiers.Shift;
            number+=12;
          }
          inputseq.v2WriteCsiSequence(meta,EncodeInputKey_v2fmap[number],mods,number>20?'$':'~');
        }
        return;
      }else{
        byte b=KeyboardInfo.ConvertToChar(code|mods&KeyModifiers.Shift);
        if(b!=0){
          if(b=='@'||'['<=b&&b<='_'){
            mods&=~KeyModifiers.Shift;
            EncodeInputKey_v2ctrl(meta,b,mods);
            return;
          }else if(':'<=b&&b<='?'||'*'<=b&&b<='/'||b=='`'||'{'<=b&&b<='~'){
            mods&=~KeyModifiers.Shift;
            EncodeInputKey_v2char(meta,b,mods);
            return;
          }
        }

        switch(code){
          case Keys.Enter: EncodeInputKey_v2char(meta,(byte)'\r',mods);return;
          case Keys.Tab:   EncodeInputKey_v2char(meta,(byte)'\t',mods);return;
          case Keys.Escape:EncodeInputKey_v2char(meta,(byte)'\x1b',mods);return;
          case Keys.Back:
            if(mods==0){
              inputseq.v2WriteChar(meta,(byte)0x7F);
            }else if(mods==KeyModifiers.Ctrl){
              inputseq.v2WriteChar(meta,(byte)0x1F);
            }else{
              inputseq.v2WriteCsiChar(meta,(byte)'\b',mods);
            }
            //EncodeInputKey_v2ctrl(meta,(byte)'\b',mods);
            return;
          case Keys.Space: EncodeInputKey_v2ctrl(meta,(byte)' ',mods);return;
        }
      }

      if(code==Keys.Up||code==Keys.Down||code==Keys.Right||code==Keys.Left){
        if((tstat.EmulationType==Poderosa.ConnectionParam.TerminalType.XTerm
          ||tstat.EmulationType==Poderosa.ConnectionParam.TerminalType.KTerm)
          &&tstat.DecCkm){
          switch(code){
            case Keys.Up:   inputseq.v2WriteSs3Sequence(meta,mods,'A');return;
            case Keys.Down: inputseq.v2WriteSs3Sequence(meta,mods,'B');return;
            case Keys.Right:inputseq.v2WriteSs3Sequence(meta,mods,'C');return;
            case Keys.Left: inputseq.v2WriteSs3Sequence(meta,mods,'D');return;
          }
        }

        switch(code){
          case Keys.Up:   inputseq.v2WriteCsiSequence(meta,mods,'A');return;
          case Keys.Down: inputseq.v2WriteCsiSequence(meta,mods,'B');return;
          case Keys.Right:inputseq.v2WriteCsiSequence(meta,mods,'C');return;
          case Keys.Left: inputseq.v2WriteCsiSequence(meta,mods,'D');return;
        }
      }

      if(tstat.EmulationType==Poderosa.ConnectionParam.TerminalType.XTerm
        ||tstat.EmulationType==Poderosa.ConnectionParam.TerminalType.KTerm
      ){
        // xterm kterm
        switch(code){
          case Keys.Home:
            if(tstat.DecCkm)
              inputseq.v2WriteSs3Sequence(meta,mods,'H');
            else
              inputseq.v2WriteCsiSequence(meta,mods,'H');
            return;
          case Keys.Insert:   inputseq.v2WriteCsiSequence(meta,2,mods,'~');return;
          case Keys.Delete:   inputseq.v2WriteCsiSequence(meta,3,mods,'~');return;
          case Keys.End:
            if(tstat.DecCkm)
              inputseq.v2WriteSs3Sequence(meta,mods,'F');
            else
              inputseq.v2WriteCsiSequence(meta,mods,'F');
            return;
          case Keys.PageUp:   inputseq.v2WriteCsiSequence(meta,5,mods,'~');return;
          case Keys.PageDown: inputseq.v2WriteCsiSequence(meta,6,mods,'~');return;
        }
      }else if(tstat.EmulationType==Poderosa.ConnectionParam.TerminalType.VT100){
        // vt100
        switch(code){
          case Keys.Home:     inputseq.v2WriteCsiSequence(meta,2,mods,'~');return;
          case Keys.Insert:   inputseq.v2WriteCsiSequence(meta,1,mods,'~');return;
          case Keys.Delete:   inputseq.v2WriteCsiSequence(meta,4,mods,'~');return;
          case Keys.End:      inputseq.v2WriteCsiSequence(meta,5,mods,'~');return;
          case Keys.PageUp:   inputseq.v2WriteCsiSequence(meta,3,mods,'~');return;
          case Keys.PageDown: inputseq.v2WriteCsiSequence(meta,6,mods,'~');return;
        }
      }else{
        // cygwin rosaterm
        switch(code){
          case Keys.Home:     inputseq.v2WriteCsiSequence(meta,1,mods,'~');return;
          case Keys.Insert:   inputseq.v2WriteCsiSequence(meta,2,mods,'~');return;
          case Keys.Delete:   inputseq.v2WriteCsiSequence(meta,3,mods,'~');return;
          case Keys.End:      inputseq.v2WriteCsiSequence(meta,4,mods,'~');return;
          case Keys.PageUp:   inputseq.v2WriteCsiSequence(meta,5,mods,'~');return;
          case Keys.PageDown: inputseq.v2WriteCsiSequence(meta,6,mods,'~');return;
        }
        //// vt52 rxvt
        //switch(code){
        //  case Keys.Home:     inputseq.v2WriteCsiSequence(meta,7,mods,'~');return;
        //  case Keys.Insert:   inputseq.v2WriteCsiSequence(meta,2,mods,'~');return;
        //  case Keys.Delete:   inputseq.v2WriteCsiSequence(meta,3,mods,'~');return;
        //  case Keys.End:      inputseq.v2WriteCsiSequence(meta,8,mods,'~');return;
        //  case Keys.PageUp:   inputseq.v2WriteCsiSequence(meta,5,mods,'~');return;
        //  case Keys.PageDown: inputseq.v2WriteCsiSequence(meta,6,mods,'~');return;
        //}
      }
    }
    private static byte[] EncodeInputKey_v2fmap = { 
    //  F1   F2   F3   F4   F5   F6   F7   F8   F9  F10  F11  F12
        11,  12,  13,  14,  15,  17,  18,  19,  20,  21,  23,  24,
    // F13  F14  F15  F16  F17  F18  F19  F20  F21  F22  F23  F24
        25,  26,  28,  29,  31,  32,  33,  34,  23,  24,  25,  26};
    void EncodeInputKey_v2char(EncodeMetaType meta,byte b,Keys mods){
      if(mods==0){
        inputseq.v2WriteChar(meta,b);
      }else{
        inputseq.v2WriteCsiChar(meta,b,mods);
      }
    }
    void EncodeInputKey_v2ctrl(EncodeMetaType meta,byte b,Keys mods){
      if(mods==0){
        inputseq.v2WriteChar(meta,b);
      }else if(mods==KeyModifiers.Ctrl){
        inputseq.v2WriteChar(meta,(byte)(0x1f&b));
      }else{
        inputseq.v2WriteCsiChar(meta,b,mods);
      }
    }
    internal override int EncodeInputKey1(Keys key,out byte[] seq) {
      return EncodeInputKey2(key,out seq);
    }
    //--------------------------------------------------------------------------
    // Mouse 入力
    //Keys mouseButtonState=0;
    internal override int EncodeInputMouse(Keys key,int x,int y,out byte[] seq,bool release){
      if(tstat.XtermSendMouse){
        Keys mods=key&Keys.Modifiers;
        Keys code=key&Keys.KeyCode;
        int button=0;
        switch(code){
          case Keys.LButton:button|=0;break;
          case Keys.RButton:button|=1;break;
          case Keys.MButton:button|=2;break;
          case Keys.XButton1:button|=64;break;
          case Keys.XButton2:button|=65;break;
          default:button|=3;break; // release
        }

        if((mods&Keys.Shift)!=0)  button|=4;
        if((mods&Keys.Alt)!=0)    button|=8;
        if((mods&Keys.Control)!=0)button|=16;

        if(x<0)x=0;
        if(y<0)y=0;

        inputseq.Clear();
        if(tstat.XtermExMouseSgr){
          // DECSET 1006
          inputseq.Add('\x1b');
          inputseq.Add('[');
          inputseq.Add('<');
          inputseq.WriteNumber((uint)button);
          inputseq.Add(';');
          inputseq.WriteNumber((uint)x);
          inputseq.Add(';');
          inputseq.WriteNumber((uint)y);
          inputseq.Add(release?'m':'M');
        }else if(tstat.XtermExMouseUrxvt){
          if(release)goto skip;

          // DECSET 1015
          inputseq.Add('\x1b');
          inputseq.Add('[');
          inputseq.WriteNumber((uint)button);
          inputseq.Add(';');
          inputseq.WriteNumber((uint)x);
          inputseq.Add(';');
          inputseq.WriteNumber((uint)y);
          inputseq.Add('M');
        }else{
          if(release)goto skip;

          button+=0x20;
          x+=0x20;
          y+=0x20;

          inputseq.Add('\x1b');
          inputseq.Add('[');
          inputseq.Add('M');
          if(tstat.XtermExMouseUtf8){
            // DECSET 1005
            inputseq.WriteUtf8Char(x);
            inputseq.WriteUtf8Char(y);
          }else{
            inputseq.Add((byte)x.Clamp(0x20,0xFF));
            inputseq.Add((byte)y.Clamp(0x20,0xFF));
          }
        }
        seq=inputseq.data;
        return inputseq.length;
      }

    skip:
      return base.EncodeInputMouse(key,x,y,out seq,release);
    }
    #endregion

    protected override void ChangeMode(TerminalMode mode) {
      if(_terminalMode==mode) return;

      if(mode==TerminalMode.Normal) {
        GetDocument().ClearScrollingRegion();
        GetConnection().TerminalOutput.Resize(GetDocument().TerminalWidth, GetDocument().TerminalHeight); //たとえばemacs起動中にリサイズし、シェルへ戻るとシェルは新しいサイズを認識していない
        //RMBoxで確認されたことだが、無用に後方にドキュメントを広げてくる奴がいる。カーソルを123回後方へ、など。
        //場当たり的だが、ノーマルモードに戻る際に後ろの空行を削除することで対応する。
        GLine l = GetDocument().LastLine;
        while(l!=null && l.DisplayLength==0 && l.ID>GetDocument().CurrentLineNumber)
          l = l.PrevLine;

        if(l!=null)  l = l.NextLine;
        if(l!=null)  GetDocument().RemoveAfter(l.ID);
      }else{
        GetDocument().SetScrollingRegion(0, GetDocument().TerminalHeight-1);
      }

      // IsApplicationMode:
      //   ProcessSGR の為に追加された無駄フィールド?
      //   良く分からないが整合性の為にここで設定する。
      this.GetDocument().IsApplicationMode=mode==TerminalMode.Application;

      _terminalMode = mode;
    }
  }
}
