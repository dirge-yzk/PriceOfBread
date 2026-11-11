# (943) c83ic 突然変異：使い捨ての木の Diplomacy.cs に M1〜M6 を 1 本ずつ当てる（バイトで読み書き＝CRLF を保つ）。
# 使い方: python mut943.py <木> <M1..M6|RESET>   RESET は git の版へ戻すのではなく、控え（Diplomacy.cs.orig）から戻す。
import sys, os, shutil
tree, mut = sys.argv[1], sys.argv[2]
p = os.path.join(tree, 'PriceOfBread.SimCore', 'Diplomacy.cs'); orig = p + '.orig'
if not os.path.exists(orig): shutil.copyfile(p, orig)
b = open(orig, 'rb').read()
COND = b"if (c.ImposeCrownFromLoser > 0 && (ww0.Polity is Polity.Absolutist or Polity.Despotic or Polity.Shogunate))"
SW = b"imposeTarget = headCrown switch { CrownForm.Despotic => Polity.Despotic, CrownForm.Shogunate => Polity.Shogunate, _ => Polity.Absolutist };"
HEAD = b"var headCrown = _crownAtPeaceHead is not null && _crownAtPeaceHead.TryGetValue(dw, out var hc) ? hc : dw.Crown;"
APPLY = b"CrownParliament.ApplyRegime(dw, c, imposeTarget);"
rep = {
    'M1': [(COND, COND.replace(b"if (", b"if (false && ", 1))],
    'M2': [(SW, b"imposeTarget = Polity.Absolutist;")],
    'M3': [(SW, b"imposeTarget = dw.Polity;")],
    'M4': [(SW, b"imposeTarget = headCrown == CrownForm.None ? Polity.Absolutist : ww0.Polity;")],
    'M5': [(APPLY, b"var m5Target = imposeTarget; CrownParliament.ApplyRegime(dw, c, ww0.Polity); if (m5Target != ww0.Polity) dw.Crown = m5Target switch { Polity.Despotic => CrownForm.Despotic, Polity.Shogunate => CrownForm.Shogunate, _ => CrownForm.Absolutist };")],
    'M6': [(HEAD, b"var headCrown = dw.Crown;")],
    'RESET': [],
}[mut]
for o, n in rep:
    assert b.count(o) == 1, (mut, o)
    b = b.replace(o, n)
open(p, 'wb').write(b)
print('applied', mut)
