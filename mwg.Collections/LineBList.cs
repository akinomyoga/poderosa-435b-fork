#define LineBList
using Gen=System.Collections.Generic;

namespace mwg{
  internal static partial class __dbg__{
    [System.Diagnostics.Conditional("DEBUG"),System.Diagnostics.DebuggerHidden]
    public static void AssertLineBList(bool condition,params object[] x){
      if(!condition)
        throw new System.InvalidProgramException();
    }
  }
}

namespace mwg.TextEdit{

  #region class LineBList
  /// <summary>
  /// BListEx の要素の基本クラスを提供します。
  /// この基本クラスを使用して、要素が現在 BListEx 内のどの位置にあるかを取得する事が可能です。
  /// </summary>
  public partial class Line{
    internal LineBList.Node0 node0=null;
    internal int index=0;
    /// <summary>
    /// この要素のリスト内に於ける位置を取得します。
    /// </summary>
    public int Index{
      get{return this.node0==null?-1:this.node0.OffsetOfIndex+this.index;}
    }
    /// <summary>
    /// この要素の直前に位置している要素を取得します。
    /// この要素がリスト内の先頭に位置している場合には null を返します。
    /// </summary>
    public Line PreviousLine{
      get{return this.node0==null?default(Line):this.node0.GetPreviousElement(this.index);}
    }
    /// <summary>
    /// この要素の直後に位置している要素を取得します。
    /// この要素がリスト内の末尾に位置している場合には null を返します。
    /// </summary>
    public Line NextLine{
      get{return this.node0==null?default(Line):this.node0.GetNextElement(this.index);}
    }
   
    public int Top{
      get{
        if(this.node0==null)return 0;
        int ret=0;
        for(int i=0;i<this.index;i++)
          ret+=this.node0.data[i].Height;
        return ret;
      }
    }
    public int Bottom{
      get{return this.Top+this.Height;}
    }
  }

	// 変更点 from BListE2:
	// 1. Node.line_height を追加。
	//    Node.weight と同じ所で値を更新
	//    Node.add_weight(weight_delta) → Node.add_weight(weight_delta,line_height_delta)
	// 2. BListE2Element -> partial Line
  //    IBListE2Node 削除 -> LineBList.Node0 を直接使用

