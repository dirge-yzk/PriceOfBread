using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PriceOfBread.SimCore;

namespace PriceOfBread.App;

/// <summary>地図の政治塗り分け（作者指示2026-09-02）。所持州（本国州＋植民地）を種に**陸の上だけ**を同時に広げ、
/// 他国の塗りとぶつかった所で止める。
///
/// ── 上限距離（Vic風） ────────────────────────────────────────────────────
/// 無制限に広げると、シベリア・サハラ・北米内陸のような1783年の未領有地まで最寄り国の色で埋まる（EU4風）。
/// 上限＝<see cref="Places.MaxSpacingKm"/>＝**州を敷いたときの最長間隔と同じ値**（作者裁定）。
/// 州はその規約で置かれているので、同じ距離で打ち切れば「そこに置かれ得た未保有の州の領分」までは届かない。
/// ⚠新しい調整値を作らない＝規約の共有。値は SimCore 側の1本だけを読む。
///
/// ── 格子と真値 ────────────────────────────────────────────────────────────
/// <see cref="Landmask"/> と同じ 0.1°格子＝**見えている陸（背景SVGを焼いた物）と塗りの陸が同じ物**。
/// ⚠陸マスクは海岸線を1格子ぶん内側へ収縮させてある（航路判定の都合）ので、塗りは最後に1格子だけ膨らませて
///   見えている海岸まで戻す＝収縮の打ち消しであり、新しい余白ではない。
/// ⚠表示のみ＝ここで作った物をエンジンは一切読まない（塗りは観測であって領有ではない）。</summary>
public static class MapFill
{
    public readonly record struct Seed(double Lat, double Lon, Color Color, double Weight = 1.0);   // Weight＝塗りの濃さ（0..1）。地図モード「支配度」＝国家区分で薄める（無統治が多い州は塗りが抜ける）

    public const double CellDeg = Landmask.Step;   // 0.1°（陸マスクと同じ）
    public const int Nx = 3600;                    // 表示は経度360°ぶん＝東西で巻く
    public static readonly int Ny = (int)Math.Round((MapData.LatTop - MapData.LatBottom) / CellDeg);
    private const double KmPerDeg = 111.32;        // 緯度1°の距離（子午線）
    private const byte FillAlpha = 105;            // 背景の陸色が透けて見える濃さ
    private const byte BorderAlpha = 200;          // 国境線の濃さ（B1'・2026-09-05）

    private static byte[]? _land;    // 表示格子の陸（0/1）。地図は不変なので一度だけ焼く
    private static float[]? _dist; private static short[]? _own; private static byte[]? _px;   // 焼くときの作業場（使い回す）
    private static byte[]? _drawn;   // **描かれている**陸（背景ビットマップから読んだ0/1）。塗りはこの内側にだけ置く

    /// <summary>背景（世界地図）を焼いたときに、その絵から読み取った陸を渡す。
    /// ⚠陸マスクは航路判定用で**湖を陸として持つ**（湖は海ではない）ので、これが無いと湖が国色に染まる。
    /// 絵そのものを型紙にすれば、塗りは見えている陸の内側にぴったり収まる（海岸線の1格子ぶんのずれも同時に消える）。</summary>
    public static void SetDrawnLand(byte[] mask)
    {
        if (mask.Length == Nx * Ny) _drawn = mask;
    }

    private static byte[] Land()
    {
        if (_land is not null) return _land;
        var a = new byte[Nx * Ny];
        for (int iy = 0; iy < Ny; iy++)
        {
            double lat = MapData.LatTop - (iy + 0.5) * CellDeg;
            int row = iy * Nx;
            for (int ix = 0; ix < Nx; ix++)
                a[row + ix] = Landmask.IsLand(lat, MapData.LonLeft + (ix + 0.5) * CellDeg) ? (byte)1 : (byte)0;
        }
        return _land = a;
    }

    private static readonly object Gate = new();   // 作業場（下の使い回し配列）を守る＝セーブ読込で新旧2つのVMが重なっても混ざらない

