using Gdi=System.Drawing;

namespace mwg.RosaTerm{
	enum TerminalColorPalette{
		Rgb24bit,
		Vga16,
		Xterm256=5,
		WinCmd16,
		MacTerminal16,
		Putty16,
		Poderosa16,
		Rosa16,
	}

	static class TerminalColors{
		private static readonly Gdi::Color[] Xterm16Color={
			Gdi::Color.FromArgb(0,0,0),       // ---
			Gdi::Color.FromArgb(205,0,0),     // r--
			Gdi::Color.FromArgb(0,205,0),     // -g-
			Gdi::Color.FromArgb(205,205,0),   // rg-
			Gdi::Color.FromArgb(0,0,238),     // --b
			Gdi::Color.FromArgb(205,0,205),   // r-b
			Gdi::Color.FromArgb(0,205,205),   // -gb
			Gdi::Color.FromArgb(229,229,229), // rgb
			Gdi::Color.FromArgb(127,127,127), // ===
			Gdi::Color.FromArgb(255,0,0),     // R==
			Gdi::Color.FromArgb(0,255,0),     // =G=
			Gdi::Color.FromArgb(255,255,0),   // RG=
			Gdi::Color.FromArgb(92,92,255),   // ==B
			Gdi::Color.FromArgb(255,0,255),   // R=B
			Gdi::Color.FromArgb(0,255,255),   // =GB
			Gdi::Color.FromArgb(255,255,255), // =GB
		};
		private static readonly int[] Xterm256Base666={0x00,0x5F,0x87,0xAF,0xD7,0xFF};//i==0?0:i*40+55;
		private static readonly int[] Xterm256Gray24={
			0x08,0x12,0x1C,0x26,0x30,0x3A,0x44,0x4E,
			0x58,0x62,0x6C,0x76,0x80,0x8A,0x94,0x9E,
			0xA8,0xB2,0xBC,0xC6,0xD0,0xDA,0xE4,0xEE};//i*10+8
		public static Gdi::Color GetXterm256Color(int index){
			if(index<0||256<=index)
				return Gdi::Color.Empty;

			if(index<16){
				return Xterm16Color[index];
			}else if(index<232){
				index-=16;
				return Gdi::Color.FromArgb(
					Xterm256Base666[index/36],
					Xterm256Base666[index/6%6],
					Xterm256Base666[index%6]
					);
			}else{
				index-=232;
				return Gdi::Color.FromArgb(
					Xterm256Gray24[index],
					Xterm256Gray24[index],
					Xterm256Gray24[index]
					);
			}
		}
    /// <summary>
    /// ISO 8613-6 [CCITT Recommendation T.416] "13.1.8 Select Graphic Rendition (SGR)"
    /// http://www.itu.int/rec/T-REC-T.416-199303-I/en
    /// に従って SGR の色指定 38/48 を読み取ります。
    /// </summary>
    /// <param name="color">色を返します。</param>
    /// <param name="args">引数の配列を表すオブジェクトを指定します。</param>
    /// <param name="offset">引数内の色指定の開始位置を指定します。38 または 48 に対応する番号です。</param>
    /// <returns>正しい色指定の場合に、消費した引数の数を返します。色指定が誤っている場合は 0 を返します。</returns>
    public static int GetISO8613_6Colors(out Gdi::Color color,mwg.RosaTerm.IReadOnlyIndexer<int> args,int offset){
      // 本来は引数は ":" 区切である上に、可変長である。
      // 38:1
      // 38:2:r:g:b:?:tol:tolc
      // 38:3:c:m:y:?:tol:tols
      // 38:4:c:m:y:k:tol:tols
      // 38:5:i
      // tol = tolerance, tols = color space associated with the tolerance
      if(args[offset]==38||args[offset]==48){
        switch(args[++offset]){
          case 0:
            color=Gdi::Color.Empty;
            return 2;
          case 1:
            color=Gdi::Color.Transparent;
            return 2;
          case 2:
            color=Gdi::Color.FromArgb(args[offset+1],args[offset+2],args[offset+3]);
            return 5;
          case 3:
            {
              int r=255-args[offset+1];
              int g=255-args[offset+2];
              int b=255-args[offset+3];
              color=Gdi::Color.FromArgb(r,g,b);
            }
            return 5;
          case 4:
            {
              int bright=255-args[offset+4];
              int r=(255-args[offset+1])*bright/255;
              int g=(255-args[offset+2])*bright/255;
              int b=(255-args[offset+3])*bright/255;
              color=Gdi::Color.FromArgb(r,g,b);
            }
            return 6;
          case 5:
            color=GetXterm256Color(args[offset+1]);
            return 3;
        }
      }
      color=Gdi::Color.Empty;
      return 0;
    }

