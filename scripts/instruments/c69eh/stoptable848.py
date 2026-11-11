# (848) c69eh 止める線の 3 世代の並べ表（読むだけ）：stoplines848.sh の出力を 1 枚の TSV に。
#   python -I scripts/instruments/c69eh/stoptable848.py <出力> <名=stop の出力> ...
import sys, re
out = sys.argv[1]; L = ['arm\tukDefaultSeeds\tgdp100_other(797)\tgdp100_833(建国/痩せ/それ以外)\t1e10\tgdpZero4q\tplvlMax\tplvl1000open\thol1/10\thol1/4\thol1/10capital']
def g(pat, t, k=1, d='-'):
    m = re.search(pat, t); return m.group(k) if m else d
for a in sys.argv[2:]:
    name, p = a.split('=', 1); t = open(p, encoding='utf-8', errors='replace').read()
    uk = g(r'不履行のあった種 (\d+/\d+)', t)
    o797 = g(r'== gdpjump（旧の台本 gdp_jump797）\nM\t種 10\t100 倍以上 (\d+)（建国の直後〔≦4 四半期〕(\d+)・それ以外 (\d+)）', t, 3)
    m833 = re.search(r'100 倍以上 (\d+)（建国の直後〔≦4 四半期〕(\d+)・分母の痩せ〔人口の比 ≧10 か ≦1/10〕(\d+)・それ以外 (\d+)）\t1e10 倍以上 (\d+)', t)
    s833 = f'{m833.group(2)}/{m833.group(3)}/{m833.group(4)}' if m833 else '-'
    e10 = m833.group(5) if m833 else '-'
    gz = g(r'4 四半期以上続く区間 (\d+)', t)
    pm = g(r'価格水準の最大 \(([0-9.]+)', t)
    p1000 = g(r'開幕の1000倍以上（[^）]*）\s*(\d+)', t)
    h10 = g(r'ピークの1/10 未満: (\d+) 件', t); h4 = g(r'ピークの1/4 未満: (\d+) 件', t); hc = g(r'1/10 の首都: (\d+)', t)
    L.append('\t'.join([name, uk, o797, s833, e10, gz, pm, p1000, h10, h4, hc]))
open(out, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
print('\n'.join(L))
