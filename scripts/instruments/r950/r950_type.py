# (950) 読むだけ：(a) の型〈投資家の貯蓄が配当の取り立てで尽きる（E）×価格水準が床（F）×名目 GDP の崩れ（G）〉を国×種で数える。
#   腕・基準・ヌル腕を同じ台本で数える。エンジンの関数を呼ばない・旗を読まない。定義は数える前にここで固定した（結果を見て変えない）。
#   E＝divhost_s で 配当の請求 ＞0 かつ 払えた額 ＜請求の 1/2 の四半期が 4 つ以上続く区間（投資家の貯蓄が尽きて払えない）
#   F＝nation_s の priceLevel ≦0.4＋1e-6 が 4 四半期以上続く区間（床に張り付く）
#   G＝nation_s の名目 GDP が直前 40 四半期の最大の 1/100 未満（人口は問わない・最初の 4 行は除く）
#   重なり＝E の区間と F の区間が ±8 四半期で重なる・G はその重なりの ±8 四半期の内
#   分かれた国＝cederule_s の how＝Seceded・tierBefore＝Colonial の receiver（植民地格の州から分かれた国）
#   python -X utf8 -I r950_type.py <名>=<ins フォルダ>:<種の始め>:<種の終わり> ...
import sys, os
FLOOR = 0.4 + 1e-6
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def runs(qs, k=4):
    qs = sorted(qs); out = []; st = None; pv = None
    for q in qs:
        if pv is not None and q == pv + 1: pv = q; continue
        if st is not None and pv - st + 1 >= k: out.append((st, pv))
        st = pv = q
    if st is not None and pv - st + 1 >= k: out.append((st, pv))
    return out
def near(a, b, m=8): return a[0] - m <= b[1] and b[0] - m <= a[1]
for arg in sys.argv[1:]:
    name, rest = arg.split('=', 1); d, a0, a1 = rest.rsplit(':', 2)
    tot = dict(nat=0, sec=0, E=0, EF=0, EFG=0, secE=0, secEF=0, secEFG=0, nonsecEF=0, F=0, secF=0, G=0)
    rows = []
    for s in range(int(a0), int(a1) + 1):
        pn = os.path.join(d, f'nation_s{s}.tsv')
        if not os.path.exists(pn): continue
        sec = {}
        pc = os.path.join(d, f'cederule_s{s}.tsv')
        with open(pc, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                x = line.rstrip('\n').split('\t')
                if x[ix['how']] == 'Seceded' and x[ix['tierBefore']] == 'Colonial': sec.setdefault(x[ix['receiver']], int(x[ix['quarter']]))
        ser = {}
        with open(pn, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inn, ip, ig, il = ix['quarter'], ix['nation'], ix['populationK'], ix['nominalGdpPerYear'], ix['priceLevel']
            for line in f:
                x = line.rstrip('\n').split('\t')
                ser.setdefault(x[inn], []).append((int(x[iq]), fl(x[ip]), fl(x[ig]), fl(x[il])))
        ex = {}
        with open(os.path.join(d, f'divhost_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inn, irq, ipd, ifc, isv = ix['quarter'], ix['nation'], ix['divReqIndex'], ix['paidIndex'], ix['fci'], ix['invSav']
            for line in f:
                x = line.rstrip('\n').split('\t')
                rq = fl(x[irq]); pd = fl(x[ipd])
                if rq > 0 and not (pd >= 0.5 * rq): ex.setdefault(x[inn], []).append(int(x[iq]))
        for n, xs in ser.items():
            tot['nat'] += 1; isS = n in sec; tot['sec'] += isS
            Er = runs(ex.get(n, []))
            Fr = runs([q for q, pk, g, l in xs if l <= FLOOR])
            Gq = []
            for i, (q, pk, g, l) in enumerate(xs):
                if i < 4: continue
                mx = max(y[2] for y in xs[max(0, i - 40):i])
                if mx > 0 and g < mx / 100: Gq.append(q)
            ef = [(e, f_) for e in Er for f_ in Fr if near(e, f_)]
            efg = [(e, f_) for e, f_ in ef if any(min(e[0], f_[0]) - 8 <= q <= max(e[1], f_[1]) + 8 for q in Gq)]
            if Er: tot['E'] += 1; tot['secE'] += isS
            if Fr: tot['F'] += 1; tot['secF'] += isS
            if Gq: tot['G'] += 1
            if ef:
                tot['EF'] += 1; tot['secEF'] += isS; tot['nonsecEF'] += not isS
                pk = max(y[1] for y in xs)
                rows.append((s, n, '分' if isS else '-', sec.get(n, ''), round(pk), ef[0][0], ef[0][1], 'G' if efg else '', len(ef)))
            if efg: tot['EFG'] += 1; tot['secEFG'] += isS
    print(f'# {name} {d} 種 {a0}〜{a1}')
    print(f'{name}\t国×種 {tot["nat"]}（分かれた国 {tot["sec"]}）\tE {tot["E"]}（分 {tot["secE"]}）\tF {tot["F"]}（分 {tot["secF"]}）\tG {tot["G"]}\tE∧F {tot["EF"]}（分 {tot["secEF"]}・他 {tot["nonsecEF"]}）\tE∧F∧G {tot["EFG"]}（分 {tot["secEFG"]}）')
    for r in rows: print('  ', *r, sep='\t')