  /// <summary>
  /// 平衡木 (B+ 木) 状の内部構造を持つ配列です。
  /// ランダムアクセス・挿入に対して O(log n) の時間計算量を要します。
  /// </summary>
  /// <typeparam name="Line">要素の型を指定します。</typeparam>
  public class LineBList
    :Gen::IList<Line>,System.Collections.IList,
    Gen::ICollection<Line>,System.Collections.ICollection,
    Gen::IEnumerable<Line>,System.Collections.IEnumerable
  {
    private const int LLIMIT=32;
    private const int ULIMIT=64;
    private const float BALANCE=3.0f; //!< must be larger than ULIMIT/LLIMIT.

    internal abstract class Node{
      public readonly int level;
      public LineBList list;
      public Node1 parent;
      public int index;

      public int weight;
			public int line_height;

      public Node(int level){
        this.level=level;
        this.list=null;
        this.parent=null;
        this.weight=0;
				this.line_height=0;
      }

      public abstract void Insert(int index,Line value);
      public abstract void RemoveAt(int index);
      public abstract Line this[int index]{get;set;}

      protected void add_weight(int delta,int line_height_delta){
        if(delta==0&&line_height_delta==0)return;
        this.weight+=delta;
				this.line_height+=line_height_delta;
        Node1 p=this.parent;
        while(p!=null){
          p.weight+=delta;
					p.line_height+=line_height_delta;
          p=p.parent;
        }
      }

      internal virtual void set_list(LineBList list){
        this.list=list;
      }

      public int OffsetOfIndex {
        get{
          if(this==this.list.root)
            return 0;
          else
            return this.parent.OffsetOfIndex+this.index;
        }
      }
      public int OffsetOfHeight{
        get{
          if(this==this.list.root)
            return 0;
          else{
            int ret=this.parent.OffsetOfHeight;
            for(int i=0;i<this.index;i++)
              ret+=this.parent.data[i].line_height;
            return ret;
          }
        }
      }
    }

    internal class Node0:Node{
      public readonly Gen::List<Line> data=new Gen::List<Line>();
      public Node0():base(0){}

      public override Line this[int index]{
        get{
          __dbg__.AssertBList(0<=index&&index<this.data.Count);
          return this.data[index];
        }
        set{
          __dbg__.AssertBList(0<=index&&index<this.data.Count);
          if(this.data[index]!=null)
            this.data[index].node0=null;
          this.data[index]=value;
          if(value!=null){
            value.node0=this;
            value.index=index;
          }
        }
      }

      public override void Insert(int index,Line value){
        __dbg__.AssertBList(0<=index&&index<=this.data.Count);
        if(this.data.Count<ULIMIT){
          this.data.Insert(index,value);
          if(value!=null)
            value.node0=this;
          for(int i=index,iN=this.data.Count;i<iN;i++){
            Line e=this.data[i];
            if(e!=null)e.index=i;
          }
          this.add_weight(1,value.Height);
        }else{
          Node0 node2=this.Split();
          if(this.list.root==this)
            this.list.PushLevel(node2);
          else
            this.parent.InsertNode(this.index+1,node2);

          int index2=index-this.data.Count;
          if(index2<0)
            this.Insert(index,value);
          else
            node2.Insert(index2,value);
        }
      }

      private Node0 Split(){
        Node0 node2=new Node0();
        int c1=this.data.Count/2;
        int c2=this.data.Count-c1;
        for(int index=0;index<c2;index++){
          Line nch=this.data[c1+index];
          if(nch!=null){
            nch.node0=node2;
            nch.index=node2.data.Count;
          }
          node2.data.Add(nch);
          node2.weight++;
					node2.line_height+=nch.Height;
        }
        this.data.RemoveRange(c1,c2);
        this.add_weight(-node2.weight,-node2.line_height);
        return node2;
      }

      public override void RemoveAt(int index){
        __dbg__.AssertBList(0<=index&&index<this.data.Count);
        {
          Line e=this.data[index];
					int lh=e.Height;
          if(e!=null)e.node0=null;
          this.data.RemoveAt(index);
          this.add_weight(-1,lh);
        }

        if(this.list.root==this)return;

        __dbg__.AssertBList(this.parent!=null&&this.parent.data.Count>1);

        if(this.data.Count>=LLIMIT)return;

        if(this.index>0){
          Node0 prev=(Node0)this.parent.data[this.index-1];
          if(prev.data.Count>LLIMIT){
            Line n=prev.data[prev.data.Count-1];
            prev.RemoveAt(prev.data.Count-1);
            this.Insert(0,n);
            return;
          }
        }
        
        if(this.index+1<this.parent.data.Count){
          Node0 next=(Node0)this.parent.data[this.index+1];
          if(next.data.Count>LLIMIT){
            Line n=next.data[0];
            next.RemoveAt(0);
            this.Insert(this.data.Count,n);
            return;
          }
        }

        // 融合
        if(this.index>0){
          Node0 prev=(Node0)this.parent.data[this.index-1];
          prev.Merge(this);
          this.parent.RemoveAtNode(this.index);
        }else{
          Node0 next=(Node0)this.parent.data[this.index+1];
          this.Merge(next);
          this.parent.RemoveAtNode(this.index+1);
        }
      }

      private void Merge(Node0 node){
        __dbg__.AssertBList(this.data.Count+node.data.Count<=ULIMIT&&this.parent==node.parent);

        int i=this.data.Count;
        for(int j=0,jN=node.data.Count;j<jN;j++)
          node.data[j].index=i++;
        this.data.AddRange(node.data);
        this.weight+=node.weight;
				this.line_height+=node.line_height;
        node.weight=0;
				node.line_height=0;
        node.data.Clear();
      }

      #region IBListE2Node<Line> メンバ
      public Line GetNextElement(int index){
        if(this==null)
          return null;
        if(index+1<this.data.Count)
          return this.data[index+1];
        
        Node  sf=this;
        Node1 n1=this.parent;
        for(;n1!=null;sf=n1,n1=n1.parent){
          if(sf.index+1<n1.data.Count)
            return n1.data[sf.index+1][0];
        }
        
        return null;
      }
      public Line GetPreviousElement(int index){
        if(this==null)
          return null;
        if(index-1>=0)
          return this.data[index-1];
        
        Node  sf=this;
        Node1 n1=this.parent;
        for(;n1!=null;sf=n1,n1=n1.parent){
          if(sf.index-1>=0){
            Node n2=n1.data[sf.index-1];
            return n2[n2.weight-1];
          }
        }
        
        return null;
      }
      #endregion
    }
    internal class Node1:Node{
      public readonly Gen::List<Node> data=new Gen::List<Node>();
      public Node1(int level):base(level){}

      internal override void set_list(LineBList list){
        if(this.list==list)return;
        this.list=list;
        for(int i=0,iN=this.data.Count;i<iN;i++)
          this.data[i].set_list(list);
      }

      public override Line this[int index]{
        get{
          __dbg__.AssertBList(0<=index&&index<this.weight);

          for(int i=0,iN=this.data.Count;i<iN;i++){
            if(index<this.data[i].weight){
              return this.data[i][index];
            }
            index-=this.data[i].weight;
          }

          __dbg__.AssertBList(false);
          throw new System.InvalidProgramException();
        }
        set{
          __dbg__.AssertBList(0<=index&&index<this.weight);

          for(int i=0,iN=this.data.Count;i<iN;i++){
            if(index<this.data[i].weight){
              this.data[i][index]=value;
              return;
            }
            index-=this.data[i].weight;
          }

          __dbg__.AssertBList(false);
        }
      }
    
      public void InsertNode(int index,Node node){
        __dbg__.AssertBList(0<=index&&index<=this.data.Count&&node.level==this.level-1);

        if(this.data.Count<ULIMIT){
          this.data.Insert(index,node);
          for(int iN=this.data.Count;index<iN;index++)
            this.data[index].index=index;
          node.set_list(this.list);
          node.parent=this;
          this.add_weight(node.weight,node.line_height);
        }else{
          // 分裂
          Node1 node2=this.Split();
          int index2=index-this.data.Count;
          if(index2<0)
            this.InsertNode(index,node);
          else
            node2.InsertNode(index2,node);

          if(this.list.root==this)
            this.list.PushLevel(node2);
          else
            this.parent.InsertNode(this.index+1,node2);
        }

        this.Balance();
      }

      private Node1 Split(){
        Node1 node2=new Node1(this.level);
        int c1=this.data.Count/2;
        int c2=this.data.Count-c1;
        for(int index=0;index<c2;index++){
          Node nch=this.data[c1+index];
          node2.data.Add(nch);
          //nch.list=; // ok
          nch.parent=node2;
          nch.index=index;
          node2.weight+=nch.weight;
					node2.line_height+=nch.line_height;
        }
        this.data.RemoveRange(c1,c2);
        this.add_weight(-node2.weight,-node2.line_height);
        return node2;
      }

      public void RemoveAtNode(int index){
        __dbg__.AssertBList(0<=index&&index<this.data.Count);

        {
          Node rem=this.data[index];
          rem.parent=null;
          rem.list=null;
          this.add_weight(-rem.weight,-rem.line_height);
          this.data.RemoveAt(index);
          for(int iN=this.data.Count;index<iN;index++)
            this.data[index].index=index;
        }

        if(this.list.root==this){
          // 昇格
          if(this.data.Count==1)
            this.list.PopLevel();
          goto exit_balance;
        }

        __dbg__.AssertBList(this.parent!=null&&this.parent.data.Count>1);

        if(this.data.Count>=LLIMIT)
          goto exit_balance;

        if(this.index>0){
          Node1 prev=(Node1)this.parent.data[this.index-1];
          if(prev.data.Count>LLIMIT){
            Node n=prev.data[prev.data.Count-1];
            prev.RemoveAtNode(prev.data.Count-1);
            this.InsertNode(0,n);
            return;
          }
        }
        
        if(this.index+1<this.parent.data.Count){
          Node1 next=(Node1)this.parent.data[this.index+1];
          if(next.data.Count>LLIMIT){
            Node n=next.data[0];
            next.RemoveAtNode(0);
            this.InsertNode(this.data.Count,n);
            return;
          }
        }

        // 融合
        if(this.index>0){
          Node1 prev=(Node1)this.parent.data[this.index-1];
          prev.Merge(this);
          this.parent.RemoveAtNode(this.index);
        }else{
          Node1 next=(Node1)this.parent.data[this.index+1];
          this.Merge(next);
          this.parent.RemoveAtNode(this.index+1);
        }
      exit_balance:
        this.Balance();
      }

      private void Merge(Node1 node){
        __dbg__.AssertBList(this.data.Count+node.data.Count<=ULIMIT&&this.parent==node.parent);

        int i=this.data.Count;
        this.data.AddRange(node.data);
        for(int iN=this.data.Count;i<iN;i++){
          this.data[i].set_list(this.list);
          this.data[i].parent=this;
          this.data[i].index=i;
        }
        this.weight+=node.weight;
				this.line_height+=node.line_height;
        node.weight=0;
				node.line_height=0;
        node.data.Clear();
      }

#if DEBUG
      internal void DbgCheckState(){
        int w=0;
        for(int i=0,iN=this.data.Count;i<iN;i++){
          if(this.data[i].list!=this.list)
            throw new System.InvalidProgramException("this.data[i].list!=this.list");
          if(this.data[i].parent!=this)
            throw new System.InvalidProgramException("this.data[i].parent!=this");
          if(this.data[i].index!=i)
            throw new System.InvalidProgramException("this.data[i].index!=i");
          if(this.data[i].weight<LLIMIT)
            throw new System.InvalidProgramException("this.data[i].weight==0");
          w+=this.data[i].weight;
        }
        if(this.weight!=w)
          throw new System.InvalidProgramException("this.weight!=(sum of children's weights)");

        if(this.level>1)
          foreach(Node1 node in this.data)
            node.DbgCheckState();
        else if(this.level==0)
          foreach(Node0 node in this.data){
            int index=0;
            foreach(Line e in node.data){
              if(e.node0!=node)
                throw new System.InvalidProgramException("node0.data[i].node0!=node0");
              if(e.index!=index++)
                throw new System.InvalidProgramException("node0.data[i].index!=i");
            }
          }
      }
#endif

      public override void Insert(int index,Line value){
        __dbg__.AssertBList(0<=index&&index<=this.weight);

        for(int i=0,iN=this.data.Count;i<iN;i++){
          if(index<=this.data[i].weight){
            this.data[i].Insert(index,value);
            return;
          }
          index-=this.data[i].weight;
        }

        __dbg__.AssertBList(false);
      }
      public override void RemoveAt(int index){
        __dbg__.AssertBList(0<=index&&index<this.weight);

        for(int i=0,iN=this.data.Count;i<iN;i++){
          if(index<this.data[i].weight){
            this.data[i].RemoveAt(index);
            return;
          }
          index-=this.data[i].weight;
        }

        __dbg__.AssertBList(false);
      }

      //----------------------------------------------------------------------
      public void Balance(){
        if(this.level<2)return;

        // 必要性判定
        float min=float.MaxValue;
        float max=0;
        for(int i=0,iN=this.data.Count;i<iN;i++){
          int w=this.data[i].weight;
          if(w<min)min=w;
          if(w>max)max=w;
        }
        if(min*BALANCE>max)return;

        // 孫要素回収
        int ngrand=0;
        for(int i=0,iN=this.data.Count;i<iN;i++)
          ngrand+=((Node1)this.data[i]).data.Count;
        Gen::List<Node> grands=new Gen::List<Node>(ngrand);
        {
          for(int i=0,iN=this.data.Count;i<iN;i++){
            Node1 node=(Node1)this.data[i];
            for(int j=0,jN=node.data.Count;j<jN;j++)
              grands.Add(node.data[j]);

            node.data.Clear();
            node.weight=0;
						node.line_height=0;
          }
        }
        
        // 再配置
        {
          int s=0;
          double n=(double)this.weight/this.data.Count;

          int i=0;
          int sN=(int)n;
          Node1 node=(Node1)this.data[0];
          for(int index=0;index<this.weight;index++){
            if(s>=sN){
              if(++i>=this.data.Count)break;
              node=(Node1)this.data[i];
              sN=(int)((i+1)*n);
            }

            Node grand=grands[index];
            grand.parent=node;
            grand.index=node.data.Count;
            node.weight+=grand.weight;
						node.line_height+=grand.line_height;
            node.data.Add(grand);

            s+=grand.weight;
          }
        }

      }
    }

    private Node root;
    /// <summary>
    /// 空の BalancedTreeList インスタンスを初期化します。
    /// </summary>
    public LineBList(){
      this.root=new Node0();
      this.root.list=this;
    }

#if DEBUG
    /// <summary>
    /// 内部状態の整合性をチェックします。
    /// 内部状態に異常がある場合には例外を投げます。
    /// </summary>
    public void DbgCheckState(){
      if(this.root.level>0){
        Node1 node=(Node1)this.root;
        node.DbgCheckState();
      }
    }
#endif

    private void PushLevel(Node node2){
      __dbg__.AssertBList(root.level==node2.level);

      Node1 newroot=new Node1(root.level+1);
      newroot.InsertNode(0,root);
      newroot.InsertNode(1,node2);
      newroot.set_list(this);
      this.root=newroot;
    }
    private void PopLevel(){
      __dbg__.AssertBList(this.root.level>0);
      Node1 r=(Node1)this.root;
      __dbg__.AssertBList(r.data.Count<=1);

      Node newroot=r.data[0];
      // newroot.list=; // ok
      newroot.parent=null;
      this.root=newroot;
    }

    /// <summary>
    /// 指定した位置に指定した要素を挿入します。
    /// </summary>
    /// <param name="index">要素を挿入する位置を指定します。</param>
    /// <param name="value">挿入する要素を指定します。</param>
    public void Insert(int index,Line value){
      if(index<0||this.root.weight<index)
        throw new System.ArgumentOutOfRangeException("index");

      this.root.Insert(index,value);
    }

    #region IList<Line> メンバ

    /// <summary>
    /// 指定した要素が格納されている場所を検索します。
    /// </summary>
    /// <param name="item">検索対象の要素を指定します。</param>
    /// <returns>指定した要素が見付かった場合に、その位置を返します。
    /// 見付からなかった場合には -1 を返します。</returns>
    public int IndexOf(Line item){
      int index=0;
      foreach(Line value in this){
        if(value.Equals(item))
          return index;
        index++;
      }
      return -1;
    }
    /// <summary>
    /// 指定した位置にある要素を削除します。
    /// </summary>
    /// <param name="index">削除する要素の位置を指定します。</param>
    public void RemoveAt(int index) {
      if(index<0||this.root.weight<index)
        throw new System.ArgumentOutOfRangeException("index");

      this.root.RemoveAt(index);
    }
    /// <summary>
    /// 指定した位置にある要素を取得又は設定します。
    /// </summary>
    /// <param name="index">捜査対象の要素の位置を指定します。</param>
    /// <returns>指定した位置にある要素を返します。</returns>
    public Line this[int index] {
      get{
        if(index<0||this.root.weight<=index)
          throw new System.ArgumentOutOfRangeException("index");
        return this.root[index];
      }
      set {
        if(index<0||this.root.weight<=index)
          throw new System.ArgumentOutOfRangeException("index");
        this.root[index]=value;
      }
    }

    #endregion

    #region ICollection<Line> メンバ
    /// <summary>
    /// 指定した要素を末尾に追加します。
    /// </summary>
    /// <param name="item">末尾に追加する要素を指定します。</param>
    public void Add(Line item){
      this.root.Insert(this.root.weight,item);
    }

    /// <summary>
    /// 含まれている要素を全て削除し、空のリストにします。
    /// </summary>
    public void Clear() {
      if(this.root.weight==0)return;
      if(this.root.level==0){
        Node0 r=(Node0)this.root;
        r.data.Clear();
        r.weight=0;
      }else{
        this.root=new Node0();
        this.root.list=this;
      }
    }

    /// <summary>
    /// 指定した要素が含まれているかどうかを調べます。
    /// </summary>
    /// <param name="item">含まれているかどうかを確認したい要素を指定します。</param>
    /// <returns>指定した要素が含まれていた場合に true を返します。</returns>
    public bool Contains(Line item){
      foreach(Line value in this)
        if(value.Equals(item))return true;
      return false;
    }

    public void CopyTo(Line[] array,int arrayIndex){
      if(array==null)
        throw new System.ArgumentNullException("array");
      if(0<arrayIndex)
        throw new System.ArgumentOutOfRangeException("arrayIndex","index is less than 0.");
      if(arrayIndex+this.root.weight>=array.Length)
        throw new System.ArgumentException("array","array is too small to copy the elements into");

      foreach(Line item in this)
        array[arrayIndex++]=item;
    }

    /// <summary>
    /// コレクションに現在含まれている要素の数を取得します。
    /// </summary>
    public int Count {
      get { return this.root.weight; }
    }

    public bool IsReadOnly {
      get { return false; }
    }

    /// <summary>
    /// 指定した要素を検索し、見付かった場合に削除します。
    /// </summary>
    /// <param name="item">削除する要素を指定します。</param>
    /// <returns>指定した要素が見付かり削除された場合に true を返します。
    /// 指定した要素が見付からなかった場合に false を返します。</returns>
    public bool Remove(Line item){
      int index=this.IndexOf(item);
      if(index<0)return false;
      this.RemoveAt(index);
      return true;
    }
    #endregion

    #region IEnumerable<Line> メンバ
    public System.Collections.Generic.IEnumerator<Line> GetEnumerator() {
      Node n=this.root;
      for(;;){
        if(n.level>0){
          Node1 n1=(Node1)n;
          n=n1.data[0];
        }else{
          Node0 n0=(Node0)n;
          for(int i=0,iN=n0.data.Count;i<iN;i++)
            yield return n0.data[i];

          for(;;){
            if(n.parent==null)yield break;

            if(n.index+1<n.parent.data.Count){
              n=n.parent.data[n.index+1];
              break;
            }else{
              n=n.parent;
            }
          }
        }
      }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator(){
      return this.GetEnumerator();
    }
    #endregion

    #region ICollection メンバ

    void System.Collections.ICollection.CopyTo(System.Array array,int arrayIndex) {
      if(array==null)
        throw new System.ArgumentNullException("array");
      if(0<arrayIndex)
        throw new System.ArgumentOutOfRangeException("arrayIndex","index is less than 0.");
      if(arrayIndex+this.root.weight>=array.Length)
        throw new System.ArgumentException("array","array is too small to copy the elements into");

      foreach(Line item in this)
        array.SetValue(item,arrayIndex++);
    }

    public bool IsSynchronized {
      get { return false; }
    }

    public object SyncRoot {
      get { return this.root; }
    }

    #endregion

    #region IList メンバ

    int System.Collections.IList.Add(object value) {
      if(value!=null?!value.GetType().IsSubclassOf(typeof(Line)):typeof(Line).IsValueType)
        throw new System.ArgumentException("value is not a instance of Node","value");
      int ret=this.root.weight;
      this.Add((Line)value);
      return ret;
    }

    bool System.Collections.IList.Contains(object value) {
      if(value!=null?!value.GetType().IsSubclassOf(typeof(Line)):typeof(Line).IsValueType)return false;
      return this.Contains((Line)value);
    }

    int System.Collections.IList.IndexOf(object value) {
      if(value!=null?!value.GetType().IsSubclassOf(typeof(Line)):typeof(Line).IsValueType)return -1;
      return this.IndexOf((Line)value);
    }

    void System.Collections.IList.Insert(int index,object value) {
      if(value!=null?!value.GetType().IsSubclassOf(typeof(Line)):typeof(Line).IsValueType)
        throw new System.ArgumentException("value is not a instance of Node","value");
      this.Insert(index,(Line)value);
    }

    public bool IsFixedSize {
      get { return false; }
    }

    void System.Collections.IList.Remove(object value) {
      if(value!=null?!value.GetType().IsSubclassOf(typeof(Line)):typeof(Line).IsValueType)
        throw new System.ArgumentException("value is not a instance of Node","value");
      this.Remove((Line)value);
    }

    object System.Collections.IList.this[int index] {
      get{return this[index];}
      set{
        if(value!=null?!value.GetType().IsSubclassOf(typeof(Line)):typeof(Line).IsValueType)
          throw new System.ArgumentException("value is not a instance of Node","value");
        this[index]=(Line)value;
      }
    }

    #endregion

  }
  #endregion

}
