# (897) c76db 読む線の 3 本目（読むだけ）：cpop_s が見ない格（Core・Integrated）の人の居ない商人の手形を cearn_s から推して数える
#   python -X utf8 debt897c.py <名>=<ins フォルダ> ... [--seeds 1 10]
#   importPayerKind＝2（商人の POP の人数 ≦1e-12）の州×四半期で、importOldRuleBudgetStartPerQuarter が有限＝その四半期の全ての清算の頭で商人の Debt ＞0
#   （InstrumentsCearn.cs:215＝商人が居れば ob＝Debt＞0 ? max(0, 貯蓄) : +∞・四半期の和＝1 回でも Debt＝0 の清算があれば ∞）。importPaidPerQuarter＞0 も数える。
import sys, os, math
from collections import defaultdict
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
for tag, d in [a.split('=', 1) for a in args]:
    C = defaultdict(int); rows = []
    for s in range(s0, s1 + 1):
        p = os.path.join(d, f'cearn_s{s}.tsv')
        if not os.path.exists(p): print(f'# {tag} 種 {s} 表なし'); continue
        info = {}
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                x = line.rstrip('\n').split('\t')
                if x[ix['importPayerKind']] != '2' or x[ix['tier']] == 'Colonial': continue
                ob = fl(x[ix['importOldRuleBudgetStartPerQuarter']]); paid = fl(x[ix['importPaidPerQuarter']])
                C['州×四半期（格≠Colonial・importPayerKind＝2）'] += 1
                if math.isfinite(ob): C['うち旧の規則の予算が有限（商人の Debt＞0 と読める）'] += 1
                if paid > 0: C['うち importPaid＞0'] += 1
                k = x[ix['region']]
                if k not in info: info[k] = [x[ix['nation']], x[ix['tier']], int(x[ix['quarter']]), 0, 0.0]
                info[k][3] += 1; info[k][4] = fl(x[ix['popK']])
        for k, v in info.items(): rows.append((s, k) + tuple(v))
    print(f'# {tag} {d} 種 {s0}-{s1}')
    for k, v in C.items(): print(f'{tag}\t{k}\t{v}')
    print(f'{tag}\t州（種ごとの和）\t{len(rows)}')
    for r in sorted(rows, key=lambda r: -r[6]): print(f'{tag}\t州\t種 {r[0]}\t{r[1]}\t{r[2]}\t{r[3]}\t最初の四半期 {r[4]}\t四半期の数 {r[5]}\t最後の popK {r[6]:.6g}')
