/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: CharDecoder.cs,v 1.3 2010/11/24 16:04:10 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Resources;

using Poderosa.Protocols;

namespace Poderosa.Terminal
{
	internal interface ICharDecoder {
		void OnReception(ByteDataFragment data);
		void Reset(EncodingProfile enc);
		EncodingProfile CurrentEncoding { get; }
	}

	internal class ISO2022CharDecoder : ICharDecoder {

		private class ByteProcessorBuffer {
			private readonly MemoryStream _buffer = new MemoryStream(0x1000);
			public void Reset() {
				_buffer.SetLength(0);
			}
			public void Write(byte[] bytes) {
				_buffer.Write(bytes, 0, bytes.Length);
			}
			public void WriteByte(byte b) {
				_buffer.WriteByte(b);
			}
			public byte[] GetBytes() {
				return _buffer.ToArray();
			}
		}

		private interface IByteProcessor {
			void ProcessByte(byte b);
			void Init();
			void Flush();
		}

		private class ASCIIByteProcessor : IByteProcessor {
			private readonly ICharProcessor _processor;
			public ASCIIByteProcessor(ICharProcessor processor) {
				_processor = processor;
			}
			public void ProcessByte(byte b) {
				_processor.ProcessChar((char)b);
			}
			public void Init() { }
			public void Flush() { }
		}

		private class DECLineByteProcessor : IByteProcessor {
			private static readonly char[] DEC_SPECIAL_LINES;
			private readonly ICharProcessor _processor;

			static DECLineByteProcessor() {
				ResourceManager m = new ResourceManager("Poderosa.TerminalEmulator.rulechars", typeof(GEnv).Assembly);
				DEC_SPECIAL_LINES = m.GetString("DECLineChars").ToCharArray();
				//���\�[�X��?�ɂȂ��Ă����Ƃ���𖄂߂�
				//DEC_SPECIAL_LINES[2] = (char)0x09;
				//DEC_SPECIAL_LINES[3] = (char)0x0C;
				//DEC_SPECIAL_LINES[4] = (char)0x0D;
				//DEC_SPECIAL_LINES[5] = (char)0x0A;
				//DEC_SPECIAL_LINES[8] = (char)0x0A; //NL�Ƃ��邪�悭�킩��Ȃ��BLF�ɂ��Ă����B
				//DEC_SPECIAL_LINES[9] = (char)0x0B;
				DEC_SPECIAL_LINES[31] = (char)0x7F;
			}

			public DECLineByteProcessor(ICharProcessor processor) {
				_processor = processor;
			}

			public void ProcessByte(byte b) {
				char ch=mwg.RosaTerm.AcsSymbolsDefinition.Decode((char)b);
				_processor.ProcessChar(ch);
				//if (0x60 <= b && b <= 0x7F) {
				//  char linechar = DEC_SPECIAL_LINES[b - 0x60];
				//  _processor.ProcessChar(linechar);
				//}
				//else if(b=='+')
				//  _processor.ProcessChar('��');
				//else if(b=='-')
				//  _processor.ProcessChar('��');
				//else if(b=='.')
				//  _processor.ProcessChar('��');
				//else if(b==',')
				//  _processor.ProcessChar('��');
				//else if(b=='0')
				//  _processor.ProcessChar('\u25AF');
				//else
				//  _processor.ProcessChar((char)b);
			}
			public void Init() { }
			public void Flush() { }
		}

		private class CJKByteProcessor : IByteProcessor {
			private readonly ICharProcessor _processor;
			private readonly Encoding _encoding;
			private readonly ByteProcessorBuffer _buffer;
			private readonly byte[] _leadingBytes;
			private readonly byte[] _trailingBytes;

			public CJKByteProcessor(ICharProcessor processor, ByteProcessorBuffer buffer, Encoding encoding, byte[] leadingBytes, byte[] trailingBytes) {
				_processor = processor;
				_encoding = encoding;
				_buffer = buffer;
				_leadingBytes = leadingBytes;
				_trailingBytes = trailingBytes;
			}

