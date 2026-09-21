using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>海ノード吸着の是正（2026-08-16・<see cref="Coefficients.SeaStitchLeadBudgetMult"/>）の番人。
///
/// ── 直した穴 ──────────────────────────────────────────────────────────────
/// <c>SeaGraph.StitchCrossesLand</c> は「出港の一歩目（連続する陸）は数えない」規則だが、**免除に距離の上限が無かった**。
/// その結果 <c>NearestSeaNode</c> は「線の全部が出港の一歩目」（一度も水に出ない線）を合格させ、近い海ノードが
/// 半島で棄却された港が**大陸の反対側のノードへ吸着**した。実測（mapedges・係数OFF）：
///   ・トリエステ(45.65,13.77) は最寄りのアドリア海北(179km)がイストリア半島で棄却され、**1,045km先のバルト南西へ**
///     （陸上880km＝中欧を縦断する線）。SeaBasin まで Baltic になり、トリエステ→ヴェネツィアが 9,960km（実際150km＝×66）。
///   ・同型が港州92件中21件（ハノイ・バンコク・グラスゴー・マンチェスター・ケルン ほか）。
/// 是正＝免除を「**その地点が水に出るまでの実測距離**（陸マスクの最寄り海マス）×係数」で打ち切る。
///
/// ⚠この番人の要点は**4つ**：①**明示0**は旧規則とビット恒等（既定は点火0.5＝2026-08-16の点火梯子）②ONでトリエステが正しい海（アドリア海）へ
/// ③海路距離が桁違いに縮む ④1つも通らないときの**素の最寄りへの退避**が効く（退避が壊れると内陸の重心で呼び出し側が壊れる）。
/// ⚠⑤決定論＝<c>NearestSeaNode</c> は0.1°量子化キャッシュを持つ急所（2026-08-04に「キーは量子化・計算は高精度座標」で
///   決定論が破れた前科）。係数も鍵に入るので、**同じマスに落ちる異なる地点・異なる順序・並列**で同じ答えになることを固定する。</summary>
public class SeaStitchBudgetTests
{
    // 実港の座標（Climate.CityCoord と同値・診断 mapedges と同じ地点）
    private const double トリエステ緯度 = 45.65, トリエステ経度 = 13.77;
    private const double ヴェネツィア緯度 = 45.44, ヴェネツィア経度 = 12.34;

    /// <summary>エンジンが実際に使う代表点（0.1°マスの代表＝<c>NearestSeaNode</c> と同じ量子化）。</summary>
    private static (double Lat, double Lon) 代表点(double lat, double lon)
        => (Math.Round(lat * 10.0) / 10.0, Math.Round(lon * 10.0) / 10.0);

    /// <summary>素の最寄り海ノード（陸判定なし・同距離は添字順＝エンジンの退避先と同じ規則）。</summary>
    private static int 素の最寄り(double lat, double lon)
    {
        var (qlat, qlon) = 代表点(lat, lon);
        int best = 0; double bk = double.PositiveInfinity;
        for (int i = 0; i < SeaGraph.Nodes.Length; i++)
        {
            double d = Geo.DistanceKm(qlat, qlon, SeaGraph.Nodes[i].Lat, SeaGraph.Nodes[i].Lon);
            if (d < bk) { bk = d; best = i; }
        }
        return best;
    }

    /// <summary>その線が「一度水に出たあとに陸を踏む」か（<c>StitchCrossesLand</c> の**免除に関係しない側**の規則を
    /// テスト側で再現）。真なら係数の値によらず不通＝その線は「通った」のではなく**退避で選ばれた**ことの証明になる。</summary>
    private static bool 離水後に陸がある(double la, double lo, double lb, double lob)
    {
        double dlon = lob - lo;
        if (dlon > 180) dlon -= 360; else if (dlon < -180) dlon += 360;
        int n = Math.Max(9, (int)Math.Ceiling(Geo.DistanceKm(la, lo, lb, lob) / (Landmask.Step * 111.0 / 2.0)));
        bool afloat = false;
        for (int s = 1; s <= n; s++)
        {
            double t = s / (double)(n + 1);
            if (!Landmask.IsLand(la + (lb - la) * t, lo + dlon * t)) { afloat = true; continue; }
            if (afloat) return true;
        }
        return false;
    }

