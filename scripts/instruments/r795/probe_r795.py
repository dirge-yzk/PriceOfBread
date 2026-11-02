#!/usr/bin/env python3
# (795) r795 読み手：助走と本走の頭で World の銀の平価の挟み（World.cs UpdatePriceLevel の silverRel の Clamp）に入る前の値・経路・入力を国×四半期で書く読むだけのプローブ。
# 使い方：R795_OUT=<出力tsv> R795_NOCLAMP=0|1 POB_WORK=... bash scripts/instruments/batch.sh prep <腕> commit:<sha> plain <この台本>
#   R795_NOCLAMP=1 のときだけ挟みを外す（silverRel の Clamp を消し、NaN の番人を〈正の有限でなければ 1〉へ）＝挙動を変える腕（読み手の比べ用）。0 は読むだけ。
# 読むだけ：乱数を引かない・世界を書かない（静的な ConditionalWeakTable と出力ファイルだけ）。
import sys, os
root = sys.argv[1]
OUT = os.environ.get('R795_OUT', r'D:\PoB_work\r795_work\probe_A.tsv').replace('/', '\\')
NOCLAMP = os.environ.get('R795_NOCLAMP', '0') == '1'

def rw(rel, fn):
    p = os.path.join(root, rel); b = open(p, 'rb').read(); n = fn(b); assert n != b, rel; open(p, 'wb').write(n)

CS = r'''using System.Globalization;
using System.Runtime.CompilerServices;
namespace PriceOfBread.SimCore;
// (795) r795 の使い捨てのプローブ（読むだけ）。main には入れない。
internal static class PobProbe795
{
    const string Out = @"__OUT__";
    static readonly object L = new();
    sealed class Box { public int Q; }
    sealed class Tag { public ulong Seed; public int Q; public string Name = ""; }
    static readonly ConditionalWeakTable<MultiNationSim, Box> SimQ = new();
    static readonly ConditionalWeakTable<World, Tag> WTag = new();
    static readonly CultureInfo I = CultureInfo.InvariantCulture;
    static string F(double x) => x.ToString("R", I);
    static void W(string s) { lock (L) System.IO.File.AppendAllText(Out, s + "\n"); }
    // 注入の直前（MultiNationSim の四半期末の共有層）。q＝その sim の注入の通し番号（1 から・助走の 20 回のあとが本走）。
    internal static void Inject(MultiNationSim sim, ulong seed, bool frozen, int ratioCnt, double meanRatio, double refRatio, double rel, bool ratioOk, double mktSilver, double silverWorld)
    {
        var box = SimQ.GetOrCreateValue(sim); box.Q++; int q = box.Q;
        foreach (var n in sim.Nations)
        {
            var t = WTag.GetOrCreateValue(n.World); t.Seed = seed; t.Q = q;
            if (t.Name.Length == 0) foreach (var p in sim.Diplomacy.Powers) if (ReferenceEquals(p.Nation, n.World)) { t.Name = p.BaseName; break; }
        }
        if (q > 40) return;
        double refAg = Catalog.RefPrices[Catalog.SilverId], refAu = Catalog.RefPrices[Catalog.GoldOreId];
        W(string.Join('\t', "Q", seed.ToString(I), q.ToString(I), frozen ? "1" : "0", ratioCnt.ToString(I), F(meanRatio), F(refRatio), F(rel), ratioOk ? "1" : "0", F(mktSilver), F(silverWorld), F(silverWorld / refAg), F(mktSilver / refAg), F(refAg), F(refAu)));
        if (!(q <= 4 || (q >= 20 && q <= 22))) return;
        foreach (var n in sim.Nations)
        {
            var w = n.World; WTag.TryGetValue(w, out var tg);
            foreach (var r in w.Regions)
            {
                if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) continue;
                var mAg = r.Markets[Catalog.SilverId]; var mAu = r.Markets[Catalog.GoldOreId];
                bool agLive = mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9, auLive = mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9;
                bool inc = agLive && auLive && mAg.Price > 1e-9 && mAu.Price > 1e-9;
                W(string.Join('\t', "R", seed.ToString(I), q.ToString(I), frozen ? "1" : "0", tg?.Name ?? "", r.Name, inc ? "1" : "0",
                    F(mAg.Price), F(mAu.Price), F(mAg.LastDemand), F(mAg.LastProduced), F(mAu.LastDemand), F(mAu.LastProduced), F(mAg.Stock), F(mAu.Stock),
                    F(inc ? (mAg.Price / mAu.Price) / (refAg / refAu) : double.NaN), F(w.ExchangeRate), F(r.PriceLevel)));
            }
        }
    }
    // UpdatePriceLevel の為替の更新の直後（毎四半期・各国）。silverRaw＝挟みに入る前の値（銀本位で WorldSilverPrice>1e-9 の国だけ・他は nan）。
    internal static void Fx(World w, Coefficients c, double ppp, double bias, double floatTarget, double parity, double rateTarget, double eBefore)
    {
        WTag.TryGetValue(w, out var tg); int q = tg?.Q ?? 0;
        if (q > 28) return;
        double refAg = Catalog.RefPrices[Catalog.SilverId];
        double raw = w.MonetaryLaw == MonetaryStandardLaw.Silver && w.WorldSilverPrice > 1e-9 && refAg > 1e-9 ? w.WorldSilverPrice / refAg : double.NaN;
        int hit = raw > c.ExchangeRateCeil ? 1 : raw < c.ExchangeRateFloor ? -1 : 0;
        W(string.Join('\t', "X", (tg?.Seed ?? 0).ToString(I), q.ToString(I), w.WarmupFrozen ? "1" : "0", tg?.Name ?? "", w.MonetaryLaw.ToString(), F(w.WorldSilverPrice), F(raw), hit.ToString(I),
            F(w.GoldParity), F(parity), F(w.Convertibility), F(ppp), F(bias), F(floatTarget), F(rateTarget), F(eBefore), F(w.ExchangeRate), F(w.PriceLevel), F(w.SilverGoldRatio)));
    }
}
'''.replace('__OUT__', OUT)
open(os.path.join(root, 'PriceOfBread.SimCore', 'PobProbe795.cs'), 'w', encoding='utf-8', newline='\n').write(CS)

