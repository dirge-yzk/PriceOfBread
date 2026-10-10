using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>経路グラフ第1段 Stage C（03 §11-6・裁定A3・2026-08-09）：内陸国トランジットの番人。
/// 休眠恒等（TransitEnabled=0 既定）は既存フルスイートが番人（新コードは全て TransitEnabled>0.5 ゲートの内側）。
/// ここでは (1)構築時静的の配線 (2)点火時の距離置換 (3)通行料の保存移転 (4)交戦時の迂回→フォールバック を固定する。</summary>
public class TransitTests
{
    // ── 合成ON世界：スイス（首都だけ地球の裏側へ＝首都基準フォールバックとの対照を最大化）＋ジェノヴァ＋トスカーナ。
    //    ベルンの候補は実世界と同じ 1=ジェノヴァ・2=リヴォルノ（所有国が違う次点＝戦時の迂回先）になる。
    private static NationConfig FarSwiss()
    {
        var c = WorldData.ByName("スイス").ShallowClone();
        c.Latitude = -44.0; c.Longitude = -171.0;   // 州（ベルン）は実座標のまま＝配線・3脚は実地理。首都基準の物差しだけ遠くなる
        return c;
    }
    private static NationConfig[] World3() => [FarSwiss(), WorldData.ByName("ジェノヴァ"), WorldData.ByName("トスカーナ")];

    [Fact]
    public void 係数の既定は点火()
    {
        // ★2026-08-11に点火（0.0→1.0）。二国間交易への置換で「世界の重心までのフォールバック」が消えたため、
        //   この機構が無いと**港を持たない内陸国は対外交易がゼロ**になる（＝鎖国）。
        //   旧・却下理由（2026-08-09＝内陸国の高コスト化にAI財政が適応できず損失financing 6,965%）は解消
        //   （二国間の実測バッチで硬性ゲート緑・フラグ0）。0で通年フォールバック無しの非接続へ戻る。
        var c = new Coefficients();
        Assert.Equal(1.0, c.TransitEnabled);
        Assert.Equal(0.05, c.TransitTollShare);   // 通行料率＝取扱高の5%（港所有国の国庫へ）
    }