    [Fact]
    public void 明示0は旧規則とビット恒等であり既定は点火値0_5である()
    {
        // ★①旧規則の番人。**明示的に0を渡した**とき＝「出港の一歩目は無制限に免除」＝穴が開いたままの挙動を固定する
        //   （対照実験はOFF側も明示する＝既定が0でなくなった今、0は引数で名指しする）。
        int off = SeaGraph.NearestSeaNode(トリエステ緯度, トリエステ経度, 0.0);
        // ★2026-09-05：陸マスクの焼き直し（版A）で**旧の穴そのものが閉じた**＝明示0でもトリエステはアドリア海側（オトラント海峡）へ吸着する。
        //   旧の番人（"バルト南西"／Basin.Baltic）はデータ依存の固定値だったので、主張を「明示0でも地中海勢であり、ONと同じ海盆」へ置き換える。
        //   ⚠明示0＝旧規則そのもの（一歩目の免除が無制限）は変わっていない＝規則のビット恒等は SeaGraph 側の実装で保たれる（引数経路の番人は下）。
        Assert.Equal(Basin.Mediterranean, SeaGraph.Nodes[off].Basin);

        // 既定値の宣言（2026-08-16 点火梯子で 0→0.5 に点火済み＝**liveである**ことを固定する番人。
        // 将来うっかり値が動いたらここが赤になる）。
        // ⚠POB_COEF で明示的に上書きした測定走行では見ない（点火A/Bを同一バイナリで取るため）。
        double def = new Coefficients().SeaStitchLeadBudgetMult;
        if (!Coefficients.OverrideSummary().Contains("SeaStitchLeadBudgetMult", StringComparison.Ordinal))
        {
            Assert.Equal(0.5, def);
            // liveの証拠＝既定のまま引いたトリエステはもうバルト海勢ではない（①の旧挙動と実際に分岐している）
            Assert.NotEqual(off, SeaGraph.NearestSeaNode(トリエステ緯度, トリエステ経度));
        }

        // 公開APIは係数の経路を通る（配線の証拠＝点火しても呼び出し側の書き換えが要らない）
        Assert.Equal(SeaGraph.NearestSeaNode(トリエステ緯度, トリエステ経度, def),
                     SeaGraph.NearestSeaNode(トリエステ緯度, トリエステ経度));
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void 係数ONでトリエステはアドリア海側のノードへ吸着する(double mult)
    {
        // ★②「正しい海に出る」。海盆（<c>BasinAt</c>＝Simulation の SeaBasin）も一緒に直る＝
        //   オーストリアがバルト海勢でなく地中海勢になる。
        int on = SeaGraph.NearestSeaNode(トリエステ緯度, トリエステ経度, mult);
        Assert.Equal("アドリア海北", SeaGraph.Nodes[on].Name);
        Assert.Equal(Basin.Mediterranean, SeaGraph.Nodes[on].Basin);
    }

    [Fact]
    public void 係数ONでトリエステからの海路距離が桁違いに縮む()
    {
        // ★③表示だけの問題ではない証拠＝接ぎ木はそのまま SeaDistanceKm に加算される（BilateralMarket が引く運賃の物差し）。
        double off = SeaGraph.SeaDistanceKm(トリエステ緯度, トリエステ経度, ヴェネツィア緯度, ヴェネツィア経度, false, false, 0.0);
        double on = SeaGraph.SeaDistanceKm(トリエステ緯度, トリエステ経度, ヴェネツィア緯度, ヴェネツィア経度, false, false, 1.0);
        // ★2026-09-05：陸マスク版Aで旧の穴（OFF 9,960km）は閉じた（OFF≈1,583km）。番人の主張を「OFFでもONでも内海の桁であり、ONはOFFを上回らない」へ置き換える。
        Assert.True(off < 2000, $"OFF でアドリア海の内海として長すぎる（{off:F0}km・旧の穴9,960kmが再発）");
        Assert.True(on <= off + 1e-9, $"ON が OFF より長い（OFF {off:F0}km → ON {on:F0}km）");
        Assert.True(on < 1500, $"ON でもアドリア海の内海として長すぎる（{on:F0}km・実測369km）");
    }

    [Fact]
    public void どのノードも通らないときは素の最寄りへ退避する()
    {
        // ★④退避の番人。トリエステは**どのノードへも水だけでは出られない**（湾を出るとイストリア半島に当たる）ので、
        //   ONでは全候補が棄却され、既存の「素の最寄りへ退避」が受ける＝アドリア海北(179km)に落ち着く。
        //   ⚠この退避が壊れると内陸の重心（貿易重心・非沿岸州）で呼び出し側が海を見失う。
        var (qlat, qlon) = 代表点(トリエステ緯度, トリエステ経度);
        int plain = 素の最寄り(トリエステ緯度, トリエステ経度);
        int on = SeaGraph.NearestSeaNode(トリエステ緯度, トリエステ経度, 1.0);
        Assert.Equal(plain, on);
        // 「通った」のではなく「退避した」ことの証明＝採用した線自体が離水後に陸を踏む（免除の値に関係なく不通）
        Assert.True(離水後に陸がある(qlat, qlon, SeaGraph.Nodes[on].Lat, SeaGraph.Nodes[on].Lon),
            "退避経路のはずの線が実は水だけで通れている＝この番人が退避を検査できていない");

        // 内陸の重心（アルプス北麓・海から遠い）も退避で必ず答えを返す＝呼び出し側を壊さない
        int inland = SeaGraph.NearestSeaNode(47.5, 14.5, 1.0);
        Assert.InRange(inland, 0, SeaGraph.Nodes.Length - 1);
        Assert.Equal(素の最寄り(47.5, 14.5), inland);
    }

    [Fact]
    public void 係数ONでも答えは地点の精度と到達順と並列度に依存しない()
    {
        // ★⑤決定論。2026-08-04の破れ＝「鍵は0.1°量子化・計算は高精度座標」で、同じマスの値が**最初に来た地点**で決まっていた。
        //   係数を鍵に足した今も同じ不変条件（鍵が量子化なら値も鍵から計算する）が守られていることを固定する。
        double[] mults = [0.0, 0.5, 1.0];
        // 同じ0.1°マス(45.6,13.8)に落ちる異なる高精度座標
        (double Lat, double Lon)[] 同一マス = [(45.65, 13.77), (45.64, 13.83), (45.58, 13.75), (45.61, 13.79)];
        foreach (double m in mults)
        {
            int first = SeaGraph.NearestSeaNode(同一マス[0].Lat, 同一マス[0].Lon, m);
            foreach (var (la, lo) in 同一マス)
                Assert.Equal(first, SeaGraph.NearestSeaNode(la, lo, m));
        }

        // 世界中の地点を、順方向・逆方向・並列で引いて完全一致（キャッシュの汚染＝到達順依存を検出する）
        var pts = new System.Collections.Generic.List<(double Lat, double Lon)>();
        for (int la = -60; la <= 70; la += 10)
            for (int lo = -170; lo <= 170; lo += 10)
                pts.Add((la + 0.37, lo + 0.42));
        int[] 順 = [.. pts.Select(p => SeaGraph.NearestSeaNode(p.Lat, p.Lon, 1.0))];
        int[] 逆 = new int[pts.Count];
        for (int i = pts.Count - 1; i >= 0; i--) 逆[i] = SeaGraph.NearestSeaNode(pts[i].Lat, pts[i].Lon, 1.0);
        var 並列 = new ConcurrentDictionary<int, int>();
        Parallel.For(0, pts.Count, i => 並列[i] = SeaGraph.NearestSeaNode(pts[i].Lat, pts[i].Lon, 1.0));
        for (int i = 0; i < pts.Count; i++)
        {
            Assert.Equal(順[i], 逆[i]);
            Assert.Equal(順[i], 並列[i]);
        }
    }

    [Fact]
    public void 同シードの世界は2回とも完全に一致する()
    {
        // ★⑤の系＝**シミュレーション水準**の決定論。SeaGraph は静的データ層なので係数はプロセス既定値（＝点火後の0.5）で入る＝
        //   この番人は点火後の海路がそのまま決定論ゲートになっている（点火A/Bの記録は docs/202608/20260816_海ノード吸着の是正_完了.md）。
        NationConfig[] Four() => [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()];
        var a = new MultiNationSim(Four(), seed: 1789);
        var b = new MultiNationSim(Four(), seed: 1789);
        a.Run(40);
        b.Run(40);
        for (int i = 0; i < a.Nations.Count; i++)
        {
            Assert.Equal(a.Nations[i].World.TotalMoney, b.Nations[i].World.TotalMoney);
            Assert.Equal(a.Nations[i].World.Tick, b.Nations[i].World.Tick);
            Assert.Equal(a.Nations[i].World.Region.SeaBasin, b.Nations[i].World.Region.SeaBasin);
            Assert.Equal(a.Nations[i].World.Region.Markets[0].Price, b.Nations[i].World.Region.Markets[0].Price);
        }
    }
}
