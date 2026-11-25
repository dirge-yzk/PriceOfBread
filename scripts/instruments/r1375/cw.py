# (1375) rw：cwend_s（keep_1369/base_ins）の内戦の終わり 84 件を、崩壊の条件の案ごとに分ける（読むだけ）
# 使い方: python cw.py <base_ins のフォルダ>
import sys, glob, os, math, collections
RSW = 0.4  # RevolutionSurrenderWeight（Coefficients.p06_rank_discovery.cs:43）＝revCut から蜂起の圧を戻すため
d = sys.argv[1]
rows = []
for f in sorted(glob.glob(os.path.join(d, 'cwend_s*.tsv'))):
    seed = f.rsplit('_s', 1)[1].split('.')[0]
    with open(f, encoding='utf-8') as fh:
        hdr = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            v = dict(zip(hdr, ln.rstrip('\n').split('\t')))
            v['seed'] = seed
            rows.append(v)
def fl(x):
    try: return float(x)
    except: return float('nan')
n = len(rows)
print('records', n)
c = collections.Counter()
for r in rows:
    S = fl(r['scoreAtPeace']); col = r['collapseFlag'] == '1'
    exP, exR = fl(r['exhParent']), fl(r['exhRebel'])
    cutP, cutR = fl(r['revCutParent']), fl(r['revCutRebel'])
    rpP, rpR = (1 - cutP) / RSW, (1 - cutR) / RSW
    kP, kR = 1 - rpP, 1 - rpR   # 案③b の K＝1−蜂起の圧
    m = int(r['months'])
    colP = exP >= cutP; colR = exR >= cutR
    cls = ('collapse' if col else 'nocollapse') + '/' + r['endPath']
    if col and abs(S) < 1: cls += '/reach'
    c[cls] += 1
    if col:
        c['col_side=' + ('both' if colP and colR else 'parent' if colP else 'rebel')] += 1
        if colP and colR: c['col_both_months<=3' if m <= 3 else 'col_both_months>3'] += 1
        c['col_months<=1' if m <= 1 else 'col_months<=3' if m <= 3 else 'col_months>3'] += 1
        # 誰が負けたか（S の符号＝A 側の勝ち；A が親かどうかは不明なので親の崩壊と負けの一致は記録しない）
    c['capRising=' + r['capitalRisingFlag']] += 1
    if r['capitalRisingFlag'] == '0': c['capRising0_' + ('collapse' if col else 'nocollapse')] += 1
    inh = fl(r['inheritedExhaustion'])
    if col and inh >= 0.99 and m <= 3: c['col_inherited>=0.99_m<=3'] += 1
    # 案③b の今の月の判定（崩壊の代入が無い行だけ＝S が本物）
    if not col:
        lose_k = kR if S > 0 else kP if S < 0 else min(kP, kR)  # 符号は A 側の向き＝A が親か反乱かは cwend に無い→両方を試す
        hit_any = abs(S) >= min(kP, kR); hit_both = abs(S) >= max(kP, kR)
        c['nocol_3b_hit_if_loserK=min'] += hit_any; c['nocol_3b_hit_if_loserK=max'] += hit_both
    c['rpP>=0.5'] += rpP >= 0.5; c['rpR>=0.5'] += rpR >= 0.5
    c['kP+kR<=1'] += (kP + kR) <= 1
rp = sorted((1 - fl(r['revCutParent'])) / RSW for r in rows); rr = sorted((1 - fl(r['revCutRebel'])) / RSW for r in rows)
q = lambda a, p: a[min(len(a) - 1, int(p * len(a)))]
print('rpParent med/p90', round(q(rp, .5), 3), round(q(rp, .9), 3), ' rpRebel med/p90', round(q(rr, .5), 3), round(q(rr, .9), 3))
for k in sorted(c): print(k, c[k])
