using System.Collections.Generic;
namespace PriceOfBread.SimCore;

/// <summary>★(848) 線 c69eh：人の居ない POP の口座のお金の相続 R1（★(839) ② の (a)・作者 2026-11-05〈作者「全て推奨で」で推奨を採った〉・確証なし・事前登録 measurements/m848_20261105_prereg_eh.txt）。
/// 旗 <see cref="Coefficients.EmptyPopEstate"/> ＞ 0 のとき、国の全ての州で、人数 ≦1e-12（<see cref="Pop.Blend"/> の門と同じ）で在庫が正の POP の在庫の全額を、
/// 同じ州の人数 ＞1e-12 の POP へ人数の比で移す（移し方の 1 本 <see cref="World.Transfer"/>・払い手ごとに自分の在庫を全額・受け手は頭数・所得には書かない）。
/// 州に人の居る POP が無い（人数の和 ≦1e-9）ときは移さない。州の種類で分けない。呼び手＝<c>Simulation.DemographicStep</c> の相続の段の前（月に 1 回・助走は呼ばれない）。
/// 新しい状態なし・乱数なし。旗 0 は計器の呼びの後に早い return（リストを確保しない）。</summary>
internal static class EmptyPopEstate
{
    internal static void Step(World w)
    {
        if (Instruments.On) Instruments.EstateDue(w);   // 計器（読むだけ）：その月に移るはずの額（計器が自分で数える）
        if (!(w.Coef.EmptyPopEstate > 0)) return;
        foreach (var r in w.Regions)
        {
            List<Pop>? payers = null; List<double>? held = null;
            double sum = 0, live = 0;
            foreach (var p in r.Pops)
            {
                if (p.Size > 1e-12) { live += p.Size; continue; }
                double s = p.Savings.Value;
                if (s > 0) { (payers ??= new List<Pop>()).Add(p); (held ??= new List<double>()).Add(s); sum += s; }
            }
            if (payers is null || !(live > 1e-9)) continue;
            var heirs = new List<Pop>();
            foreach (var p in r.Pops) if (p.Size > 1e-12) heirs.Add(p);
            World.Transfer((w, payers), (w, heirs, null), sum, fromWeights: held);
        }
    }
}
