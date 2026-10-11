# (268) 反証役 vi 低〜中：帯の選択（an_i265.py の band_step）と今の総動員（mobilizedAtDecisionFlag）が
# 食い違う行を全ての国の nation_s から出す（読むだけ）。an_i265.py の c_band と同じ回し方（prev は判断の来ない
# 四半期をまたいで持ち越す）。行ごとに、前の判断の四半期との間隔・規模と存亡の生の文字列・線との距離を出す。
import sys, os, glob, re, collections
D = sys.argv[1]
UK = 'イギリス'
UP_S, UP_L = 1.2, 0.5
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def step(prev, at_war, s, l):
    if not at_war: return 0
    if s >= UP_S or l > UP_L: return 1
    if s < 1.0 / UP_S and not (l > 0): return 0
    return prev
cols = ['quarter', 'nationIndex', 'nation', 'atWarAtDecisionFlag', 'warScaleRatioAtDecision', 'homeLosingScoreAtDecision', 'mobilizedAtDecisionFlag']
out = []; kinds = collections.Counter()
for f in sorted(glob.glob(os.path.join(D, 'nation_s*.tsv')), key=lambda x: int(re.findall(r'_s(\d+)\.tsv', x)[0])):
    sd = int(re.findall(r'_s(\d+)\.tsv', f)[0])
    by = collections.defaultdict(list)
    with open(f, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t'); ix = {c: head.index(c) for c in cols}
        for ln in fh:
            p = ln.rstrip('\n').split('\t')
            if len(p) != len(head): continue
            by[p[ix['nationIndex']]].append({c: p[ix[c]] for c in cols})
    for ni, rs in by.items():
        rs.sort(key=lambda r: int(fl(r['quarter'])))
        prev, lastq = 0, None
        for r in rs:
            fwar = fl(r['atWarAtDecisionFlag'])
            if not (fwar >= 0): continue
            at_war = fwar == 1
            s, l = fl(r['warScaleRatioAtDecision']), fl(r['homeLosingScoreAtDecision'])
            ch = step(prev, at_war, s, l); mob = fl(r['mobilizedAtDecisionFlag']) == 1
            q = int(fl(r['quarter']))
            if at_war and (ch == 1) != mob:
                gap = None if lastq is None else q - lastq
                near = []
                if abs(s - UP_S) < 1e-6: near.append('規模≈1.2')
                if abs(s - 1.0 / UP_S) < 1e-6: near.append('規模≈1/1.2')
                if abs(l - UP_L) < 1e-6: near.append('存亡≈0.5')
                if 0 < l < 1e-9: near.append('存亡≈0+')
                k = ('選択1・総動員なし' if ch == 1 else '選択0・総動員中') + ('／' + '・'.join(near) if near else '') + ('／判断の間隔' + str(gap) if gap not in (None, 1) else '') + ('／前は未選択の持ち越し' if ch == prev else '')
                kinds[k] += 1
                out.append(f'{sd}\t{q}\t{ni}\t{r["nation"]}\t{"英" if r["nation"] == UK else "他"}\t{ch}\t{int(mob)}\tprev={prev}\tgap={gap}\tscale={r["warScaleRatioAtDecision"]}\tlosing={r["homeLosingScoreAtDecision"]}\t{k}')
            prev, lastq = ch, q
print('食い違いの行の数', len(out))
for k, v in kinds.most_common(): print(v, k)
print('種\t四半期\tnationIndex\t国\t英/他\t選択\t総動員\t前の選択\t間隔\t規模（印字）\t存亡（印字）\t分類')
for o in out: print(o)
