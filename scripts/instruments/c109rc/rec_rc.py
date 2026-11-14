# (1123) c109rc 記録（事前登録 §8）：rulepower_s から、外国の取り分＞0 の州の数・Rule[1]＞Rule[0] の州の数・外国の取り分の和・持ち主の取り分の平均を、
# 腕と基準（とヌル腕）で並べる（種ごとに全ての四半期の平均→種の中央）。表を読むだけ。
# 使い方: python -X utf8 -I rec_rc.py 名=<生のフォルダ> ...
import sys, os, statistics as st
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
print('# (1123) 記録 §8：種ごとに四半期の平均→種の中央（外国の取り分＞0 の州の数・Rule1＞Rule0 の州の数・外国の取り分の和・持ち主の取り分 nowRule0 の州の平均・最後の四半期の外国の取り分＞0 の州の数）')
print('腕\t外国＞0 の州\tRule1＞Rule0 の州\t外国の取り分の和\t持ち主の取り分の平均\t最後の四半期の外国＞0\t種')
for a in sys.argv[1:]:
    name, folder = a.split('=', 1)
    cols = [[], [], [], [], []]
    for s in range(1, 11):
        p = os.path.join(folder, f'rulepower_s{s}.tsv')
        if not os.path.exists(p): continue
        q_f = {}; q_l = {}; q_sum = {}; q_o = {}; q_n = {}
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
            i0, i1, i2 = ix['nowRule0Share'], ix['nowRule1Share'], ix['nowRule2Share']
            for line in f:
                c = line.rstrip('\n').split('\t'); q = int(c[0])
                r0, r1, r2 = fl(c[i0]), fl(c[i1]), fl(c[i2])
                q_f[q] = q_f.get(q, 0) + (1 if r2 > 0 else 0)
                q_l[q] = q_l.get(q, 0) + (1 if r1 > r0 else 0)
                q_sum[q] = q_sum.get(q, 0.0) + (r2 if r2 == r2 else 0.0)
                q_o[q] = q_o.get(q, 0.0) + (r0 if r0 == r0 else 0.0); q_n[q] = q_n.get(q, 0) + 1
        if not q_f: continue
        qs = sorted(q_f)
        cols[0].append(sum(q_f.values()) / len(qs)); cols[1].append(sum(q_l.values()) / len(qs)); cols[2].append(sum(q_sum.values()) / len(qs))
        cols[3].append(sum(q_o[q] / q_n[q] for q in qs) / len(qs)); cols[4].append(q_f[qs[-1]])
    if cols[0]: print(f'{name}\t' + '\t'.join(f'{st.median(c):.4g}' for c in cols) + f'\t{len(cols[0])}')
    else: print(f'{name}\t(表なし)')