    /// <summary>種から塗りを1枚焼く（重いのは呼び出し側が背景スレッドで回す）。種が無ければ null。
    /// ★地図の州○の廃止（作者裁定 F7・2026-09-28）：州の当たり（ホバー・クリック）は**この塗りの割り当てそのもの**で取る＝
    ///   <paramref name="hit"/>＝格子ごとの種の番号（-1＝海・塗られていない）。塗りと当たりが同じ規則の1本。
    ///   種の [0, painted) は塗る種、[painted, Count) は当たりだけの種（未領有の植民地化標的）＝塗られた格子には入らず、
    ///   どの国も届かない陸だけを取る＝塗りの絵は当たりだけの種の有無で1画素も変わらない。</summary>
    public static WriteableBitmap? Build(IReadOnlyList<Seed> seeds, int painted, out short[]? hit)
    {
        hit = null;
        if (seeds.Count == 0) return null;
        lock (Gate) return BuildCore(seeds, painted, out hit);
    }

    private static WriteableBitmap BuildCore(IReadOnlyList<Seed> seeds, int painted, out short[] hit)
    {
        var land = Land();
        int n = Nx * Ny;
        // 作業配列は使い回す（1枚ずつしか焼かない＝呼び出し側が重ならないよう見張っている）。
        // ⚠30MB級の確保を領土が動くたびに繰り返すと大きなオブジェクトの山が荒れる＝毎回作らない。
        var dist = _dist ??= new float[n];
        var owner = _own ??= new short[n];
        var px = _px ??= new byte[n * 4];
        Array.Fill(dist, float.PositiveInfinity);
        Array.Fill(owner, (short)-1);
        Array.Clear(px);

        // 歩幅＝実距離（km）。南北は一定、東西は緯度で縮む（子午線の収束）＝格子数でなく距離で切るための最小限。
        float stepY = (float)(KmPerDeg * CellDeg);
        var stepX = new float[Ny];
        for (int iy = 0; iy < Ny; iy++)
            stepX[iy] = (float)(KmPerDeg * CellDeg * Math.Cos((MapData.LatTop - (iy + 0.5) * CellDeg) * Math.PI / 180.0));

        // 多源流ダイクストラ（1kmのバケツ）。最小の歩幅は表示北端（緯度82°）でも約1.5km＞1km＝
        // 押し込む先のバケツは必ず今より後ろ＝バケツ順の処理で厳密に最短が出る。
        double maxKm = Places.MaxSpacingKm;
        int nb = (int)Math.Ceiling(maxKm) + 2;
        var buckets = new List<int>?[nb];

        void Push(int cell, float d)
        {
            int b = (int)d;
            if (b >= nb) return;
            (buckets[b] ??= new List<int>(512)).Add(cell);
        }

        // 種 [s0, s1) を置いて広げる。s0 より前の種が既に取った格子には入らない（s0=0 の1回目は制約なし）。
        void Spread(int s0, int s1)
        {
            for (int si = s0; si < s1; si++)
            {
                int c = CellOf(seeds[si].Lat, seeds[si].Lon);
                if (c < 0) continue;
                if (land[c] == 0) { int c2 = Nearest(c, c3 => land[c3] != 0); if (c2 < 0) continue; c = c2; }   // 海へ丸められた港町は近くの陸へ吸着
                if (owner[c] >= 0) continue;   // 同じ格子に2つ目＝先着（決定論：種の並び順）
                dist[c] = 0f; owner[c] = (short)si; Push(c, 0f);
            }

            for (int b = 0; b < nb; b++)
            {
                var q = buckets[b];
                if (q is null) continue;
                for (int qi = 0; qi < q.Count; qi++)
                {
                    int c = q[qi];
                    float dc = dist[c];
                    if ((int)dc != b) continue;   // より短い距離で入れ直された古い項目
                    int iy = c / Nx, ix = c - iy * Nx;
                    short own = owner[c];
                    float sx = stepX[iy];
                    float sd = MathF.Sqrt(sx * sx + stepY * stepY);   // 斜めの実距離＝前線が45°の直線に折れない（4近傍だとひし形になる）
                    int e = ix == Nx - 1 ? c - (Nx - 1) : c + 1;      // 東（端で巻く＝東西ループ）
                    int w2 = ix == 0 ? c + (Nx - 1) : c - 1;          // 西
                    Relax(e, dc + sx, own);
                    Relax(w2, dc + sx, own);
                    if (iy > 0)
                    {
                        Relax(c - Nx, dc + stepY, own);
                        Relax(e - Nx, dc + sd, own); Relax(w2 - Nx, dc + sd, own);
                    }
                    if (iy < Ny - 1)
                    {
                        Relax(c + Nx, dc + stepY, own);
                        Relax(e + Nx, dc + sd, own); Relax(w2 + Nx, dc + sd, own);
                    }
                }
                buckets[b] = null;   // 使い終わったバケツは捨てる（ピークメモリを抑える）
            }

            void Relax(int c2, float nd, short own)
            {
                if (!(nd <= maxKm)) return;     // 否定形＝NaNは広げない
                if (land[c2] == 0) return;      // 陸だけ（海を渡らない＝州の無い島は塗らない）
                if (owner[c2] >= 0 && owner[c2] < s0) return;   // 前の回の種が取った格子＝当たりだけの種は塗られた土地を奪わない
                if (!(nd < dist[c2])) return;
                dist[c2] = nd; owner[c2] = own; Push(c2, nd);
            }
        }

        Spread(0, painted);

        var drawn = _drawn;
        // ★国境線（B1'・2026-09-05）：最終の持ち主（膨らませ後）を1本の配列に残し、東/南の隣が別の持ち主なら暗い画素にする＝国境が見える。
        //   海岸は型紙（背景の絵）が決めるので線を引かない（陸と海の色境界がそのまま海岸線）。
        // ★F7（2026-09-28）：この配列がそのまま州の当たりになる＝呼び出し側へ渡すので、焼くたびに新しく作る（UIが読んでいる間に次の1枚が書き換えない）。
        var ownFinal = new short[n];
        System.Array.Fill(ownFinal, (short)-1);
        for (int c = 0; c < n; c++)
        {
            if (drawn is not null && drawn[c] == 0) continue;   // 絵の上で海/湖の所は塗らない（型紙＝背景そのもの）
            short own = owner[c];
            if (own < 0)
            {
                // 1格子ぶんの膨らませ＝陸マスクの収縮の打ち消し（見えている海岸まで塗る）。4近傍で最も近い持ち主を借りる。
                int iy = c / Nx, ix = c - iy * Nx;
                float best = float.PositiveInfinity;
                int e = ix == Nx - 1 ? c - (Nx - 1) : c + 1, w = ix == 0 ? c + (Nx - 1) : c - 1;
                if (owner[e] >= 0 && dist[e] < best) { best = dist[e]; own = owner[e]; }
                if (owner[w] >= 0 && dist[w] < best) { best = dist[w]; own = owner[w]; }
                if (iy > 0 && owner[c - Nx] >= 0 && dist[c - Nx] < best) { best = dist[c - Nx]; own = owner[c - Nx]; }
                if (iy < Ny - 1 && owner[c + Nx] >= 0 && dist[c + Nx] < best) { own = owner[c + Nx]; }
                if (own < 0) continue;
            }
            ownFinal[c] = own;
            var col = seeds[own].Color;
            int o = c * 4;
            int a = (int)Math.Clamp(FillAlpha * seeds[own].Weight, 0, 255);   // 支配度モード＝国家区分の濃淡（Weight=1 で従来と同じ値）
            px[o] = (byte)(col.B * a / 255);       // プリマルチプライ済みBGRA
            px[o + 1] = (byte)(col.G * a / 255);
            px[o + 2] = (byte)(col.R * a / 255);
            px[o + 3] = (byte)a;
        }
        // 国境線＝東または南の隣が別の国（両方とも塗られている所だけ）。線の色＝自国色を暗く・不透明寄り（1格子＝約0.5画面px＝細線）。
        for (int c = 0; c < n; c++)
        {
            short own = ownFinal[c];
            if (own < 0) continue;
            int iy = c / Nx, ix = c - iy * Nx;
            int e = ix == Nx - 1 ? c - (Nx - 1) : c + 1;
            bool border = (ownFinal[e] >= 0 && ownFinal[e] != own) || (iy < Ny - 1 && ownFinal[c + Nx] >= 0 && ownFinal[c + Nx] != own);
            if (!border) continue;
            var col = seeds[own].Color;
            int o = c * 4;
            px[o] = (byte)(col.B * BorderAlpha * 45 / 100 / 255);   // 暗い自国色（プリマルチプライ）
            px[o + 1] = (byte)(col.G * BorderAlpha * 45 / 100 / 255);
            px[o + 2] = (byte)(col.R * BorderAlpha * 45 / 100 / 255);
            px[o + 3] = BorderAlpha;
        }
        // 当たりだけの種（未領有の標的）＝塗りと国境線を焼いた後に広げる＝絵は変わらない。どの国も届かない陸（背景の絵の上の陸）だけを当たりに足す。
        Spread(painted, seeds.Count);
        for (int c = 0; c < n; c++)
            if (ownFinal[c] < 0 && owner[c] >= painted && (drawn is null || drawn[c] != 0)) ownFinal[c] = owner[c];
        hit = ownFinal;

        var bmp = new WriteableBitmap(new PixelSize(Nx, Ny), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var fb = bmp.Lock())
        {
            int rowBytes = Nx * 4;
            for (int iy = 0; iy < Ny; iy++)   // 行ごと＝確保側の行幅（RowBytes）が広い場合でもずれない
                System.Runtime.InteropServices.Marshal.Copy(px, iy * rowBytes, fb.Address + iy * fb.RowBytes, rowBytes);
        }
        return bmp;
    }

