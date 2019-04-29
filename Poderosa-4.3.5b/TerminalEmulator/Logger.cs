/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: Logger.cs,v 1.1 2010/11/19 15:41:11 kzmi Exp $
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using Poderosa.Document;
using Poderosa.Protocols;
using Poderosa.Util;
using Poderosa.ConnectionParam;

namespace Poderosa.Terminal {
    internal class LoggerBase {
        private ISimpleLogSettings _logSetting;
        public ISimpleLogSettings LogSettings {
            get {
                return _logSetting;
            }
        }

        public LoggerBase(ISimpleLogSettings log) {
            _logSetting = log;
            Debug.Assert(log!=null);
        }
    }


    internal class BinaryLogger : LoggerBase, IBinaryLogger {
        private Stream _strm;

        public BinaryLogger(ISimpleLogSettings log, Stream s) : base(log) {
            _strm = s;
        }
        public void Write(ByteDataFragment data) {
            _strm.Write(data.Buffer, data.Offset, data.Length);
        }
        public void Flush() {
            _strm.Flush();
        }
        public void Close() {
            _strm.Close();
        }
    }

    internal class DefaultLogger : LoggerBase, ITextLogger {

        private StreamWriter _writer;

        public DefaultLogger(ISimpleLogSettings log, StreamWriter w) : base(log) {
            _writer = w;
        }
        public void WriteLine(GLine line) {
            char[] t = line.Text;
            for(int i=0; i<line.DisplayLength; i++) {
                char ch = t[i];
                if(ch!=GLine.WIDECHAR_PAD) _writer.Write(ch);
            }

            if(line.EOLType!=EOLType.Continue)
                _writer.WriteLine();
        }

        public void Flush() {
            _writer.Flush();
        }
        public void Close() {
            _writer.Close();
        }
        public void Comment(string comment) {
            _writer.Write(comment);
        }
    }


    //複数のログを取るための分岐
    internal class BinaryLoggerList : ListenerList<IBinaryLogger>, IBinaryLogger {
        public void Write(ByteDataFragment data) {
            if(this.IsEmpty) return;

            foreach(IBinaryLogger logger in this) {
                logger.Write(data);
            }
        }

        public void Close() {
            if(this.IsEmpty) return;

            foreach(IBinaryLogger logger in this) {
                logger.Close();
            }
            base.Clear();
        }

        public void Flush() {
            if(this.IsEmpty) return;

            foreach(IBinaryLogger logger in this) {
                logger.Flush();
            }
        }
    }

    internal class TextLoggerList : ListenerList<ITextLogger>, ITextLogger {
        public void WriteLine(GLine line) {
            if(this.IsEmpty) return;
            foreach(ITextLogger logger in this) {
                logger.WriteLine(line);
            }
        }

        public void Comment(string comment) {
            if(this.IsEmpty) return;
            foreach(ITextLogger logger in this) {
                logger.Comment(comment);
            }
        }

        public void Close() {
            if(this.IsEmpty) return;
            foreach(ITextLogger logger in this) {
                logger.Close();
            }
            base.Clear();
        }

        public void Flush() {
            if(this.IsEmpty) return;
            foreach(ITextLogger logger in this) {
                logger.Flush();
            }
        }
    }

    internal class XmlLoggerList : ListenerList<IXmlLogger>, IXmlLogger {
        public void Write(char ch) {
            if(this.IsEmpty) return;
            foreach(IXmlLogger logger in this) {
                logger.Write(ch);
            }
        }

        public void EscapeSequence(char[] body) {
            if(this.IsEmpty) return;
            foreach(IXmlLogger logger in this) {
                logger.EscapeSequence(body);
            }
        }

        public void Comment(string comment) {
            if(this.IsEmpty) return;
            foreach(IXmlLogger logger in this) {
                logger.Comment(comment);
            }
        }

        public void Close() {
            if(this.IsEmpty) return;
            foreach(IXmlLogger logger in this) {
                logger.Close();
            }
            base.Clear();
        }

        public void Flush() {
            if(this.IsEmpty) return;
            foreach(IXmlLogger logger in this) {
                logger.Flush();
            }
        }
    }

    //ログに関する機能のまとめクラス
    internal class LogService : ILogService {
        private BinaryLoggerList _binaryLoggers;
        private TextLoggerList _textLoggers;
        private XmlLoggerList _xmlLoggers;

