using System.Collections.Generic;
namespace PriceOfBread.SimCore;

/// <summary>★(848) 線 c69eh：人の居ない POP の口座のお金の相続 R1（★(839) ② の (a)・作者 2026-11-05〈作者「全て推奨で」で推奨を採った〉・確証なし・事前登録 measurements/m848_20261105_prereg_eh.txt）。
/// 旗 <see cref="Coefficients.EmptyPopEstate"/> ＞ 0 のとき、国の全ての州で、人数 ≦1e-12（<see cref="Pop.Blend"/> の門と同じ）で在庫が正の POP の在庫の全額を、
/// 同じ州の人数 ＞1e-12 の POP へ人数の比で移す（移し方の 1 本 <see cref="World.Transfer"/>・払い手ごとに自分の在庫を全額・受け手は頭数・所得には書かない）。
/// 州に人の居る POP が無い（人数の和 ≦1e-9）ときは移さない。州の種類で分けない。呼び手＝<c>Simulation.DemographicStep</c> の相続の段の前（月に 1 回・助走は呼ばれない）。
/// 新しい状態なし・乱数なし。旗 0 は計器の呼びの後に早い return（リストを確保しない）。★(910) 旗 <see cref="Coefficients.EmptyPopDebtFirst"/> ＞0 では、人の居ない POP の債務を相続より先に返す（:22・事前登録 measurements/m910_20261107_prereg_ep.txt）。</summary>
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
                if (w.Coef.EmptyPopDebtFirst > 0 && p.Debt.Value > 0) { double back = Math.Min(p.Debt.Value, Math.Max(0, p.Savings.Value)); if (back > 0) { if (Instruments.On) Instruments.EstateRepay(true); Bank.Lend(w, p, -back); if (Instruments.On) Instruments.EstateRepay(false); } } double s = p.Savings.Value;   // ★(910) c78ep 案 B5（旗 EmptyPopDebtFirst・既定 0）：相続の前に人の居ない POP の債務を自分の貯蓄から返す（Bank.Lend の負の側＝決済 ① と同じ書き手・受け手の有無に依らない・否定形の門＝NaN は返さない）→ 貯蓄を読み直して残りを今の規則で相続人へ。この 1 行に置く＝:31 の移しの行番号（cearn_s の経路の名）を動かさない
                if (s > 0) { (payers ??= new List<Pop>()).Add(p); (held ??= new List<double>()).Add(s); sum += s; }
            }
            if (payers is null || !(live > 1e-9)) continue;
            var heirs = new List<Pop>();
            foreach (var p in r.Pops) if (p.Size > 1e-12) heirs.Add(p);
            int big = 0;   // ★(848) §11：人数が最大の受け手を末尾へ（Pop.Distribute は端数を末尾の受け手へ寄せる＝末尾の人数がごく小さいと 1 人あたりの受け取りが 1 本にならない）
            for (int i = 1; i < heirs.Count; i++) if (heirs[i].Size > heirs[big].Size) big = i;
            (heirs[big], heirs[^1]) = (heirs[^1], heirs[big]);
            World.Transfer((w, payers), (w, heirs, null), sum, fromWeights: held);
        }
    }
}
