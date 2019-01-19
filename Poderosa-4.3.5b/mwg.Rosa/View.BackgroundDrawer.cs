using Gdi=System.Drawing;
using Color=System.Drawing.Color;
using Interop=System.Runtime.InteropServices;

namespace mwg.RosaTerm.View{
	public class BackgroundDrawer:System.IDisposable{
		public void Dispose(){
			this.FreeCache();
		}

		// 変化検知用
		Gdi::Image original=null;
		float scaleX;
		float scaleY;
		float offsetX;
		float offsetY;
		Gdi::Size displaySize;

		// 描画用
		/// <summary>
		/// 画面座標での背景画像配置領域を保持します。
		/// </summary>
		Gdi::RectangleF imageArea;
		Gdi::Bitmap cache=null;
#if WIN32PINVOKE
		System.IntPtr cache_hbmp=System.IntPtr.Zero;
		System.IntPtr cache_hdc=System.IntPtr.Zero;
#endif
		void FreeCache(){
#if WIN32PINVOKE
			if(this.cache_hdc!=System.IntPtr.Zero){
				DeleteDC(cache_hdc);
				cache_hdc=System.IntPtr.Zero;
			}
			if(this.cache_hbmp!=System.IntPtr.Zero){
				DeleteObject(cache_hbmp);
				cache_hbmp=System.IntPtr.Zero;
			}
#endif
			if(this.cache!=null){
				this.cache.Dispose();
				this.cache=null;
			}
		}

#if WIN32PINVOKE
		System.IntPtr GetCacheHdc(System.IntPtr targetDC){
			if(cache_hdc==System.IntPtr.Zero){
				cache_hbmp=this.cache.GetHbitmap();
				cache_hdc=CreateCompatibleDC(targetDC);
				SelectObject(cache_hdc,cache_hbmp);
			}

			return cache_hdc;
		}
#endif

		/// <summary>
		/// 描画する背景画像の情報を更新します。
		/// </summary>
		/// <param name="image">描画する背景画像を指定します。</param>
		/// <param name="scaleX">画像の表示 X 倍率を指定します。</param>
		/// <param name="scaleY">画像の表示 Y 倍率を指定します。</param>
		/// <param name="offsetX">画像の表示 X 位置を指定します。</param>
		/// <param name="offsetY">画像の表示 Y 位置を指定します。</param>
		/// <param name="displaySize">画面上の表示の大きさを指定します。</param>
		public void UpdateImage(Gdi::Image image,float scaleX,float scaleY,float offsetX,float offsetY,Gdi::Size displaySize){
			if(image==this.original
				&&scaleX==this.scaleX&&scaleY==this.scaleY
				&&offsetX==this.offsetX&&offsetY==this.offsetY
				&&displaySize==this.displaySize
				)return;

			this.original=image;
			this.scaleX=scaleX;
			this.scaleY=scaleY;
			this.offsetX=offsetX;
			this.offsetY=offsetY;
			this.displaySize=displaySize;

			imageArea=new Gdi::RectangleF(offsetX,offsetY,image.Width*scaleX,image.Height*scaleY);
			// 画面座標での描画領域
			Gdi::RectangleF srcRectD=Gdi::RectangleF.Intersect(new Gdi::RectangleF(Gdi::PointF.Empty,displaySize),imageArea);
			// 画像座標での描画領域
			Gdi::RectangleF srcRectI=new Gdi::RectangleF(
				(srcRectD.X-offsetX)/scaleX,
				(srcRectD.Y-offsetY)/scaleY,
				srcRectD.Width/scaleX,
				srcRectD.Height/scaleY
				);
			srcRectD.X=0;
			srcRectD.Y=0;

			this.FreeCache();
			this.cache=new System.Drawing.Bitmap((int)(srcRectD.Width+.5f),(int)(srcRectD.Height+.5f));
			using(Gdi::Graphics g=Gdi::Graphics.FromImage(this.cache))
				g.DrawImage(image,srcRectD,srcRectI,Gdi::GraphicsUnit.Pixel);

			ImageEnlight(this.cache);
		}

