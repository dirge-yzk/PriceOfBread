"""(721) rdh：an_h2 -v4 の出力の 1/4 未満の事象（名簿の外）ごとに、ピークの四半期から落ちた四半期までの州の間の移民の出のうち 3,000 km 超の割合（読むだけ）。
python -X utf8 tail_far.py <an_h2 の出力> <計器の出力ディレクトリ>"""
import sys, os, re
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'hol'))
import hol_common
hol, d = sys.argv[1], sys.argv[2]
ev = []; seen = set()
for line in open(hol, encoding='utf-8'):
    m = re.match(r'\s+\[(\S+?)・1/(\d+)(・例外の名簿)?\] s(\d+) (\S+?)（(\S+?)・(\w+)(・首都)?） ([\d.e+]+)\((\d+)\)→([\d.e+]+)\((\d+)\)', line)
    if m and not m.group(3) and (int(m.group(4)), m.group(5)) not in seen:
        seen.add((int(m.group(4)), m.group(5))); ev.append((int(m.group(4)), m.group(5), m.group(6), m.group(7), int(m.group(10)), int(m.group(12))))
n = nf = 0
for sd, reg, nat, tier, y0, y1 in ev:
    tot = far = 0.0
    for line in open(os.path.join(d, f'migration_s{sd}.tsv'), encoding='utf-8'):
        c = line.rstrip('\n').split('\t')
        if len(c) < 4 or c[1] != reg or not c[0].isdigit(): continue
        y = 1783 + int(c[0]) // 4
        if not (y0 <= y <= y1): continue
        v = float(c[3]); tot += v
        if hol_common.dist(c[1], c[2]) > 3000: far += v
    sh = far / tot if tot > 0 else float('nan')
    n += 1; nf += 1 if sh >= 0.5 else 0
    print(f's{sd}\t{reg}\t{nat}\t{tier}\t{y0}-{y1}\t出 {tot:.1f}\t3000km超 {sh:.3f}')
print(f'## 事象 {n} 件のうち出の半分以上が 3,000 km 超: {nf} 件')
