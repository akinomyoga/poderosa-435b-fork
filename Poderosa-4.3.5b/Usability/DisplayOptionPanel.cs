/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: DisplayOptionPanel.cs,v 1.6 2010/12/29 12:51:20 kzmi Exp $
 */
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;

using Poderosa.Util;
using Poderosa.View;
using Poderosa.Terminal;
using Poderosa.Preferences;
using Poderosa.ConnectionParam;
using Poderosa.UI;
using Poderosa.Usability;

namespace Poderosa.Forms
{
	internal class DisplayOptionPanel : UserControl
	{
		private EscapesequenceColorSet _ESColorSet;
		private bool _useClearType;
		private bool _enableBoldStyle;
		private bool _forceBoldStyle;
		private Font _font;
		private Font _cjkFont;
	
		private System.Windows.Forms.GroupBox _colorFontGroup;
		private System.Windows.Forms.Label _bgColorLabel;
		private ColorButton _bgColorBox;
		private System.Windows.Forms.Label _textColorLabel;
		private ColorButton _textColorBox;
		private Button _editColorEscapeSequence;
		private System.Windows.Forms.Label _fontLabel;
		private System.Windows.Forms.Label _fontDescription;
		private System.Windows.Forms.Button _fontSelectButton;
		private ClearTypeAwareLabel _fontSample;
		private Label _backgroundImageLabel;
		private TextBox _backgroundImageBox;
		private Button _backgroundImageSelectButton;
		private Label _imageStyleLabel;
		private ComboBox _imageStyleBox;
		private System.Windows.Forms.GroupBox _caretGroup;
		private System.Windows.Forms.Label _caretStyleLabel;
		private ComboBox _caretStyleBox;
		private CheckBox _caretSpecifyColor;
		private Label _caretColorLabel;
		private ColorButton _caretColorBox;
		private CheckBox _caretBlink;
		private System.Windows.Forms.NumericUpDown _lineSpacingBox;
		private System.Windows.Forms.Label _lineSpacingLabel;
		private Label _pixelsLabel;

