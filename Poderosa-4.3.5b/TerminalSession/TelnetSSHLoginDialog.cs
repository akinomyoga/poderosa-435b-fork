/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TelnetSSHLoginDialog.cs,v 1.7 2011/08/03 16:07:51 kzmi Exp $
 */
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

using Poderosa.Plugins;
using Poderosa.Util;
using Poderosa.Terminal;
using Poderosa.ConnectionParam;
using Poderosa.Protocols;
using Poderosa.Forms;
using Poderosa.View;

using Granados;


namespace Poderosa.Sessions
{
	internal class TelnetSSHLoginDialog : LoginDialogBase
	{
    private ITerminalParameter _param;

		private bool _initializing;
		private bool _firstFlag;
		//private ConnectionHistory _history;

		private System.ComponentModel.Container components = null;

		private System.Windows.Forms.Label _hostLabel;
		private ComboBox _hostBox;
		private System.Windows.Forms.Label _portLabel;
		private ComboBox _portBox;
		private System.Windows.Forms.Label _methodLabel;
		private ComboBox _methodBox;

		private System.Windows.Forms.GroupBox _sshGroup;
		private System.Windows.Forms.Label _usernameLabel;
		private ComboBox _userNameBox;
		private System.Windows.Forms.Label _authenticationLabel;
		private ComboBox _authOptions;
		private System.Windows.Forms.Label _passphraseLabel;
		private TextBox _passphraseBox;
		private System.Windows.Forms.Label _privateKeyLabel;
		private TextBox _privateKeyFile;
		private Button _privateKeySelect;

		private System.Windows.Forms.GroupBox _terminalGroup;
		private ComboBox _encodingBox;
		private System.Windows.Forms.Label _encodingLabel;
		private System.Windows.Forms.Label _logFileLabel;
		private ComboBox _logFileBox;
		private Button _selectLogButton;
		private System.Windows.Forms.Label _newLineLabel;
		private System.Windows.Forms.Label _localEchoLabel;
		private ComboBox _localEchoBox;
		private ComboBox _newLineBox;
		private System.Windows.Forms.Label _logTypeLabel;
		private ComboBox _logTypeBox;
		private ComboBox _terminalTypeBox;
		private System.Windows.Forms.Label _terminalTypeLabel;

		private Label _autoExecMacroPathLabel;
		private TextBox _autoExecMacroPathBox;
		private Button _selectAutoExecMacroButton;

