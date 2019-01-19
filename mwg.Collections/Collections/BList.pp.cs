using Gen=System.Collections.Generic;

namespace mwg{
  internal static partial class __dbg__{
    [System.Diagnostics.Conditional("DEBUG"),System.Diagnostics.DebuggerHidden]
    public static void AssertBList(bool condition,params object[] x){
      if(!condition)
        throw new System.InvalidProgramException();
    }
  }
}

namespace mwg.Collections{

//%m mwg::Collections::BList (
  #region class BList
//%%if IsEx==1 (
  internal interface IBListExNode{
    int OffsetOfIndex{get;}
    BListExElement GetPreviousElement(int index);
    BListExElement GetNextElement(int index);
  }
  /// <summary>
  /// BListEx の要素の基本クラスを提供します。
  /// この基本クラスを使用して、要素が現在 BListEx 内のどの位置にあるかを取得する事が可能です。
  /// </summary>
  public class BListExElement{
    internal IBListExNode node0=null;
    internal int index=0;
    /// <summary>
    /// この要素のリスト内に於ける位置を取得します。
    /// </summary>
    public int IndexInList{
      get{return this.node0==null?-1:this.node0.OffsetOfIndex+this.index;}
    }
    /// <summary>
    /// この要素の直前に位置している要素を取得します。
    /// この要素がリスト内の先頭に位置している場合には null を返します。
    /// </summary>
    public BListExElement PreviousElement{
      get{return this.node0==null?(BListExElement)null:this.node0.GetPreviousElement(this.index);}
    }
    /// <summary>
    /// この要素の直後に位置している要素を取得します。
    /// この要素がリスト内の末尾に位置している場合には null を返します。
    /// </summary>
    public BListExElement NextElement{
      get{return this.node0==null?(BListExElement)null:this.node0.GetNextElement(this.index);}
    }
  }
//%%elif IsEx==2
  public interface IBListE2Node<T>{
    int OffsetOfIndex{get;}
    T GetPreviousElement(int index);
    T GetNextElement(int index);
  }
  /// <summary>
  /// BListEx の要素の基本クラスを提供します。
  /// この基本クラスを使用して、要素が現在 BListEx 内のどの位置にあるかを取得する事が可能です。
  /// </summary>
  public class BListE2Element<T>{
    internal IBListE2Node<T> node0=null;
    internal int index=0;
    /// <summary>
    /// この要素のリスト内に於ける位置を取得します。
    /// </summary>
    public int IndexInList{
      get{return this.node0==null?-1:this.node0.OffsetOfIndex+this.index;}
    }
    /// <summary>
    /// この要素の直前に位置している要素を取得します。
    /// この要素がリスト内の先頭に位置している場合には null を返します。
    /// </summary>
    public T PreviousElement{
      get{return this.node0==null?default(T):this.node0.GetPreviousElement(this.index);}
    }
    /// <summary>
    /// この要素の直後に位置している要素を取得します。
    /// この要素がリスト内の末尾に位置している場合には null を返します。
    /// </summary>
    public T NextElement{
      get{return this.node0==null?default(T):this.node0.GetNextElement(this.index);}
    }
   
  }
//%%)

