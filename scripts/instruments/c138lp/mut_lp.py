# (1323) c138lp 突然変異（事前登録の追記 2 の N1〜N7・使い捨ての木の PolicyPreferences.cs をバイトで書き換える）。使い方: python mut_lp.py <木> <N1..N7>
import sys
t, m = sys.argv[1], sys.argv[2]
p = t + '/PriceOfBread.SimCore/PolicyPreferences.cs'
b = open(p, 'rb').read()
SUF = 'var suf = back && a == PolicyAxis.Suffrage && w.Suffrage > Suffrage.None ? w.Suffrage - 1 : w.Suffrage;'
R = {
 'N1': [('bool back = !increasing && w.Coef.LawPrefSinglePeaked > 0;', 'bool back = false;')],
 'N2': [(SUF, 'var suf = w.Suffrage;')],
 'N3': [('InheritanceStepSize(w, back)', 'InheritanceStepSize(w)')],
 'N4': [('var monLaw = back && w.MonetaryLaw > MonetaryStandardLaw.Fiat ? w.MonetaryLaw - 1 : w.MonetaryLaw;', 'var monLaw = w.MonetaryLaw;')],
 'N5': [(SUF, SUF.replace('w.Suffrage - 1', 'w.Suffrage + 1'))],
 'N6': [('double threat = Politics.AvgMassMilitancy(w) * w.Coef.SuffrageConcession;', 'double threat = 0.0;')],
 'N7': [('if (pref < 0 && w.Coef.LawPrefSinglePeaked > 0 && !(Pref(a, b, e, w, false) < 0)) continue;', '')],
}
for o, n in R[m]:
    o = o.encode(); n = n.encode(); c = b.count(o); assert c == 1, (m, c, o[:50]); b = b.replace(o, n)
open(p, 'wb').write(b); print('mutated', m)