		public TelnetSSHLoginDialog(IPoderosaMainWindow parentWindow) : base(parentWindow)
		{
			_firstFlag = true;
			_initializing = true;
			//_history = GApp.ConnectionHistory;
			//
			// Windows フォーム デザイナ サポートに必要です。
			//
			InitializeComponent();
			InitializeText();

			InitializeLoginParams();
			_initializing = false;
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

		#region Windows Form Designer generated code
		/// <summary>
		/// デザイナ サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディタで変更しないでください。
		/// this._newLineBox.Items.AddRange(EnumDescAttributeT.For(typeof(NewLine)).DescriptionCollection());
		/// this._logTypeBox.Items.AddRange(EnumDescAttributeT.For(typeof(LogType)).DescriptionCollection());
		/// </summary>
		private void InitializeComponent()
		{
			this._hostLabel = new System.Windows.Forms.Label();
			this._hostBox = new System.Windows.Forms.ComboBox();
			this._methodLabel = new System.Windows.Forms.Label();
			this._methodBox = new System.Windows.Forms.ComboBox();
			this._portLabel = new System.Windows.Forms.Label();
			this._portBox = new System.Windows.Forms.ComboBox();
			this._sshGroup = new System.Windows.Forms.GroupBox();
			this._usernameLabel = new System.Windows.Forms.Label();
			this._userNameBox = new System.Windows.Forms.ComboBox();
			this._authenticationLabel = new System.Windows.Forms.Label();
			this._authOptions = new System.Windows.Forms.ComboBox();
			this._passphraseLabel = new System.Windows.Forms.Label();
			this._passphraseBox = new System.Windows.Forms.TextBox();
			this._privateKeyLabel = new System.Windows.Forms.Label();
			this._privateKeyFile = new System.Windows.Forms.TextBox();
			this._privateKeySelect = new System.Windows.Forms.Button();
			this._terminalGroup = new System.Windows.Forms.GroupBox();
			this._logTypeLabel = new System.Windows.Forms.Label();
			this._logTypeBox = new System.Windows.Forms.ComboBox();
			this._logFileLabel = new System.Windows.Forms.Label();
			this._logFileBox = new System.Windows.Forms.ComboBox();
			this._selectLogButton = new System.Windows.Forms.Button();
			this._encodingLabel = new System.Windows.Forms.Label();
			this._encodingBox = new System.Windows.Forms.ComboBox();
			this._localEchoLabel = new System.Windows.Forms.Label();
			this._localEchoBox = new System.Windows.Forms.ComboBox();
			this._newLineLabel = new System.Windows.Forms.Label();
			this._newLineBox = new System.Windows.Forms.ComboBox();
			this._terminalTypeLabel = new System.Windows.Forms.Label();
			this._terminalTypeBox = new System.Windows.Forms.ComboBox();
			this._autoExecMacroPathLabel = new System.Windows.Forms.Label();
			this._autoExecMacroPathBox = new System.Windows.Forms.TextBox();
			this._selectAutoExecMacroButton = new System.Windows.Forms.Button();
			this._sshGroup.SuspendLayout();
			this._terminalGroup.SuspendLayout();
			this.SuspendLayout();
			// 
			// _loginButton
			// 
			this._loginButton.ImageIndex = 0;
			this._loginButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._loginButton.Location = new System.Drawing.Point(160, 424);
			this._loginButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._loginButton.Size = new System.Drawing.Size(72, 25);
			this._loginButton.TabIndex = 11;
			// 
			// _cancelButton
			// 
			this._cancelButton.ImageIndex = 0;
			this._cancelButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._cancelButton.Location = new System.Drawing.Point(248, 424);
			this._cancelButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._cancelButton.Size = new System.Drawing.Size(72, 25);
			this._cancelButton.TabIndex = 12;
			// 
			// _hostLabel
			// 
			this._hostLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._hostLabel.Location = new System.Drawing.Point(16, 12);
			this._hostLabel.Name = "_hostLabel";
			this._hostLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._hostLabel.Size = new System.Drawing.Size(80, 16);
			this._hostLabel.TabIndex = 0;
			this._hostLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _hostBox
			// 
			this._hostBox.Location = new System.Drawing.Point(104, 8);
			this._hostBox.Name = "_hostBox";
			this._hostBox.Size = new System.Drawing.Size(208, 20);
			this._hostBox.TabIndex = 1;
			this._hostBox.SelectedIndexChanged += new System.EventHandler(this.OnHostIsSelected);
			// 
			// _methodLabel
			// 
			this._methodLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._methodLabel.Location = new System.Drawing.Point(16, 36);
			this._methodLabel.Name = "_methodLabel";
			this._methodLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._methodLabel.Size = new System.Drawing.Size(80, 16);
			this._methodLabel.TabIndex = 2;
			this._methodLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _methodBox
			// 
			this._methodBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._methodBox.Location = new System.Drawing.Point(104, 32);
			this._methodBox.Name = "_methodBox";
			this._methodBox.Size = new System.Drawing.Size(208, 20);
			this._methodBox.TabIndex = 3;
			this._methodBox.SelectedIndexChanged += new System.EventHandler(this.AdjustConnectionUI);
			// 
			// _portLabel
			// 
			this._portLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._portLabel.Location = new System.Drawing.Point(16, 60);
			this._portLabel.Name = "_portLabel";
			this._portLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._portLabel.Size = new System.Drawing.Size(80, 16);
			this._portLabel.TabIndex = 4;
			this._portLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _portBox
			// 
			this._portBox.Location = new System.Drawing.Point(104, 56);
			this._portBox.Name = "_portBox";
			this._portBox.Size = new System.Drawing.Size(208, 20);
			this._portBox.TabIndex = 5;
			// 
			// _sshGroup
			// 
			this._sshGroup.Controls.Add(this._usernameLabel);
			this._sshGroup.Controls.Add(this._userNameBox);
			this._sshGroup.Controls.Add(this._authenticationLabel);
			this._sshGroup.Controls.Add(this._authOptions);
			this._sshGroup.Controls.Add(this._passphraseLabel);
			this._sshGroup.Controls.Add(this._passphraseBox);
			this._sshGroup.Controls.Add(this._privateKeyLabel);
			this._sshGroup.Controls.Add(this._privateKeyFile);
			this._sshGroup.Controls.Add(this._privateKeySelect);
			this._sshGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._sshGroup.Location = new System.Drawing.Point(8, 88);
			this._sshGroup.Name = "_sshGroup";
			this._sshGroup.Size = new System.Drawing.Size(312, 112);
			this._sshGroup.TabIndex = 6;
			this._sshGroup.TabStop = false;
			// 
			// _usernameLabel
			// 
			this._usernameLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._usernameLabel.Location = new System.Drawing.Point(8, 16);
			this._usernameLabel.Name = "_usernameLabel";
			this._usernameLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._usernameLabel.Size = new System.Drawing.Size(80, 16);
			this._usernameLabel.TabIndex = 0;
			this._usernameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _userNameBox
			// 
			this._userNameBox.Location = new System.Drawing.Point(96, 16);
			this._userNameBox.Name = "_userNameBox";
			this._userNameBox.Size = new System.Drawing.Size(208, 20);
			this._userNameBox.TabIndex = 1;
			// 
			// _authenticationLabel
			// 
			this._authenticationLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._authenticationLabel.Location = new System.Drawing.Point(8, 40);
			this._authenticationLabel.Name = "_authenticationLabel";
			this._authenticationLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._authenticationLabel.Size = new System.Drawing.Size(80, 16);
			this._authenticationLabel.TabIndex = 2;
			this._authenticationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _authOptions
			// 
			this._authOptions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._authOptions.Location = new System.Drawing.Point(96, 40);
			this._authOptions.Name = "_authOptions";
			this._authOptions.Size = new System.Drawing.Size(208, 20);
			this._authOptions.TabIndex = 3;
			this._authOptions.SelectedIndexChanged += new System.EventHandler(this.AdjustAuthenticationUI);
			// 
			// _passphraseLabel
			// 
			this._passphraseLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._passphraseLabel.Location = new System.Drawing.Point(8, 64);
			this._passphraseLabel.Name = "_passphraseLabel";
			this._passphraseLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._passphraseLabel.Size = new System.Drawing.Size(80, 16);
			this._passphraseLabel.TabIndex = 4;
			this._passphraseLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _passphraseBox
			// 
			this._passphraseBox.Location = new System.Drawing.Point(96, 64);
			this._passphraseBox.Name = "_passphraseBox";
			this._passphraseBox.PasswordChar = '*';
			this._passphraseBox.Size = new System.Drawing.Size(208, 19);
			this._passphraseBox.TabIndex = 5;
			// 
			// _privateKeyLabel
			// 
			this._privateKeyLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._privateKeyLabel.Location = new System.Drawing.Point(8, 88);
			this._privateKeyLabel.Name = "_privateKeyLabel";
			this._privateKeyLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._privateKeyLabel.Size = new System.Drawing.Size(80, 16);
			this._privateKeyLabel.TabIndex = 6;
			this._privateKeyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _privateKeyFile
			// 
			this._privateKeyFile.Location = new System.Drawing.Point(96, 88);
			this._privateKeyFile.Name = "_privateKeyFile";
			this._privateKeyFile.Size = new System.Drawing.Size(188, 19);
			this._privateKeyFile.TabIndex = 7;
			// 
			// _privateKeySelect
			// 
			this._privateKeySelect.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._privateKeySelect.ImageIndex = 0;
			this._privateKeySelect.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._privateKeySelect.Location = new System.Drawing.Point(285, 87);
			this._privateKeySelect.Name = "_privateKeySelect";
			this._privateKeySelect.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._privateKeySelect.Size = new System.Drawing.Size(19, 19);
			this._privateKeySelect.TabIndex = 8;
			this._privateKeySelect.Text = "...";
			this._privateKeySelect.Click += new System.EventHandler(this.OnOpenPrivateKey);
			// 
			// _terminalGroup
			// 
			this._terminalGroup.Controls.Add(this._logTypeLabel);
			this._terminalGroup.Controls.Add(this._logTypeBox);
			this._terminalGroup.Controls.Add(this._logFileLabel);
			this._terminalGroup.Controls.Add(this._logFileBox);
			this._terminalGroup.Controls.Add(this._selectLogButton);
			this._terminalGroup.Controls.Add(this._encodingLabel);
			this._terminalGroup.Controls.Add(this._encodingBox);
			this._terminalGroup.Controls.Add(this._localEchoLabel);
			this._terminalGroup.Controls.Add(this._localEchoBox);
			this._terminalGroup.Controls.Add(this._newLineLabel);
			this._terminalGroup.Controls.Add(this._newLineBox);
			this._terminalGroup.Controls.Add(this._terminalTypeLabel);
			this._terminalGroup.Controls.Add(this._terminalTypeBox);
			this._terminalGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._terminalGroup.Location = new System.Drawing.Point(8, 208);
			this._terminalGroup.Name = "_terminalGroup";
			this._terminalGroup.Size = new System.Drawing.Size(312, 168);
			this._terminalGroup.TabIndex = 7;
			this._terminalGroup.TabStop = false;
			// 
			// _logTypeLabel
			// 
			this._logTypeLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._logTypeLabel.Location = new System.Drawing.Point(8, 16);
			this._logTypeLabel.Name = "_logTypeLabel";
			this._logTypeLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._logTypeLabel.Size = new System.Drawing.Size(96, 16);
			this._logTypeLabel.TabIndex = 0;
			this._logTypeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _logTypeBox
			// 
			this._logTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._logTypeBox.Location = new System.Drawing.Point(112, 16);
			this._logTypeBox.Name = "_logTypeBox";
			this._logTypeBox.Size = new System.Drawing.Size(96, 20);
			this._logTypeBox.TabIndex = 1;
			this._logTypeBox.SelectionChangeCommitted += new System.EventHandler(this.OnLogTypeChanged);
			// 
			// _logFileLabel
			// 
			this._logFileLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._logFileLabel.Location = new System.Drawing.Point(8, 40);
			this._logFileLabel.Name = "_logFileLabel";
			this._logFileLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._logFileLabel.Size = new System.Drawing.Size(88, 16);
			this._logFileLabel.TabIndex = 2;
			this._logFileLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _logFileBox
			// 
			this._logFileBox.Location = new System.Drawing.Point(112, 40);
			this._logFileBox.Name = "_logFileBox";
			this._logFileBox.Size = new System.Drawing.Size(172, 20);
			this._logFileBox.TabIndex = 3;
			// 
			// _selectLogButton
			// 
			this._selectLogButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._selectLogButton.ImageIndex = 0;
			this._selectLogButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._selectLogButton.Location = new System.Drawing.Point(285, 41);
			this._selectLogButton.Name = "_selectLogButton";
			this._selectLogButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._selectLogButton.Size = new System.Drawing.Size(19, 19);
			this._selectLogButton.TabIndex = 4;
			this._selectLogButton.Text = "...";
			this._selectLogButton.Click += new System.EventHandler(this.SelectLog);
			// 
			// _encodingLabel
			// 
			this._encodingLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._encodingLabel.Location = new System.Drawing.Point(8, 64);
			this._encodingLabel.Name = "_encodingLabel";
			this._encodingLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._encodingLabel.Size = new System.Drawing.Size(96, 16);
			this._encodingLabel.TabIndex = 5;
			this._encodingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _encodingBox
			// 
			this._encodingBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._encodingBox.Location = new System.Drawing.Point(112, 64);
			this._encodingBox.Name = "_encodingBox";
			this._encodingBox.Size = new System.Drawing.Size(96, 20);
			this._encodingBox.TabIndex = 6;
			// 
			// _localEchoLabel
			// 
			this._localEchoLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._localEchoLabel.Location = new System.Drawing.Point(8, 88);
			this._localEchoLabel.Name = "_localEchoLabel";
			this._localEchoLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._localEchoLabel.Size = new System.Drawing.Size(96, 16);
			this._localEchoLabel.TabIndex = 7;
			this._localEchoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _localEchoBox
			// 
			this._localEchoBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._localEchoBox.Location = new System.Drawing.Point(112, 88);
			this._localEchoBox.Name = "_localEchoBox";
			this._localEchoBox.Size = new System.Drawing.Size(96, 20);
			this._localEchoBox.TabIndex = 8;
			// 
			// _newLineLabel
			// 
			this._newLineLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._newLineLabel.Location = new System.Drawing.Point(8, 112);
			this._newLineLabel.Name = "_newLineLabel";
			this._newLineLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._newLineLabel.Size = new System.Drawing.Size(96, 16);
			this._newLineLabel.TabIndex = 9;
			this._newLineLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _newLineBox
			// 
			this._newLineBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._newLineBox.Location = new System.Drawing.Point(112, 112);
			this._newLineBox.Name = "_newLineBox";
			this._newLineBox.Size = new System.Drawing.Size(96, 20);
			this._newLineBox.TabIndex = 10;
			// 
			// _terminalTypeLabel
			// 
			this._terminalTypeLabel.Location = new System.Drawing.Point(8, 136);
			this._terminalTypeLabel.Name = "_terminalTypeLabel";
			this._terminalTypeLabel.Size = new System.Drawing.Size(96, 23);
			this._terminalTypeLabel.TabIndex = 11;
			this._terminalTypeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _terminalTypeBox
			// 
			this._terminalTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._terminalTypeBox.Location = new System.Drawing.Point(112, 136);
			this._terminalTypeBox.Name = "_terminalTypeBox";
			this._terminalTypeBox.Size = new System.Drawing.Size(96, 20);
			this._terminalTypeBox.TabIndex = 12;
			// 
			// _autoExecMacroPathLabel
			// 
			this._autoExecMacroPathLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._autoExecMacroPathLabel.Location = new System.Drawing.Point(16, 391);
			this._autoExecMacroPathLabel.Name = "_autoExecMacroPathLabel";
			this._autoExecMacroPathLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._autoExecMacroPathLabel.Size = new System.Drawing.Size(100, 16);
			this._autoExecMacroPathLabel.TabIndex = 8;
			this._autoExecMacroPathLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _autoExecMacroPathBox
			// 
			this._autoExecMacroPathBox.Location = new System.Drawing.Point(120, 390);
			this._autoExecMacroPathBox.Name = "_autoExecMacroPathBox";
			this._autoExecMacroPathBox.Size = new System.Drawing.Size(172, 19);
			this._autoExecMacroPathBox.TabIndex = 9;
			// 
			// _selectAutoExecMacroButton
			// 
			this._selectAutoExecMacroButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._selectAutoExecMacroButton.ImageIndex = 0;
			this._selectAutoExecMacroButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this._selectAutoExecMacroButton.Location = new System.Drawing.Point(293, 390);
			this._selectAutoExecMacroButton.Name = "_selectAutoExecMacroButton";
			this._selectAutoExecMacroButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this._selectAutoExecMacroButton.Size = new System.Drawing.Size(19, 19);
			this._selectAutoExecMacroButton.TabIndex = 10;
			this._selectAutoExecMacroButton.Text = "...";
			this._selectAutoExecMacroButton.Click += new System.EventHandler(this._selectAutoExecMacroButton_Click);
			// 
			// TelnetSSHLoginDialog
			// 
			this.AcceptButton = this._loginButton;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
			this.CancelButton = this._cancelButton;
			this.ClientSize = new System.Drawing.Size(330, 457);
			this.Controls.Add(this._hostLabel);
			this.Controls.Add(this._hostBox);
			this.Controls.Add(this._methodLabel);
			this.Controls.Add(this._methodBox);
			this.Controls.Add(this._portLabel);
			this.Controls.Add(this._portBox);
			this.Controls.Add(this._sshGroup);
			this.Controls.Add(this._autoExecMacroPathLabel);
			this.Controls.Add(this._terminalGroup);
			this.Controls.Add(this._autoExecMacroPathBox);
			this.Controls.Add(this._selectAutoExecMacroButton);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "TelnetSSHLoginDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Controls.SetChildIndex(this._selectAutoExecMacroButton, 0);
			this.Controls.SetChildIndex(this._autoExecMacroPathBox, 0);
			this.Controls.SetChildIndex(this._terminalGroup, 0);
			this.Controls.SetChildIndex(this._autoExecMacroPathLabel, 0);
			this.Controls.SetChildIndex(this._sshGroup, 0);
			this.Controls.SetChildIndex(this._portBox, 0);
			this.Controls.SetChildIndex(this._portLabel, 0);
			this.Controls.SetChildIndex(this._methodBox, 0);
			this.Controls.SetChildIndex(this._methodLabel, 0);
			this.Controls.SetChildIndex(this._loginButton, 0);
			this.Controls.SetChildIndex(this._hostBox, 0);
			this.Controls.SetChildIndex(this._cancelButton, 0);
			this.Controls.SetChildIndex(this._hostLabel, 0);
			this._sshGroup.ResumeLayout(false);
			this._sshGroup.PerformLayout();
			this._terminalGroup.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void InitializeText() {
			this._hostLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._hostLabel");
			this._portLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._portLabel");
			this._methodLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._methodLabel");
			this._sshGroup.Text = TEnv.Strings.GetString("Form.LoginDialog._sshGroup");
			this._privateKeyLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._privateKeyLabel");
			this._authenticationLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._authenticationLabel");
			this._passphraseLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._passphraseLabel");
			this._usernameLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._usernameLabel");
			this._autoExecMacroPathLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._autoExecMacroPathLabel");
			this._terminalGroup.Text = TEnv.Strings.GetString("Form.LoginDialog._terminalGroup");
			this._localEchoLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._localEchoLabel");
			this._newLineLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._newLineLabel");
			this._logFileLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._logFileLabel");
			this._encodingLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._encodingLabel");
			this._logTypeLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._logTypeLabel");
			this._terminalTypeLabel.Text = TEnv.Strings.GetString("Form.LoginDialog._terminalTypeLabel");
			this._loginButton.Text = TEnv.Strings.GetString("Common.OK");
			this._cancelButton.Text = TEnv.Strings.GetString("Common.Cancel");
			this.Text = TEnv.Strings.GetString("Form.LoginDialog.Text");

			this._methodBox.Items.AddRange(new object[] {
				"Telnet",
				"SSH1",
				"SSH2"
			});
			this._logTypeBox.Items.AddRange(EnumDescAttribute.For(typeof(LogType)).DescriptionCollection());
			this._terminalTypeBox.Items.AddRange(EnumDescAttribute.For(typeof(TerminalType)).DescriptionCollection());
			this._encodingBox.Items.AddRange(EnumDescAttribute.For(typeof(EncodingType)).DescriptionCollection());
			this._localEchoBox.Items.AddRange(new object[] {
			   TEnv.Strings.GetString("Common.DoNot"),
			   TEnv.Strings.GetString("Common.Do")
			});
			this._newLineBox.Items.AddRange(EnumDescAttribute.For(typeof(NewLine)).DescriptionCollection());
			this._authOptions.Items.AddRange(EnumDescAttribute.For(typeof(AuthType)).DescriptionCollection());

			this._passphraseBox.Text = "";
			this._privateKeyFile.Text = "";
		}

		private void AdjustConnectionUI(object sender, System.EventArgs e) {
			if(_initializing) return;
			if(_methodBox.Text=="Telnet") {
                _portBox.SelectedIndex = FindPortIndex(23);
			}
			else {
				_portBox.SelectedIndex = FindPortIndex(22); //SSH:22
				if(_authOptions.SelectedIndex==-1) _authOptions.SelectedIndex=0;
			}
			EnableValidControls();
		}
		private void AdjustAuthenticationUI(object sender, System.EventArgs e) {
			EnableValidControls();
		}

		private void InitializeLoginParams() {
            //作っておく
            AdjustLoginDialogUISupport("org.poderosa.terminalsessions.loginDialogUISupport", "telnetSSHLoginDialogUISupport");

            LoginDialogInitializeInfo info = new LoginDialogInitializeInfo();
            IExtensionPoint extp = TerminalSessionsPlugin.Instance.PoderosaWorld.PluginManager.FindExtensionPoint("org.poderosa.terminalsessions.telnetSSHLoginDialogInitializer");
            Debug.Assert(extp!=null);
            ITelnetSSHLoginDialogInitializer[] suppliers = (ITelnetSSHLoginDialogInitializer[])extp.GetExtensions();

            //順序を問わず全部突っ込む
            foreach(ITelnetSSHLoginDialogInitializer s in suppliers)
                s.ApplyLoginDialogInfo(info);

			foreach(string h in info.Hosts) _hostBox.Items.Add(h);
			if(_hostBox.Items.Count>0) _hostBox.SelectedIndex = 0;

			foreach(string a in info.Accounts) _userNameBox.Items.Add(a);
			if(_userNameBox.Items.Count>0) _userNameBox.SelectedIndex = 0;

			foreach(int p in info.Ports) _portBox.Items.Add(PortDescription(p));

            //TODO ここをEXTP化して、ログの初期設定を行えるようにする
            _logTypeBox.SelectedIndex = 0;
        }

		private void EnableValidControls() {
			bool ssh = _methodBox.Text!="Telnet";
			bool pubkey = _authOptions.SelectedIndex==(int)AuthType.PublicKey;
			bool kbd = _authOptions.SelectedIndex==(int)AuthType.KeyboardInteractive;

			_userNameBox.Enabled = ssh;
			_authOptions.Enabled = ssh;
			_passphraseBox.Enabled = ssh && (pubkey || !kbd);
			_privateKeyFile.Enabled = ssh && pubkey;
			_privateKeySelect.Enabled = ssh && pubkey;
			
			bool e = ((LogType)EnumDescAttribute.For(typeof(LogType)).FromDescription(_logTypeBox.Text, LogType.None)!=LogType.None);
			_logFileBox.Enabled = e;
			_selectLogButton.Enabled = e;
		}

        //拡張ポイントから取ったインタフェースを使ってApplyParam。なければデフォルトで
        public void ApplyParam() {
            ITerminalParameter parameter = null;
            ITerminalSettings settings = null;
            if(_loginDialogUISupport!=null) {
                _loginDialogUISupport.FillTopDestination(typeof(ITCPParameter), out parameter, out settings);
            }
            if(parameter==null) parameter = (ITerminalParameter)TerminalSessionsPlugin.Instance.ProtocolService.CreateDefaultSSHParameter().GetAdapter(typeof(ITerminalParameter));
            if(settings==null) settings = TerminalSessionsPlugin.Instance.TerminalEmulatorService.CreateDefaultTerminalSettings("", null);

            ApplyParam(parameter, settings);
        }

        public void ApplyParam(IAdaptable destination, ITerminalSettings terminal) {
			_initializing = true;
            Debug.Assert(destination!=null);
            Debug.Assert(terminal!=null);
            this.TerminalSettings = terminal.Clone();
            
            ITCPParameter tcp_destination = (ITCPParameter)destination.GetAdapter(typeof(ITCPParameter));
            ISSHLoginParameter ssh_destination = (ISSHLoginParameter)destination.GetAdapter(typeof(ISSHLoginParameter));
            bool is_telnet = ssh_destination==null;

			_methodBox.SelectedIndex = is_telnet? 0 : ssh_destination.Method==SSHProtocol.SSH1? 1 : 2;
			_portBox.SelectedIndex = _portBox.FindStringExact(PortDescription(tcp_destination.Port));
			if(ssh_destination!=null) {
				_userNameBox.SelectedIndex = _userNameBox.FindStringExact(ssh_destination.Account);
                _passphraseBox.Text = ssh_destination.PasswordOrPassphrase;

                if(ssh_destination.AuthenticationType==AuthenticationType.PublicKey)
                    _privateKeyFile.Text = ssh_destination.IdentityFileName;
				else
					_privateKeyFile.Text = "";
                _authOptions.SelectedIndex = ToAuthenticationIndex(ssh_destination.AuthenticationType);
			}

			IAutoExecMacroParameter autoExecParams = destination.GetAdapter(typeof(IAutoExecMacroParameter)) as IAutoExecMacroParameter;
            if (autoExecParams != null && TelnetSSHPlugin.Instance.MacroEngine != null) {
				_autoExecMacroPathBox.Text = (autoExecParams.AutoExecMacroPath != null) ? autoExecParams.AutoExecMacroPath : String.Empty;
			}
			else {
				_autoExecMacroPathLabel.Enabled = false;
				_autoExecMacroPathBox.Enabled = false;
				_selectAutoExecMacroButton.Enabled = false;
			}

			_encodingBox.SelectedIndex = (int)terminal.Encoding;
			_newLineBox.SelectedIndex = (int)terminal.TransmitNL;
			_localEchoBox.SelectedIndex = terminal.LocalEcho? 1 : 0;
			_terminalTypeBox.SelectedIndex = (int)terminal.TerminalType;
			_initializing = false;
			
			EnableValidControls();
		}
       
		private void OnHostIsSelected(object sender, System.EventArgs e) {
			if(_initializing || _loginDialogUISupport==null) return;

            ITerminalParameter parameter = null;
            ITerminalSettings settings = null;
            _loginDialogUISupport.FillCorrespondingDestination(typeof(ITCPParameter), _hostBox.Text, out parameter, out settings);
            if(parameter!=null && settings!=null) { //原理的には片方のみの適用もあるが
                ISSHLoginParameter ssh = (ISSHLoginParameter)parameter.GetAdapter(typeof(ISSHLoginParameter));
                ITCPParameter tcp = (ITCPParameter)parameter.GetAdapter(typeof(ITCPParameter));
                if(ssh!=null) {
                    ssh.PasswordOrPassphrase = "";
                    if(TerminalSessionsPlugin.Instance.ProtocolService.ProtocolOptions.RetainsPassphrase)
                        ssh.PasswordOrPassphrase = TerminalSessionsPlugin.Instance.ProtocolService.PassphraseCache.GetOrEmpty(tcp.Destination, ssh.Account);
                }

                ApplyParam(parameter, settings);
            }
		}

		private static int ParsePort(string text) {
			//頻出のやつ
			if(text.IndexOf("(22)")!=-1)
				return 22;
			if(text.IndexOf("(23)")!=-1)
				return 23;
			
			try {
				return Int32.Parse(text);
			}
			catch(FormatException) {
				throw new FormatException(String.Format(TEnv.Strings.GetString("Message.LoginDialog.InvalidPort"), text));
			}
		}
		private static ConnectionMethod ParseMethod(string text) {
			if(text.IndexOf("SSH1")!=-1)
				return ConnectionMethod.SSH1;
			else if(text.IndexOf("SSH2")!=-1)
				return ConnectionMethod.SSH2;
			else if(text.IndexOf("Telnet")!=-1)
				return ConnectionMethod.Telnet;
			else
				throw new ArgumentException("unknown method "+text);
		}

		private static string PortDescription(int port) {
			if(port==22)
				return "SSH(22)";
			else if(port==23)
				return "Telnet(23)";
			else
				return port.ToString();
		}


		private void OnOpenPrivateKey(object sender, System.EventArgs e) {
			string fn = TerminalUtil.SelectPrivateKeyFileByDialog(this);
			if(fn!=null) _privateKeyFile.Text = fn;
            _privateKeySelect.Focus(); //どっちにしても次のフォーカスは鍵選択ボタンへ
		}

        protected override ITerminalParameter PrepareTerminalParameter() {
            _param = ValidateContent();
            return _param;
        }
        protected override void StartConnection() {
            ISSHLoginParameter ssh = (ISSHLoginParameter)_param.GetAdapter(typeof(ISSHLoginParameter));
            ITCPParameter tcp = (ITCPParameter)_param.GetAdapter(typeof(ITCPParameter));
            IProtocolService protocolservice = TerminalSessionsPlugin.Instance.ProtocolService;
            if(ssh!=null)
                _connector = protocolservice.AsyncSSHConnect(this, ssh);
            else
                _connector = protocolservice.AsyncTelnetConnect(this, tcp);

            if(_connector==null) ClearConnectingState();
        }

		//入力内容に誤りがあればそれを警告してnullを返す。なければ必要なところを埋めたTCPTerminalParamを返す
		private ITerminalParameter ValidateContent() {
			string msg = null;
			ITCPParameter tcp = null;
			ISSHLoginParameter ssh = null;
			try {
				ConnectionMethod m = ParseMethod(_methodBox.Text);
                if(m==ConnectionMethod.Telnet)
                    tcp = TerminalSessionsPlugin.Instance.ProtocolService.CreateDefaultTelnetParameter();
                else {
                    ISSHLoginParameter sp = TerminalSessionsPlugin.Instance.ProtocolService.CreateDefaultSSHParameter();
                    tcp = (ITCPParameter)sp.GetAdapter(typeof(ITCPParameter));
                    ssh = sp;
                    ssh.Method = m==ConnectionMethod.SSH1? SSHProtocol.SSH1 : SSHProtocol.SSH2;
                    ssh.Account = _userNameBox.Text;
                    ssh.PasswordOrPassphrase = _passphraseBox.Text;
                }

				tcp.Destination = _hostBox.Text;
				try {
					tcp.Port = ParsePort(_portBox.Text);
				}
				catch(FormatException ex) {
					msg = ex.Message;
				}

				if(_hostBox.Text.Length==0)
					msg = TEnv.Strings.GetString("Message.LoginDialog.HostIsEmpty");
				
                //ログ設定
                LogType logtype = (LogType)EnumDescAttribute.For(typeof(LogType)).FromDescription(_logTypeBox.Text, LogType.None);
                ISimpleLogSettings logsettings = null;
				if(logtype!=LogType.None) {
                    logsettings = CreateSimpleLogSettings(logtype, _logFileBox.Text);
                    if(logsettings==null) return null; //動作キャンセル
				}

                ITerminalParameter param = (ITerminalParameter)tcp.GetAdapter(typeof(ITerminalParameter));
                TerminalType terminal_type = (TerminalType)_terminalTypeBox.SelectedIndex;
                param.SetTerminalName(ToTerminalName(terminal_type));

				if(ssh!=null) {
					Debug.Assert(ssh!=null);
                    ssh.AuthenticationType = ToAuthenticationType(_authOptions.SelectedIndex);
					if(ssh.AuthenticationType==AuthenticationType.PublicKey) {
						if(!File.Exists(_privateKeyFile.Text))
							msg = TEnv.Strings.GetString("Message.LoginDialog.KeyFileNotExist");
						else
							ssh.IdentityFileName = _privateKeyFile.Text;
					}
				}

				string autoExecMacroPath = null;
				if (_autoExecMacroPathBox.Text.Length != 0)
					autoExecMacroPath = _autoExecMacroPathBox.Text;

				IAutoExecMacroParameter autoExecParams = tcp.GetAdapter(typeof(IAutoExecMacroParameter)) as IAutoExecMacroParameter;
				if (autoExecParams != null)
					autoExecParams.AutoExecMacroPath = autoExecMacroPath;

				ITerminalSettings settings = this.TerminalSettings;
				settings.BeginUpdate();
				settings.Caption = _hostBox.Text;
				settings.Icon = Poderosa.TerminalSession.Properties.Resources.NewConnection16x16;
				settings.Encoding = (EncodingType)_encodingBox.SelectedIndex;
				settings.LocalEcho = _localEchoBox.SelectedIndex == 1;
				settings.TransmitNL = (NewLine)EnumDescAttribute.For(typeof(NewLine)).FromDescription(_newLineBox.Text, NewLine.CR);
				settings.TerminalType = terminal_type;
				if (logsettings != null) settings.LogSettings.Reset(logsettings);
				settings.EndUpdate();

				if(msg!=null) {
					ShowError(msg);
					return null;
				}
				else
					return (ITerminalParameter)tcp.GetAdapter(typeof(ITerminalParameter));
			}
			catch(Exception ex) {
				GUtil.Warning(this, ex.Message);
				return null;
			}

		}

		protected override void OnActivated(EventArgs args) {
			if(_firstFlag) {
				_firstFlag = false;
				_hostBox.Focus();
			}
		}

		private void SelectLog(object sender, System.EventArgs e) {
            string fn = LogUtil.SelectLogFileByDialog(this);
			if(fn!=null) _logFileBox.Text = fn;
		}
		private void OnLogTypeChanged(object sender, System.EventArgs e) {
			if(_initializing) return;
			EnableValidControls();
		}

		protected override void ClearConnectingState() {
            base.ClearConnectingState();
			this.Text = TEnv.Strings.GetString("Form.LoginDialog.Text");
			_connector = null;
		}

		protected override void ShowError(string msg) {
			GUtil.Warning(this, msg, TEnv.Strings.GetString("Caption.LoginDialog.ConnectionError"));
		}


        private int ToAuthenticationIndex(AuthenticationType at) {
            if(at==AuthenticationType.Password)
                return 0;
            else if(at==AuthenticationType.PublicKey)
                return 1;
            else //if(at==AuthenticationType.KeyboardInteractive)
                return 2;
        }
        private AuthenticationType ToAuthenticationType(int index) {
            if(index==1)
                return AuthenticationType.PublicKey;
            else if(index==2)
                return AuthenticationType.KeyboardInteractive;
            else
                return AuthenticationType.Password;
        }

        private int FindPortIndex(int port) {
            for(int i=0; i<_portBox.Items.Count; i++) {
                if(ParsePort((string)_portBox.Items[i])==port) return i;
            }
            return -1;
        }

        private void _selectAutoExecMacroButton_Click(object sender, EventArgs e) {
            if (TelnetSSHPlugin.Instance.MacroEngine != null) {
                string path = TelnetSSHPlugin.Instance.MacroEngine.SelectMacro(this);
                if (path != null)
                    _autoExecMacroPathBox.Text = path;
            }
        }
    }
}
