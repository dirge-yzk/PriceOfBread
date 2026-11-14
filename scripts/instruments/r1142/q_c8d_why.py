# (1142) 腕 B（C′ の 1 回目）の C8d＞0 の内訳：平時・O＞0 の国で L が 0.8 倍以下に落ちた国×四半期（落ち）と 0.95〜1.05 倍（変わらない）の、
#   4 四半期の差＝持ち主・在地・外国・無統治の取り分の国の平均と、H・O・q の比（エンジンの列 hEngNationIndex・oEngNationIndex・qEngIndex）。
# 呼び方：python -X utf8 -I q_c8d_why.py <出力フォルダ> <腕 B の生>
import sys, os, math, statistics as st
from collections import defaultdict
outd, ins = sys.argv[1], sys.argv[2]
def fl(s):
    try: return float(s)
    except ValueError: return float('nan')
def fin(*v): return all(not (math.isnan(x) or math.isinf(x)) for x in v)
acc = {'drop': defaultdict(list), 'stab': defaultdict(list)}
for seed in range(1, 11):
    p = os.path.join(ins, f'rulepower_s{seed}.tsv')
    natq = defaultdict(dict)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, inn = ix['quarter'], ix['nation']
        cols = ['nowRule0Share', 'nowRule1Share', 'nowRule2Share', 'nowRule3Share']
        ic = [ix[k] for k in cols]
        iw, ie, iL, iH, iO, iQ = ix['wNationIndex'], ix['wEngageNationIndex'], ix['lNationIndex'], ix['hEngNationIndex'], ix['oEngNationIndex'], ix['qEngIndex']
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            q = int(c[iq]); nat = c[inn]
            e = natq[q].get(nat)
            if e is None:
                e = natq[q][nat] = {'L': fl(c[iL]), 'H': fl(c[iH]), 'O': fl(c[iO]), 'q': fl(c[iQ]), 'peace': fl(c[iw]) == 0 and fl(c[ie]) == 0, 's': [0.0] * 4, 'n': 0}
            v = [fl(c[i]) for i in ic]
            if fin(*v):
                for k in range(4): e['s'][k] += v[k]
                e['n'] += 1
    qs = sorted(natq)
    for qi, q in enumerate(qs):
        if qi < 4: continue
        for nat, v in natq[q].items():
            back = [natq[qs[qi - j]].get(nat) for j in range(1, 5)]
            if any(b is None for b in back) or v['n'] == 0 or back[3]['n'] == 0: continue
            if not (v['peace'] and all(b['peace'] for b in back)): continue
            mL = sum(b['L'] for b in back) / 4
            if not (mL > 0 and fin(v['L'], v['O'])) or not (v['O'] > 0): continue
            r = v['L'] / mL
            g = 'drop' if r <= 0.8 else ('stab' if 0.95 <= r <= 1.05 else None)
            if g is None: continue
            b = back[3]; a = acc[g]
            for k in range(4): a[f'd{k}'].append(v['s'][k] / v['n'] - b['s'][k] / b['n'])
            for k in ('H', 'O', 'q', 'L'):
                if fin(v[k], b[k]) and b[k] > 0: a[f'r{k}'].append(v[k] / b[k])
            a['nat'].append(nat)
with open(os.path.join(outd, 'q_c8d_why.txt'), 'w', encoding='utf-8', newline='\n') as out:
    for g in ('drop', 'stab'):
        a = acc[g]; n = len(a['d0'])
        out.write(f'## {g} 国×四半期 {n}（10 種の和）\n')
        for k in ('d0', 'd1', 'd2', 'd3'): out.write(f'  {k} 平均 {sum(a[k]) / n:.5f}\n')
        for k in ('rH', 'rO', 'rq', 'rL'): out.write(f'  {k} 中央 {st.median(a[k]):.4f}（{len(a[k])}）\n')
        cnt = defaultdict(int)
        for x in a['nat']: cnt[x] += 1
        out.write('  国 ' + '・'.join(f'{k} {v}' for k, v in sorted(cnt.items(), key=lambda t: -t[1])[:8]) + '\n')
    out.write('差（落ち−変わらない）：' + ' '.join(f'{k} {sum(acc["drop"][k]) / len(acc["drop"][k]) - sum(acc["stab"][k]) / len(acc["stab"][k]):.5f}' for k in ('d0', 'd1', 'd2', 'd3')) + '\n')
