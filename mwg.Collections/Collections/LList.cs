using Gen=System.Collections.Generic;

namespace mwg{
	internal static partial class __dbg__{
		[System.Diagnostics.Conditional("DEBUG"),System.Diagnostics.DebuggerHidden]
		public static void AssertLList(bool condition,params object[] x){
			if(!condition)
				throw new System.InvalidProgramException();
		}
	}
}

namespace mwg.Collections{
	//public class LList<T>{
	//  readonly Gen::LinkedList<T> data=new Gen::LinkedList<T>();
	//  readonly Gen::List<View> views=new Gen::List<View>();
	//  int version;

	//  public class View{
	//    LList<T> list;
	//    int listVersion;
	//    int index;
	//    Gen::LinkedListNode<T> value;
	//  }

	//  public LList(){
	//    Gen::LinkedListNode<int> a;
	//  }
	//}
}