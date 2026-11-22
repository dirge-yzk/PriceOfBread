# (1299) 問い1の道：首都が植民地格になった区間ごとに、直前の四半期の首都・その格・同じ四半期の州の移り（cederule_s）を並べる。
# 使い方: python -X utf8 -I q1b_trace.py <生のフォルダ> <q1 の runs.tsv> <出力>
import sys, csv, os, collections

raw, runsp, out = sys.argv[1], sys.argv[2], sys.argv[3]

def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: yield ix, row

runs = list(rd(runsp))
bySeed = collections.defaultdict(list)
for ix, row in runs: bySeed[int(row[ix['seed']])].append({k: row[i] for k, i in ix.items()})
lines = []
for s, rs in sorted(bySeed.items()):
    names = {r['nation'] for r in rs}
    caps = {}    # (q, nation) -> (cap, tier, list)
    for ix, row in rd(os.path.join(raw, f'region_s{s}.tsv')):
        n = row[ix['nation']]
        if n not in names: continue
        k = (int(row[ix['quarter']]), n)
        if k not in caps: caps[k] = [row[ix['region']], row[ix['tier']], []]
        caps[k][2].append((row[ix['region']], row[ix['tier']], float(row[ix['factoryOutputRefValuePerMonth']] or 0) + float(row[ix['extractionOutputRefValuePerMonth']] or 0), float(row[ix['populationK']])))
    ced = collections.defaultdict(list)
    cp = os.path.join(raw, f'cederule_s{s}.tsv')
    if os.path.exists(cp):
        for ix, row in rd(cp):
            q = int(row[ix['quarter']])
            for n in (row[ix['sender']], row[ix['receiver']]):
                if n in names: ced[(q, n)].append(f"{row[ix['how']]}:{row[ix['region']]}({row[ix['tierBefore']]}→{row[ix['tierAfter']]}){row[ix['sender']]}→{row[ix['receiver']]}")
    for r in rs:
        n = r['nation']; q0 = int(r['q0']); q1 = int(r['q1'])
        prev = caps.get((q0 - 1, n)); cur = caps.get((q0, n)); nxt = caps.get((q1 + 1, n))
        ev = []
        for q in range(q0 - 1, q0 + 1): ev += [f'q{q} ' + e for e in ced.get((q, n), [])]
        evEnd = []
        for q in range(q1, q1 + 2): evEnd += [f'q{q} ' + e for e in ced.get((q, n), [])]
        # 首都の州の産出の順位（植民地格の首都が本国格の州より産出が大きいか）
        rank = ''
        if cur:
            lst = sorted(cur[2], key=lambda x: -x[2])
            capOut = cur[2][0][2]
            bestHome = max((x for x in cur[2] if x[1] != 'Colonial'), key=lambda x: x[2], default=None)
            rank = f"首都の産出 {capOut:.0f}・本国格の最大 {bestHome[0] + ' ' + format(bestHome[2], '.0f') if bestHome else 'なし'}・産出1位 {lst[0][0]}"
        lines.append(f"seed {s} {n} 首都 {r['capital']} q{q0}〜q{q1}（{r['y0']}〜{r['y1']}・{r['quarters']} 四半期・内戦 {r['civilWarQuarters']}・本国格の州の最大 {r['homeRegionsMax']}・国の初出 q{r['nationFirstQuarter']}）")
        lines.append(f"  直前 q{q0-1} の首都: {prev[0] + '(' + prev[1] + ')' if prev else '国なし'}／終わりの次 q{q1+1}: {nxt[0] + '(' + nxt[1] + ')' if nxt else '国なし'}")
        lines.append(f"  {rank}")
        lines.append(f"  始まりの移り: {' ; '.join(ev) if ev else 'なし'}")
        lines.append(f"  終わりの移り: {' ; '.join(evEnd) if evEnd else 'なし'}")
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(lines) + '\n')
print(len(lines))