		// from http://en.wikipedia.org/wiki/ANSI_escape_code
		private static readonly Gdi::Color[] Vga16Color={
			Gdi::Color.FromArgb(0,0,0),       // ---
			Gdi::Color.FromArgb(170,0,0),     // r--
			Gdi::Color.FromArgb(0,170,0),     // -g-
			Gdi::Color.FromArgb(170,85,0),    // rg-
			Gdi::Color.FromArgb(0,0,170),     // --b
			Gdi::Color.FromArgb(170,0,170),   // r-b
			Gdi::Color.FromArgb(0,170,170),   // -gb
			Gdi::Color.FromArgb(170,170,170), // rgb
			Gdi::Color.FromArgb(85,85,85),    // ===
			Gdi::Color.FromArgb(255,85,85),   // R==
			Gdi::Color.FromArgb(85,255,85),   // =G=
			Gdi::Color.FromArgb(255,255,85),  // RG=
			Gdi::Color.FromArgb(85,85,255),   // ==B
			Gdi::Color.FromArgb(255,85,255),  // R=B
			Gdi::Color.FromArgb(85,255,255),  // =GB
			Gdi::Color.FromArgb(255,255,255), // =GB
		};
		public static Gdi::Color GetVga16Color(int index){
			return 0<=index&&index<16?Vga16Color[index]:Gdi::Color.Empty;
		}

		private static Gdi::Color[] WinCmd16Color={
			Gdi::Color.Black,
			Gdi::Color.Maroon,
			Gdi::Color.Green,
			Gdi::Color.Olive,
			Gdi::Color.Navy,
			Gdi::Color.Purple,
			Gdi::Color.Teal,
			Gdi::Color.Silver,
			Gdi::Color.Gray,
			Gdi::Color.Red,
			Gdi::Color.Lime,
			Gdi::Color.Yellow,
			Gdi::Color.Blue,
			Gdi::Color.Magenta,
			Gdi::Color.Cyan,
			Gdi::Color.White,
		};
		public static Gdi::Color GetWinCmd16Color(int index){
			return 0<=index&&index<16?WinCmd16Color[index]:Gdi::Color.Empty;
		}

		private static readonly Gdi::Color[] MacTerminal16Color={
			Gdi::Color.FromArgb(0,0,0),      // ---
			Gdi::Color.FromArgb(194,54,33),  // r--
			Gdi::Color.FromArgb(37,188,36),  // -g-
			Gdi::Color.FromArgb(173,173,39), // rg-
			Gdi::Color.FromArgb(73,46,225),  // --b
			Gdi::Color.FromArgb(211,56,211), // r-b
			Gdi::Color.FromArgb(51,187,200), // -gb
			Gdi::Color.FromArgb(203,204,205),// rgb
			Gdi::Color.FromArgb(129,131,131),// ===
			Gdi::Color.FromArgb(252,57,31),  // R==
			Gdi::Color.FromArgb(49,231,34),  // =G=
			Gdi::Color.FromArgb(234,236,35), // RG=
			Gdi::Color.FromArgb(88,51,255),  // ==B
			Gdi::Color.FromArgb(249,53,248), // R=B
			Gdi::Color.FromArgb(20,240,240), // =GB
			Gdi::Color.FromArgb(233,235,235),// =GB
		};
		public static Gdi::Color GetMacTerminal16Color(int index){
			return 0<=index&&index<16?MacTerminal16Color[index]:Gdi::Color.Empty;
		}