def mns(b):
    a = b'                if (_coef.SilverParityFromRatio > 0 && ratioOk) silverWorld = rel * Catalog.RefPrices[Catalog.SilverId];\n'
    assert b.count(a) == 1
    return b.replace(a, a + b'                PobProbe795.Inject(this, _seed, _calendarFrozen, ratioCnt, meanRatio, refRatio, rel, ratioOk, Market.Price[Catalog.SilverId], silverWorld);\n')

def world(b):
    a = b'        ExchangeRate += c.ExchangeRateSpeed * (rateTarget - ExchangeRate);'
    assert b.count(a) == 1
    b = b.replace(a, b'        double pobEb = ExchangeRate;\n' + a + b'\n        PobProbe795.Fx(this, c, ppp, bias, floatTarget, parity, rateTarget, pobEb);')
    if NOCLAMP:
        c1 = b'            silverRel = Math.Clamp(silverRel, c.ExchangeRateFloor, c.ExchangeRateCeil);\n'
        c2 = b'            if (!(silverRel >= c.ExchangeRateFloor && silverRel <= c.ExchangeRateCeil)) silverRel = 1.0;'
        assert b.count(c1) == 1 and b.count(c2) == 1
        b = b.replace(c1, b'').replace(c2, b'            if (!(silverRel > 0 && silverRel < double.PositiveInfinity)) silverRel = 1.0;')
    return b

rw('PriceOfBread.SimCore/MultiNationSim.cs', mns)
rw('PriceOfBread.SimCore/World.cs', world)
print('r795 probe edit ok', OUT, 'NOCLAMP' if NOCLAMP else 'READONLY')
