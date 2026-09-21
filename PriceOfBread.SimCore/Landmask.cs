using System;
using System.IO;

namespace PriceOfBread.SimCore;

/// <summary>陸か海かの**真値**（2026-08-02）。地図の背景に使っている世界地図SVGを 0.1°格子へ焼いたビットマップ。
///
/// ── なぜ要るか（点群で陸を近似する方式が破れた）──────────────────────────────
/// 旧 <c>SeaGraph.CrossesLand</c> は「内陸の州がどれだけ近いか」「沿岸の州がどれだけ近いか」「海ノードがどれだけ近いか」
/// の3つを比べて陸かどうかを当てていた。**州の点は陸の形ではない**ので、州が疎な地域と、
/// 沿岸州が半島の内側に立つ地域の両方で破れる。実測（2026-08-02の多エージェント監査＋自前の通路迂回検査）：
///   ・**パナマ運河が1783年から実質開通していた**。カリブ西↔中米沖など中米地峡を横切る辺が3本あり、
///     ニューヨーク→サンフランシスコは運河の開閉によらず約11,000km（実際のホーン岬回りは約22,000km）。
///   ・**ジブラルタルを閉じても地中海へ入れた**。リスボン沖↔アルボラン海(719km)がアンダルシアを陸上横断していて、
///     ジブラルタル通路は最短路に載ってすらいなかった（リスボン沖→ティレニア海は通路の有無に関わらず2,451km）。
///   ・同型の「通路バイパス辺」がボスポラス・マラッカ・ズンド・喜望峰にも計12本。健全なのはスエズだけだった。
/// 破れ方は地域ごとに違う（マレー半島・中米・スカンジナビア内陸・南アフリカは内陸州が420km圏に無く、
///   イベリア・アナトリアは沿岸州が「海の証拠」に数えられていた）＝**規則の調整では塞げない**。
///
/// ── 何を真値にしたか ──────────────────────────────────────────────────────
/// 画面に映っている地図そのもの（<c>PriceOfBread.App/Assets/worldmap.svg</c>・2,291個の国境ポリゴン）。
/// 見えている陸と判定に使う陸が同じものになる＝作者が絵で見つけた誤りと、エンジンが測る誤りが一致する。
/// 格子は 0.1°（赤道で約11km）×3600×1800＝791KB。海峡（ジブラルタル14km・ボスポラス1km・ズンド4km）は
/// この粗さでも陸に潰れるが、**それでよい**——海峡は passages.json の手置き通路が担い、陸判定を通らない。
/// むしろ潰れてくれるおかげで「海峡を通らずに海峡を越える」辺が張れなくなる。
/// ★**海岸線を格子1つぶん内側へ収縮させてある**（4近傍のどれかが海なら陸としない）。
///   ⚠0.25°（28km）で収縮なしだと過剰棄却になり、海グラフの辺 262→187・連結成分 1→15 まで砕けた
///     （北海がユーラシアから切れ、ベンガル湾とマラッカが孤立し、アドリア海も孤立した）。
///   格子は必ず海岸線を外側へ丸めるので、その丸めぶんを戻すのが収縮＝**新しい調整値ではなく格子の粗さの補正**。
///   収縮1セル(11km)ならパナマ地峡(約50km)は残り、沿岸をかすめる正しい航路は通る。
///
/// ⚠決定論：ビットマップは埋め込み資源＝どの環境でも同一。実行時に何も計算しない。
/// ⚠再生成：<c>docs/20260802</c> の手順（SVGのパスを平坦化してスキャンライン塗り・全図を覆う背景パスは除外）。
///   42点の既知座標（ロンドン/モスクワ/スエズ地峡/マレー半島中央〜地中海/黒海/シャム湾/中米沖）で全一致を確認済み。
/// </summary>
public static class Landmask
{
    public const double Step = 0.1;
    public const int Nx = 3600, Ny = 1800;
    private static readonly byte[] Bits = Load();
    // ★陸路用（2026-09-05・作者指摘「陸上取引が内海を無視」の是正で追加）：同じSVGを any-coverage・収縮なしで焼いたビットマップ。
    //   海路用の収縮は「沿岸をかすめる航路を通す」ための補正で、陸路の判定に使うと幅1〜2格子の地峡（諫早・クラ）が消えて半島が島になる。
    //   陸の辺（PathGraph.CrossesWater）だけが読む。海路の判定（IsLand）は従来のまま＝海グラフはビット恒等。
    private static readonly byte[] RoadBits = LoadNamed("landmask_land.bin");

    private static byte[] Load() => LoadNamed("landmask.bin");

    private static byte[] LoadNamed(string file)
    {
        var asm = typeof(Landmask).Assembly;
        string? name = Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith(file, StringComparison.Ordinal) && !(file == "landmask.bin" && n.EndsWith("landmask_land.bin", StringComparison.Ordinal)));
        if (name is null) throw new InvalidOperationException($"{file} が埋め込まれていない。SimCore.csproj の EmbeddedResource を確認");
        using var st = asm.GetManifestResourceStream(name)!;
        using var ms = new MemoryStream();
        st.CopyTo(ms);
        var b = ms.ToArray();
        int need = (Nx * Ny + 7) / 8;
        if (b.Length != need) throw new InvalidOperationException($"{file} の大きさが合わない（{b.Length} ≠ {need}）");
        return b;
    }

    /// <summary>その座標が陸か。⚠緯度が範囲外（極域）は海として扱う（航行できないので経路に現れない）。</summary>
    public static bool IsLand(double lat, double lon) => Test(Bits, lat, lon);

    /// <summary>陸路用の陸判定（any-coverage・収縮なし）。陸の辺の水判定（PathGraph）だけが読む。</summary>
    public static bool IsLandForRoads(double lat, double lon) => Test(RoadBits, lat, lon);

    private static bool Test(byte[] bits, double lat, double lon)
    {
        // ⚠否定形＝NaN は陸として扱わない（辺を消さない側＝既存の辺を静かに消さない）
        if (!(lat > -90.0 && lat < 90.0)) return false;
        if (!double.IsFinite(lon)) return false;
        int yi = (int)((90.0 - lat) / Step);
        if (yi < 0 || yi >= Ny) return false;
        double x = (lon + 180.0) / Step;
        int xi = (int)Math.Floor(x) % Nx;
        if (xi < 0) xi += Nx;
        int idx = yi * Nx + xi;
        return (bits[idx >> 3] & (1 << (idx & 7))) != 0;
    }
}