  /// <summary>
  /// 平衡木 (B+ 木) 状の内部構造を持つ配列です。
  /// ランダムアクセス・挿入に対して O(log n) の時間計算量を要します。
  /// </summary>
  /// <typeparam name="T">要素の型を指定します。</typeparam>
  public class BList<T>
    :Gen::IList<T>,System.Collections.IList,
    Gen::ICollection<T>,System.Collections.ICollection,
    Gen::IEnumerable<T>,System.Collections.IEnumerable
//%%if IsEx==1 (
    where T:BListExElement
//%%elif IsEx==2
    where T:BListE2Element<T>
//%%)
  {
    private const int LLIMIT=32;
    private const int ULIMIT=64;
    private const float BALANCE=3.0f; //!< must be larger than ULIMIT/LLIMIT.
    // 要件:
    //   LLIMIT < 要素数 <= ULIMIT
    // 子を追加した時に更新するべき物
    //   1 子.list (子孫も)
    //   2 子.parent
    //   3 子.index
    //   4 自.weight
    // root を設定した時に更新するべき物
    //   1 root.parent=null;
    //   2 root.list=this;

    private abstract class Node{
      public readonly int level;
      public BList<T> list;
      public Node1 parent;
      public int index;
      public int weight;
      public Node(int level){
        this.level=level;
        this.list=null;
        this.parent=null;
        this.weight=0;
      }

      public abstract void Insert(int index,T value);
      public abstract void RemoveAt(int index);
      public abstract T this[int index]{get;set;}

      protected void add_weight(int delta){
        if(delta==0)return;
        this.weight+=delta;
        Node1 p=this.parent;
        while(p!=null){
          p.weight+=delta;
          p=p.parent;
        }
      }
      internal virtual void set_list(BList<T> list){
        this.list=list;
      }
//%%if IsEx==1||IsEx==2 (

      #region IBListExNode メンバ
      public int OffsetOfIndex {
        get{
          if(this==this.list.root)
            return 0;
          else
            return this.parent.OffsetOfIndex+this.index;
        }
      }
      #endregion
//%%)
    }
    private class Node0:Node
//%%if IsEx==1 (
      ,IBListExNode
//%%elif IsEx==2
      ,IBListE2Node<T>
//%%)
    {
      public readonly Gen::List<T> data=new Gen::List<T>();
      public Node0():base(0){}

      public override T this[int index]{
        get{
          __dbg__.AssertBList(0<=index&&index<this.data.Count);
          return this.data[index];
        }
        set{
          __dbg__.AssertBList(0<=index&&index<this.data.Count);
//%%if IsEx==1||IsEx==2 (
          if(this.data[index]!=null)
            this.data[index].node0=null;
//%%)
          this.data[index]=value;
//%%if IsEx==1||IsEx==2 (
          if(value!=null){
            value.node0=this;
            value.index=index;
          }
//%%)
        }
      }

      public override void Insert(int index,T value){
        __dbg__.AssertBList(0<=index&&index<=this.data.Count);
        if(this.data.Count<ULIMIT){
          this.data.Insert(index,value);
//%%if IsEx==1||IsEx==2 (
          if(value!=null)
            value.node0=this;
          for(int i=index,iN=this.data.Count;i<iN;i++){
            T e=this.data[i];
            if(e!=null)e.index=i;
          }
//%%)
          this.add_weight(1);
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
          T nch=this.data[c1+index];
//%%if IsEx==1||IsEx==2 (
          if(nch!=null){
            nch.node0=node2;
            nch.index=node2.data.Count;
          }
//%%)
          node2.data.Add(nch);
          node2.weight++;
        }
        this.data.RemoveRange(c1,c2);
        this.add_weight(-node2.weight);
        return node2;
      }

      public override void RemoveAt(int index) {
        __dbg__.AssertBList(0<=index&&index<this.data.Count);
        {
//%%if IsEx==1||IsEx==2 (
          T e=this.data[index];
          if(e!=null)e.node0=null;
//%%)
          this.data.RemoveAt(index);
          this.add_weight(-1);
        }

        if(this.list.root==this)return;

        __dbg__.AssertBList(this.parent!=null&&this.parent.data.Count>1);

        if(this.data.Count>=LLIMIT)return;

        if(this.index>0){
          Node0 prev=(Node0)this.parent.data[this.index-1];
          if(prev.data.Count>LLIMIT){
            T n=prev.data[prev.data.Count-1];
            prev.RemoveAt(prev.data.Count-1);
            this.Insert(0,n);
            return;
          }
        }
        
        if(this.index+1<this.parent.data.Count){
          Node0 next=(Node0)this.parent.data[this.index+1];
          if(next.data.Count>LLIMIT){
            T n=next.data[0];
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

//%%if IsEx==1||IsEx==2 (
        int i=this.data.Count;
        for(int j=0,jN=node.data.Count;j<jN;j++)
          node.data[j].index=i++;
//%%)
        this.data.AddRange(node.data);
        this.weight+=node.weight;
        node.weight=0;
        node.data.Clear();
      }
//%%if IsEx==1 (

      #region IBListExNode メンバ
      public BListExElement GetNextElement(int index){
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
      public BListExElement GetPreviousElement(int index){
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
//%%elif IsEx==2

      #region IBListE2Node<T> メンバ
      public T GetNextElement(int index){
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
      public T GetPreviousElement(int index){
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
//%%)
    }
    private class Node1:Node{
      public readonly Gen::List<Node> data=new Gen::List<Node>();
      public Node1(int level):base(level){}

      internal override void set_list(BList<T> list){
        if(this.list==list)return;
        this.list=list;
        for(int i=0,iN=this.data.Count;i<iN;i++)
          this.data[i].set_list(list);
      }

      public override T this[int index]{
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
          this.add_weight(node.weight);
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
        }
        this.data.RemoveRange(c1,c2);
        this.add_weight(-node2.weight);
        return node2;
      }

      public void RemoveAtNode(int index){
        __dbg__.AssertBList(0<=index&&index<this.data.Count);

        {
          Node rem=this.data[index];
          rem.parent=null;
          rem.list=null;
          this.add_weight(-rem.weight);
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
        node.weight=0;
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
//%%if IsEx==1||IsEx==2 (
        else if(this.level==0)
          foreach(Node0 node in this.data){
            int index=0;
            foreach(T e in node.data){
              if(e.node0!=node)
                throw new System.InvalidProgramException("node0.data[i].node0!=node0");
              if(e.index!=index++)
                throw new System.InvalidProgramException("node0.data[i].index!=i");
            }
          }
//%%)
      }
#endif

      public override void Insert(int index,T value){
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
        // level==2
        //   □
        //   □    □  □
        //   1 5 3 2 4 5 2 4 ... 調整出来る。
        // level==1
        //   □
        //   5 4 8 ... LLIMIT/ULIMIT で制限されているので調整不要

//%%if IsEx==0 (
#if DEBUG
        Test.dbg_nbalance++;
#endif

//%%)
        // 必要性判定
        float min=float.MaxValue;
        float max=0;
        for(int i=0,iN=this.data.Count;i<iN;i++){
          int w=this.data[i].weight;
          if(w<min)min=w;
          if(w>max)max=w;
        }
        if(min*BALANCE>max)return;

//%%if IsEx==0 (
#if DEBUG
        Test.dbg_nbalance++;
#endif

//%%)
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
    public BList(){
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
    public void Insert(int index,T value){
      if(index<0||this.root.weight<index)
        throw new System.ArgumentOutOfRangeException("index");

      this.root.Insert(index,value);
    }

    //ルール
    //  1      <  (根ノードの子供の数)   <= ULIMIT
    //  LLIMIT <= (内部ノードの子供の数) <= ULIMIT
    //  内部ノードの子供の数が ULIMIT を越えたら分裂する。
    //  内部ノードの子供の数が c1 以下になったら隣(左右で多い方)から補給する
    //    隣(多い方)も c1 以下になったら融合する
    //    ※少ない方と融合した方がメモリが節約できる気もするが、
    //      多少のヒステリシスはないと、振動する可能性がある。
    //  子供の数に変化があったらバランスさせる。■■■TODO
    //    特に「子供の重さ」が均等になるようにする。
    //    深さ 1 の子供の重さは: [c1, c2]
    //    深さ n の子供の重さは: [c1^n, c2^n]
    //    例えば、重さに2倍以上の違いがある時には、孫達を移動する。
    //  根の要素数が c2 以上になったら「根の根」を用意する。
    //    この時、全体の深さが一斉に一段増える。
    //    これで、どの葉の深さも、常に同じに保たれる。

    #region IList<T> メンバ

    /// <summary>
    /// 指定した要素が格納されている場所を検索します。
    /// </summary>
    /// <param name="item">検索対象の要素を指定します。</param>
    /// <returns>指定した要素が見付かった場合に、その位置を返します。
    /// 見付からなかった場合には -1 を返します。</returns>
    public int IndexOf(T item){
      int index=0;
      foreach(T value in this){
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
    public T this[int index] {
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

    #region ICollection<T> メンバ
    /// <summary>
    /// 指定した要素を末尾に追加します。
    /// </summary>
    /// <param name="item">末尾に追加する要素を指定します。</param>
    public void Add(T item){
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
    public bool Contains(T item){
      foreach(T value in this)
        if(value.Equals(item))return true;
      return false;
    }

    public void CopyTo(T[] array,int arrayIndex){
      if(array==null)
        throw new System.ArgumentNullException("array");
      if(0<arrayIndex)
        throw new System.ArgumentOutOfRangeException("arrayIndex","index is less than 0.");
      if(arrayIndex+this.root.weight>=array.Length)
        throw new System.ArgumentException("array","array is too small to copy the elements into");

      foreach(T item in this)
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
    public bool Remove(T item){
      int index=this.IndexOf(item);
      if(index<0)return false;
      this.RemoveAt(index);
      return true;
    }
    #endregion

    #region IEnumerable<T> メンバ
    public System.Collections.Generic.IEnumerator<T> GetEnumerator() {
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

      foreach(T item in this)
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
      if(value!=null?!value.GetType().IsSubclassOf(typeof(T)):typeof(T).IsValueType)
        throw new System.ArgumentException("value is not a instance of Node","value");
      int ret=this.root.weight;
      this.Add((T)value);
      return ret;
    }

    bool System.Collections.IList.Contains(object value) {
      if(value!=null?!value.GetType().IsSubclassOf(typeof(T)):typeof(T).IsValueType)return false;
      return this.Contains((T)value);
    }

    int System.Collections.IList.IndexOf(object value) {
      if(value!=null?!value.GetType().IsSubclassOf(typeof(T)):typeof(T).IsValueType)return -1;
      return this.IndexOf((T)value);
    }

    void System.Collections.IList.Insert(int index,object value) {
      if(value!=null?!value.GetType().IsSubclassOf(typeof(T)):typeof(T).IsValueType)
        throw new System.ArgumentException("value is not a instance of Node","value");
      this.Insert(index,(T)value);
    }

    public bool IsFixedSize {
      get { return false; }
    }

    void System.Collections.IList.Remove(object value) {
      if(value!=null?!value.GetType().IsSubclassOf(typeof(T)):typeof(T).IsValueType)
        throw new System.ArgumentException("value is not a instance of Node","value");
      this.Remove((T)value);
    }

    object System.Collections.IList.this[int index] {
      get{return this[index];}
      set{
        if(value!=null?!value.GetType().IsSubclassOf(typeof(T)):typeof(T).IsValueType)
          throw new System.ArgumentException("value is not a instance of Node","value");
        this[index]=(T)value;
      }
    }

    #endregion

//%%if IsEx==0||IsEx==1 (
#if DEBUG
//%%if IsEx==1 (
    class TestElement:BListExElement{
      int value;
      public TestElement(int num){
        this.value=num;
      }
    }
//%%)
    class Test{
      public static bool Test1(){
        System.Random ran=new System.Random(2134);
//%%if IsEx==0 (
        BList<int> list1=new BList<int>();
        Gen::List<int> list2=new System.Collections.Generic.List<int>();
//%%elif IsEx==1
        BListEx<TestElement> list1=new BListEx<TestElement>();
        Gen::List<TestElement> list2=new System.Collections.Generic.List<TestElement>();
//%%)
        const int N=10000;
        for(int i=0;i<N;i++){
//%%if IsEx==0 (
          int n=(int)(ran.NextDouble()*10000);
//%%elif IsEx==1
          TestElement n=new TestElement((int)(ran.NextDouble()*10000));
//%%)
          list1.Add(n);
          list2.Add(n);
        }

        for(int i=0;i<N;i++){
          if(list1[i]!=list2[i])
            return false;
        }
        return true;
      }

      public static int dbg_nbalance=0;
      static bool Test2(){
        System.Random ran=new System.Random(2134);

        {
//%%if IsEx==0 (
          BList<int> list1=new BList<int>();
          Gen::List<int> list2=new System.Collections.Generic.List<int>();
//%%elif IsEx==1
          BListEx<TestElement> list1=new BListEx<TestElement>();
          Gen::List<TestElement> list2=new System.Collections.Generic.List<TestElement>();
//%%)
          const int N=100000;
          for(int i=0;i<N;i++){
            int idx=(int)(ran.NextDouble()*list1.Count);
//%%if IsEx==0 (
            list1.Insert(idx,i);
            list2.Insert(idx,i);
//%%elif IsEx==1
            TestElement e=new TestElement(i);
            list1.Insert(idx,e);
            list2.Insert(idx,e);
//%%)
          }

          for(int i=0;i<N;i++){
            if(list1[i]!=list2[i])
              return false;
          }


          for(int i=0;i<N/2;i++){
            int idx=(int)(ran.NextDouble()*list1.Count);
            list1.RemoveAt(idx);
            list2.RemoveAt(idx);
          }

          for(int i=0;i<N/2;i++){
            if(list1[i]!=list2[i])
              return false;
          }
        }
        return true;
      }

      static bool Test2a(){
        System.Random ran=new System.Random(2134);

        {
//%%if IsEx==0 (
          BList<int> list1=new BList<int>();
//%%elif IsEx==1
          BListEx<TestElement> list1=new BListEx<TestElement>();
//%%)
          const int N=1000000;
          for(int i=0;i<N;i++){
            int idx=(int)(ran.NextDouble()*list1.Count);
//%%if IsEx==0 (
            list1.Insert(idx,i);
//%%elif IsEx==1
            TestElement e=new TestElement(i);
            list1.Insert(idx,e);
//%%)
            if(i%1000==0)
              list1.DbgCheckState();
          }

          for(int i=0;i<N/2;i++){
            int idx=(int)(ran.NextDouble()*list1.Count);
            list1.RemoveAt(idx);
            if(i%1000==0)
              list1.DbgCheckState();
          }

          list1.DbgCheckState();
        }
        return true;
      }
      static bool Test2b(){
        System.Random ran=new System.Random(2134);

        {
//%%if IsEx==0 (
          Gen::List<int> list2=new System.Collections.Generic.List<int>();
//%%elif IsEx==1
          Gen::List<TestElement> list2=new System.Collections.Generic.List<TestElement>();
//%%)
          const int N=100000;
          for(int i=0;i<N;i++){
            int idx=(int)(ran.NextDouble()*list2.Count);
//%%if IsEx==0 (
            list2.Insert(idx,i);
//%%elif IsEx==1
            TestElement e=new TestElement(i);
            list2.Insert(idx,e);
//%%)
          }

          for(int i=0;i<N/2;i++){
            int idx=(int)(ran.NextDouble()*list2.Count);
            list2.RemoveAt(idx);
          }
        }
        return true;
      }
    }
#endif
//%%)
  }
  #endregion

//%)
//%[IsEx=0]
//%x mwg::Collections::BList
//%[IsEx=1]
//%x mwg::Collections::BList.r/\<BList\>/BListEx/
//%[IsEx=2]
//%x mwg::Collections::BList.r/\<BList\>/BListE2/

}
