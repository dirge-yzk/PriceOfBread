using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>03地理 手順3（2026-07-28）：海グラフの番人。旧SeaDistanceKmの系統誤差（欧州域内×0.23＝仏直線貫通・アフリカ横断×0.38）の再発防止と、
/// 通路ゲートの構造保証（未開通の運河＝辺が存在しない・迂回kNN辺の穴なし）を固定する。</summary>
public class SeaGraphTests
{
    private static void AssertBand(double km, double actualKm, string label)
    {
        double r = km / actualKm;
        Assert.True(r is >= 0.6 and <= 1.7, $"{label}: 許容帯外 ×{r:F2}（グラフ{km:F0}km/実際{actualKm}km）");
    }

    [Fact]
    public void 実測9経路が許容帯に収まる()
    {
        AssertBand(SeaGraph.SeaDistanceKm(53.55, 9.99, 43.3, 5.37, false, false), 5100, "ハンブルク→マルセイユ");
        AssertBand(SeaGraph.SeaDistanceKm(51.51, -0.13, 40.85, 14.27, false, false), 5300, "ロンドン→ナポリ");
        AssertBand(SeaGraph.SeaDistanceKm(59.33, 18.07, 40.85, 14.27, false, false), 6800, "ストックホルム→ナポリ");
        AssertBand(SeaGraph.SeaDistanceKm(6.45, 3.4, -6.2, 39.2, false, false), 11000, "ラゴス→ザンジバル");
        AssertBand(SeaGraph.SeaDistanceKm(51.51, -0.13, 19.0, 72.8, false, false), 18000, "ロンドン→ボンベイ（喜望峰）");
        AssertBand(SeaGraph.SeaDistanceKm(-22.9, -43.2, -12.0, -77.0, false, false), 7000, "リオ→リマ（ホーン岬）");
    }

    [Fact]
    public void ペルシャ湾の辺は生きている()
    {
        // ★経路グラフ第1段 Stage A（2026-08-09）：ブーシェフルを実港として追加し、シーラーズ（ブーシェフル代理＝
        //   2026-07-28手順3の暫定で landmass 側 coastal=true だった）を内陸真値（coastal=false）へ戻した。
        // ⚠この番人の趣旨＝シーラーズが内陸ノードへ戻っても、ペルシャ湾内の海辺（ペルシャ湾央↔ホルムズ海峡）が
        //   死なないこと。陸横断判定は Landmask（地図SVG焼き格子）が正なので理屈上は不変だが、湾はノードが疎で
        //   救済頼みの海域＝データ変更の巻き添えが出やすい。帯外や到達不能（大圏×2退避＝×1.78で帯外に出る）なら赤。
        AssertBand(SeaGraph.SeaDistanceKm(28.97, 50.84, 19.0, 72.8, false, false), 2700, "ブーシェフル→ボンベイ（ホルムズ海峡経由）");
        var path = SeaGraph.RoutePath(28.97, 50.84, 19.0, 72.8, false, false);
        Assert.Contains("ホルムズ海峡", path);   // 湾の出口は海峡＝経路の形も固定（湾内の辺が死ぬと空になる）
    }

    [Fact]
    public void マンガロールのアラビア海接続は生きている()
    {
        // ★経路グラフ第1段 Stage B（§13州再配分・2026-08-09）：マンガロール（1783年ティプー・スルターン領の実港）を
        //   追加し、マイソールが港州ゼロ23カ国から抜けた。ブーシェフル番人と同型＝海グラフの巻き添えでこの港の
        //   アラビア海接続が死んでいないことを固定する（帯外や大圏×2退避＝×1.78で帯外に出るなら赤）。
        // ⚠経路の形も固定：モカ（紅海口）への航路はアデン湾を通るはず（バブ・エル・マンデブへの唯一の玄関）。
        AssertBand(SeaGraph.SeaDistanceKm(12.87, 74.84, 13.32, 43.25, false, false), 3600, "マンガロール→モカ（アラビア海横断）");
        var path = SeaGraph.RoutePath(12.87, 74.84, 13.32, 43.25, false, false);
        Assert.Contains("アデン湾", path);
    }

