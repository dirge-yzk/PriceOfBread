using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1420) 伝わりの接触の内訳 <c>disccontact_s</c> の機構の試験。旧の番人（InstrumentsTests の 450 四半期の版・Category=FullLength へ移した）が 5 つの誤りの型で守っていた性質を、
/// 盤面を試験の中で置いて直に <c>BilateralMarket.AccumulateDiscovery</c> を呼ぶ形で守る（軌道の運に依らない）。
/// 型と Assert の対応：★(420)(478)＝名前で引かない（A5）／★(427)＝建国した国が売り手でも記帳（A4）／★(490)＝買い手は個体で引く（A2・A3）／★(499)＝売り手も個体で引き nameOfUid と一致（A2・A3）／規則そのもの＝A1。
/// 突然変異（使い捨て）：μ1 呼びを述語の外へ→A1 赤／μ2 買い手の未解禁の項を外す→A1 赤／μ3 売り手の World を同じ名の最初の個体で引く→A2 赤／μ4 buyerUid に売り手の番号→A3 赤。
/// </summary>
[Collection("Instruments")]
public class DiscoveryContactMechanismTests
{
    private const ulong Seed = 1796UL;

    private static (string[] H, List<string[]> R) Read(string dir, string table)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, table + Seed + ".tsv"));
        return (lines[0].Split('\t'), lines.Skip(1).Where(l => l.Length > 0).Select(l => l.Split('\t')).ToList());
    }

    [Fact]
    public void 接触の内訳は売り手の解禁済みと買い手の未解禁にだけ記帳され国は個体で引かれる()
    {
        var coef = new Coefficients { DiscoveryCarrierDownstream = 0 };
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("日本")], seed: (int)Seed, coef: coef) { DiplomacyFrozen = true };
        int steam = Array.FindIndex(Catalog.Discoveries, d => d.Name == "蒸気機関"), loco = Array.FindIndex(Catalog.Discoveries, d => d.Name == "機関車");
        Assert.True(steam >= 0 && loco >= 0);
        var uk = sim.Nations[0].World; var jp = sim.Nations[1].World;
        // 盤面：英は蒸気機関と機関車を解禁済み・日は両方とも未解禁（ビットを試験の中で置く）。建国した国は親の解禁を写して生まれる（自分の E 行は無い）
        uk.Discovered[steam] = true; uk.Discovered[loco] = true; jp.Discovered[steam] = false; jp.Discovered[loco] = false;
        var ukRegs = uk.Regions.Where(r => r.Id != 0).Take(2).ToList(); var jpRegs = jp.Regions.Where(r => r.Id != 0).Take(1).ToList();
        Assert.True(ukRegs.Count == 2 && jpRegs.Count == 1, "前提が崩れた（分離できる州が足りない）");
        uk.PendingSecessionWars = [ukRegs[0], ukRegs[1]]; jp.PendingSecessionWars = [jpRegs[0]];
        sim.SpawnSecededNations();
        Assert.Equal(5, sim.Nations.Count);
        var x = sim.Nations[2]; var z = sim.Nations[3]; var y = sim.Nations[4];   // x＝英から建国（名は別）・z＝英から建国（名を英と同じにする）・y＝日から建国（名を日と同じにする）
        Assert.True(Discovery.Known(x.World.Discovered, steam) && Discovery.Known(x.World.Discovered, loco), "前提が崩れた（建国した国が親の解禁を写していない）");
        Assert.True(!Discovery.Known(y.World.Discovered, steam) && !Discovery.Known(y.World.Discovered, loco));
        string xName = Instruments.NameOf(x.World);
        Assert.DoesNotContain(xName, new[] { "イギリス", "日本" });
        z.World.SelfPower!.BaseName = "イギリス"; y.World.SelfPower!.BaseName = "日本";   // 名前の再利用（同じ名の別の個体）を盤面に置く
        Assert.Equal("イギリス", Instruments.NameOf(z.World)); Assert.Equal("日本", Instruments.NameOf(y.World));

        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_dc_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            Instruments.Phase(sim, Seed, "warm");
            var m = sim.Market;
            // 記帳される 4 つ（売り手＝知る・買い手＝知らない）
            m.AccumulateDiscovery(sim.Nations, coef, 0, 1, Catalog.SteamEngineId, 10.0);   // 英→日 蒸気機関の財
            m.AccumulateDiscovery(sim.Nations, coef, 0, 1, Catalog.SteamEngineId, 3.0);    // 同じ組の 2 回目（合算される）
            m.AccumulateDiscovery(sim.Nations, coef, 2, 1, Catalog.LocomotiveId, 5.0);     // 建国した x→日 機関車の財（A4）
            m.AccumulateDiscovery(sim.Nations, coef, 3, 1, Catalog.SteamEngineId, 7.0);    // 英と同じ名の別の個体 z→日（A2 売り手側）
            m.AccumulateDiscovery(sim.Nations, coef, 0, 4, Catalog.SteamEngineId, 11.0);   // 英→日と同じ名の別の個体 y（A2 買い手側）
            // 記帳されない組（売り手が知らない・買い手が知る・両方知らない・運び手でない財・額 0）
            m.AccumulateDiscovery(sim.Nations, coef, 0, 2, Catalog.SteamEngineId, 100.0);  // 買い手 x は知る
            m.AccumulateDiscovery(sim.Nations, coef, 0, 3, Catalog.SteamEngineId, 100.0);  // 買い手 z は知る
            m.AccumulateDiscovery(sim.Nations, coef, 1, 0, Catalog.SteamEngineId, 100.0);  // 売り手 日は知らない・買い手 英は知る
            m.AccumulateDiscovery(sim.Nations, coef, 1, 4, Catalog.SteamEngineId, 100.0);  // 両方知らない
            m.AccumulateDiscovery(sim.Nations, coef, 0, 1, 0, 100.0);                      // 運び手の財でない（財 0）
            m.AccumulateDiscovery(sim.Nations, coef, 0, 1, Catalog.SteamEngineId, 0.0);    // 額 0
            Instruments.Quarter(sim, Seed, 0);
            Instruments.Close(sim);
        }
        finally { Instruments.Configure(null, null); }
        try
        {
            var (h, rows) = Read(dir, "disccontact_s");
            var (nh, nrows) = Read(dir, "nation_s");
            int iRow = Array.IndexOf(h, "row"), iBuyer = Array.IndexOf(h, "buyer"), iSeller = Array.IndexOf(h, "seller"), iCar = Array.IndexOf(h, "carrier"), iV = Array.IndexOf(h, "valuePerQuarter");
            int iBU = Array.IndexOf(h, "buyerUid"), iSU = Array.IndexOf(h, "sellerUid"), nUid = Array.IndexOf(nh, "nationUid"), nName = Array.IndexOf(nh, "nation");
            Assert.True(iRow >= 0 && iBuyer >= 0 && iSeller >= 0 && iCar >= 0 && iV >= 0 && iBU >= 0 && iSU >= 0 && nUid >= 0 && nName >= 0);
            var nameOfUid = new Dictionary<string, string>(); foreach (var nr in nrows) nameOfUid[nr[nUid]] = nr[nName];
            // 同じ名が 2 つの個体にある＝nation_s は名前で一意に引けない（試験の前提）。個体の番号は行の出る順でなく、売り手・買い手の組と額で読む
            // A3：全ての行で個体の番号が 0 でなく、名前が nameOfUid と一致する
            foreach (var r in rows)
            {
                Assert.NotEqual("0", r[iBU]); Assert.NotEqual("0", r[iSU]);
                Assert.Equal(r[iSeller], nameOfUid[r[iSU]]); Assert.Equal(r[iBuyer], nameOfUid[r[iBU]]);
            }
            // A1：行は蒸気機関と機関車だけ・額は呼んだ値・carrier は運び手の表のとおり。記帳された組は 7 行（英→日：蒸気機関・機関車＝13／x→日：機関車＝5／z→日：蒸気機関・機関車＝7／英→y：蒸気機関・機関車＝11）で、買い手が知る・売り手が知らない・両方知らない・財・額 0 の呼びは 1 行も無い
            Assert.All(rows, r => Assert.Contains(r[iRow], new[] { "蒸気機関", "機関車" }));
            double V(string[] r) => double.Parse(r[iV], System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(7, rows.Count);
            Assert.Equal(67.0, rows.Sum(V), 9);
            foreach (var r in rows)
            {
                bool steamRow = r[iRow] == "蒸気機関";
                Assert.Contains(r[iCar], steamRow ? new[] { "蒸気機関" } : new[] { "機関車", "蒸気機関" });
            }
            // A4：建国した国 x（自分の E 行が無く親の解禁を写した）が売り手のときも記帳（機関車の行・運び手＝機関車・額 5・買い手は日）
            var xr = rows.Where(r => r[iSeller] == xName).ToList();
            Assert.Single(xr); Assert.Equal("機関車", xr[0][iRow]); Assert.Equal("機関車", xr[0][iCar]); Assert.Equal(5.0, V(xr[0]), 9); Assert.Equal("日本", xr[0][iBuyer]);
            // A2：同じ名の別の個体は別の行で合算されない。売り手「イギリス」は 2 個体（英 13・z 7）・買い手「日本」は 2 個体（日 13+5+7・y 11）
            var ukSteam = rows.Where(r => r[iSeller] == "イギリス" && r[iRow] == "蒸気機関").ToList();
            Assert.Equal(3, ukSteam.Count);   // 英→日 13・z→日 7・英→y 11
            Assert.Equal(3, ukSteam.Select(r => (r[iSU], r[iBU])).Distinct().Count());
            Assert.Equal(2, ukSteam.Select(r => r[iSU]).Distinct().Count());
            Assert.Equal(2, ukSteam.Select(r => r[iBU]).Distinct().Count());
            Assert.Equal(new[] { 7.0, 11.0, 13.0 }, ukSteam.Select(V).OrderBy(v => v).ToArray());
            var toYuid = ukSteam.Single(r => Math.Abs(V(r) - 11.0) < 1e-9);   // y 宛て
            var toJp13 = ukSteam.Single(r => Math.Abs(V(r) - 13.0) < 1e-9);
            var fromZ = ukSteam.Single(r => Math.Abs(V(r) - 7.0) < 1e-9);
            Assert.NotEqual(toYuid[iBU], toJp13[iBU]);                 // 同じ名「日本」の別の個体（買い手）
            Assert.NotEqual(fromZ[iSU], toJp13[iSU]);                  // 同じ名「イギリス」の別の個体（売り手）
            Assert.Equal(toJp13[iBU], fromZ[iBU]);                     // z→日 の買い手は日（個体が同じ）
            Assert.Equal(toJp13[iSU], toYuid[iSU]);                    // 英の売りは同じ個体
            // A5：名前で引かない＝個体の番号は 5 つの国の個体のうち売り手 4 つ・買い手 3 つの組合せで読め、同じ名の 2 個体が同じ番号に潰れていない
            Assert.True(rows.Select(r => r[iBU]).Distinct().Count() == 2 && rows.Select(r => r[iSU]).Distinct().Count() == 3);
            Assert.Equal(5, nameOfUid.Count);
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
