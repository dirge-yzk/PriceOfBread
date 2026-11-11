# (893) c76lg 読むだけ：事件の政変（crownpc_s regimeEventFlag）の四半期に、土地改革の法・参政権・報道が動いたかを reform_s で数える。引力点が軍政・プロ独・無政府の行も数える。
# 使い方: python -X utf8 s15_events.py <生のフォルダ>
import sys, os, collections
d = sys.argv[1]
P = ['絶対', '専制', '幕府', '立憲', '共和', 'ブル独', 'プロ独', '軍政', '無政府']
def rows(t, s):
    p = os.path.join(d, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return []
    L = open(p, 'rb').read().decode('utf-8').split('\n'); h = L[0].rstrip('\r').split('\t')
    return [dict(zip(h, l.rstrip('\r').split('\t'))) for l in L[1:] if l.strip()]
att = collections.Counter(); ev_to = collections.Counter(); lr = collections.Counter(); suf = collections.Counter(); cen = collections.Counter(); seen = collections.Counter()
for s in range(1, 11):
    for r in rows('coupgate', s): att[P[int(r['attractor'])]] += 1
    rf = {(r['nation'], int(r['quarter'])): r for r in rows('reform', s)}
    for r in rows('crownpc', s):
        if r['regimeEventFlag'] != '1': continue
        to = P[int(r['polityCode'])]; ev_to[to] += 1
        k = (r['nation'], int(r['quarter'])); a = rf.get(k); b = rf.get((k[0], k[1] - 1))
        if a is None or b is None: continue
        seen[to] += 1
        if int(a['landReformLawCode']) > int(b['landReformLawCode']): lr[to] += 1
        if a['suffrageLawCode'] != b['suffrageLawCode']: suf[to] += 1
        if a['censorshipLawCode'] != b['censorshipLawCode']: cen[to] += 1
print('coupgate の引力点（第 1 の門を通った行）', dict(att.most_common()))
print('事件の政変の行き先', dict(ev_to.most_common()))
print('reform_s に前の四半期の行がある事件（挙げた国）', dict(seen.most_common()))
print('  土地改革の法が上がった', dict(lr.most_common()), '・参政権が動いた', dict(suf.most_common()), '・報道が動いた', dict(cen.most_common()))
sp = 0
for s in range(1, 11):
    for r in rows('reform', s):
        try:
            if float(r['spendStockAppliedIndex']) > 0 or float(r['spendLegitOnlyIndex']) > 0: sp += 1
        except: pass
print('reform_s で押し切りの代価を記帳した行（spendStockApplied か spendLegitOnly が正）', sp)
