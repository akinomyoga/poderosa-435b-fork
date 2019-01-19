using Gen=System.Collections.Generic;
using Gdi=System.Drawing;

namespace mwg.Experimental{
	public enum EndOfLine{
		Continue,
		Cr,
		CrLf,
		Lf,
		Eof
	}

	public sealed class CharStyle{
		public Gdi::Color color;
	}
	public sealed class Char{
		public char  c;         // 文字
		public short width;     // 半角文字何個分?
		public CharStyle style;
	}
	public sealed class Line{
		internal Lines parent=null;
		internal int index=0;

		public Line PrevLine{
			get{return parent==null?null:parent.GetLine(index-1);}
		}
		public Line NextLine{
			get{return parent==null?null:parent.GetLine(index+1);}
		}
		public Lines Parent{get{return parent;}}
		public int Index{get{return this.index;}}

		//-----------------------------------------------------
		// content
		readonly Gen::List<Char> data=new System.Collections.Generic.List<Char>();
		EndOfLine eol;

		public int Length{
			get{return data.Count;}
		}
		public int Width{
			get{
				int r=0;
				for(int i=0;i<data.Count;i++)r+=data[i].width;
				return r;
			}
		}
	}

	public sealed class Lines{
		readonly Gen::List<Line> lines=new Gen::List<Line>();

		//-----------------------------------------------------
		//  Get lines
		public Line GetLine(int index){
			if(index<0||lines.Count<=index)return null;
			return this.lines[index];
		}
		public Line FirstLine{
			get{
				if(lines.Count==0)return null;
				return lines[0];
			}
		}
		public Line LastLine{
			get{
				if(lines.Count==0)return null;
				return lines[lines.Count-1];
			}
		}
		public int LineCount{
			get{return lines.Count;}
		}

		//-----------------------------------------------------
		public void AddLine(Line line){
			if(line.parent!=null){
				if(line.parent==this){
					this.RotateU(line.index,lines.Count);
				}else{
					line.parent.RemoveLine(line);
				}
			}
			line.parent=this;
			line.index=lines.Count;
			lines.Add(line);
		}
		public void RemoveLine(Line line){
			if(line.parent!=this)return;

			lines.RemoveAt(line.index);
			for(int i=line.index,iN=lines.Count;i<iN;i++)
				lines[i].index=i;

			line.parent=null;
			line.index=0;
		}
		public void InsertBefore(Line pos,Line line){
			if(pos.parent!=this)return;

			this.lines.Insert(pos.index,line);
			for(int i=pos.index,iN=lines.Count;i<iN;i++)
				lines[i].index=i;
			line.parent=this;
		}
		public void InsertAfter(Line pos,Line line){
			if(pos.parent!=this)return;

			this.lines.Insert(pos.index+1,line);
			for(int i=pos.index,iN=lines.Count;i<iN;i++)
				lines[i].index=i;
			line.parent=this;
		}

		/// <summary>
		/// 指定した範囲の行を一段上へ回転します。
		/// </summary>
		/// <param name="start">範囲の開始位置を指定します。</param>
		/// <param name="end">範囲の終端位置を指定します。</param>
		public void RotateU(int start,int end){
			Line tmp=lines[start];
			for(int i=start,iN=end-1;i<iN;i++){
				lines[i]=lines[i+1];
				lines[i].index=i;
			}
			lines[end-1]=tmp;
			lines[end-1].index=end-1;
		}
		/// <summary>
		/// 指定した範囲の行を一段下へ回転します。
		/// </summary>
		/// <param name="start">範囲の開始位置を指定します。</param>
		/// <param name="end">範囲の終端位置を指定します。</param>
		public void RotateD(int start,int end){
			Line tmp=lines[end-1];
			for(int i=end-1,iN=start;i>iN;i--){
				lines[i]=lines[i-1];
				lines[i].index=i;
			}
			lines[start]=tmp;
			lines[start].index=start;
		}
	}

	public class Document{
		protected string caption;
		protected Gdi::Image icon;
		protected Poderosa.Sessions.ISession owner=null;
		public void SetOwner(Poderosa.Sessions.ISession owner){
			this.owner=owner;
		}

		protected readonly Poderosa.Document.InvalidatedRegion invalidated
			=new Poderosa.Document.InvalidatedRegion();
		public Poderosa.Document.InvalidatedRegion InvalidatedRegion{
			get{return this.invalidated;}
		}

		readonly Lines lines=new Lines();
		Lines Lines{get{return this.lines;}}
	}
}