			public void ProcessByte(byte b) {
				_buffer.WriteByte(b);
			}
			public void Init() {
				_buffer.Reset();
				if (_leadingBytes != null)
					_buffer.Write(_leadingBytes);
			}
			public void Flush() {
				if (_trailingBytes != null)
					_buffer.Write(_trailingBytes);
				string text = _encoding.GetString(_buffer.GetBytes());
				foreach (char c in text) {
					_processor.ProcessChar(c);
				}
			}
		}

		private class ISO2022JPByteProcessor : CJKByteProcessor {
			private static byte[] _jpLeadingBytes = new byte[] { (byte)0x1b, (byte)'$', (byte)'(', (byte)'D' };

			public ISO2022JPByteProcessor(ICharProcessor processor, ByteProcessorBuffer buffer)
				: base(processor, buffer, Encoding.GetEncoding("iso-2022-jp"), _jpLeadingBytes, null) { }
		}

		private class ISO2022JPKanaByteProcessor : CJKByteProcessor {
			private static byte[] _kanaLeadingBytes = new byte[] { (byte)0x1b, (byte)'(', (byte)'I' };

			public ISO2022JPKanaByteProcessor(ICharProcessor processor, ByteProcessorBuffer buffer)
				: base(processor, buffer, Encoding.GetEncoding("iso-2022-jp"), _kanaLeadingBytes, null) { }
		}

		private class ISO2022KRByteProcessor : CJKByteProcessor {
			private static byte[] _krLeadingBytes = new byte[] { (byte)0x1b, (byte)'$', (byte)')', (byte)'C', (byte)0x0e };

			public ISO2022KRByteProcessor(ICharProcessor processor, ByteProcessorBuffer buffer)
				: base(processor, buffer, Encoding.GetEncoding("iso-2022-kr"), _krLeadingBytes, null) { }
		}


		private class EscapeSequenceBuffer {
			private readonly byte[] _buffer = new byte[10];
			private int _len = 0;

			public EscapeSequenceBuffer() {
			}

			public byte[] Buffer { get { return _buffer; } }
			public int Length { get { return _len; } }

			public void Append(byte b) {
				if (_len < _buffer.Length)
					_buffer[_len++] = b;
			}
			public void Reset() {
				_len = 0;
			}
		}

		//���̓��͂Ɍq���邽�߂̏��
		private enum State {
			Normal, //�W��
			ESC,    //ESC�������Ƃ���
			ESC_DOLLAR,    //ESC $�������Ƃ���
			ESC_BRACKET,   //ESC (�������Ƃ���
			ESC_ENDBRACKET,   //ESC )�������Ƃ���
			ESC_DOLLAR_BRACKET,   //ESC $ (�������Ƃ���
			ESC_DOLLAR_ENDBRACKET    //ESC $ )�������Ƃ���
		}
		private State _state;

		private EscapeSequenceBuffer _escseq;

		//MBCS�̏�ԊǗ�
		private EncodingProfile _encoding;


		//��������������^�[�~�i��
		private ICharProcessor _processor;

		public ISO2022CharDecoder(ICharProcessor processor, EncodingProfile enc) {
			_escseq = new EscapeSequenceBuffer();
            _processor = processor;
			_state = State.Normal;
			_encoding = enc;

			_asciiByteProcessor = new ASCIIByteProcessor(processor);
			_currentByteProcessor = _asciiByteProcessor;
			_G0ByteProcessor = _asciiByteProcessor;
			_G1ByteProcessor = _asciiByteProcessor;

			_byteProcessorBuffer = new ByteProcessorBuffer();
		}
		public EncodingProfile CurrentEncoding {
			get {
				return _encoding;
			}
		}


