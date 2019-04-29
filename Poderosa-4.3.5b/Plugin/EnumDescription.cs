/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: EnumDescription.cs,v 1.3 2010/11/24 16:04:10 kzmi Exp $
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace Poderosa.Util {

	//整数のenum値に表記をつけたり相互変換したりする　構造上
	/// <summary>
	/// 
	/// </summary>
	/// <exclude/>
	[AttributeUsage(AttributeTargets.Enum)]
	public class EnumDescAttribute : Attribute {

		private static Dictionary<Assembly, List<StringResource>> _assemblyToResource = new Dictionary<Assembly, List<StringResource>>();
		//文字列リソースを使うやつはこれが必要
		public static void AddResourceTable(Assembly asm, StringResource res) {
			if(_assemblyToResource.ContainsKey(asm))
				_assemblyToResource[asm].Add(res);
			else {
				List<StringResource> l = new List<StringResource>();
				l.Add(res);
				_assemblyToResource.Add(asm, l);
			}
		}
		public static void RemoveResourceTable(Assembly asm, StringResource res) {
			Debug.Assert(_assemblyToResource.ContainsKey(asm));
			List<StringResource> l  = _assemblyToResource[asm];
			l.Remove(res);

			//エントリの削除まではいいや
		}

		private Assembly _assembly;
		private string[] _descriptions;
		private Hashtable _descToValue;
		private string[] _names;
		private Hashtable _nameToValue;
		private StringResource _strResource;

		public EnumDescAttribute(Type t) {
			Init(t);
		}

		public void Init(Type t) {
			_strResource = null;
			_assembly = t.Assembly;

			MemberInfo[] ms = t.GetMembers();
			_descToValue = new Hashtable(ms.Length);
			_nameToValue = new Hashtable(ms.Length);

			ArrayList descriptions = new ArrayList(ms.Length);
			ArrayList names = new ArrayList(ms.Length);

			int expected = 0;
			foreach(MemberInfo mi in ms) {
				FieldInfo fi = mi as FieldInfo;
				if(fi!=null && fi.IsStatic && fi.IsPublic) {
					int intVal = (int)fi.GetValue(null); //int以外をベースにしているEnum値はサポート外
					if(intVal!=expected) throw new Exception("unexpected enum value order");
					EnumValueAttribute a = (EnumValueAttribute)(fi.GetCustomAttributes(typeof(EnumValueAttribute), false)[0]);
				
					string desc = a.Description;
					descriptions.Add(desc);
					_descToValue[desc] = intVal;

					string name = fi.Name;
					names.Add(name);
					_nameToValue[name] = intVal;

					expected++;
				}
			}

			_descriptions = (string[])descriptions.ToArray(typeof(string));
			_names        = (string[])names.ToArray(typeof(string));
		}

		public virtual string GetDescription(ValueType i) {
			return LoadString(_descriptions[(int)i]);
		}
		public virtual string GetRawDescription(ValueType i)
		{
			return _descriptions[(int)i];
		}
		public virtual ValueType FromDescription(string v, ValueType d)
		{
			if(v==null) return d;
			IDictionaryEnumerator ie = _descToValue.GetEnumerator();
			while(ie.MoveNext()) {
				if(v==LoadString((string)ie.Key)) return (ValueType)ie.Value;
			}
			return d;
		}
		public virtual string GetName(ValueType i) {
			return _names[(int)i];
		}
		public virtual ValueType FromName(string v) {
			return (ValueType)_nameToValue[v];
		}
		public virtual ValueType FromName(string v, ValueType d) {
			if(v==null) return d;
			ValueType t = (ValueType)_nameToValue[v];
			return t==null? d : t;
		}

		public virtual string[] DescriptionCollection() {
			string[] r = new string[_descriptions.Length];
			for(int i=0; i<r.Length; i++)
				r[i] = LoadString(_descriptions[i]);
			return r;
		}
		private string LoadString(string id) {
			if(_strResource==null) ResolveStringResource(id);
			string t = _strResource.GetString(id);
			return t==null? id : t;
		}
		private void ResolveStringResource(string id) {
			List<StringResource> l = _assemblyToResource[_assembly];
			foreach(StringResource res in l) {
				if(res.GetString(id)!=null) { //見つかれば採用
					_strResource = res;
					return;
				}
			}
			throw new Exception("String resource not found for " + id);
		}


		//アトリビュートを取得する
		private static Hashtable _typeToAttr = new Hashtable();
		public static EnumDescAttribute For(Type type) {
			EnumDescAttribute a = _typeToAttr[type] as EnumDescAttribute;
			if(a==null) {
				a = (EnumDescAttribute)(type.GetCustomAttributes(typeof(EnumDescAttribute), false)[0]);
				_typeToAttr.Add(type, a);
			}
			return a;
		}

	}

	/// <summary>
	/// 
	/// </summary>
	/// <exclude/>
	[AttributeUsage(AttributeTargets.Field)]
	public class EnumValueAttribute : Attribute {
		private string _description;

		public string Description {
			get {
				return _description;
			}
			set {
				_description = value;
			}
		}
	}

}