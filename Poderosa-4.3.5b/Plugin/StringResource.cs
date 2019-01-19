/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: StringResource.cs,v 1.1 2010/11/19 15:40:51 kzmi Exp $
 */
using System;
using System.Globalization;
using System.Resources;
using System.Reflection;

using Poderosa.Util;
using Poderosa.Plugins;

namespace Poderosa {
    /// <summary>
    /// <ja>
    /// カルチャ情報を示すオブジェクトです。
    /// </ja>
    /// <en>
    /// Object that shows culture information.
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>
    /// このクラスの解説は、まだありません。
    /// </ja>
    /// <en>
    /// This class has not explained yet. 
    /// </en>
    /// </remarks>
    public class StringResource : ICultureChangeListener {
		private string _resourceName;
		private ResourceManager _resMan;
        private Assembly _asm;

		public StringResource(string name, Assembly asm) {
            Init(name, asm, true);
        }
        public StringResource(string name, Assembly asm, bool register_enumdesc) {
            Init(name, asm, register_enumdesc);
        }
        private void Init(string name, Assembly asm, bool register_enumdesc) {
            _resourceName = name;
            _asm = asm;
            LoadResourceManager();
            if(register_enumdesc) EnumDescAttribute.AddResourceTable(asm, this);
        }

		public string GetString(string id) {
			return _resMan.GetString(id); //もしこれが遅いようならこのクラスでキャッシュでもつくればいいだろう
		}

		private void LoadResourceManager() {
            CultureInfo ci = System.Threading.Thread.CurrentThread.CurrentUICulture;
            OnCultureChanged(ci); //これでResourceManagerをリフレッシュ
        }

        public void OnCultureChanged(CultureInfo newculture) {
            //当面は英語・日本語しかしない
            if(newculture.Name.StartsWith("ja"))
                _resMan = new ResourceManager(_resourceName+"_ja", _asm);
            else
                _resMan = new ResourceManager(_resourceName, _asm);
        }
    }
}