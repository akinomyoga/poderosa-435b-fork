/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PromptRecognizer.cs,v 1.3 2011/09/07 11:16:14 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

using Poderosa.Util.Collections;
using Poderosa.Document;
using Poderosa.Preferences;

namespace Poderosa.Terminal {
    //�O���ɒʒm����C���^�t�F�[�X
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IPromptProcessor {
        void OnPromptLine(GLine line, string prompt, string command); //line�̓v�����v�g�̂���s�B�J�����g�̍s�Ƃ͌���Ȃ�
        void OnNotPromptLine();
    }




    internal class PromptRecognizer : ITerminalSettingsChangeListener {
        private AbstractTerminal _terminal;
        private Regex _promptExpression;
        private List<IPromptProcessor> _listeners;

        private StringBuilder _commandBuffer;

        private bool _contentUpdateMark;

        private struct PromptInfo {
            public readonly string Prompt;
            public readonly int NextOffset;

            public PromptInfo(string prompt, int nextOffset) {
                Prompt = prompt;
                NextOffset = nextOffset;
            }
        }


        public PromptRecognizer(AbstractTerminal term) {
            _terminal = term;
            _commandBuffer = new StringBuilder();
            ITerminalSettings ts = term.TerminalHost.TerminalSettings;
            ts.AddListener(this);
            _promptExpression = new Regex(ts.ShellScheme.PromptExpression, RegexOptions.Compiled); //����̓V�F���ɂ���
            _listeners = new List<IPromptProcessor>();
        }

        public void AddListener(IPromptProcessor l) {
            _listeners.Add(l);
        }
        public void RemoveListener(IPromptProcessor l) {
            _listeners.Remove(l);
        }

        //���K�\���̃}�b�`�������肷��̂����炷�ׂ��A�^�C�}�[�������Ƃ��ɍX�V����X�^�C�����m��
        public void SetContentUpdateMark() {
            _contentUpdateMark = true;
        }
        public void CheckIfUpdated() {
            if(_contentUpdateMark)
                Recognize();
            _contentUpdateMark = false;
        }


        public void Recognize() {
            if(_promptExpression==null) return;
            if(_terminal.TerminalMode==TerminalMode.Application) return; //�A�v���P�[�V�������[�h�͒ʒm�̕K�v�Ȃ�
            //�ꉞ�A�O��`�F�b�N���ƃf�[�^��M�̗L����������Ă���Ώ����̊ȗ����͉\

            TerminalDocument doc = _terminal.GetDocument();
            GLine prompt_candidate = FindPromptCandidateLine(doc);
            if (prompt_candidate == null)
                return; // too large command line
            string prompt;
            string command;

            if(!DeterminePromptLine(prompt_candidate, doc.CurrentLine.ID, doc.CaretColumn, out prompt, out command)) { //�v�����v�g�ł͂Ȃ��Ƃ�
                NotifyNotPromptLine();
            }
            else {
                Debug.WriteLineIf(DebugOpt.PromptRecog, "Prompt " + command);
                NotifyPromptLine(prompt_candidate, prompt, command);
            }
        }

        //�O���Ŏw�肵��GLine�ɂ��āACurrentLine�܂ł̗̈�Ɋւ��Ĕ�����s��
        public bool DeterminePromptLine(GLine line, int limit_line_id, int limit_column, out string prompt, out string command) {
            prompt = command = null;
            if(_promptExpression==null) return false;
            if(_terminal.TerminalMode==TerminalMode.Application) return false; //�A�v���P�[�V�������[�h�͒ʒm�̕K�v�Ȃ�

            PromptInfo promptInfo = CheckPrompt(line);
            if (promptInfo.Prompt == null) //�v�����v�g�ł͂Ȃ��Ƃ�
                return false;
            else {
                prompt = promptInfo.Prompt;
                command = ParseCommand(line, limit_line_id, limit_column, promptInfo);
                return true;
            }
        }