    [Fact]
    public void 港州ゼロ国18カ国の全州に外国港トランジット候補が配線される()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL);
        int portless = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            bool has = false;
            foreach (var r in w.Regions)
                if (r.IsPort) { var pc = Climate.CityCoord(r.Name); if (pc.Lat != 0 || pc.Lon != 0) { has = true; break; } }
            if (has)
            {
                foreach (var r in w.Regions) Assert.Equal("", r.TransitPortName);   // 港のある国は自国港3脚が担う＝候補を書かない
                continue;
            }
            portless++;
            foreach (var r in w.Regions)
            {
                var cc = Climate.CityCoord(r.Name);
                if (cc.Lat == 0 && cc.Lon == 0) continue;   // 座標なし州＝旧フォールバックのまま
                Assert.True(r.TransitPortName.Length > 0, $"{w.SelfPower!.Name}の{r.Name}にトランジット候補がない");
                Assert.True(r.TransitLandKm > 0 && r.TransitLandKm < 5000, $"{w.SelfPower!.Name}の{r.Name}の陸路距離が異常（{r.TransitLandKm:F0}km）");
                if (r.TransitPort2Name.Length > 0)
                {
                    Assert.NotEqual(r.TransitPortName, r.TransitPort2Name);
                    Assert.True(r.TransitLand2Km >= r.TransitLandKm, $"{r.Name}の第2候補が第1候補より近い");
                }
            }
        }
        // Stage A/B 後の真の内陸国＝18カ国（マイソールはマンガロール追加で離脱済み）。国データ変更時はこの数字を更新＝差分が意図的であることの宣言
        // ★2026-08-13：18→20。アフリカ5カ国の実国化で**ソコト**と**ボルヌ帝国**が完全内陸国として加わった
        //   （コンゴ王国＝ボマ／アルジェ摂政国＝アルジェ・オラン／オヨ帝国＝バダグリ が沿岸なので、この3国は内陸に数えない）。
        // ★2026-08-16：20→17。地理データ実装（棚卸キューA群）＝**アラビア**（カティーフ）・**エチオピア**（マサワ）・
        //   **ハノーファー**（クックスハーフェン・E2裁定=与える）が実港州を得て港州ゼロ国から離脱＝自国港3脚へ移行（トランジット候補は書かれない）。
        // ★(227)：17→18。マサワはオスマンの海岸州（1557〜・ナイブ＝アルキコの太守が統治）で1783 のエチオピア領でない＝エチオピアはまた内陸国（港州ゼロ）。
        //   2026-08-16 の『エチオピア（マサワ）が実港州を得て離脱』を史実（マサワ＝オスマンの紅海岸）へ差し戻した（measurements/pop227_20261010・修正一覧 C3）。
        Assert.Equal(18, portless);
        // 代表例のピン（2026-08-09の陸グラフでの実測値）：スイス＝ジェノヴァ332km/リヴォルノ443km・ネパール＝バローダ
        var bern = sim.Nations.First(n => n.World.SelfPower!.Name == "スイス").World.Regions[0];
        Assert.Equal("ジェノヴァ", bern.TransitPortName);
        // ★2026-09-06：リヴォルノ→ケルン。バリアの壁を線分にした（BarrierWallSegments=1）ので、ベルン→ミラノはゴッタルド峠経由（×1.5）となり
        //   リヴォルノが遠のき、ライン川の港ケルンが次点になる（史実のスイスの主要出口＝ライン→ロッテルダム）。1位ジェノヴァは不変。
        Assert.Equal("ケルン", bern.TransitPort2Name);
        var kathmandu = sim.Nations.First(n => n.World.SelfPower!.Name == "ネパール").World.Regions[0];
        // ★2026-08-14：バローダ→バサイン。R2でマラータに**実港バサイン**（コンカン海岸）を足し、
        //   内陸のバローダに立っていた coastal=true の嘘を解いた＝ネパールの外国港トランジット先が実港へ移った。
        Assert.Equal("バサイン", kathmandu.TransitPortName);
    }

    [Fact]
    public void 点火で内陸国州の距離が首都基準フォールバックより改善する()
    {
        var off = new MultiNationSim(World3(), seed: 7) { DiplomacyFrozen = true };
        var on = new MultiNationSim(World3(), seed: 7) { DiplomacyFrozen = true };
        off.Coef.TransitEnabled = 0.0;   // OFF側を明示＝将来の点火で対照が無言で壊れない（2026-08-09の空洞化3件と同型の予防）
        on.Coef.TransitEnabled = 1.0;
        // ⚠2026-08-28：陸路直行（LandRouteEnabled=1 が既定）で内陸州は**陸路の経路を持つ**ようになった＝
        //   このテストの前提「港に繋がらない内陸州は経路を持たない（距離0）」が設計どおり崩れた。
        //   検証対象は**トランジット機構**（隣国の港を借りる3脚）なので、陸路を落として旧前提の世界で検証する（制度テストと同じ環境固定の作法）。
        off.Coef.LandRouteEnabled = 0.0;
        on.Coef.LandRouteEnabled = 0.0;
        off.Run(1); on.Run(1);
        var rOff = off.Nations[0].World.Regions[0];   // ベルン
        var rOn = on.Nations[0].World.Regions[0];
        Assert.Equal("", rOff.LastTransitPortName);                 // 休眠＝トランジット不使用（旧フォールバック）
        Assert.Equal("ジェノヴァ", rOn.LastTransitPortName);        // 点火＝第1候補経由
        // ★二国間交易への置換（2026-08-11）で判定を取り直した。
        //   旧＝世界の重心までの「首都基準フォールバック」があり、港の無い内陸州もそれで交易できていたので
        //       「トランジットの3脚 < 首都基準」を比べていた。重心を廃した今、比べる相手が存在しない。
        //   新＝港に繋がらない内陸州は**そもそも海上交易の経路を持たない**（距離0＝未接続）。
        //       トランジットは「経路が無い」を「隣国の港を借りた実経路」に変える機構＝これが本来の意味。
        Assert.Equal(0.0, rOff.LastTradeDistanceKm);                // 休眠＝経路なし
        Assert.True(rOn.LastTradeDistanceKm > 0, "点火してもトランジットの経路が立たない");
        // 3脚の下限＝陸脚×LandSeaCostRatio（同式であることの下限チェック）
        Assert.True(rOn.LastTradeDistanceKm >= rOn.TransitLandKm * on.Coef.LandSeaCostRatio,
            $"3脚（{rOn.LastTradeDistanceKm:F0}km）が陸脚×{on.Coef.LandSeaCostRatio}（{rOn.TransitLandKm * on.Coef.LandSeaCostRatio:F0}km）を下回る");
    }

    [Fact]
    public void 通行料は利用国国庫から港所有国国庫へ移転し保存する()
    {
        var sim = new MultiNationSim(World3(), seed: 11) { DiplomacyFrozen = true };
        sim.Coef.TransitEnabled = 1.0;
        // ★2026-09-27（回4-1・項目12②）：港の取扱高は海路の成約だけになった。この世界（スイス・ジェノヴァ・トスカーナ）の組は全て陸で繋がり、
        //   ベルンの荷は陸路の直行が勝つ（ジェノヴァ行きもリヴォルノ行きも＝港を通らない＝通行料は立たない・正しい）。検証対象は通行料の移転と保存なので、
        //   上のテストと同じく陸路を落として、借りた港を通る経路だけの世界で検証する（環境固定の作法）。
        sim.Coef.LandRouteEnabled = 0.0;
        var swiss = sim.Nations[0].World; var genoa = sim.Nations[1].World; var tuscany = sim.Nations[2].World;
        var bern = swiss.Regions[0]; var genoaPort = genoa.Regions[0];
        // 交易を確実に立てる（関税・売買方針の国庫経路を全て黙らせ、国庫の増減＝通行料だけにする）
        swiss.Tariff = 0; genoa.Tariff = 0; tuscany.Tariff = 0;
        swiss.RailCapacity = 1e6; swiss.ShipCapacity = 1e6;   // 内陸州は鉄道0だと世界市場に非接続＝テストでは容量を与える
        var goods = swiss.Region.Goods;
        var gd = goods.First(g => !g.IsColonial && !g.NonTradeable && g.Tier != GoodTier.Staple && !g.GrainChain
                                  && Array.IndexOf(sim.Coef.MilitaryExportGoods, g.Id) < 0);
        double wp = sim.Market.Price[gd.Id];
        var bm = bern.Markets[gd.Id]; bm.Price = 0.001 * wp; bm.Stock = 20000; bm.TargetStock = 10000;      // ベルン＝激安在庫＝輸出側
        var gm = genoaPort.Markets[gd.Id]; gm.Price = 10.0 * wp; gm.TargetStock = 10000;                     // ジェノヴァ＝高値＝輸入側（取引の相手）
        // ★2026-09-26（棚卸し econ-15＝旧経路の削除）：清算は S4 の水準式だけ＝量は売り手の産出と買い手の需要から解く
        //   （旧経路＝定数速度×規模の前提で仕込んでいた）。ベルンに産出、ジェノヴァに需要を置き、両国に容量を与える。
        bm.LastProduced = 20000; bm.LastDemand = 0; bm.LastExport = 0;
        gm.LastDemand = 20000; gm.LastProduced = 0; gm.LastImport = 0; gm.Stock = 0;
        genoa.RailCapacity = 1e6; genoa.ShipCapacity = 1e6;
        double tA0 = swiss.Treasury.Value, tB0 = genoa.Treasury.Value, tC0 = tuscany.Treasury.Value;
        double D(World w) => w.BaseMoney - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued;
        double dA0 = D(swiss), dB0 = D(genoa), dC0 = D(tuscany);
        sim.Market.Step(sim.Nations, sim.Coef);
        Assert.Equal("ジェノヴァ", bern.LastTransitPortName);
        Assert.True(bern.LastPortThroughput > 0, "トランジット州の交易が立たなかった（テスト前提の破れ）");
        double toll = sim.Coef.TransitTollShare * bern.LastPortThroughput;
        Assert.True(toll > 0);
        Assert.True(Math.Abs(genoa.Treasury.Value - (tB0 + toll)) < 1e-6, $"港所有国の受取が通行料と一致しない（Δ={genoa.Treasury.Value - tB0:F6} 期待={toll:F6}）");
        Assert.True(Math.Abs(swiss.Treasury.Value - (tA0 - toll)) < 1e-6, $"利用国の支払が通行料と一致しない（Δ={swiss.Treasury.Value - tA0:F6} 期待={-toll:F6}）");
        Assert.True(Math.Abs(tuscany.Treasury.Value - tC0) < 1e-9, "無関係国の国庫が動いた");
        // 保存則（leakhunt の D ブラケット）：通行料は Treasury と CumulativeTradeFlow の両建て＝D不変
        Assert.True(Math.Abs(D(swiss) - dA0) < 1e-6, $"利用国のDが動いた（{D(swiss) - dA0:E2}）");
        Assert.True(Math.Abs(D(genoa) - dB0) < 1e-6, $"所有国のDが動いた（{D(genoa) - dB0:E2}）");
        Assert.True(Math.Abs(D(tuscany) - dC0) < 1e-6, $"無関係国のDが動いた（{D(tuscany) - dC0:E2}）");
    }

    [Fact]
    public void 交戦中は第2候補へ迂回し両方交戦なら旧フォールバックへ退避する()
    {
        var sim = new MultiNationSim(World3(), seed: 13) { DiplomacyFrozen = true };
        sim.Coef.TransitEnabled = 1.0;
        var swiss = sim.Nations[0].World; var genoa = sim.Nations[1].World; var tuscany = sim.Nations[2].World;
        var bern = swiss.Regions[0];
        sim.Run(1);
        Assert.Equal("ジェノヴァ", bern.LastTransitPortName);   // 平時＝第1候補
        // 第1候補の所有国と開戦→第2候補（リヴォルノ＝トスカーナ）へ迂回
        sim.Diplomacy.Wars.Add(new War { A = swiss.SelfPower!, B = genoa.SelfPower! });
        sim.Market.Step(sim.Nations, sim.Coef);
        Assert.Equal("リヴォルノ", bern.LastTransitPortName);
        // 第2候補の所有国とも開戦→港を借りない（陸路の直行だけ）＝鎖国はしない・通行料も発生しない
        sim.Diplomacy.Wars.Add(new War { A = swiss.SelfPower!, B = tuscany.SelfPower! });
        double tB0 = genoa.Treasury.Value, tC0 = tuscany.Treasury.Value;
        swiss.Tariff = 0; genoa.Tariff = 0; tuscany.Tariff = 0;   // 国庫の動き＝通行料だけにして「不発」を金でも確認
        // ★2026-09-26（棚卸し econ-15＝旧経路の削除）：S4 の水準式で交易を1本立てる（ベルン＝産出のある激安の売り手・ジェノヴァ＝需要のある高値の買い手）
        var gd = swiss.Region.Goods.First(g => !g.IsColonial && !g.NonTradeable && g.Tier != GoodTier.Staple && !g.GrainChain
                                               && Array.IndexOf(sim.Coef.MilitaryExportGoods, g.Id) < 0);
        double wp = sim.Market.Price[gd.Id];
        var bm = bern.Markets[gd.Id]; bm.Price = 0.001 * wp; bm.Stock = 20000; bm.TargetStock = 10000; bm.LastProduced = 20000; bm.LastDemand = 0; bm.LastExport = 0;
        var gm = genoa.Regions[0].Markets[gd.Id]; gm.Price = 10.0 * wp; gm.TargetStock = 10000; gm.LastDemand = 20000; gm.LastProduced = 0; gm.LastImport = 0; gm.Stock = 0;
        swiss.RailCapacity = 1e6; swiss.ShipCapacity = 1e6; genoa.RailCapacity = 1e6; genoa.ShipCapacity = 1e6;
        sim.Market.Step(sim.Nations, sim.Coef);
        Assert.Equal("", bern.LastTransitPortName);
        Assert.True(bern.LastTradeDistanceKm > 0);   // 陸路の直行は生きている＝交易は死なない
        Assert.True(Math.Abs(genoa.Treasury.Value - tB0) < 1e-9 && Math.Abs(tuscany.Treasury.Value - tC0) < 1e-9, "フォールバック時に通行料が発生した");
    }
}
