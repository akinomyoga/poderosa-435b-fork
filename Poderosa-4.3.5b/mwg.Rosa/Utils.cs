#define NETFX_2

#if NETFX_2
namespace System.Runtime.CompilerServices{
	[System.AttributeUsage(AttributeTargets.Method)]
	class ExtensionAttribute:System.Attribute{}
}
#endif

namespace mwg.RosaTerm{
  interface IReadOnlyIndexer<T>{
    T this[int index]{get;}
    int Count{get;}
  }
}

namespace mwg.RosaTerm.Utils{
	static class RosaTermUtil{
		public static int Clamp(this int value,int lower,int upper){
			if(value<lower)return lower;
			if(value>upper)return upper;
			return value;
		}
	}
}