        public LogService(ITerminalParameter param, ITerminalSettings settings) {
            _binaryLoggers = new BinaryLoggerList();
            _textLoggers = new TextLoggerList();
            _xmlLoggers = new XmlLoggerList();
            ITerminalEmulatorOptions opt = GEnv.Options;
            if(opt.DefaultLogType!=LogType.None)
                ApplySimpleLogSetting(new SimpleLogSettings(opt.DefaultLogType, CreateAutoLogFileName(opt, param, settings)));
        }
        public void AddBinaryLogger(IBinaryLogger logger) {
            _binaryLoggers.Add(logger);
        }
        public void RemoveBinaryLogger(IBinaryLogger logger) {
            _binaryLoggers.Remove(logger);
        }
        public void AddTextLogger(ITextLogger logger) {
            _textLoggers.Add(logger);
        }
        public void RemoveTextLogger(ITextLogger logger) {
            _textLoggers.Remove(logger);
        }
        public void AddXmlLogger(IXmlLogger logger) {
            _xmlLoggers.Add(logger);
        }
        public void RemoveXmlLogger(IXmlLogger logger) {
            _xmlLoggers.Remove(logger);
        }

        //以下はAbstractTerminalから
        public IBinaryLogger BinaryLogger {
            get {
                return _binaryLoggers;
            }
        }
        public ITextLogger TextLogger {
            get {
                return _textLoggers;
            }
        }
        public IXmlLogger XmlLogger {
            get {
                return _xmlLoggers;
            }
        }

        public void Flush() {
            _binaryLoggers.Flush();
            _textLoggers.Flush();
            _xmlLoggers.Flush();
        }
        public void Close(GLine last_line) {
            _textLoggers.WriteLine(last_line); //TextLogは改行ごとであるから、Close時に最終行を書き込むようにする
            InternalClose();
        }
        private void InternalClose() {
            _binaryLoggers.Close();
            _textLoggers.Close();
            _xmlLoggers.Close();
        }
        public void Comment(string comment) {
            _textLoggers.Comment(comment);
            _xmlLoggers.Comment(comment);
        }

        public void ApplyLogSettings(ILogSettings settings, bool clear_previous) {
            if(clear_previous) InternalClose();
            ApplyLogSettingsInternal(settings);
        }
        private void ApplyLogSettingsInternal(ILogSettings settings) {
            ISimpleLogSettings sl = (ISimpleLogSettings)settings.GetAdapter(typeof(ISimpleLogSettings));
            if(sl!=null) {
                ApplySimpleLogSetting(sl);
                return;
            }

            IMultiLogSettings ml = (IMultiLogSettings)settings.GetAdapter(typeof(IMultiLogSettings));
            if(ml!=null) {
                foreach(ILogSettings e in ml) ApplyLogSettingsInternal(e);
            }
        }
        private void ApplySimpleLogSetting(ISimpleLogSettings sl) {
            if(sl.LogType==LogType.None) return;

            FileStream fs = new FileStream(sl.LogPath, sl.LogAppend? FileMode.Append : FileMode.Create, FileAccess.Write);
            ISimpleLogSettings loginfo = (ISimpleLogSettings)sl.Clone();
            switch(sl.LogType) {
                case LogType.Binary:
                    _binaryLoggers.Add(new BinaryLogger(loginfo, fs));
                    break;
                case LogType.Default:
                    _textLoggers.Add(new DefaultLogger(loginfo, new StreamWriter(fs, Encoding.Default)));
                    break;
                case LogType.Xml:
                    _xmlLoggers.Add(new XmlLogger(loginfo, new StreamWriter(fs, Encoding.Default)));
                    break;
            }
        }


        private static string CreateAutoLogFileName(ITerminalEmulatorOptions opt, ITerminalParameter param, ITerminalSettings settings) {
            IAutoLogFileFormatter[] fmts = TerminalEmulatorPlugin.Instance.AutoLogFileFormatter;
            string filebody;
            if(fmts.Length==0) {
                DateTime now = DateTime.Now;
                filebody = String.Format("{0}\\{1}_{2}{3,2:D2}{4,2:D2}", opt.DefaultLogDirectory, ReplaceCharForLogFile(settings.Caption), now.Year, now.Month, now.Day);
            }
            else
                filebody = fmts[0].FormatFileName(opt.DefaultLogDirectory, param, settings);


            int n = 1;
            do {
                string filename;
                if(n==1)
                    filename = String.Format("{0}.log", filebody);
                else
                    filename = String.Format("{0}_{1}.log", filebody, n);

                if(!File.Exists(filename))
                    return filename;
                else
                    n++;
            } while(true);
        }