		private IByteProcessor _currentByteProcessor;
		private IByteProcessor _G0ByteProcessor; //iso2022��G0,G1
		private IByteProcessor _G1ByteProcessor;

		private ASCIIByteProcessor _asciiByteProcessor;

		private DECLineByteProcessor _decLineByteProcessor = null;
		private ISO2022JPByteProcessor _iso2022jpByteProcessor = null;
		private ISO2022JPKanaByteProcessor _iso2022jpkanaByteProcessor = null;
		private ISO2022KRByteProcessor _iso2022krByteProcessor = null;

		private ByteProcessorBuffer _byteProcessorBuffer;

		private DECLineByteProcessor GetDECLineByteProcessor() {
			if (_decLineByteProcessor == null)
				_decLineByteProcessor = new DECLineByteProcessor(_processor);
			return _decLineByteProcessor;
		}

		private ISO2022JPByteProcessor GetISO2022JPByteProcessor() {
			if (_iso2022jpByteProcessor == null)
				_iso2022jpByteProcessor = new ISO2022JPByteProcessor(_processor, _byteProcessorBuffer);
			return _iso2022jpByteProcessor;
		}

		private ISO2022JPKanaByteProcessor GetISO2022JPKanaByteProcessor() {
			if (_iso2022jpkanaByteProcessor == null)
				_iso2022jpkanaByteProcessor = new ISO2022JPKanaByteProcessor(_processor, _byteProcessorBuffer);
			return _iso2022jpkanaByteProcessor;
		}

		private ISO2022KRByteProcessor GetISO2022KRByteProcessor() {
			if (_iso2022krByteProcessor == null)
				_iso2022krByteProcessor = new ISO2022KRByteProcessor(_processor, _byteProcessorBuffer);
			return _iso2022krByteProcessor;
		}

		public void OnReception(ByteDataFragment data) {
			//�����{��
            byte[] t = data.Buffer;
			int last = data.Offset + data.Length;
            int offset = data.Offset;
			while(offset < last) {
				ProcessByte(t[offset++]);
			}
		}

