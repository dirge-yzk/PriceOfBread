"""(414) m412 のヌル腕の要約（読むだけ）。
使い方：python an_nl414.py M=<m412 の ins> N1=<N1 の ins> N2=<N2 の ins> [rM=<m412 の報告> rN1=… rN2=…]
読む：nation_s<種>.tsv の supplyFillShare<0.3 かつ populationK>=100 の国×四半期（補給崩壊・(412) の記録と同じ定義）、報告の exact／near の行。
NG の集合は ng.sh と ngset192.py が読む（このファイルの外）。"""
import csv, glob, os, re, sys
sys.stdout.reconfigure(encoding='utf-8')
csv.field_size_limit(1 << 30)


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def collapse(d):
    per = {}
    for f in glob.glob(os.path.join(d, 'nation_s[0-9]*.tsv')):
        s = int(re.search(r'(\d+)\.tsv$', f).group(1))
        n = 0
        with open(f, encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                if fl(r['supplyFillShare']) < 0.3 and fl(r['populationK']) >= 100:
                    n += 1
        per[s] = n
    return per


def rep(p):
    ex = ne = None
    for l in open(p, encoding='utf-8'):
        m = re.search(r'政体(exact|near): [\d.]+ / \d+ = ([\d.]+)（参照', l)
        if m:
            if m.group(1) == 'exact':
                ex = round(100 * float(m.group(2)), 1)
            else:
                ne = round(100 * float(m.group(2)), 1)
    return ex, ne


args = dict(a.split('=', 1) for a in sys.argv[1:])
# ★(491) 名乗り：L=<基線の名>（既定〈基線〉）・LN1=・LN2=＝出力の行頭の名（反証役 pf〔bv〕低#4・mg2〔bv〕低#3＝m412 の名札が残って読み違える）
name = {'M': args.get('L', '基線'), 'N1': args.get('LN1', 'N1'), 'N2': args.get('LN2', 'N2')}
arms = [k for k in ('M', 'N1', 'N2') if k in args]
res = {}
for k in arms:
    c = collapse(args[k])
    res[k] = c
    ex, ne = rep(args['r' + k]) if 'r' + k in args else (None, None)
    print(f'{k}（{name[k]}）：補給崩壊(人口100以上)の合計 {sum(c.values())}・千件以上の種 {sum(1 for v in c.values() if v >= 1000)}/{len(c)}・種別 ' +
          ' '.join(f'{s}:{c[s]}' for s in sorted(c)) + (f'・exact {ex}・near {ne}' if ex is not None else ''))
if 'N1' in res and 'N2' in res and 'M' in res:
    tot = {k: sum(v.values()) for k, v in res.items()}
    lo, hi = min(tot['N1'], tot['N2']), max(tot['N1'], tot['N2'])
    print(f'ヌル腕の幅（補給崩壊）{lo}〜{hi}・{name["M"]} {tot["M"]}')
