# 問い3・6：英の内戦が白紙（判定 0）で終わった件で、親（イギリス）の政体と厭戦を内戦の四半期の前後で並べる（読むだけ）
import sys, os, csv, re
NAT = 'イギリス'
print("腕\t種\t内戦q\t反乱国\t月数\t判定\t親の政体 q-1/q/q+1/q+2\t親の厭戦 q-1/q/q+1\t親の正統性 q-1/q")
for spec in sys.argv[1:]:
    name, d = spec.split('=', 1)
    for f in sorted(os.listdir(d)):
        m = re.match(r'wars_s(\d+)\.tsv$', f)
        if not m: continue
        s = m.group(1)
        with open(os.path.join(d, f), encoding='utf-8') as fh:
            cws = [r for r in csv.DictReader(fh, delimiter='\t') if r['civilWarFlag'] == '1' and r['sideA'] == NAT]
        if not cws: continue
        sc = {}
        with open(os.path.join(d, f"scalars_s{s}.tsv"), encoding='utf-8') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                if r['nation'] == NAT: sc[int(r['quarter'])] = r
        for r in cws:
            q = int(r['q']); g = lambda k, qq: sc[qq][k] if qq in sc else '-'
            print(f"{name}\t{s}\t{q}\t{r['sideB']}\t{r['months']}\t{r['verdictCode']}\t{'/'.join(g('polityCode', x) for x in (q-1, q, q+1, q+2))}\t{'/'.join(g('warExhaustion', x)[:4] for x in (q-1, q, q+1))}\t{'/'.join(g('legitimacy', x)[:4] for x in (q-1, q))}")