		private void ProcessByte(byte b) {
			if(_processor.State==ProcessCharResult.Escaping)
				_processor.ProcessChar((char)b);
			else {
				if(_state==State.Normal && !IsControlChar(b) && _encoding.IsInterestingByte(b)) {
					PutMBCSByte(b);
				}
				else {
					switch(_state) {
						case State.Normal:
							if(b==0x1B) { //ESC
								_escseq.Reset();
								_escseq.Append(b);
								_state = State.ESC;
							}
							else if(b==14) //SO
								ChangeProcessor(_G1ByteProcessor);
							else if(b==15) //SI
								ChangeProcessor(_G0ByteProcessor);
							else
								ConsumeByte(b);
							break;
						case State.ESC:
							_escseq.Append(b);
							if (b == (byte)'$')
								_state = State.ESC_DOLLAR;
							else if(b==(byte)'(')
								_state=State.ESC_BRACKET;
							else if(b==(byte)')')
								_state = State.ESC_ENDBRACKET;
							else {
								ConsumeBytes(_escseq.Buffer, _escseq.Length);
								_state = State.Normal;
							}
							break;
						case State.ESC_BRACKET:
							_escseq.Append(b);
							if (b == (byte)'0') {
								_G0ByteProcessor = GetDECLineByteProcessor();
								ChangeProcessor(_G0ByteProcessor);
								_state = State.Normal;
							}
							else if(b==(byte)'B' || b==(byte)'J' || b==(byte)'~') { //!!less��ssh2architecture.txt�����Ă����痈���B�ڍׂ͂܂����ׂĂ��Ȃ��B
								_G0ByteProcessor = _asciiByteProcessor;
								ChangeProcessor(_G0ByteProcessor);
								_state = State.Normal;
							}
							else {
								_processor.UnsupportedCharSetDetected((char)b);
								ConsumeBytes(_escseq.Buffer, _escseq.Length);
								_state = State.Normal;
							}
							break;
						case State.ESC_ENDBRACKET:
							_escseq.Append(b);
							if (b == (byte)'0') {
								_G1ByteProcessor = GetDECLineByteProcessor();
								_state = State.Normal;
							}
							else if(b==(byte)'B' || b==(byte)'J' || b==(byte)'~') { //!!less��ssh2architecture.txt�����Ă����痈���B�ڍׂ͂܂����ׂĂ��Ȃ��B
								_G1ByteProcessor = _asciiByteProcessor;
								_state = State.Normal;
							}
							else {
								ConsumeBytes(_escseq.Buffer, _escseq.Length);
								_state = State.Normal;
							}
							break;
						case State.ESC_DOLLAR:
							_escseq.Append(b);
							if (b == (byte)'(')
								_state = State.ESC_DOLLAR_BRACKET;
							else if (b == (byte)')')
								_state = State.ESC_DOLLAR_ENDBRACKET;
							else if (b == (byte)'B' || b == (byte)'@') {
								_G0ByteProcessor = GetISO2022JPByteProcessor();
								ChangeProcessor(_G0ByteProcessor);
								_state = State.Normal;
							}
							else {
								_processor.UnsupportedCharSetDetected((char)b);
								ConsumeBytes(_escseq.Buffer, _escseq.Length);
								_state = State.Normal;
							}
							break;
						case State.ESC_DOLLAR_BRACKET:
							_escseq.Append(b);
							if (b == (byte)'C') {
								_G0ByteProcessor = GetISO2022KRByteProcessor();
								ChangeProcessor(_G0ByteProcessor);
								_state = State.Normal;
							}
							else if (b == (byte)'D') {
								_G0ByteProcessor = GetISO2022JPByteProcessor();
								ChangeProcessor(_G0ByteProcessor);
								_state = State.Normal;
							}
							else if (b == (byte)'I') {
								_G0ByteProcessor = GetISO2022JPKanaByteProcessor();
								ChangeProcessor(_G0ByteProcessor);
								_state = State.Normal;
							}
							else {
								_processor.UnsupportedCharSetDetected((char)b);
								ConsumeBytes(_escseq.Buffer, _escseq.Length);
								_state = State.Normal;
							}
							break;
						case State.ESC_DOLLAR_ENDBRACKET:
							_escseq.Append(b);
							if (b == (byte)'C') {
								_G1ByteProcessor = GetISO2022KRByteProcessor();
								_state = State.Normal;
							}
							else {
								ConsumeBytes(_escseq.Buffer, _escseq.Length);
								_state = State.Normal;
							}
							break;
						default:
							Debug.Assert(false, "unexpected state transition");
							break;
					}
				}
			}
		}

		private void ChangeProcessor(IByteProcessor newprocessor) {
			//�����̂������΃��Z�b�g
			if(_currentByteProcessor != null) {
				_currentByteProcessor.Flush();
			}

			if(newprocessor != null) {
				newprocessor.Init();
			}

			_currentByteProcessor = newprocessor;
			_state = State.Normal;
		}

		private void ConsumeBytes(byte[] buff, int len) {
			for (int i = 0; i < len; i++) {
				ConsumeByte(buff[i]);
			}
		}

		private void ConsumeByte(byte b) {
			_currentByteProcessor.ProcessByte(b);
		}


		public void Reset(EncodingProfile enc) {
			_encoding.Reset();
			_encoding = enc;
			_encoding.Reset();
		}

		private static bool IsControlChar(byte b) {
			return b<=0x1F;
		}

		private void PutMBCSByte(byte b) {
			try {
				char ch = _encoding.PutByte(b);
				if(ch!='\0')
					_processor.ProcessChar(ch);
			}
			catch(Exception) {
				_processor.InvalidCharDetected(_encoding.Buffer);
				_encoding.Reset();
			}
		}
	}
}
