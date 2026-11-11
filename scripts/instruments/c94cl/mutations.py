#!/usr/bin/env python
# (1012) c94cl 突然変異の適用（使い捨ての木の中でだけ使う）。使い方：python mutations.py <木の根> <M の番号>
# 古い文字列が木の中にちょうど 1 回あることを確かめてから置き換える（無ければ異常終了＝突然変異が当たっていない）。バイトで読み書きする。
import sys
root, mid = sys.argv[1], sys.argv[2]
C = 'PriceOfBread.SimCore/InstrumentsCorncf.cs'
L = 'PriceOfBread.SimCore/InstrumentsLordstat.cs'
M = {
 'M1': (C, 'for (int k = 0, nk = LsSlots(w, ls); k < nk; k++)', 'for (int k = 0, nk = ls.Length; k < nk; k++)'),
 'M2': (C, 'double x = v * ls[k];', 'double x = v * LsW(w, ls, k);'),
 'M3': (C, 'double trAll = allRent[0] + allRent[1] + allRent[2];', 'double trAll = 0; foreach (var rr in w.Regions) foreach (var pp0 in rr.Pops) trAll += System.Math.Max(0, pp0.LandRentPerMonth.Value);'),
 'M4': (C, 'crown + priv > 0 ? crown / (crown + priv) : double.NaN', 'priv > 0 ? crown / priv : double.NaN'),
 'M5': (C, '        return f;\n    }\n\n    /// <summary>産地の期待', '        f = World.StateProfitShare(s, r, c);\n        return f;\n    }\n\n    /// <summary>産地の期待'),
 'M6': (L, 'double drop = r.RuleInit[1] * (imNow - imNext);', 'double drop = r.Rule[1] * (imNow - imNext);'),
 'M7': (L, 'c.InnerFreedomTithe : s == StateReligionLaw.ReligiousFreedom ? c.ReligiousFreedomTithe', 'c.ReligiousFreedomTithe : s == StateReligionLaw.ReligiousFreedom ? c.InnerFreedomTithe'),
 'M8': (L, '!p.IsCriminal && p.IsCommoner && !p.IsModernElite && p.LastEarnedPerMonth.Value > 0', '!p.IsCriminal && p.LastEarnedPerMonth.Value > 0'),
 'M9': (C, 'level == 1 ? 0.5 : 0.0;', 'level == 1 ? 0.5 : 0.5;'),
 'M10': (C, '                    if (i == j) continue;\n                    var ds =', '                    var ds ='),
 'M11': (C, 'System.Math.Max(0, System.Math.Min(p, l) - lDn) / p', 'System.Math.Max(0, p - lDn) / p'),
 'M12': (L, '            if (!clergyHere) tithBase = 0;\n', ''),
 'M13': (L, 'if (r.RuleInit[1] > 0 && imNext < imNow) moves += pop;', 'if (imNext < imNow) moves += pop;'),
}
f, old, new = M[mid]
p = root.rstrip('/') + '/' + f
b = open(p, 'rb').read().decode('utf8')
assert b.count(old) == 1, f'{mid}: 古い文字列が {b.count(old)} 回'
open(p, 'wb').write(b.replace(old, new).encode('utf8'))
print(mid, 'ok')
