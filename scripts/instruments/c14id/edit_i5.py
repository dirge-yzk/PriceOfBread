# (817) c14id I5 の残り 3 本（NavalCedeFleetReturn・InheritedStateIntentDrop・HcdColonyCessionDistance）の焼き込み。
# 使い方: python -I edit_i5.py <木の根> <A1|A2|A3|T1|T2|T3>   （群ごとに別のコミット）
# バイトで読み書きする（CR の数と BOM を保つ）。旧の文字列がちょうど 1 回のときだけ書く。
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'c13ib'))
from edithelp import Editor  # noqa: E402

root, grp = sys.argv[1], sys.argv[2]
E = Editor(root)
NL = chr(10)


def _replace_all(rel, old, new, expect):
    """旧の文字列がちょうど expect 回のときだけ全て置き換える（バイトで読み書き）。"""
    p = root + '/' + rel
    b = open(p, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    c = b.count(o)
    if c != expect:
        print('NG', rel, c, old[:70])
        sys.exit(1)
    open(p, 'wb').write(b.replace(o, n))
    print('ok', rel, 'x%d' % c)


E.replace_all = _replace_all
S = 'PriceOfBread.SimCore/'
T = 'PriceOfBread.Tests/'

if grp == 'A1':   # NavalCedeFleetReturn
    E.remove_range(S + 'Coefficients.cs', '★(688) 線 navf5（割譲で艦が移る経路の切り分け）', 'public double NavalCedeFleetReturn = 1.0;',
                   '    // 【墓標2026-11-04 (817)・旗の焼き込み】NavalCedeFleetReturn（(688) 線 navf5・2026-10-30 に既定 1 で点火）を消した＝割譲（Rehoming.Ceded だけ）のとき、送り手に州が残るなら出ていく州の艦の在庫を送り手の首都の州（首都の州が割譲されるときは遷都の後の新しい首都）へ戻して空にする（艦は国のもの＝州の艦の在庫は帳簿上の置き場）。建国〔Seceded〕・再併合〔Returned〕は変えない。旧（艦が州ごと受け手の国へ渡る）の枝は消した。確証なし（AI の推奨）。')
    E.edit(S + 'Territory.cs', '（旗 NavalCedeFleetReturn が正のとき・割譲だけ）', '（割譲だけ・旗は (817) で焼き込み）')
    E.edit(S + 'Territory.cs', '旗 0 は 1 ビットも触らない。送り手の州が 0（国が消える', '送り手の州が 0（国が消える')
    E.edit(S + 'Territory.cs', 'if (how == Rehoming.Ceded && from.Coef.NavalCedeFleetReturn > 0 && r.FleetUnits is { } cededFleet)',
           'if (how == Rehoming.Ceded && r.FleetUnits is { } cededFleet)')
elif grp == 'A2':   # InheritedStateIntentDrop
    E.remove_range(S + 'Coefficients.cs', '★(774) 継いだ着工の意志の取り下げ（線 c7sw・線の枝の既定 1・0＝旧）', 'public double InheritedStateIntentDrop = 1.0;',
                   '    // 【墓標2026-11-04 (817)・旗の焼き込み】InheritedStateIntentDrop（(774) 線 c7sw・既定 1）を消した＝州が別の国へ移るとき（割譲・分離独立・再併合＝Territory.TransferRegion）、移る州の生産者の StateTarget（国営工場を建てる意志）を今の棟数まで下げる（意志は政府のもの＝建物は州と一緒に移るが旧の政府の着工の意志は移らない）。旧（意志がそのまま移る）の枝は消した。確証なし（AI の推奨）。')
    E.edit(S + 'Territory.cs', '（旗 InheritedStateIntentDrop が正のとき）', '（旗は (817) で焼き込み）')
    E.edit(S + 'Territory.cs', '旗に依らず同じ純関数で作った新の値を並べる', '同じ純関数で作った新の値を並べる')
    E.edit(S + 'Territory.cs',
           '        if (from.Coef.InheritedStateIntentDrop > 0)\n            foreach (var pr in r.Producers) pr.StateTarget = DroppedStateTarget(pr.StateTarget, World.StateUnits(pr, from.Coef));\n',
           '        foreach (var pr in r.Producers) pr.StateTarget = DroppedStateTarget(pr.StateTarget, World.StateUnits(pr, from.Coef));\n')
    E.edit(S + 'InstrumentsInheritIntent.cs', 'Territory.DroppedStateTarget＝旗 InheritedStateIntentDrop に依らず同じ純関数', 'Territory.DroppedStateTarget＝同じ純関数・旗 InheritedStateIntentDrop は (817) で焼き込み')
elif grp == 'A3':   # HcdColonyCessionDistance
    E.remove_range(S + 'Coefficients.cs', '★(696) 線 hcd（講和の植民地格の州の複数割譲に距離を戻す', 'public double HcdColonyCessionDistance = 1;',
                   '    // 【墓標2026-11-04 (817)・旗の焼き込み】HcdColonyCessionDistance（(696) 線 hcd・2026-10-31 に既定 1 で点火）を消した＝講和の植民地格の州の複数割譲の費用に、ColonyList 経路と同じ距離の項 ×(1＋ColonyCaptureDistanceWeight×海路 km÷DistanceNormKm×(1−勝者の艦÷世界最大の艦)) を常に掛ける（ColonyCaptureDistanceWeight が 0 のときは掛けない）。旧（距離を見ない）の枝は消した。')
    E.edit(S + 'Diplomacy.cs', '（旗 HcdColonyCessionDistance が正のとき）', '（旗 HcdColonyCessionDistance は (817) で焼き込み）')
    E.edit(S + 'Diplomacy.cs', '規約も上と同じ。0 のときは何も計算しない。', '規約も上と同じ。ColonyCaptureDistanceWeight が 0 のときは何も計算しない。')
    E.edit(S + 'Diplomacy.cs', 'bool hcdDist = c.HcdColonyCessionDistance > 0 && c.ColonyCaptureDistanceWeight > 0;', 'bool hcdDist = c.ColonyCaptureDistanceWeight > 0;')
elif grp == 'A1t':   # NavalCedeFleetReturn の試験
    f = T + 'NavalCedeFleetReturnTests.cs'
    E.remove_method(f, '旗0では割譲の州の艦は州ごと受け手に渡る')   # ①
    E.edit(f, 'private static (World from, World to, Region r) Setup(double flag, bool ceddCapital)', 'private static (World from, World to, Region r) Setup(bool ceddCapital)')
    E.edit(f, '        from.Coef.NavalCedeFleetReturn = flag;' + chr(10), '')   # ③
    E.replace_all(f, 'Setup(1.0, false)', 'Setup(false)', 3)
    E.replace_all(f, 'Setup(1.0, true)', 'Setup(true)', 1)
    E.edit(f, '旗 <see cref="Coefficients.NavalCedeFleetReturn"/>（割譲のとき出ていく州の艦の在庫を送り手の首都の州へ戻す）の番人。既定 0＝今のまま（艦ごと渡る）。',
           '割譲のとき出ていく州の艦の在庫を送り手の首都の州へ戻す規則の番人（旗 NavalCedeFleetReturn は (817) で焼き込み＝常に戻す）。')
    E.edit(T + 'DormantIdentityTests.cs', '旗 0 の経路の番人は NavalCedeFleetReturnTests（旗を 0 に置いた走行）。', '(817) で旗ごと削除（RetiredFields）。番人は NavalCedeFleetReturnTests。')
    E.edit(T + 'DormantIdentityTests.cs', '        "NavalProcurementNeedsPort", "CrossBorderMigrationByOpportunity",' + NL,
           '        "NavalProcurementNeedsPort", "CrossBorderMigrationByOpportunity",' + NL
           + '        "NavalCedeFleetReturn",   // (817) c14id：I5 の旗の焼き込み（割譲で艦を送り手の首都の州へ戻す・既定 1 の経路だけ残した）' + NL)
elif grp == 'A2t':   # InheritedStateIntentDrop の試験
    f = T + 'InheritIntentTests.cs'
    E.remove_method(f, '旗0では意志がそのまま移る')   # ①
    E.edit(f, 'static (MultiNationSim Sim, World Uk, World Fr, Region R, Region Other) Setup(double flag)', 'static (MultiNationSim Sim, World Uk, World Fr, Region R, Region Other) Setup()')
    E.edit(f, '        uk.Coef.InheritedStateIntentDrop = flag; fr.Coef.InheritedStateIntentDrop = flag;' + NL, '')   # ③
    E.replace_all(f, 'Setup(1.0)', 'Setup()', 7)
    E.remove_line(f, '        foreach (double flag in ')   # ① 旗 0 の半分（旗の 2 値を回す foreach の頭の行だけを消し、続く { } は素のブロックとして残す）
    E.edit(f, 'var (sim, uk, fr, r, _) = Setup(flag);', 'var (sim, uk, fr, r, _) = Setup();')
    E.edit(f, 'Assert.Equal(flag > 0 ? nw : old, used);                // 実際＝旗 1 で新・旗 0 で旧', 'Assert.Equal(nw, used);                // 実際＝新')   # ②
    E.edit(T + 'DormantIdentityTests.cs', '        "NavalCedeFleetReturn",   // (817) c14id：I5 の旗の焼き込み（割譲で艦を送り手の首都の州へ戻す・既定 1 の経路だけ残した）' + NL,
           '        "NavalCedeFleetReturn",   // (817) c14id：I5 の旗の焼き込み（割譲で艦を送り手の首都の州へ戻す・既定 1 の経路だけ残した）' + NL
           + '        "InheritedStateIntentDrop",   // (817) c14id：I5 の旗の焼き込み（移る州の生産者の意志を今の棟数まで下げる・既定 1 の経路だけ残した）' + NL)
elif grp == 'A3t':   # HcdColonyCessionDistance の試験
    f = T + 'ColonyCessionDistanceTests.cs'
    E.remove_method(f, '旗0は距離を見ず予算で買える最も価値の高い遠い州が移る')   # ①
    E.edit(f, 'private (string[] moved, int regionsTotal, double popTotal) Peace(double flag, double w, double l, double t)', 'private (string[] moved, int regionsTotal, double popTotal) Peace(double w, double l, double t)')
    E.edit(f, '        sim.Coef.HcdColonyCessionDistance = flag;' + NL, '')   # ③
    E.edit(f, 'var a = Peace(0, 100, 10, 50); var b = Peace(1, 100, 10, 50);   // 勝者 100 が世界最大（第三者 50）', 'var b = Peace(100, 10, 50);   // 勝者 100 が世界最大（第三者 50）')   # ① 旗 0 の側
    E.edit(f, '        Assert.Equal(a.moved, b.moved);' + NL, '')   # ①
    E.edit(f, 'Peace(1, 100, 10, third)', 'Peace(100, 10, third)')
    E.edit(f, 'Peace(1, 100, 10, 300)', 'Peace(100, 10, 300)')
    E.edit(T + 'DormantIdentityTests.cs', '旗 0 の経路の番人は ColonyCessionDistanceTests（旗を 0 に置いた走行）。', '(817) で旗ごと削除（RetiredFields）。番人は ColonyCessionDistanceTests。')
    E.edit(T + 'DormantIdentityTests.cs', '        "InheritedStateIntentDrop",   // (817) c14id：I5 の旗の焼き込み（移る州の生産者の意志を今の棟数まで下げる・既定 1 の経路だけ残した）' + NL,
           '        "InheritedStateIntentDrop",   // (817) c14id：I5 の旗の焼き込み（移る州の生産者の意志を今の棟数まで下げる・既定 1 の経路だけ残した）' + NL
           + '        "HcdColonyCessionDistance",   // (817) c14id：I5 の旗の焼き込み（講和の植民地格の州の複数割譲に距離の項・既定 1 の経路だけ残した）' + NL)
else:
    print('群が違う', grp)
    sys.exit(2)
