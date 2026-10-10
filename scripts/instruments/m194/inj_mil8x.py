"""[計測の写しだけ] (180) 線M の計器＝m170/inj_mil7.py を当てたうえで、A 行の末尾に（列 78〜）
  78 農奴解放の法（World.SerfEmancipation・int＝なし0／限定1／全面2）
  79 陸軍の不安（World.SecurityThreatArmy・木に無ければ -9）|80 陸軍の不安の割合（÷(それ＋自国の養える軍)・木に無ければ -9）
  81 本国の州（Core）の数|82 統合格（Integrated）の州の数|83 植民地格（Colonial）の州の数（稼働中の州だけ）
  84 皆兵の門（Government.LawPrereq(ConscriptionLaw, 皆兵).Ok＝1 開・0 閉）
を足し、4四半期ごとに J 行（J|四半期|年|添字|BaseName|統合格と植民地格の州の名前〔格の頭文字 I/C と名前を ; で〕）を書く。
力学は読まない（副作用の無い照会〔LawPrereq〕と記録だけ）。使い方：python inj_mil8.py <写しの木>"""
import sys, subprocess, os
root = sys.argv[1]
here = os.path.dirname(os.path.abspath(__file__))
sp = os.path.dirname(here)
subprocess.run([sys.executable, os.path.join(sp, 'm194', 'inj_mil7x.py'), root], check=True, stdout=subprocess.DEVNULL)
p = root + '/PriceOfBread.SimCore/World.cs'
has = 'public double SecurityThreatArmy' in open(p, 'rb').read().decode('utf-8-sig')
p = root + '/PriceOfBread.SimCore/SelfPlay.cs'
raw = open(p, 'rb').read(); bom = raw.startswith(b'\xef\xbb\xbf'); s = raw.decode('utf-8-sig')
crlf = '\r\n' in s
a = 'UtilityAgent.YM6Cons(yw) + "|" + '
assert s.count(a) == 1, s.count(a)
i = s.index(a); j = s.index(').AppendLine();', i)
old = s[i:j]
arm = 'yw.SecurityThreatArmy' if has else '(-9.0)'
armsh = '(yw.SecurityThreatArmy + yw.SecurityThreatOwnMass > 0 ? yw.SecurityThreatArmy / (yw.SecurityThreatArmy + yw.SecurityThreatOwnMass) : 0)' if has else '(-9.0)'
new = (old + ' + UtilityAgent.YM8Tail(yw, ' + arm + ', ' + armsh + ')')
s = s[:i] + new + s[j:]
# J 行（4四半期ごと・A 行の後）
b = ').AppendLine();'
k = s.index(b, i) + len(b)
nl = '\r\n' if crlf else '\n'
s = s[:k] + nl + '                        if ((q + 1) % 4 == 0) ymSb.Append(UtilityAgent.YM8J(yw, q + 1, yi)).AppendLine();   // [計器の写しだけ]' + s[k:]
helper = ('    internal static string YM8Tail(World yw, double arm, double armsh)   // [計器の写しだけ]' + nl +
          '    {' + nl +
          '        int nc = 0, ni = 0, ncol = 0;' + nl +
          '        foreach (var r in yw.Regions) { if (r.State != RegionState.Active) continue; if (r.Tier == RegionTier.Core) nc++; else if (r.Tier == RegionTier.Integrated) ni++; else ncol++; }' + nl +
          '        bool gate = Government.LawPrereq(yw, PolicyAxis.ConscriptionLaw, (int)ConscriptionLaw.Universal, yw.Coef).Ok;' + nl +
          '        return $"|{(int)yw.SerfEmancipation}|{arm:F4}|{armsh:F4}|{nc}|{ni}|{ncol}|{(gate ? 1 : 0)}";' + nl +
          '    }' + nl +
          '    internal static string YM8J(World yw, int q, int yi)   // [計器の写しだけ]' + nl +
          '    {' + nl +
          '        var sb = new System.Text.StringBuilder();' + nl +
          '        foreach (var r in yw.Regions) { if (r.State != RegionState.Active || r.Tier == RegionTier.Core) continue; sb.Append(r.Tier == RegionTier.Integrated ? "I:" : "C:").Append(r.Name).Append(\';\'); }' + nl +
          '        return $"J|{q}|{yw.Year}|{yi}|{yw.SelfPower?.BaseName}|{sb}";' + nl +
          '    }' + nl)
a = '    internal static void Apply(World w, Stance s)'
assert s.count(a) == 1, s.count(a)
s = s.replace(a, helper + a)
open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))
print('ok mil8', root, 'army=' + str(has))
