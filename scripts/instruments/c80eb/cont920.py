# (920) c80eb 続きの終わり方の表（★(919) 反証役の訂正 1 の台本の形）。読むだけ・門を作らない。
#   python -X utf8 -I cont920.py <名> <ins フォルダ> [--seeds 1 10]
#   cpop_s の商人の行（人数 ≦1e-12）で〈本当の持ち越し〉（D0＞ε かつ C≦ε かつ D1＞ε）が連続した四半期の列（続き）を取り、続きの次の行で分ける：
#   行が無い／人が戻った（popK ＞1e-12）／新しい手形（C＞ε）／債務≦ε（D1≦ε）／その他。続きの長さの中央と、終わり方ごとの件数と割合を出す。
import sys, os
from collections import defaultdict
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
tag, d = args[0], args[1]
DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def tol(v): return 1e-9 * max(1.0, abs(v))
ends = defaultdict(int); lens = []
for s in range(s0, s1 + 1):
    p = os.path.join(d, f'cpop_s{s}.tsv')
    if not os.path.exists(p): print(f'# 種 {s} 表なし'); continue
    ser = defaultdict(dict)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            if '\t商人\t' not in line: continue
            x = line.rstrip('\n').split('\t')
            if x[ix['className']] != '商人': continue
            ser[(x[ix['region']], x[ix['popIndex']])][int(x[ix['quarter']])] = (fl(x[ix['popK']]), fl(x[ix['debt']]), fl(x[ix['inCreditPerQuarter']]))
    for rows in ser.values():
        def tc(q):
            r, pr = rows.get(q), rows.get(q - 1)
            if r is None or pr is None: return False
            pk, D1, C = r
            return not (pk > DEAD) and pr[1] > tol(pr[1]) and not (C > tol(C)) and D1 > tol(D1)
        for q in sorted(rows):
            if not tc(q) or tc(q - 1): continue
            e = q
            while tc(e + 1): e += 1
            lens.append(e - q + 1)
            nxt = rows.get(e + 1)
            if nxt is None: k = '行が無い'
            elif nxt[0] > DEAD: k = '人が戻った'
            elif nxt[2] > tol(nxt[2]): k = '新しい手形'
            elif nxt[1] <= tol(nxt[1]): k = '債務≦ε'
            else: k = 'その他'
            ends[k] += 1
lens = sorted(lens); tot = sum(ends.values())
print(f'# {tag} 続き {tot} 本・長さの中央 {lens[len(lens)//2] if lens else float("nan")}')
for k in ['新しい手形', '債務≦ε', '人が戻った', '行が無い', 'その他']:
    print(f'{k}\t{ends.get(k, 0)}\t{100.0 * ends.get(k, 0) / tot if tot else float("nan"):.1f}%')
