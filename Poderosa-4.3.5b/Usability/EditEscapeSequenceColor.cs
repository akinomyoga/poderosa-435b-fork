/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: EditEscapeSequenceColor.cs,v 1.1 2010/11/19 15:41:31 kzmi Exp $
 */
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using Poderosa.Terminal;
using Poderosa.UI;
using Poderosa.View;
using Poderosa.Usability;

namespace Poderosa.Forms
{
	internal class EditEscapeSequenceColor : System.Windows.Forms.Form
	{
		private Color _backColor;
		private Color _foreColor;
		private ColorButton _backColorBox;
		private ColorButton _foreColorBox;
		private EscapesequenceColorSet _esColorSet;
		private ColorButton[] _colorBoxes;

		private Button _setDefaultButton;
		private System.Windows.Forms.Button _okButton;
		private System.Windows.Forms.Button _cancelButton;
		/// <summary>
		/// 必要なデザイナ変数です。
		/// </summary>
		private System.ComponentModel.Container components = null;

		public EscapesequenceColorSet Result {
			get {
				return _esColorSet;
			}
		}
		public Color GForeColor {
			get {
				return _foreColor;
			}
			set {
				_foreColor = value;
			}
		}
		public Color GBackColor {
			get {
				return _backColor;
			}
			set {
				_backColor = value;
			}
		}

		public EditEscapeSequenceColor(Color back, Color fore, EscapesequenceColorSet cs)
		{
			StringResource sr = TerminalUIPlugin.Instance.Strings;
			//
			// Windows フォーム デザイナ サポートに必要です。
			//
			InitializeComponent();

			_colorBoxes = new ColorButton[16];
			_backColor = back;
			_foreColor = fore;
			_esColorSet = (EscapesequenceColorSet)cs.Clone();
			int ti = 0;

			int y = 8;
			AddBackColorUI(y, ref ti);
			y += 24;

			{
				AddUI(-1, 0, y, ref ti); //-1はデフォルト色設定
				y += 24;

				int y1=y;
				for(int i=0; i<8; i++) {
					AddUI(i, 0, y, ref ti); //-1はデフォルト色設定
					y += 24;
				}
				y=y1;
				for(int i=8; i<16; i++) {
					AddUI(i, 250, y, ref ti);
					y += 24;
				}
			}

			y += 8;
			_setDefaultButton = new Button();
			_setDefaultButton.Left = 356;
			_setDefaultButton.Width = 144;
			_setDefaultButton.Click += new EventHandler(OnSetDefault);
			_setDefaultButton.Text = sr.GetString("Form.EditEscapesequenceColor._setDefaultButton");
			_setDefaultButton.Top = y;
			_setDefaultButton.TabIndex = ti++;
			_setDefaultButton.FlatStyle = FlatStyle.System;
			this.Controls.Add(_setDefaultButton);
			
			y += 32;
			_okButton.Text = sr.GetString("Common.OK");
			_okButton.Top = y;
			_cancelButton.Text = sr.GetString("Common.Cancel");
			_cancelButton.Top = y;
			this.Text = sr.GetString("Form.EditEscapesequenceColor.Text");

			this.ClientSize = new Size(this.ClientSize.Width, y + 32);
		}

		/// <summary>
		/// 使用されているリソースに後処理を実行します。
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows フォーム デザイナで生成されたコード 
		/// <summary>
		/// デザイナ サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディタで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			this._okButton = new System.Windows.Forms.Button();
			this._cancelButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// _okButton
			// 
			this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._okButton.Location = new System.Drawing.Point(338, 232);
			this._okButton.Name = "_okButton";
			this._okButton.Click += new EventHandler(OnOK);
			this._okButton.TabIndex = 0;
			// 
			// _cancelButton
			// 
			this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._cancelButton.Location = new System.Drawing.Point(426, 232);
			this._cancelButton.Name = "_cancelButton";
			this._cancelButton.TabIndex = 1;
			// 
			// EditEscapeSequenceColor
			// 
			this.AcceptButton = this._okButton;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
			this.CancelButton = this._cancelButton;
			this.ClientSize = new System.Drawing.Size(508, 266);
			this.Controls.Add(this._cancelButton);
			this.Controls.Add(this._okButton);
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EditEscapeSequenceColor";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
		}
		#endregion