    [Fact]
    public void スエズ開通の短絡が実史規模で効く()
    {
        double closed = SeaGraph.SeaDistanceKm(51.51, -0.13, 19.0, 72.8, false, false);
        double open = SeaGraph.SeaDistanceKm(51.51, -0.13, 19.0, 72.8, true, false);
        Assert.True(closed - open > 6000, $"スエズ短絡が小さすぎる: {closed - open:F0}km（実史≈8,000km）");
        // 閉鎖時の経路がスエズ湾を通らない（クレタ→スエズ湾のkNN迂回辺＝エジプト陸横断の穴の再発防止）
        var path = SeaGraph.RoutePath(51.51, -0.13, 19.0, 72.8, false, false);
        Assert.DoesNotContain("スエズ湾", path);
    }

    [Fact]
    public void 海グラフは全海が1成分に連結し辺数が固定される()
    {
        var (comps, largest) = SeaGraph.Connectivity();
        // ★2026-08-14 D1：カスピ海ノード2個（バクー沖・カスピ北）を追加＝**意図的な第2成分**。
        //   カスピ海は外洋への出口を持たない閉鎖海（ヴォルガ・ドン運河は1952年）＝どこにも繋がないのが史実として正しい。
        //   宣言＝成分は2・大成分は「全ノード−カスピ海ノード数」ちょうど。これでカスピ以外のどの海が分裂しても検出は落ちない。
        int caspian = 0;
        foreach (var nd in SeaGraph.Nodes) if (nd.Basin == Basin.Caspian) caspian++;
        Assert.Equal(2, caspian);
        Assert.Equal(2, comps);
        Assert.Equal(SeaGraph.Nodes.Length - caspian, largest);
        // 決定論：同じ入力から同じ辺数（データ変更時はこの数字を更新＝差分が意図的であることの宣言）
        // ★2026-09-05：222→229。陸マスクを App の描画器で焼き直し（版A＝スーダン〜ヒジャーズの穴を修正・多数決・収縮1格子）＝海の辺+7
        //   （ニューヨーク沖↔バミューダ沖・カリブ内・オトラント等）。成分2/最大175・通路の迂回比（ジブラルタル×60/マラッカ×59/パナマ×47）は不変。
        Assert.Equal(229, SeaGraph.Edges.Length);   // 2026-07-29: §13バッチ3/4＋黄海北ノード（朝鮮内陸州が半島横断偽辺を正しく棄却→山東沖で再接続）で340→342。2026-08-14 D1：カスピ2ノード＝湖内1辺で221→222（実測確定）
        // ★2026-08-02：K=4→3 で 342→262（作者指摘「ウェイポイント同士が網を張り、不必要に経路を複雑化している」）。
        //   k=2 は辺183・**成分5に分裂**＝ラゴス→ザンジバル／広州→長崎／ロンドン→ボンベイが到達不能なので k=3 が下限。
        // ★同日さらに 262→213：陸判定を**真値**（Landmask＝地図の背景SVGを焼いた0.1°格子）へ差し替えた。
        //   旧は州の点群で陸を近似しており、**通路を通らずに通路を越える辺が12本**あった：
        //     ・パナマ運河が1783年から実質開通（NY→SFが運河の開閉によらず約11,000km／実際のホーン岬回りは約22,000km）
        //     ・ジブラルタルを閉じても地中海へ入れた（リスボン沖↔アルボラン海719kmがアンダルシアを陸上横断）
        //     ・ボスポラス（アナトリア横断）・マラッカ（クラ地峡）・ズンド（スウェーデン横断）・喜望峰も同様
        //   健全だったのはスエズだけ。是正後は**10通路すべてが唯一の道**（迂回は∞または地球一周の×55〜105）。
        //   代表9経路も全部が許容帯 ×0.6〜1.7 に入った（ハンブルク→マルセイユ ×0.47→×1.24＝欧州を陸路で
        //   突っ切っていたのが直った・ロンドン→ケープタウン ×1.76→×1.16・ロンドン→ボンベイ ×0.80→×1.21）。
        // ⚠ノードは 158→169：真値が**海ノード自身の配置ミス**も暴いた（白海口はコラ半島の上、英仏海峡西は
        //   地図上のコーンウォールの中、マラッカ南口とフォークランド沖も陸の上）＝4個を水へ移し、
        //   海峡の中継を11個足した（スカゲン沖・オトラント海峡・ホルムズ海峡・スンダ海峡口・カリマタ海峡・
        //   イギリス海峡東/中央・白海口外・マルタ沖・セイロン東方・バハマ北西）。通路にイギリス海峡を追加。
    }
}