		public DisplayOptionPanel() {
			InitializeComponent();
			FillText();
		}
		private void InitializeComponent() {
			this._colorFontGroup = new System.Windows.Forms.GroupBox();
			this._bgColorLabel = new System.Windows.Forms.Label();
			this._bgColorBox = new ColorButton();
			this._textColorLabel = new System.Windows.Forms.Label();
			this._textColorBox = new ColorButton();
			this._editColorEscapeSequence = new Button();
			this._fontLabel = new System.Windows.Forms.Label();
			this._fontDescription = new System.Windows.Forms.Label();
			this._fontSelectButton = new System.Windows.Forms.Button();
			this._fontSample = new Poderosa.Forms.ClearTypeAwareLabel();
			this._backgroundImageLabel = new Label();
			this._backgroundImageBox = new TextBox();
			this._backgroundImageSelectButton = new Button();
			this._imageStyleLabel = new Label();
			this._imageStyleBox = new ComboBox();
			this._caretGroup = new GroupBox();
			this._caretStyleLabel = new System.Windows.Forms.Label();
			this._caretStyleBox = new ComboBox();
			this._caretSpecifyColor = new CheckBox();
			this._caretColorLabel = new Label();
			this._caretColorBox = new ColorButton();
			this._caretBlink = new CheckBox();
			this._lineSpacingLabel = new Label();
			this._lineSpacingBox = new NumericUpDown();
			this._pixelsLabel = new Label();

			this._colorFontGroup.SuspendLayout();
			this._caretGroup.SuspendLayout();
		
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																					   this._caretGroup,
																					   this._colorFontGroup});
			// 
			// _colorFontGroup
			// 
			this._colorFontGroup.Controls.AddRange(new System.Windows.Forms.Control[] {
																						  this._bgColorLabel,
																						  this._bgColorBox,
																						  this._textColorLabel,
																						  this._textColorBox,
																						  this._editColorEscapeSequence,
																						  this._fontLabel,
																						  this._fontDescription,
																						  this._fontSelectButton,
																						  this._fontSample,
																						  this._backgroundImageLabel,
																						  this._backgroundImageBox,
																						  this._backgroundImageSelectButton,
																						  this._imageStyleLabel,
																						  this._imageStyleBox,
																						  this._lineSpacingBox,
																						  this._lineSpacingLabel,
																						  this._pixelsLabel});
			this._colorFontGroup.Location = new System.Drawing.Point(9, 8);
			this._colorFontGroup.Name = "_colorFontGroup";
			this._colorFontGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._colorFontGroup.Size = new System.Drawing.Size(416, 180);
			this._colorFontGroup.TabIndex = 0;
			this._colorFontGroup.TabStop = false;
			// 
			// _bgColorLabel
			// 
			this._bgColorLabel.Location = new System.Drawing.Point(16, 16);
			this._bgColorLabel.Name = "_bgColorLabel";
			this._bgColorLabel.Size = new System.Drawing.Size(72, 24);
			this._bgColorLabel.TabIndex = 1;
			this._bgColorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _bgColorBox
			// 
			//this._bgColorBox.BackColor = System.Drawing.Color.White;
			this._bgColorBox.Location = new System.Drawing.Point(120, 16);
			this._bgColorBox.Name = "_bgColorBox";
			this._bgColorBox.Size = new System.Drawing.Size(152, 20);
			this._bgColorBox.TabIndex = 2;
			this._bgColorBox.ColorChanged += new ColorButton.NewColorEventHandler(this.OnBGColorChanged);
			// 
			// _textColorLabel
			// 
			this._textColorLabel.Location = new System.Drawing.Point(16, 40);
			this._textColorLabel.Name = "_textColorLabel";
			this._textColorLabel.Size = new System.Drawing.Size(72, 24);
			this._textColorLabel.TabIndex = 3;
			this._textColorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _textColorBox
			// 
			this._textColorBox.Location = new System.Drawing.Point(120, 40);
			this._textColorBox.Name = "_textColorBox";
			this._textColorBox.Size = new System.Drawing.Size(152, 20);
			this._textColorBox.TabIndex = 4;
			this._textColorBox.ColorChanged += new ColorButton.NewColorEventHandler(this.OnTextColorChanged);
			//
			// _editColorEscapeSequence
			//
			this._editColorEscapeSequence.Location = new Point(120, 66);
			this._editColorEscapeSequence.Size = new Size(280, 24);
			this._editColorEscapeSequence.TabIndex = 5;
			this._editColorEscapeSequence.Click += new EventHandler(OnEditColorEscapeSequence);
			this._editColorEscapeSequence.FlatStyle = FlatStyle.System;
			// 
			// _fontLabel
			// 
			this._fontLabel.Location = new System.Drawing.Point(16, 96);
			this._fontLabel.Name = "_fontLabel";
			this._fontLabel.Size = new System.Drawing.Size(72, 16);
			this._fontLabel.TabIndex = 6;
			this._fontLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _fontDescription
			// 
			this._fontDescription.Location = new System.Drawing.Point(120, 92);
			this._fontDescription.Name = "_fontDescription";
			this._fontDescription.Size = new System.Drawing.Size(210, 24);
			this._fontDescription.TabIndex = 7;
			this._fontDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _fontSelectButton
			// 
			this._fontSelectButton.Location = new System.Drawing.Point(340, 92);
			this._fontSelectButton.Name = "_fontSelectButton";
			this._fontSelectButton.FlatStyle = FlatStyle.System;
			this._fontSelectButton.Size = new System.Drawing.Size(60, 23);
			this._fontSelectButton.TabIndex = 8;
			this._fontSelectButton.Click += new System.EventHandler(this.OnFontSelect);
			// 
			// _fontSample
			// 
			this._fontSample.BackColor = System.Drawing.Color.White;
			this._fontSample.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this._fontSample.ClearType = false;
			this._fontSample.Location = new System.Drawing.Point(280, 16);
			this._fontSample.Name = "_fontSample";
			this._fontSample.Size = new System.Drawing.Size(120, 46);
			this._fontSample.TabIndex = 9;
			this._fontSample.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// _backgroundImageLabel
			// 
			this._backgroundImageLabel.Location = new System.Drawing.Point(16, 128);
			this._backgroundImageLabel.Name = "_backgroundImageLabel";
			this._backgroundImageLabel.Size = new System.Drawing.Size(72, 16);
			this._backgroundImageLabel.TabIndex = 10;
			this._backgroundImageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _backgroundImageBox
			// 
			this._backgroundImageBox.Location = new System.Drawing.Point(120, 128);
			this._backgroundImageBox.Name = "_backgroundImageBox";
			this._backgroundImageBox.Size = new System.Drawing.Size(260, 19);
			this._backgroundImageBox.TabIndex = 11;
			// 
			// _backgroundImageSelectButton
			// 
			this._backgroundImageSelectButton.Location = new System.Drawing.Point(380, 128);
			this._backgroundImageSelectButton.Name = "_backgroundImageSelectButton";
			this._backgroundImageSelectButton.FlatStyle = FlatStyle.System;
			this._backgroundImageSelectButton.Size = new System.Drawing.Size(19, 19);
			this._backgroundImageSelectButton.TabIndex = 12;
			this._backgroundImageSelectButton.Text = "...";
			this._backgroundImageSelectButton.Click += new EventHandler(OnSelectBackgroundImage);
			// 
			// _imageStyleLabel
			// 
			this._imageStyleLabel.Location = new System.Drawing.Point(16, 153);
			this._imageStyleLabel.Name = "_imageStyleLabel";
			this._imageStyleLabel.Size = new System.Drawing.Size(96, 16);
			this._imageStyleLabel.TabIndex = 13;
			this._imageStyleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _imageStyleBox
			// 
			this._imageStyleBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._imageStyleBox.Location = new System.Drawing.Point(120, 152);
			this._imageStyleBox.Name = "_imageStyleBox";
			this._imageStyleBox.Size = new System.Drawing.Size(100, 19);
			this._imageStyleBox.TabIndex = 14;
			//
			// _lineSpacingLabel
			//
			this._lineSpacingLabel.Location = new Point(270, 153);
			this._lineSpacingLabel.Name = "_llineSpacing";
			this._lineSpacingLabel.Size = new Size(69, 16);
			this._lineSpacingLabel.TabIndex = 14;
			this._lineSpacingLabel.TextAlign = ContentAlignment.MiddleLeft;
			//
			// _lineSpacingBox
			//
			this._lineSpacingBox.Location = new Point(338, 152);
			this._lineSpacingBox.Maximum = new Decimal(new int[] { 10, 0, 0, 0 });
			this._lineSpacingBox.Name = "_lineSpacingBox";
			this._lineSpacingBox.Size = new Size(35, 20);
			this._lineSpacingBox.TabIndex = 18;
			//
			// _pixelsLabel
			//
			this._pixelsLabel.AutoSize = true;
			this._pixelsLabel.Location = new Point(373, 155);
			this._pixelsLabel.Name = "_labelPix";
			this._pixelsLabel.Size = new Size(21, 16);
			this._pixelsLabel.TabIndex = 19;
			// 
			// _caretGroup
			// 
			this._caretGroup.Controls.AddRange(new System.Windows.Forms.Control[] {
																					  this._caretStyleLabel,
																					  this._caretStyleBox,
																					  this._caretSpecifyColor,
																					  this._caretColorBox,
																					  this._caretBlink});
			this._caretGroup.Location = new System.Drawing.Point(9, 200);
			this._caretGroup.Name = "_caretGroup";
			this._caretGroup.FlatStyle = FlatStyle.System;
			this._caretGroup.Size = new System.Drawing.Size(416, 72);
			this._caretGroup.TabIndex = 16;
			this._caretGroup.TabStop = false;
			// 
			// _caretStyleLabel
			// 
			this._caretStyleLabel.Location = new System.Drawing.Point(16, 16);
			this._caretStyleLabel.Name = "_caretStyleLabel";
			this._caretStyleLabel.Size = new System.Drawing.Size(104, 23);
			this._caretStyleLabel.TabIndex = 17;
			this._caretStyleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _caretStyleBox
			// 
			this._caretStyleBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._caretStyleBox.Location = new System.Drawing.Point(120, 16);
			this._caretStyleBox.Name = "_caretStyle";
			this._caretStyleBox.Size = new System.Drawing.Size(152, 20);
			this._caretStyleBox.TabIndex = 18;
			// 
			// _caretBlink
			// 
			this._caretBlink.Location = new System.Drawing.Point(296, 16);
			this._caretBlink.Name = "_caretBlink";
			this._caretBlink.Size = new System.Drawing.Size(96, 20);
			this._caretBlink.TabIndex = 19;
			this._caretBlink.FlatStyle = FlatStyle.System;
			// 
			// _caretSpecifyColor
			// 
			this._caretSpecifyColor.Location = new System.Drawing.Point(16, 40);
			this._caretSpecifyColor.Name = "_caretSpecifyColor";
			this._caretSpecifyColor.Size = new System.Drawing.Size(104, 20);
			this._caretSpecifyColor.TabIndex = 20;
			this._caretSpecifyColor.FlatStyle = FlatStyle.System;
			this._caretSpecifyColor.CheckedChanged += new EventHandler(OnCaretSpecifyColorChanged);
			// 
			// _caretColorBox
			// 
			this._caretColorBox.Location = new System.Drawing.Point(120, 40);
			this._caretColorBox.Name = "_caretColor";
			this._caretColorBox.Size = new System.Drawing.Size(151, 20);
			this._caretColorBox.TabIndex = 21;
			this._caretColorBox.Enabled = false;

