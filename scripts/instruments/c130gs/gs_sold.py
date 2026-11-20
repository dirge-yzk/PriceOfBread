# (1271) c130gs 史実の桁 H1：ng.sh の兵士÷人口%（sold_<タグ>.txt）を基準 m1260 の同じ表と並べる。
#   NG＝腕で〈NG〉の期間のうち、基準が NG でない物・または基準も NG で腕の倍率が max(10, 基準の倍率) を超える物（線が史実から遠ざけた時だけ）。
#   使い方: python -X utf8 -I gs_sold.py <基準の sold> <腕の sold>
import sys, re
def load(p):
    d = {}
    for line in open(p, encoding='utf-8'):
        if line.startswith('#') or line.startswith('NG'): continue
        parts = line.strip().split(' ', 1)
        if len(parts) < 2: continue
        nat = parts[0]
        for seg in parts[1].split('／'):
            m = re.search(r'(\d{4}-\d{4}):\s*([\d.]+)［([\d.]+)］([\d.]+)倍\s*(\S+)', seg)
            if m: d[(nat, m.group(1))] = (float(m.group(2)), float(m.group(4)), m.group(5))
    return d
b = load(sys.argv[1]); a = load(sys.argv[2])
ng = []
for k in sorted(set(a) | set(b)):
    va = a.get(k); vb = b.get(k)
    if va is None or vb is None:
        print(f'{k[0]} {k[1]}: 欠け（腕 {va}・基準 {vb}）'); continue
    bad = va[2] == 'NG' and (vb[2] != 'NG' or va[1] > max(10.0, vb[1]))
    if bad: ng.append(f'{k[0]} {k[1]} {va[1]}倍')
    print(f'{k[0]} {k[1]}: 腕 {va[0]}%（{va[1]}倍 {va[2]}）・基準 {vb[0]}%（{vb[1]}倍 {vb[2]}）{"  NG" if bad else ""}')
print(f'H1 NG: {ng}')