    /// <summary>緯度経度→表示格子の番号。表示の緯度帯の外（極域）は -1。</summary>
    private static int CellOf(double lat, double lon)
    {
        if (!(lat < MapData.LatTop && lat > MapData.LatBottom)) return -1;   // 否定形＝NaNは外
        if (!double.IsFinite(lon)) return -1;
        int ix = (int)Math.Floor((lon - MapData.LonLeft) / CellDeg) % Nx;
        if (ix < 0) ix += Nx;
        int iy = (int)((MapData.LatTop - lat) / CellDeg);
        if (iy < 0 || iy >= Ny) return -1;
        return iy * Nx + ix;
    }

    /// <summary>★F7（2026-09-28）：州の当たり＝キャンバス座標（幅 MapWidth・高さ MapHeight の塗りの絵の上の点）→その画素の持ち主の種の番号（-1＝無し）。
    /// 塗られていない格子（海岸の町の座標・小島＝0.1°格子で海へ丸められた所）は、**種を陸へ吸着したのと同じ半径**で最寄りの持ち主を借りる
    /// ＝種の吸着と当たりが同じ規則（海岸の州名の上でも、その州に当たる）。</summary>
    public static int HitAt(short[] hit, double x, double y)
    {
        x %= MapData.MapWidth; if (x < 0) x += MapData.MapWidth;
        int ix = (int)(x * Nx / MapData.MapWidth), iy = (int)(y * Ny / MapData.MapHeight);
        if (!(ix >= 0 && ix < Nx && iy >= 0 && iy < Ny) || hit.Length != Nx * Ny) return -1;   // 否定形＝NaN も外
        int c = iy * Nx + ix;
        if (hit[c] < 0) { c = Nearest(c, c3 => hit[c3] >= 0); if (c < 0) return -1; }
        return hit[c];
    }

    /// <summary>近傍で条件を満たす最寄りの格子（半径5格子までの環）。種の陸への吸着（0.1°格子は海峡や小島を海へ丸める）と、州の当たりの両方が使う。
    /// 見つからなければ -1＝その種は捨てる／当たりは無し。</summary>
    private static int Nearest(int c, Func<int, bool> ok)
    {
        int iy0 = c / Nx, ix0 = c - iy0 * Nx;
        for (int r = 1; r <= 5; r++)
            for (int dy = -r; dy <= r; dy++)
            {
                int iy = iy0 + dy;
                if (iy < 0 || iy >= Ny) continue;
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Abs(dy) != r && Math.Abs(dx) != r) continue;   // 環の上だけ見る（内側は前の r で見た）
                    int ix = (ix0 + dx) % Nx;
                    if (ix < 0) ix += Nx;
                    int c2 = iy * Nx + ix;
                    if (ok(c2)) return c2;
                }
            }
        return -1;
    }
}
