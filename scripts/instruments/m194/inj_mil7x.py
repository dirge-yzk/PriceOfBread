"""[計測の写しだけ] (170) 線M の計器＝m166/inj_mil6.py を当てたうえで、A 行の末尾に（列 77）
  77 戦前の徴兵法の記録（World.ConscriptionPreWar・-1＝記録なし。腕の木に無ければ -9）
を足す。力学は読まない（記録だけ）。使い方：python inj_mil7.py <写しの木>"""
import sys, subprocess, os
root = sys.argv[1]
here = os.path.dirname(os.path.abspath(__file__))
sp = os.path.dirname(here)
subprocess.run([sys.executable, os.path.join(sp, 'm194', 'inj_mil6x.py'), root], check=True, stdout=subprocess.DEVNULL)
p = root + '/PriceOfBread.SimCore/World.cs'
has = 'public int ConscriptionPreWar' in open(p, 'rb').read().decode('utf-8-sig')
p = root + '/PriceOfBread.SimCore/SelfPlay.cs'
raw = open(p, 'rb').read(); bom = raw.startswith(b'\xef\xbb\xbf'); s = raw.decode('utf-8-sig')
a = '+ UtilityAgent.YM6Cons(yw)).AppendLine();'
assert s.count(a) == 1, s.count(a)
s = s.replace(a, '+ UtilityAgent.YM6Cons(yw) + "|" + ' + ('yw.ConscriptionPreWar' if has else '(-9)') + ').AppendLine();')
open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))
print('ok mil7', root, 'prewar=' + str(has))
