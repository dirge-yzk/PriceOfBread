# (1071) 影の表 rulepower_s・rulepeace_s の読み手（読むだけ）。使い方: python an_rp.py <表のフォルダ> [種...]
# 出す物（段 C′ 以降の門の材料）：①開幕の最初の観測の行の 〈今の最大の区分〉と〈影の最大の区分〉の件数 ②国ごとの O の世界の取り分（種の中央値・四半期の平均）
# ③講和ごとの取る州の数・φ* の中央 ④平時に外へ向けられる国（O>0）の数と q ⑤被覆 L÷ΣN の分布（開幕）と 助走の直後の L の跳び ⑥自己検査（開幕の差の最大・4 区分の和）
import csv, sys, collections, statistics as st, os, glob
d = sys.argv[1]; seeds = [int(s) for s in sys.argv[2:]] or sorted(int(os.path.basename(p)[len('rulepower_s'):-4]) for p in glob.glob(os.path.join(d, 'rulepower_s*.tsv')))
def f(x):
    try: return float(x)
    except: return float('nan')
agg = collections.defaultdict(list); first_now = collections.Counter(); first_sh = collections.Counter(); diffmax = 0.0; sumdev = 0.0; qual = 0
oshare = collections.defaultdict(list); peace = []; phis = []; nOpen = []; qv = []; cover = []; ljump = []
for s in seeds:
    p = os.path.join(d, f'rulepower_s{s}.tsv')
    if not os.path.exists(p): continue
    rows = list(csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'))
    seen = set(); lq = {}; qs = {}
    perq = collections.defaultdict(dict)
    for x in rows:
        k = x['region']
        if k not in seen:
            seen.add(k); first_now[x['nowBestClass']] += 1; first_sh[x['bestClass']] += 1
            if x['openQualFlag'] == '1': qual += 1; diffmax = max(diffmax, f(x['openDiffIndex']))
        v = f(x['classSumShare'])
        if v == v: sumdev = max(sumdev, abs(v - 1))
        perq[int(x['quarter'])][x['nation']] = (f(x['lNationIndex']), f(x['oNationIndex']), f(x['oWorldShare']), f(x['nIndex']), f(x['hNationIndex']), f(x['qIndex']))
    qk = sorted(perq)
    for q in qk:
        for n, (L, O, sh, N, H, qq) in perq[q].items():
            oshare[n].append(sh)
    q0 = qk[0]; qv.append(next(iter(perq[q0].values()))[5])
    nOpen.append([sum(1 for v in perq[q].values() if v[1] > 0) for q in qk[:1] + qk[len(qk)//2:len(qk)//2+1] + qk[-1:]])
    for q in qk[1:6]:
        for n, v in perq[q].items():
            pv = perq[qk[0]].get(n)
            if pv and pv[0] > 0: ljump.append(v[0] / pv[0])
    pp = os.path.join(d, f'rulepeace_s{s}.tsv')
    if os.path.exists(pp):
        for x in csv.DictReader(open(pp, encoding='utf-8'), delimiter='\t'):
            peace.append((s, x['winner'], x['loser'], int(x['loserRegionCount']), int(x['takenCount']), int(x['greatCircleOnlyTakenCount']), f(x['phiStarMedianIndex'])))
print('開幕の最初の観測の行：今の最大の区分', dict(first_now), '／影の最大の区分', dict(first_sh))
print('開幕の自己検査：当たる州', qual, '差の最大', diffmax, '／4 区分の和の最大の逸脱', sumdev)
print('q（開幕の世界の陸軍力÷人口K）', [round(v, 6) for v in qv])
print('O>0 の国の数（最初・中間・最後の四半期）', nOpen)
top = sorted(((st.mean([v for v in vs if v == v] or [0]), n) for n, vs in oshare.items()), reverse=True)[:8]
print('O の世界の取り分の平均の上位', [(n, round(m, 3)) for m, n in top])
if peace:
    tk = [p[4] for p in peace]; print('講和', len(peace), '件・取る州の数の中央', st.median(tk), '平均', round(st.mean(tk), 2), '0 件の割合', round(sum(1 for t in tk if t == 0) / len(tk), 2),
        '大圏だけで数えた取る州の中央', st.median([p[5] for p in peace]), 'φ* 中央の中央', round(st.median([p[6] for p in peace if p[6] == p[6]] or [float('nan')]), 3))
else: print('講和 0 件')
if ljump: print('開幕後 5 四半期の L の比（対 最初の観測）の中央/最大', round(st.median(ljump), 3), round(max(ljump), 3))