			this.BackColor = SystemColors.Window;
			this._colorFontGroup.ResumeLayout();
			this._caretGroup.ResumeLayout();
		}
		private void FillText() {
			StringResource sr = OptionDialogPlugin.Instance.Strings;
			this._colorFontGroup.Text = sr.GetString("Form.OptionDialog._colorFontGroup");
			this._bgColorLabel.Text = sr.GetString("Form.OptionDialog._bgColorLabel");
			this._textColorLabel.Text = sr.GetString("Form.OptionDialog._textColorLabel");
			this._editColorEscapeSequence.Text = sr.GetString("Form.OptionDialog._editEscapeSequenceColorBox");
			this._fontLabel.Text = sr.GetString("Form.OptionDialog._fontLabel");
			this._fontSample.Text = sr.GetString("Common.FontSample");
			this._fontSelectButton.Text = sr.GetString("Form.OptionDialog._fontSelectButton");
			this._backgroundImageLabel.Text = sr.GetString("Form.OptionDialog._backgroundImageLabel");
			this._imageStyleLabel.Text = sr.GetString("Form.OptionDialog._imageStyleLabel");
			this._caretGroup.Text = sr.GetString("Form.OptionDialog._caretGroup");
			this._caretStyleLabel.Text = sr.GetString("Form.OptionDialog._caretStyleLabel");
			this._caretSpecifyColor.Text = sr.GetString("Form.OptionDialog._caretSpecifyColor");
			this._caretColorLabel.Text = sr.GetString("Form.OptionDialog._caretColorLabel");
			this._caretBlink.Text = sr.GetString("Form.OptionDialog._caretBlink");
			this._lineSpacingLabel.Text = sr.GetString("Form.OptionDialog._lineSpacingLabel");
			this._pixelsLabel.Text = sr.GetString("Form.OptionDialog._pixelsLabel");

			this._caretStyleBox.Items.AddRange(GetCaretStyleDescriptions());
			this._imageStyleBox.Items.AddRange(EnumDescAttribute.For(typeof(ImageStyle)).DescriptionCollection());
		}

		public void InitUI(ITerminalEmulatorOptions options) {
			AdjustFontDescription(options.Font, options.CJKFont);
			_fontSample.Font = options.Font;
			_fontSample.BackColor = options.BGColor;
			_fontSample.ForeColor = options.TextColor;
			_fontSample.ClearType = options.UseClearType;
			_fontSample.Invalidate(true);
			_backgroundImageBox.Text = options.BackgroundImageFileName;
			_imageStyleBox.SelectedIndex = (int)options.ImageStyle;
			_bgColorBox.SelectedColor = options.BGColor;
			_textColorBox.SelectedColor =  options.TextColor;
			_caretStyleBox.SelectedIndex = CaretTypeToIndex(options.CaretType);
			_caretSpecifyColor.Checked = !options.CaretColor.IsEmpty;
			_caretBlink.Checked = options.CaretBlink;
			_caretColorBox.SelectedColor =  options.CaretColor;

			_ESColorSet = options.EscapeSequenceColorSet;
			_useClearType = options.UseClearType;
			_enableBoldStyle = options.EnableBoldStyle;
			_font = options.Font;
			_cjkFont = options.CJKFont;
			_forceBoldStyle = options.ForceBoldStyle;
			_lineSpacingBox.Text = options.LineSpacing.ToString();
		}
		public bool Commit(ITerminalEmulatorOptions options) {
			StringResource sr = OptionDialogPlugin.Instance.Strings;
			string itemname = null;
			bool successful = false;
			try {
				if(_backgroundImageBox.Text.Length>0) {
					try {
						Image.FromFile(_backgroundImageBox.Text);
					}
					catch(Exception) {
						GUtil.Warning(this, String.Format(sr.GetString("Message.OptionDialog.InvalidPictureFile"), _backgroundImageBox.Text));
						return false;
					}
				}
				options.BackgroundImageFileName = _backgroundImageBox.Text;
				options.ImageStyle = (ImageStyle)_imageStyleBox.SelectedIndex;
				
				options.BGColor = _bgColorBox.SelectedColor;
				options.TextColor = _textColorBox.SelectedColor;

				options.CaretColor = _caretSpecifyColor.Checked? _caretColorBox.SelectedColor : Color.Empty;
				options.CaretType = IndexToCaretType(_caretStyleBox.SelectedIndex) ;
				options.CaretBlink = _caretBlink.Checked;
				options.EscapeSequenceColorSet = _ESColorSet;
				
				options.UseClearType = _useClearType;
				options.EnableBoldStyle = _enableBoldStyle;
				options.ForceBoldStyle = this._forceBoldStyle;
				options.LineSpacing = int.Parse(this._lineSpacingBox.Text);
				options.Font = _font;
				options.CJKFont = _cjkFont;

				successful = true;
			}
			catch(FormatException) {
				GUtil.Warning(this, String.Format(sr.GetString("Message.OptionDialog.InvalidItem"), itemname));
			}
			catch(InvalidOptionException ex) {
				GUtil.Warning(this, ex.Message);
			}

			return successful;
		}

		private void OnEditColorEscapeSequence(object sender, EventArgs args) {
			EditEscapeSequenceColor dlg = new EditEscapeSequenceColor(_fontSample.BackColor, _fontSample.ForeColor, _ESColorSet);
			if(dlg.ShowDialog(FindForm())==DialogResult.OK) {
				_ESColorSet = dlg.Result;
				_bgColorBox.SelectedColor = dlg.GBackColor;
				_textColorBox.SelectedColor = dlg.GForeColor;
				_fontSample.ForeColor = dlg.GForeColor;
				_fontSample.BackColor = dlg.GBackColor;
				_fontSample.Invalidate();
				_bgColorBox.Invalidate();
				_textColorBox.Invalidate();
			}
		}
		private void OnFontSelect(object sender, EventArgs args) {
			GFontDialog gd = new GFontDialog();
			gd.SetFont(_useClearType, _enableBoldStyle, _forceBoldStyle, _font, _cjkFont);
			DialogResult r = gd.ShowDialog(FindForm());
			if(r==DialogResult.OK) {
				Font f = gd.ASCIIFont;
				_font = f;
				_cjkFont = gd.CJKFont;
				_useClearType = gd.UseClearType;
				_enableBoldStyle = gd.EnableBoldStyle;
				_forceBoldStyle = gd.ForceBoldStyle;
				_fontSample.Font = f;
				AdjustFontDescription(f, gd.CJKFont);
			}
		}
		private void OnSelectBackgroundImage(object sender, EventArgs args) {
			string t = EditRenderProfile.SelectPictureFileByDialog(FindForm());
			if(t!=null) {
				_backgroundImageBox.Text = t;
			}
		}

		private void AdjustFontDescription(Font ascii, Font cjk) {
			int sz = (int)(ascii.Size+0.5);
			if(/*GEnv.Options.Language==Language.English ||*/ ascii.Name==cjk.Name)
				_fontDescription.Text = String.Format("{0},{1}pt", ascii.Name, sz); //Singleをintにキャストすると切り捨てだが、四捨五入にしてほしいので0.5を足してから切り捨てる
			else
				_fontDescription.Text = String.Format("{0}/{1},{2}pt", ascii.Name, cjk.Name, sz); 
		}

		private void OnBGColorChanged(object sender, Color e) {
			_fontSample.BackColor = e;
		}
		private void OnTextColorChanged(object sender, Color e) {
			_fontSample.ForeColor = e;
		}
		private void OnCaretSpecifyColorChanged(object sender, EventArgs args) {
			_caretColorBox.Enabled = _caretSpecifyColor.Checked;
		}


		private static CaretType IndexToCaretType(int index) {
			switch(index) {
				case 0:return CaretType.Box;
				case 1:return CaretType.Line;
				case 2:return CaretType.Underline;
				case 3:return CaretType.BoldUnderline;
				default:return CaretType.Box;
			}
		}
		private static int CaretTypeToIndex(CaretType t) {
			switch(t){
				case CaretType.Box:return 0;
				case CaretType.Line:return 1;
				case CaretType.Underline:return 2;
				case CaretType.BoldUnderline:return 3;
				default:return 0;
			}
		}
		private static object[] GetCaretStyleDescriptions() {
			StringResource sr = OptionDialogPlugin.Instance.Strings;
			object[] r = new object[4];
			//ここの作り方は変則的
			for(int i=0; i<4; i++) {
				r[i] = sr.GetString("Caption.OptionDialog.CaretStyle"+i.ToString());
			}
			return r;
		}
	}


	internal class DisplayOptionPanelExtension : OptionPanelExtensionBase {
		private DisplayOptionPanel _panel;

		public DisplayOptionPanelExtension()
			: base("Form.OptionDialog._displayPanel", 0) {
		}

		public override string[] PreferenceFolderIDsToEdit {
			get {
				return new string[] { "org.poderosa.terminalemulator" };
			}
		}

		public override Control ContentPanel {
			get {
				return _panel;
			}
		}

		public override void InitiUI(IPreferenceFolder[] values) {
			if(_panel==null) _panel = new DisplayOptionPanel();
			_panel.InitUI((ITerminalEmulatorOptions)values[0].QueryAdapter(typeof(ITerminalEmulatorOptions)));
		}

		public override bool Commit(IPreferenceFolder[] values) {
			Debug.Assert(_panel!=null);
			return _panel.Commit((ITerminalEmulatorOptions)values[0].QueryAdapter(typeof(ITerminalEmulatorOptions)));
		}

		public override void Dispose() {
			if(_panel!=null) {
				_panel.Dispose();
				_panel = null;
			}
		}
	}
}
