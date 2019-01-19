#define DEBUG

using Gen=System.Collections.Generic;
//using Gdi=System.Drawing;

namespace mwg.TextEdit{
	// 操作履歴
	//   特定のポイントに於ける状態を復元出来ればそれで良い
	//   A 復元する為の操作として記録
	//   B 行に対する変更として、前後の行内容を記録
	//   C 
	public class TextDocument{
		int version;
		LineBList lines;
	}
	public class TextDocumentSection{
		
	}

	public partial class Line{
		Gen::List<Char> data;
		public int Height{
			get{return 1;} // 表示行数
		}
	}
	public sealed class Char{
		public char c;
		public short width;
		public CharStyle style;
	}
	public sealed class CharStyle{}
}
