# (592) 合流の記録（読むだけ）：発見の解禁の年・国ごとの1年の窓の固まり（G3b）・戦争の数・為替と国債の金利の経路・全ての国の価格水準・英とプロイセンの軍需の着工・奨学金の国の数
# 使い方：PYTHONUTF8=1 python rec592.py <腕名>=<計器の出力フォルダ（…/ins）> ...（種は 1〜10）
import csv, os, sys, math, statistics as st
csv.field_size_limit(10**9)
arms = [a.split('=', 1) for a in sys.argv[1:]]
NINE = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア', 'オスマン', 'オーストリア']
ROWS = ['蒸気機関', '汽船', '機関車', '転炉鋼', '石炭火力', '電動機']
MILADV = {'火砲', '小銃', '弾薬', '帆船', '軍艦', '汽船', '機関車', '電化製品', '送電網', '電力', '機関銃', '戦車', '装甲艦', '弩級戦艦'}   # ukf579/g_lines.py の G1 と同じ集合
SEEDS = range(1, 11)
def rd(d, name, s):
    p = os.path.join(d, f'{name}_s{s}.tsv')
    if not os.path.exists(p): return None, iter(())
    fh = open(p, encoding='utf-8-sig', newline=''); r = csv.reader(fh, delimiter='\t'); h = next(r)
    return {n: i for i, n in enumerate(h)}, r
def fl(v):
    try: return float(v)
    except: return float('nan')
def med(v):
    v = [x for x in v if x == x]; return st.median(v) if v else float('nan')
def base(n):   # 変名（formable）の国名は元の名前を含む
    for m in NINE:
        if m in n: return m
    return None