        private static string ReplaceCharForLogFile(string src) {
            StringBuilder bld = new StringBuilder();
            foreach(char ch in src) {
                if(ch=='\\' || ch=='/' || ch==':' || ch==';' || ch==',' || ch=='*' || ch=='?' || ch=='"' || ch=='<' || ch=='>' || ch=='|')
                    bld.Append('_');
                else
                    bld.Append(ch);
            }
            return bld.ToString();
        }
    }

    //基本マルチログ実装
    internal class MultiLogSettings : IMultiLogSettings {
        private List<ILogSettings> _data;

        public MultiLogSettings() {
            _data = new List<ILogSettings>();
        }

        public void Add(ILogSettings log) {
            Debug.Assert(log!=null);
            _data.Add(log);
        }
        public void Remove(ILogSettings log) {
            if(_data.Contains(log)) {
                _data.Remove(log);
            }
        }
        public void Reset(ILogSettings log) {
            _data.Clear();
            _data.Add(log);
        }

        public ILogSettings Clone() {
            MultiLogSettings ml = new MultiLogSettings();
            foreach(ILogSettings l in _data) ml.Add(l.Clone());
            return ml;
        }

        public IAdaptable GetAdapter(Type adapter) {
            return TerminalEmulatorPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public IEnumerator<ILogSettings> GetEnumerator() {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _data.GetEnumerator();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public enum LogFileCheckResult {
        Create,
        Append,
        Cancel,
        Error
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public static class LogUtil {
        public static string SelectLogFileByDialog(Form parent) {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = "log";
            dlg.Title = "Select Log";
            dlg.Filter = "Log Files(*.log)|*.log|All Files(*.*)|*.*";
            if(dlg.ShowDialog(parent)==DialogResult.OK) {
                return dlg.FileName;
            }
            else
                return null;
        }

        //既存のファイルであったり、書き込み不可能だったら警告する
        public static LogFileCheckResult CheckLogFileName(string path, Form parent) {
            try {
                StringResource sr = GEnv.Strings;
                if(path.Length==0) {
                    GUtil.Warning(parent, sr.GetString("Message.CheckLogFileName.EmptyPath"));
                    return LogFileCheckResult.Cancel;
                }

                string dir = Path.GetDirectoryName(path);
                if(!Directory.Exists(dir)) {
                    GUtil.Warning(parent, String.Format(sr.GetString("Message.CheckLogFileName.BadPathName"), path));
                    return LogFileCheckResult.Cancel;
                }

                if(File.Exists(path)) {
                    if((FileAttributes.ReadOnly & File.GetAttributes(path)) != (FileAttributes)0) {
                        GUtil.Warning(parent, String.Format(sr.GetString("Message.CheckLogFileName.NotWritable"), path));
                        return LogFileCheckResult.Cancel;
                    }

                    Poderosa.Forms.ThreeButtonMessageBox mb = new Poderosa.Forms.ThreeButtonMessageBox();
                    mb.Message = String.Format(sr.GetString("Message.CheckLogFileName.AlreadyExist"), path);
                    mb.Text = sr.GetString("Util.CheckLogFileName.Caption");
                    mb.YesButtonText = sr.GetString("Util.CheckLogFileName.OverWrite");
                    mb.NoButtonText  = sr.GetString("Util.CheckLogFileName.Append");
                    mb.CancelButtonText = sr.GetString("Util.CheckLogFileName.Cancel");
                    switch(mb.ShowDialog(parent)) {
                        case DialogResult.Cancel:
                            return LogFileCheckResult.Cancel;
                        case DialogResult.Yes: //上書き
                            return LogFileCheckResult.Create;
                        case DialogResult.No:  //追記
                            return LogFileCheckResult.Append;
                        default:
                            break;
                    }
                }

                return LogFileCheckResult.Create; //!!書き込み可能なディレクトリにあることを確認すればなおよし

            }
            catch(Exception ex) {
                GUtil.Warning(parent, ex.Message);
                return LogFileCheckResult.Error;
            }
        }

    }
}
