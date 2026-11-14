# (1133) 問い1の続き・問い2：旗 1 の腕 B で、取り分 ≧0.05・≧0.25 の州を出どころ別に（初出の出どころ・跳び〔講和の付与〈推〉〕の後は〈講和の付与〉へ）、
# 跳びの後の取り分の道（+4・+20・+40・+80 四半期）を〈寄せだけで目標 0 のときの減り 0.975^n〉と比べ、その間にエンジンの外国の力（fForeignEngIndex）があった四半期の割合を出す。
# 使い方: python -X utf8 -I q2_jumps.py <out/q1> <腕 B の ins>
import sys, os, statistics as st
from collections import defaultdict
d, ins = sys.argv[1], sys.argv[2]
def fnum(s):
    try: return float(s)
    except ValueError: return float('nan')
cls_of = defaultdict(dict)
with open(os.path.join(d, 'onsets_armB_all.tsv'), encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for ln in f:
        c = ln.rstrip('\n').split('\t')
        if c[ix['kind']] == 'first': cls_of[int(c[ix['seed']])][c[ix['region']]] = c[ix['class']]
jumps = defaultdict(list)
with open(os.path.join(d, 'jumps_armB_all.tsv'), encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for ln in f:
        c = ln.rstrip('\n').split('\t'); jumps[int(c[ix['seed']])].append((int(c[ix['quarter']]), c[ix['region']], c[ix['nation']], c[ix['tier']], int(c[ix['port']]), float(c[ix['prev']]), float(c[ix['value']]), int(c[ix['ownerAtWar']])))
CL = ['開幕データ', '開幕の表', 'エンジン平時', 'エンジン戦時', '講和の付与']
avg = {t: defaultdict(list) for t in (0.05, 0.25)}
traj = []
for seed in range(1, 11):
    jq = defaultdict(list)
    for j in jumps[seed]: jq[j[1]].append(j[0])
    series = defaultdict(dict)
    want = set(jq)
    cnt = {t: defaultdict(int) for t in (0.05, 0.25)}; nq = set()
    with open(os.path.join(ins, f'rulepower_s{seed}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, ir, i2, ie = ix['quarter'], ix['region'], ix['nowRule2Share'], ix['fForeignEngIndex']
        for ln in f:
            c = ln.rstrip('\n').split('\t'); q = int(c[iq]); r = c[ir]; F = fnum(c[i2]); nq.add(q)
            if r in want: series[r][q] = (F, fnum(c[ie]) > 0)
            cl = '講和の付与' if any(q >= x for x in jq.get(r, [])) else cls_of[seed].get(r, '?')
            for t in (0.05, 0.25):
                if F >= t: cnt[t][cl] += 1
    for t in (0.05, 0.25):
        for cl in CL + ['?']: avg[t][cl].append(cnt[t][cl] / max(1, len(nq)))
    for (q0, r, n, tier, pt, pv, v, aw) in jumps[seed]:
        row = [seed, q0, r, n, tier, pt, f'{pv:.4g}', f'{v:.4g}', aw]
        for k in (4, 20, 40, 80):
            x = series[r].get(q0 + k)
            row += [f'{x[0]:.4g}' if x else '', f'{v * 0.975 ** k:.4g}']
        span = [series[r][q][1] for q in range(q0 + 1, q0 + 41) if q in series[r]]
        row.append(f'{sum(span) / len(span):.3f}' if span else '')
        traj.append(row)
with open(os.path.join(d, 'q2_jumps_traj.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('seed\tquarter\tregion\tnation\ttier\tport\tprev\tvalue\townerAtWar\tF+4\tdecay+4\tF+20\tdecay+20\tF+40\tdecay+40\tF+80\tdecay+80\tengForceShareNext40\n')
    for row in traj: f.write('\t'.join(map(str, row)) + '\n')
with open(os.path.join(d, 'q1_thresh_by_class.txt'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('腕 B（旗 1）・全ての国の州・四半期の平均の州の数（種の中央）\n出どころ\t取り分≧0.05\t取り分≧0.25\n')
    for cl in CL + ['?']:
        f.write(f'{cl}\t{st.median(avg[0.05][cl]):.4g}\t{st.median(avg[0.25][cl]):.4g}\n')
# 跳びの後の道の要約
def ratio(i, j):
    xs = [float(r[i]) / float(r[j]) for r in traj if r[i] != '' and float(r[j]) > 0]
    return (st.median(xs), len(xs)) if xs else (float('nan'), 0)
with open(os.path.join(d, 'q2_summary.txt'), 'w', encoding='utf-8', newline='\n') as f:
    f.write(f'跳び（≧+0.1・講和の付与〈推〉）の数 {len(traj)}（10 種の和）\n')
    for k, (i, j) in zip((4, 20, 40, 80), ((9, 10), (11, 12), (13, 14), (15, 16))):
        m, n = ratio(i, j); f.write(f'+{k} 四半期：実際の取り分÷〈目標 0 の減り〉の中央 {m:.3g}（{n} 件）\n')
    es = [float(r[17]) for r in traj if r[17] != '']
    f.write(f'跳びの後 40 四半期でその州にエンジンの外国の力があった四半期の割合の中央 {st.median(es):.3g}（{len(es)} 件）\n')
print(open(os.path.join(d, 'q1_thresh_by_class.txt'), encoding='utf-8').read()); print(open(os.path.join(d, 'q2_summary.txt'), encoding='utf-8').read())
