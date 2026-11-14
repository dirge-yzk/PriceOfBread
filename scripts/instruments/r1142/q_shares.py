# (1142) 4 区分の取り分の州の平均（全ての州・首都＝国の最初の行）と、Rule[1]＞Rule[0] の州の数（四半期の平均→種の中央）
#   ＝持ち主の取り分の落ち（旗 1 で約 3 割）のうち、外国の取り分で説明できる分を分ける。
# 呼び方：python -X utf8 -I q_shares.py <出力フォルダ> base=<生> n1=<生> n2=<生> armB=<生>
import sys, os, math, statistics as st
from collections import defaultdict
outd = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
def fl(s):
    try: return float(s)
    except ValueError: return float('nan')
out = open(os.path.join(outd, 'q_shares.txt'), 'w', encoding='utf-8', newline='\n')
for lb, ins in arms:
    per = defaultdict(list)
    for seed in range(1, 11):
        p = os.path.join(ins, f'rulepower_s{seed}.tsv')
        if not os.path.exists(p): continue
        acc = defaultdict(lambda: [0.0] * 10)   # q -> [n, s0..s3, capN, cap0..cap3]  ＋ local>owner は別
        lo = defaultdict(int); seen = set()
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inn = ix['quarter'], ix['nation']; ic = [ix[f'nowRule{k}Share'] for k in range(4)]
            for ln in f:
                c = ln.rstrip('\n').split('\t'); q = int(c[iq]); v = [fl(c[i]) for i in ic]
                if any(math.isnan(x) for x in v): continue
                a = acc[q]; a[0] += 1
                for k in range(4): a[1 + k] += v[k]
                if v[1] > v[0]: lo[q] += 1
                key = (q, c[inn])
                if key not in seen:
                    seen.add(key); a[5] += 1
                    for k in range(4): a[6 + k] += v[k]
        qs = sorted(acc)
        for k in range(4):
            per[f'mean{k}'].append(sum(acc[q][1 + k] / acc[q][0] for q in qs) / len(qs))
            per[f'cap{k}'].append(sum(acc[q][6 + k] / acc[q][5] for q in qs) / len(qs))
        per['localGtOwner'].append(sum(lo[q] for q in qs) / len(qs))
        per['regions'].append(sum(acc[q][0] for q in qs) / len(qs))
    out.write(f'{lb}\t' + '\t'.join(f'{k} {st.median(v):.4f}' for k, v in per.items()) + '\n')
out.close()
