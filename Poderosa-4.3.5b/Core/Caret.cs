/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: Caret.cs,v 1.1 2010/11/19 15:40:39 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using Poderosa.Util;
using Poderosa.Forms;

using Gdi=System.Drawing;

namespace Poderosa.View {

	/// <summary>
	/// 
	/// </summary>
	/// <exclude/>
	public enum CaretType {
		Line=0,
		Box=2,
		Underline=4,
		BoldUnderline=6,
		StyleMask=Box|Underline,
	}

	//Caretの座標と状態を収録
	/// <summary>
	/// <ja>
	/// キャレットの座標と状態を格納するオブジェクトです。
	/// </ja>
	/// <en>
	/// Object that stores coordinates and state of caret
	/// </en>
	/// </summary>
	/// <exclude/>
	public class Caret : System.IDisposable{
		private const int TICKER_LOOP_INTERVAL = 2;

		private CaretType _style; //Line, Box, Underlineのいずれか
		private Color _color;
		private int _x; //文字単位での座標
		private int _y;
		private int _tick; //一定時間毎の切り替わり
		private bool _enabled;
		private bool _blink;
		private Pen _pen;
		private Brush _brush;

		public Caret() {
			_style = CaretType.Box;
			_color = Color.Empty;
			_enabled = false;
			_blink = true;
		}

		public CaretType Style {
			get {
				return _style;
			}
			set {
				_style = value & CaretType.StyleMask;
			}
		}
		public int X {
			get {
				return _x;
			}
			set {
				_x = value;
			}
		}
		public int Y {
			get {
				return _y;
			}
			set {
				_y = value;
			}
		}
		public bool Enabled {
			get {
				return _enabled;
			}
			set {
				_enabled = value;
			}
		}
		public bool Blink {
			get {
				return _blink;
			}
			set {
				_blink = value;
			}
		}
		public Color Color {
			get {
				return _color;
			}
			set {
				_color = value;
				DisposePen();
			}
		}


		public bool IsActiveTick {
			get {
				return _tick<=0;
			}
		}
		public void Tick() {
			if(++_tick==TICKER_LOOP_INTERVAL) _tick = 0;
		}
		public void KeepActiveUntilNextTick() {
			//TODO タイマーのリセットまでできるとよい
			_tick = -1;
		}
		public void Reset() {
			DisposePen();
		}

		//Pen
		internal Pen ToPen(RenderProfile p){
			if(_pen!=null){
				_pen=new Pen(_color==Color.Empty?p.ForeColor:_color);
			}
			return _pen;
		}
		internal Brush ToBrush(RenderProfile p){
			if(_brush!=null){
				_brush=new SolidBrush(_color==Color.Empty?p.ForeColor:_color);
			}
			return _brush;
		}

		public void Dispose() {
			this.DisposePen();
		}

		private void DisposePen() {
			if(_pen!=null) {
				_pen.Dispose(); //ペンのセットでリセット
				_pen = null;
			}
			if(_brush!=null){
				_brush.Dispose();
				_brush=null;
			}
		}


	}
}
