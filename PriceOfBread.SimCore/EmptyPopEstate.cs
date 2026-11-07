using System.Collections.Generic;
namespace PriceOfBread.SimCore;

/// <summary>★(848) 線 c69eh：人の居ない POP の口座のお金の相続 R1（★(839) ② の (a)・作者 2026-11-05〈作者「全て推奨で」で推奨を採った〉・確証なし・事前登録 measurements/m848_20261105_prereg_eh.txt）。
/// 旗 <see cref="Coefficients.EmptyPopEstate"/> ＞ 0 のとき、国の全ての州で、人数 ≦1e-12（<see cref="Pop.Blend"/> の門と同じ）で在庫が正の POP の在庫の全額を、
/// 同じ州の人数 ＞1e-12 の POP へ人数の比で移す（移し方の 1 本 <see cref="World.Transfer"/>・払い手ごとに自分の在庫を全額・受け手は頭数・所得には書かない）。
/// 州に人の居る POP が無い（人数の和 ≦1e-9）ときは移さない。州の種類で分けない。呼び手＝<c>Simulation.DemographicStep</c> の相続の段の前（月に 1 回・助走は呼ばれない）。
/// 新しい状態なし・乱数なし。旗 0 は計器の呼びの後に早い return（リストを確保しない）。★(910) 旗 <see cref="Coefficients.EmptyPopDebtFirst"/> ＞0 では、人の居ない POP の債務を相続より先に返す（:22・事前登録 measurements/m910_20261107_prereg_ep.txt）。</summary>
internal static class EmptyPopEstate
{
    internal static void Step(World w) { Core(w); if (Instruments.On) Instruments.EstateAfter(w); } private static void Core(World w)   // (920) 計器（読むだけ）：相続の段の直後の断面。本体は下の Core（この 1 行に置く＝:31 の移しの行番号を動かさない）
    {
        if (Instruments.On) Instruments.EstateDue(w);   // 計器（読むだけ）：その月に移るはずの額（計器が自分で数える）
        if (!(w.Coef.EmptyPopEstate > 0)) return;
        foreach (var r in w.Regions)
        {
            List<Pop>? payers = null; List<double>? held = null; List<Pop>? debtors = null;   // ★(936) debtors＝旗 EmptyPopDebtToHeirs ＞0 で債務を相続人へ移す送り手
            double sum = 0, live = 0;
            foreach (var p in r.Pops)
            {
                if (p.Size > 1e-12) { live += p.Size; continue; }
                if (w.Coef.EmptyPopDebtFirst > 0 && p.Debt.Value > 0) { double back = Math.Min(p.Debt.Value, Math.Max(0, p.Savings.Value)); if (back > 0) { if (Instruments.On) Instruments.EstateRepay(true); Bank.Lend(w, p, -back); if (Instruments.On) Instruments.EstateRepay(false); } } double s = p.Savings.Value;   // ★(910) c78ep 案 B5（旗 EmptyPopDebtFirst・既定 0）：相続の前に人の居ない POP の債務を自分の貯蓄から返す（Bank.Lend の負の側＝決済 ① と同じ書き手・受け手の有無に依らない・比較が偽＝NaN は返さない）→ 貯蓄を読み直して残りを今の規則で相続人へ。この 1 行に置く＝:31 の移しの行番号（cearn_s の経路の名）を動かさない
                if (s > 0) { (payers ??= new List<Pop>()).Add(p); (held ??= new List<double>()).Add(s); sum += s; } if (w.Coef.EmptyPopDebtToHeirs > 0 && p.Debt.Value > 0) (debtors ??= new List<Pop>()).Add(p);   // ★(936) 貯蓄 0 でも債務 ＞0 の人の居ない POP を債務の送り手に含める（B5 の返しの後の債務を読む・比較が偽＝NaN は含めない）
            }
            if ((payers is null && debtors is null) || !(live > 1e-9)) continue;   // ★(936) 貯蓄の送り手か債務の送り手のどちらかが居れば相続の段へ（旗 0 は debtors が常に null＝今の門と同じ）
            var heirs = new List<Pop>();
            foreach (var p in r.Pops) if (p.Size > 1e-12) heirs.Add(p);
            int big = 0;   // ★(848) §11：人数が最大の受け手を末尾へ（Pop.Distribute は端数を末尾の受け手へ寄せる＝末尾の人数がごく小さいと 1 人あたりの受け取りが 1 本にならない）
            for (int i = 1; i < heirs.Count; i++) if (heirs[i].Size > heirs[big].Size) big = i;
            (heirs[big], heirs[^1]) = (heirs[^1], heirs[big]);
            if (payers is not null) World.Transfer((w, payers), (w, heirs, null), sum, fromWeights: held); if (debtors is not null) DebtToHeirs(heirs, debtors, live);   // ★(936) World.Transfer の呼び口はこの行のまま（cearn_s の経路の名）・債務の移しは貯蓄に触れない＝順でビットは変わらない
        }
    }

    /// <summary>★(936) 線 c82hd 案 B1（旗 <see cref="Coefficients.EmptyPopDebtToHeirs"/>・事前登録 measurements/m936_20261107_prereg_hd.txt [2]）：人の居ない POP の債務を、
    /// 同じ州の相続人（<paramref name="heirs"/>＝人数 ＞1e-12・人数が最大の受け手が末尾）へ人数の比（÷<paramref name="live"/>＝相続人の人数の和）で移す。末尾には送り手の残りをそのまま渡し、送り手を厳密な 0 にする。
    /// 債務の記録だけを動かす（貯蓄と国債の保有は触らない・同じ州＝同じ国の銀行＝HouseholdLoans と基礎貨幣は動かない）。書き方は Pop.Carry の債務の対と同じ 2 文
    /// （Pop.Carry は WriterRatchetTests の呼び手の番人が MultiNationSim.cs に閉じる＝ここから呼べない・債務の対だけを書く既存の関数は無い）。⚠Pop.Carry の計器 debtInCarryPerQuarter には載らない。
    /// 小さい相続人に 1e-12 以下の債務が配られうる（Pop.Take の床で決済 ① が返さない塵＝★(919) ③・名乗って記録）。</summary>
    private static void DebtToHeirs(List<Pop> heirs, List<Pop> debtors, double live)
    {
        if (PopWindow.Verify) PopWindow.GuardWrite("相続の債務");
        foreach (var d in debtors)
        {
            var d0 = d.Debt;
            for (int i = 0; i < heirs.Count - 1; i++) { var x = d0 * (heirs[i].Size / live); d.Debt -= x; heirs[i].Debt += x; }
            heirs[^1].Debt += d.Debt; d.Debt = default;
        }
    }
}