		private static readonly Gdi::Color[] Putty16Color={
			Gdi::Color.FromArgb(0,0,0),       // ---
			Gdi::Color.FromArgb(187,0,0),     // r--
			Gdi::Color.FromArgb(0,187,0),     // -g-
			Gdi::Color.FromArgb(187,187,0),   // rg-
			Gdi::Color.FromArgb(0,0,187),     // --b
			Gdi::Color.FromArgb(187,0,187),   // r-b
			Gdi::Color.FromArgb(0,187,187),   // -gb
			Gdi::Color.FromArgb(187,187,187), // rgb
			Gdi::Color.FromArgb(85,85,85),    // ===
			Gdi::Color.FromArgb(255,85,85),   // R==
			Gdi::Color.FromArgb(85,255,85),   // =G=
			Gdi::Color.FromArgb(255,255,85),  // RG=
			Gdi::Color.FromArgb(85,85,255),   // ==B
			Gdi::Color.FromArgb(255,85,255),  // R=B
			Gdi::Color.FromArgb(85,255,255),  // =GB
			Gdi::Color.FromArgb(255,255,255), // =GB
		};
		public static Gdi::Color GetPutty16Color(int index){
			return 0<=index&&index<16?Putty16Color[index]:Gdi::Color.Empty;
		}

		// original
		private static readonly Gdi::Color[] Poderosa16Color={
			Gdi::Color.Black,
			Gdi::Color.Red,
			Gdi::Color.Green,
			Gdi::Color.Yellow,
			Gdi::Color.Blue,
			Gdi::Color.Magenta,
			Gdi::Color.Cyan,
			Gdi::Color.White,
			Gdi::Color.FromArgb(64,64,64),
			Gdi::Color.FromArgb(255,64,64),
			Gdi::Color.FromArgb(64,255,64),
			Gdi::Color.FromArgb(255,255,64),
			Gdi::Color.FromArgb(64,64,255),
			Gdi::Color.FromArgb(255,64,255),
			Gdi::Color.FromArgb(64,255,255),
			Gdi::Color.White,
		};
		public static Gdi::Color GetPoderosa16Color(int index){
			return 0<=index&&index<16?Poderosa16Color[index]:Gdi::Color.Empty;
		}

		private static readonly Gdi::Color[] Rosa16Color={
			Gdi::Color.Black,
			Gdi::Color.Maroon,
			Gdi::Color.Green,
			Gdi::Color.Olive,
			Gdi::Color.Navy,
			Gdi::Color.Purple,
			Gdi::Color.Teal,
			Gdi::Color.Silver,
			Gdi::Color.Gray,
			Gdi::Color.Red,
			Gdi::Color.LimeGreen,
			Gdi::Color.Gold,
			Gdi::Color.Blue,
			Gdi::Color.Magenta,
			Gdi::Color.Turquoise,
			Gdi::Color.White,
		};
		public static Gdi::Color GetRosa16Color(int index){
			return 0<=index&&index<16?Rosa16Color[index]:Gdi::Color.Empty;
		}

		public static Gdi::Color GetPaletteColor(TerminalColorPalette palette,int index){
			switch(palette){
				case TerminalColorPalette.Rgb24bit:return Gdi::Color.FromArgb(0x00FFFFFF&index);
				case TerminalColorPalette.Vga16:return GetVga16Color(index);
				case TerminalColorPalette.Xterm256:return GetXterm256Color(index);
				case TerminalColorPalette.WinCmd16:return GetWinCmd16Color(index);
				case TerminalColorPalette.MacTerminal16:return GetMacTerminal16Color(index);
				case TerminalColorPalette.Putty16:return GetPutty16Color(index);
				case TerminalColorPalette.Poderosa16:return GetPoderosa16Color(index);
				case TerminalColorPalette.Rosa16:return GetRosa16Color(index);
				default:return Gdi::Color.Empty;
			}
		}
	}
}