		private static void ImageEndark(Gdi::Bitmap bmp){
			// 色を暗くする -> これは GetImage の方に追加するべきではないか
			Gdi::Imaging.BitmapData data=bmp.LockBits(
			  new Gdi::Rectangle(Gdi::Point.Empty,bmp.Size),
			  Gdi::Imaging.ImageLockMode.ReadWrite,
			  Gdi::Imaging.PixelFormat.Format24bppRgb
			  );
			for(int y=0;y<bmp.Height;y++)unsafe{
			  byte* p=(byte*)data.Scan0+data.Stride*y;
			  byte* pM=p+bmp.Width*3;
			  for(;p<pM;p++){
			    //*p+=(byte)((255-*p)/3*2);
					*p/=5;
			  }
			}
			bmp.UnlockBits(data);
		}
		private static void ImageEnlight(Gdi::Bitmap bmp){
			// 色を薄くする -> これは GetImage の方に追加するべきではないか
			Gdi::Imaging.BitmapData data=bmp.LockBits(
			  new Gdi::Rectangle(Gdi::Point.Empty,bmp.Size),
			  Gdi::Imaging.ImageLockMode.ReadWrite,
			  Gdi::Imaging.PixelFormat.Format24bppRgb
			  );
			for(int y=0;y<bmp.Height;y++)unsafe{
			  byte* p=(byte*)data.Scan0+data.Stride*y;
			  byte* pM=p+bmp.Width*3;
			  for(;p<pM;p++){
			    *p+=(byte)((255-*p)/3*2);
					//*p/=5;
			  }
			}
			bmp.UnlockBits(data);
		}

		public void DrawImageClipped(Gdi::Graphics g,Gdi::RectangleF clip){
			clip.Intersect(imageArea);
			float cacheX=clip.X+0.5f;
			if(offsetX>0)cacheX-=offsetX;
			float cacheY=clip.Y+0.5f;
			if(offsetY>0)cacheY-=offsetY;
#if WIN32PINVOKE
			using(Gdi::Graphics gImg=Gdi::Graphics.FromImage(this.cache)){
				const int SRCCOPY=0x00CC0020;
				System.IntPtr dstDC=g.GetHdc();
				System.IntPtr srcDC=GetCacheHdc(dstDC);
				BitBlt(
					dstDC,(int)clip.X,(int)clip.Y,
					(int)clip.Width,(int)clip.Height,
					srcDC,(int)cacheX,(int)cacheY,
					SRCCOPY
					);
				g.ReleaseHdc(dstDC);
			}
#else
			Gdi::RectangleF rectI=new Gdi::RectangleF(cacheX,cacheY,clip.Width,clip.Height);
			g.DrawImage(this.cache,clip,rectI,Gdi::GraphicsUnit.Pixel);
#endif
		}
		
#if WIN32PINVOKE
		[Interop::DllImport("gdi32")]
		[return:Interop::MarshalAs(Interop::UnmanagedType.Bool)]
		static extern bool BitBlt(
			System.IntPtr   hdcDest,    // コピー先デバイスコンテキスト
			int   nXDest,     // コピー先x座標
			int   nYDest,     // コピー先y座標
			int   nWidth,     // コピーする幅
			int   nHeight,    // コピーする高さ
			System.IntPtr hdcSource,  // コピー元デバイスコンテキスト
			int   nXSource,   // コピー元x座標
			int   nYSource,   // コピー元y座標
			uint dwRaster
			);

		[Interop::DllImport("gdi32.dll",SetLastError=true)]
		public static extern System.IntPtr CreateCompatibleDC(System.IntPtr hDC);

		[Interop::DllImport("gdi32.dll")]
		public static extern System.IntPtr SelectObject(System.IntPtr hDC,System.IntPtr hObject);

		[Interop::DllImport("gdi32.dll")]
		public static extern System.IntPtr DeleteObject(System.IntPtr hObject);

		[Interop::DllImport("gdi32.dll")]
		[return:Interop::MarshalAs(Interop::UnmanagedType.Bool)]
		public static extern bool DeleteDC(System.IntPtr hdc);
#endif
	}
}