        //�ӂ��͌��ݍs�����A�O�s������s�I�[�Ȃ炳���̂ڂ�
        private GLine FindPromptCandidateLine(TerminalDocument doc) {
            GLine line = doc.CurrentLine;
            int maxLines = PromptRecognizerPreferences.Instance.PromptSearchMaxLines;
            for (int i = 0; i < maxLines; i++) {
                GLine prev = line.PrevLine;
                if (prev == null || prev.EOLType != EOLType.Continue)
                    return line;
                line = prev;
            }
            return null;
        }
        private PromptInfo CheckPrompt(GLine prompt_candidate) {
            char[] content = prompt_candidate.Text;
            //�����̒����݂�string�����˂΂Ȃ�񂪁A���{�ꕶ�����l����̂͂������̂Ŋȗ��������ł���
            int offset = 0;
            do {
                char ch = content[offset];
                if(ch==GLine.WIDECHAR_PAD) {
                    offset--; //���{�ꕶ���̎�O�܂łŔ���
                    break;
                }
                else if(ch=='\0') {
                    break;
                }

                offset++;
            } while(offset < content.Length);

            //���g���Ȃ���΃`�F�b�N�����Ȃ�
            if (offset <= 0)
                return new PromptInfo(null, 0);

            Match match = _promptExpression.Match(new string(content, 0, offset));
            if (match.Success)
                return new PromptInfo(match.Value, match.Index + match.Length);
            else
                return new PromptInfo(null, 0);
        }

        //�R�}���h�S�e������ ����ʒu�̏I�[��limit_line_id, limit_column�Ō��߂���
        private string ParseCommand(GLine prompt_candidate, int limit_line_id, int limit_column, PromptInfo promptInfo) {
            _commandBuffer.Remove(0, _commandBuffer.Length);

            Debug.Assert(prompt_candidate.ID <= _terminal.GetDocument().CurrentLine.ID);

            int offset = promptInfo.NextOffset; // initial offset of the first line
            for (GLine line = prompt_candidate; line != null && line.ID <= limit_line_id; line = line.NextLine) {
                //�s�S�̂���荞��
                char[] content = line.Text;
                int limit = line.ID == limit_line_id ? Math.Min(limit_column, content.Length) : content.Length;
                while (offset < limit) {
                    char ch = content[offset];
                    if (ch == '\0') {
                        break;
                    }
                    else if (ch != GLine.WIDECHAR_PAD) {
                        _commandBuffer.Append(ch);
                    }

                    offset++;
                }
                offset = 0; // initial offset of the next line
            }

            return _commandBuffer.ToString();
        }

        private void NotifyPromptLine(GLine line, string prompt, string command) {
            foreach(IPromptProcessor l in _listeners) l.OnPromptLine(line, prompt, command);
        }
        private void NotifyNotPromptLine() {
            foreach(IPromptProcessor l in _listeners) l.OnNotPromptLine();
        }

        //ITerminalSettingChangeListener
        public void OnBeginUpdate(ITerminalSettings current) {
        }

        public void OnEndUpdate(ITerminalSettings current) {
            _promptExpression = new Regex(current.ShellScheme.PromptExpression, RegexOptions.Compiled);
            Debug.WriteLineIf(DebugOpt.IntelliSenseMenu, "UpdatePrompt");
        }
    }


    /// <summary>
    /// Preferences for PromptRecognizer
    /// </summary>
    internal class PromptRecognizerPreferences : IPreferenceSupplier {

        private static PromptRecognizerPreferences _instance = new PromptRecognizerPreferences();

        public static PromptRecognizerPreferences Instance {
            get { return _instance; }
        }

        private const int DEFAULT_PROMPT_SEARCH_MAX_LINES = 5;

        private IIntPreferenceItem _promptSearchMaxLines;

        /// <summary>
        /// Get max lines for searching prompt
        /// </summary>
        public int PromptSearchMaxLines {
            get {
                if (_promptSearchMaxLines != null)
                    return _promptSearchMaxLines.Value;
                else
                    return DEFAULT_PROMPT_SEARCH_MAX_LINES;
            }
        }

        #region IPreferenceSupplier

        public string PreferenceID {
            get {
                return TerminalEmulatorPlugin.PLUGIN_ID + ".promptrecognizer";
            }
        }

        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            _promptSearchMaxLines = builder.DefineIntValue(folder, "promptSearchMaxLines", DEFAULT_PROMPT_SEARCH_MAX_LINES, PreferenceValidatorUtil.PositiveIntegerValidator);
        }

        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            return null;
        }

        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
        }

        #endregion
    }

    
}
