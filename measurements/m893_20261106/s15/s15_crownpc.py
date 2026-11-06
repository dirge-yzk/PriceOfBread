# (893) c76lg 読むだけ：crownpc_s（全ての国×四半期）と scalars_s から、S15 の政変・下賜・政権への失望の床・信任帝政の床を数える。
# 使い方: python -X utf8 s15_crownpc.py <生のフォルダ>
import sys, os, collections, math
d = sys.argv[1]
P = ['絶対', '専制', '幕府', '立憲', '共和', 'ブル独', 'プロ独', '軍政', '無政府']
def rows(t, s):
    p = os.path.join(d, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    L = open(p, 'rb').read().decode('utf-8').split('\n'); h = L[0].rstrip('\r').split('\t')
    return [dict(zip(h, l.rstrip('\r').split('\t'))) for l in L[1:] if l.strip()]
def f(x):
    try: return float(x)
    except: return float('nan')
ev = collections.Counter(); ev_seed = collections.Counter(); grant = 0; grant_moved = 0; rl = collections.Counter()
rrc_gt1 = 0; n = 0; rrc_max = 0.0; floor_vals = []; ord_bind = 0; mand_bind = 0; mand_cond3 = 0
for s in range(1, 11):
    C = rows('crownpc', s)
    if C is None: print('crownpc 無い', s); continue
    S = rows('scalars', s) or []
    sc = {(r['nation'], r['quarter']): r for r in S}
    for r in C:
        n += 1
        pc, pp = r['polityCode'], r['polityPrevCode']
        if r['regimeEventFlag'] == '1':
            ev[(P[int(pp)] if pp.isdigit() else pp, P[int(pc)])] += 1; ev_seed[s] += 1
        if r['relabelFlag'] == '1' and pp.isdigit(): rl[(P[int(pp)], P[int(pc)])] += 1
        if r['grantFlag'] == '1':
            grant += 1
            if f(r['pcNowShare']) > f(r['pcPrevShare']) + 1e-12: grant_moved += 1
        k = f(r['recentRegimeChangesIndex'])
        if k > 1: rrc_gt1 += 1; floor_vals.append(1 - 1 / k)
        if k > rrc_max: rrc_max = k
        x = sc.get((r['nation'], r['quarter']))
        if x is not None:
            L = f(x['legitimacy']); D = f(x['dynMandate']); ds = f(x['defeatShock']) / 0.85
            fl = (1 - 1 / k if k > 1 else 0) * (1 - min(1, max(0, ds)))
            if fl > 0 and abs(L - fl) < 1e-6: ord_bind += 1
            if x['polityCode'] == '3' and x['crown'] != '0' and D >= 0.6:
                mand_cond3 += 1
                if abs(L - 0.6 * D * (1 - min(1, max(0, ds)))) < 1e-6: mand_bind += 1
floor_vals.sort()
print(f'crownpc 行 {n}')
print(f'事件の政変（regimeEventFlag=1）計 {sum(ev.values())}・種ごと', dict(sorted(ev_seed.items())))
print('  前→後', dict(ev.most_common(20)))
print(f'床交差の再ラベル（relabelFlag=1）計 {sum(rl.values())}', dict(rl.most_common(10)))
print(f'下賜の印 grantFlag=1 {grant}・そのうち議会の統制力が上がった行 {grant_moved}')
print(f'直近の政変の数＞1（正統性の床 1−H＞0）の行 {rrc_gt1}・最大 {rrc_max:.3f}・床の p50 {floor_vals[len(floor_vals)//2] if floor_vals else 0:.3f} p95 {floor_vals[int(.95*(len(floor_vals)-1))] if floor_vals else 0:.3f}')
print(f'  scalars と突き合わせ：正統性＝(1−H)×(1−敗戦ショック÷0.85) に 1e-6 で一致（床が噛んだ・近似）{ord_bind}')
print(f'  信任帝政：立憲×王冠×天命≥0.6 {mand_cond3}・正統性＝0.6×天命×(1−敗戦ショック÷0.85) に 1e-6 で一致（床が噛んだ・近似） {mand_bind}')