		private void AddBackColorUI(int y, ref int tabindex) {
			Label num = new Label();
			num.TextAlign = ContentAlignment.MiddleCenter;
			num.Text = TerminalUIPlugin.Instance.Strings.GetString("Caption.EditEscapesequenceColor.BackColor");
			num.Left = 8;
			num.Width = 48;
			num.Top = y;
			num.Height = 24;
			num.TabIndex = tabindex++;

			ColorButton col = new ColorButton();
			col.SelectedColor = _backColor;
			col.Left = 122;
			col.Width = 128;
			col.Top = y+2;
			col.ColorChanged += new ColorButton.NewColorEventHandler(OnNewBackColor);
			col.TabIndex = tabindex++;
			_backColorBox = col;

			this.Controls.Add(num);
			this.Controls.Add(col);
		}

		private void AddUI(int index, int x, int y, ref int tabindex) {
			Label num = new Label();
			num.TextAlign = ContentAlignment.MiddleCenter;
			num.Text = index==-1? TerminalUIPlugin.Instance.Strings.GetString("Caption.EditEscapesequenceColor.DefaultColor") : index.ToString();
			num.Left = x+8;
			num.Width = 48;
			num.Top = y;
			num.Height = 24;
			num.TabIndex = tabindex++;

			Label sample = new Label();
			sample.TextAlign = ContentAlignment.MiddleCenter;
			sample.Text = GetIndexDesc(index);
			sample.Left = x+64;
			sample.Width = 48;
			sample.Top = y;
			sample.Height = 24;
			sample.BorderStyle = BorderStyle.FixedSingle;
			sample.BackColor = _backColor;
			sample.ForeColor = index==-1? _foreColor : _esColorSet[index];
			sample.TabIndex = tabindex++;

			ColorButton col = new ColorButton();
			col.SelectedColor = index==-1? _foreColor : _esColorSet[index];
			col.Left = x+122;
			col.Width = 128;
			col.Top = y+2;
			col.ColorChanged += new ColorButton.NewColorEventHandler(OnNewColor);
			col.Tag = sample;
			col.TabIndex = tabindex++;
			if(index==-1)
				_foreColorBox = col;
			else
				_colorBoxes[index] = col;

			this.Controls.Add(num);
			this.Controls.Add(sample);
			this.Controls.Add(col);
		}

		private void OnNewColor(object sender, Color arg) {
			Control col = (Control)sender;
			((Label)col.Tag).ForeColor = arg;
		}
		private void OnNewBackColor(object sender, Color arg) {
			((Label)_foreColorBox.Tag).BackColor = arg;
			for(int i=0; i<_colorBoxes.Length; i++) {
				((Label)_colorBoxes[i].Tag).BackColor = arg;
			}
			this.Invalidate(true);
		}

		private void OnSetDefault(object sender, EventArgs args) {
			for(int i=0; i<_colorBoxes.Length; i++) {
				Color c = EscapesequenceColorSet.GetDefaultColor(i);
				_colorBoxes[i].SelectedColor = c;
				_colorBoxes[i].Invalidate();
				((Label)_colorBoxes[i].Tag).ForeColor = c;
			}
		}
		private void OnOK(object sender, EventArgs args) {
			_backColor = _backColorBox.SelectedColor;
			_foreColor = _foreColorBox.SelectedColor;
			for(int i=0; i<_colorBoxes.Length; i++) {
				Color c = _colorBoxes[i].SelectedColor;
				_esColorSet[i] = c;				
			}
		}

		private static string GetIndexDesc(int index) {
			char[] t = new char[3];
			t[0] = (index & 4)!=0? '1' : '0';
			t[1] = (index & 2)!=0? '1' : '0';
			t[2] = (index & 1)!=0? '1' : '0';
			return new string(t);
		}

		private void CmdSetPaletteDefault(){
			for(int i=0;i<_colorBoxes.Length;i++){
				Color c = EscapesequenceColorSet.GetDefaultColor(i);
				_colorBoxes[i].SelectedColor = c;
				_colorBoxes[i].Invalidate();
				((Label)_colorBoxes[i].Tag).ForeColor = c;
			}
		}
	}
}