for lab, d in arms:
    print(f'######## {lab}  {d}')
    # ① 解禁の年（disc_s の E 行・開幕の既知〔四半期 0〕は 1783）
    first = {}   # (s, nation, row) -> year
    opened = {}  # (s, nation) -> [quarter]（四半期 0 を除く）
    for s in SEEDS:
        ix, r = rd(d, 'disc', s)
        if ix is None: continue
        for x in r:
            if x[ix['kind']] != 'E': continue
            q = int(x[ix['quarter']]); y = int(x[ix['year']]); n = x[ix['nation']]; row = x[ix['row']]
            first.setdefault((s, n, row), y)
            if q > 0: opened.setdefault((s, n), []).append(q)
    print('# ① 解禁の年（種の中央値・未解禁の種は数えない・括弧は解禁した種の数／10）')
    for row in ROWS:
        cells = []
        for n in NINE:
            ys = [first[(s, m, row)] for s in SEEDS for m in [k[1] for k in first if k[0] == s and k[2] == row and base(k[1]) == n][:1]]
            cells.append(f'{n} {med(ys):.0f}({len(ys)})' if ys else f'{n} 未か開幕の既知(0)')
        oth = [sum(1 for (ss, m, rr), y in first.items() if ss == s and rr == row and base(m) is None and y <= 1900) for s in SEEDS]
        print(f'  {row}：' + '・'.join(cells) + f'｜周辺の国で 1900 年までに解禁した国の数（種の中央値）{med(oth):.1f}')
    # ② G3b（国ごとに 4 四半期の窓に開いた行の数の最大・四半期 0 を除く）
    cl = {}
    for (s, n), qq in opened.items():
        b = base(n)
        if b is None: continue
        qq.sort(); best = max(sum(1 for x in qq[i:] if x <= q0 + 4) for i, q0 in enumerate(qq))
        cl.setdefault(b, []).append(best)
    allv = [v for vs in cl.values() for v in vs]
    print('# ② G3b（disc_s の E 行から・国×種の 1 年の窓の最大）：中央 ' + (f'{med(allv):.1f}・最大 {max(allv)}' if allv else 'nan') + '｜' + '・'.join(f'{n} {med(cl.get(n, [])):.1f}' for n in NINE))
    # ③ 戦争の数（wars_s・種の中央値）
    iw, cw = [], []
    for s in SEEDS:
        ix, r = rd(d, 'wars', s)
        if ix is None: continue
        a = b = 0
        for x in r:
            if x[ix['civilWarFlag']] == '1': b += 1
            else: a += 1
        iw.append(a); cw.append(b)
    print(f'# ③ 戦争の数（種の中央値）：国家間 {med(iw):.1f}・内戦 {med(cw):.1f}')
    # ④ 為替・国債の金利の経路（nation_s・年の最後の四半期・9 国・種の中央値）と全ての国の価格水準
    YS = [1800, 1820, 1850, 1880, 1913, 1930, 1950]
    fx = {}; bd = {}; Pmax = {}; nonfin = 0; fxlow = 0; bondhi = 0; nrow = 0; mil = {}
    for s in SEEDS:
        ix, r = rd(d, 'nation', s)
        if ix is None: continue
        for x in r:
            n = x[ix['nation']]; y = int(x[ix['year']]); nrow += 1
            p = fl(x[ix['priceLevel']])
            if not math.isfinite(p): nonfin += 1
            else:
                k = (s, n)
                if p > Pmax.get(k, (-1, 0))[0]: Pmax[k] = (p, y)
            f = fl(x[ix['fxRate']]); bo = fl(x[ix['bondRateMeanPerYear']])
            if f == f and f < 0.3001: fxlow += 1
            if bo == bo and bo >= 0.2999: bondhi += 1
            b = base(n)
            if b and y in YS:
                fx[(b, y, s)] = f; bd[(b, y, s)] = bo
            if b in ('イギリス', 'プロイセン'):
                gs = [g for g in x[ix['stateFacilityStartedGoods']].split(';') if g]
                per = '1783-1820' if y <= 1820 else ('1821-1880' if y <= 1880 else '1881-1950')
                mil.setdefault((b, per, s), [0, 0])
                mil[(b, per, s)][0] += sum(1 for g in gs if g in MILADV); mil[(b, per, s)][1] += len(gs)
    print('# ④ 為替 fxRate（年の最後の四半期の値・種の中央値）')
    for n in NINE: print(f'  {n}\t' + '\t'.join(f'{y}: {med([fx.get((n, y, s), float("nan")) for s in SEEDS]):.3f}' for y in YS))
    print('# ④ 国債の金利 bondRateMeanPerYear（同）')
    for n in NINE: print(f'  {n}\t' + '\t'.join(f'{y}: {med([bd.get((n, y, s), float("nan")) for s in SEEDS]):.3f}' for y in YS))
    print(f'  為替が床 0.3 に触れた行（<0.3001）{fxlow}／{nrow}・国債の金利が上限 0.30 に触れた行（≥0.2999）{bondhi}')
    big = sorted(((v[0], k[0], k[1], v[1]) for k, v in Pmax.items() if v[0] > 4), reverse=True)
    print(f'# ⑤ 全ての国の価格水準（nation_s の priceLevel）：非有限 {nonfin} 行・P>4 の（種,国）{len(big)}・P>10 {sum(1 for b in big if b[0] > 10)}・P>100 {sum(1 for b in big if b[0] > 100)}・最大 ' + ('・'.join(f'{p:.2f}(種{s} {n} {y})' for p, s, n, y in big[:8]) if big else 'なし'))
    allmax = sorted(((v[0], k[0], k[1], v[1]) for k, v in Pmax.items()), reverse=True)[:5]
    print('  最大 5 つ：' + '・'.join(f'{p:.2f}(種{s} {n} {y})' for p, s, n, y in allmax))
    # ⑥ 英とプロイセンの軍需・先端の着工（nation_s の stateFacilityStartedGoods・種の中央値と、件数>0 の種の数）
    print('# ⑥ 軍需・先端の着工（ukf579 の G1 と同じ財の集合・種の中央値〔件数>0 の種／10〕・括弧の後は全ての着工の中央値）')
    for b in ('イギリス', 'プロイセン'):
        print(f'  {b}\t' + '\t'.join(f"{per}: {med([mil.get((b, per, s), [0, 0])[0] for s in SEEDS]):.1f}〔{sum(1 for s in SEEDS if mil.get((b, per, s), [0, 0])[0] > 0)}/10〕 全 {med([mil.get((b, per, s), [0, 0])[1] for s in SEEDS]):.1f}" for per in ('1783-1820', '1821-1880', '1881-1950')))
    # ⑦ 奨学金（ed_s の higherSubsidyPerMonth＞0 の国の数・列が無い木は〈列なし〉）
    cnt = []; has = False
    for s in SEEDS:
        ix, r = rd(d, 'ed', s)
        if ix is None or 'higherSubsidyPerMonth' not in ix: continue
        has = True; ns = set()
        for x in r:
            if fl(x[ix['higherSubsidyPerMonth']]) > 0: ns.add(x[ix['nation']])
        cnt.append(len(ns))
    print('# ⑦ 奨学金の補助が出た国の数（ed_s・種ごと）：' + (str(cnt) if has else '列なし'))
