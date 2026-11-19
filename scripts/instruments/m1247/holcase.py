# (1247) 空洞化 1/10 未満の件ごとの振り分け（an_h2 -v4 の出力から件を拾い、基線の同じ種・同じ州の〈ピークの後の最小÷ピーク〉を数える）
#   python holcase.py <an_h2 -v4 の出力> <基線の生（region_s<種>.tsv がある所）> [ヌル腕の生]
#   基線で 1/4 未満なら〈元からの弱さ〉・それ以外は〈線が原因〉（★(813)・合流の指示の規則のまま）
import sys, csv, re
def traj(p, name):
    try: f = open(p, encoding='utf-8')
    except OSError: return None
    r = csv.reader(f, delimiter='\t'); h = next(r)
    ir, ip, iy, io = h.index('region'), h.index('populationK'), h.index('year'), h.index('nation')
    return [(row[iy], float(row[ip]), row[io]) for row in r if row[ir] == name]
def ratio(t):
    if not t: return None
    pk = max(range(len(t)), key=lambda i: t[i][1]); mn = min(t[pk:], key=lambda x: x[1])
    return round(mn[1] / t[pk][1], 3), t[pk], mn
pat = re.compile(r'^\s+\[(.+?)・1/10(・例外の名簿)?\] s(\d+) (\S+?)（')
n = 0
for line in open(sys.argv[1], encoding='utf-8'):
    m = pat.match(line)
    if not m: continue
    if m.group(2): continue
    n += 1; seed, reg = m.group(3), m.group(4)
    out = [f'種 {seed}・{reg}・原因 {m.group(1)}']
    for tag, d in (('基線', sys.argv[2]),) + ((('N1', sys.argv[3]),) if len(sys.argv) > 3 else ()):
        r = ratio(traj(f'{d}/region_s{seed}.tsv', reg))
        if r is None: out.append(f'{tag} 表なし'); continue
        out.append(f'{tag} 比 {r[0]}（ピーク {r[1][1]:.2f}@{r[1][0]}・{r[1][2]} → 最小 {r[2][1]:.2f}@{r[2][0]}・{r[2][2]}）')
        if tag == '基線': out.append('振り分け＝' + ('元からの弱さ（基線で 1/4 未満）' if r[0] < 0.25 else '線が原因（基線で 1/4 以上）'))
    print('｜'.join(out))
print(f'名簿の外の 1/10 未満 {n} 件')
