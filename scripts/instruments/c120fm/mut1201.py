# (1201) c120fm 突然変異の当て方（使い捨ての木に 1 つずつ・バイトで読み書き＝CR と BOM を保つ）。
#   python -X utf8 -I mut1201.py <木の根> <M1|M2|M3|M4|M5>
#   事前登録 measurements/m1201_20261118_prereg_fm.txt §6 の表どおり。旧の字がちょうど 1 回でなければ止める。
import sys
ROOT, M = sys.argv[1], sys.argv[2]
F = ROOT + '/PriceOfBread.SimCore/Formations.cs'
I = ROOT + '/PriceOfBread.SimCore/Instruments/InstrumentsFormation.cs'
TAB = {
    'M1': (F, 'if (w.Coef.FormationNatioGate > 0 && !NationalistReady(w, w.Coef)) continue;',
              'if (w.Coef.FormationNatioGate > 0 && !NationalistReady(w, w.Coef)) { }'),            # 門の比べを外す（旗を読むが止めない）
    'M2': (F, '=> Ideologies.National(w)[NationalistShareIndex] >= c.UnificationNatioShare.Value;',
              '=> Ideologies.National(w)[NationalistShareIndex] >= 0.0;'),                         # 閾値を 0 に
    'M3': (F, 'pw.Coef.FormationOwnDirectOnly > 0) return false;',
              'pw.Coef.FormationOwnDirectOnly > 0) { }'),                                         # 旗 b でも従属国の数えを戻す
    'M4': (I, 'I(w.Regions.Count), I(depRegions),',
              'I(w.Regions.Count), I(w.Regions.Count),'),                                        # 計器の従属国の州の数に自分の州の数を書く
    'M5': (F, '            if (power.Nation is not { } w) continue;\n            FormationRule? best = null;',
              '            if (power.Nation is not { } w || power.IsPlayer) continue;\n            FormationRule? best = null;'),   # Check がプレイヤーの国を飛ばす
}
p, old, new = TAB[M]
b = open(p, 'rb').read()
o, n = old.encode('utf-8'), new.encode('utf-8')
if b.count(o) != 1:
    print(f'MUTFAIL {M} 旧の字の数 {b.count(o)}'); sys.exit(1)
open(p, 'wb').write(b.replace(o, n))
print(f'MUTOK {M} {p}')
