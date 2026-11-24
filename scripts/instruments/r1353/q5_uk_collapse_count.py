# 問い5：英の崩れの型を腕ごと・種ごとに数える（読むだけ・種の番号は basename から）
# 列：最大人口・最小÷最大・1/10 未満に落ちた最初の四半期・ロンドンを失った最初の四半期と行き先・
#     開幕の政体から最初に変わった四半期（scalars）・英の内戦（wars の civilWarFlag=1 で sideA=イギリス）の年と四半期・最後の四半期の本国格（Core）の州の数
# 使い方: python q5_uk_collapse_count.py <名前=フォルダ> ...
import sys, os, csv, re, collections
NAT = 'イギリス'; CAP = 'ロンドン'
def seeds(d):
    return sorted({int(m.group(1)) for f in os.listdir(d) for m in [re.match(r'nation_s(\d+)\.tsv$', os.path.basename(f))] if m})
def rows(p):
    with open(p, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: yield ix, row
print("腕\t種\t最大人口千\t最小÷最大\t1/10未満の最初q\tロンドンを失ったq→行き先\t政体の最初の変化q(前→後)\t英の内戦(年q)\t終わりのCore数\t終わりの人口千")
summary = collections.defaultdict(lambda: collections.Counter())
for spec in sys.argv[1:]:
    name, d = spec.split('=', 1)
    for s in seeds(d):
        pop = []
        for ix, row in rows(os.path.join(d, f"nation_s{s}.tsv")):
            if row[ix['nation']] == NAT: pop.append((int(row[ix['quarter']]), float(row[ix['totalPopulationK']])))
        if not pop: print(f"{name}\t{s}\t(英の行なし)"); continue
        mx = max(p for _, p in pop); mn_after = mx; peakq = max(pop, key=lambda x: x[1])[0]
        tenth = None; run = mx
        for q, p in pop:
            run = max(run, p) if False else run
        best = 0
        for q, p in pop:
            best = max(best, p)
            if tenth is None and p < best / 10: tenth = q
        mn = min(p for q, p in pop if q >= peakq)
        lost = None; core_end = None; lastq = None; corecnt = collections.Counter()
        for ix, row in rows(os.path.join(d, f"region_s{s}.tsv")):
            q = int(row[ix['quarter']])
            if row[ix['region']] == CAP and lost is None and row[ix['nation']] != NAT: lost = f"{q}→{row[ix['nation']]}"
            if row[ix['nation']] == NAT and row[ix['tier']] == 'Core': corecnt[q] += 1
            lastq = q
        core_end = corecnt.get(lastq, 0)
        pol0 = None; polchg = None
        for ix, row in rows(os.path.join(d, f"scalars_s{s}.tsv")):
            if row[ix['nation']] != NAT: continue
            pc = row[ix['polityCode']]
            if pol0 is None: pol0 = pc
            elif polchg is None and pc != pol0: polchg = f"{row[ix['quarter']]}({pol0}→{pc})"
        cw = []
        for ix, row in rows(os.path.join(d, f"wars_s{s}.tsv")):
            if row[ix['civilWarFlag']] == '1' and row[ix['sideA']] == NAT: cw.append(f"{row[ix['startYear']]}q{row[ix['q']]}")
        endpop = pop[-1][1]
        print(f"{name}\t{s}\t{mx:.0f}\t{mn/mx:.3f}\t{tenth}\t{lost}\t{polchg}\t{','.join(cw) or '-'}\t{core_end}\t{endpop:.0f}")
        c = summary[name]; c['種'] += 1
        if tenth is not None: c['1/10未満'] += 1
        if lost: c['ロンドン喪失'] += 1
        if lost and int(lost.split('→')[0]) < 120: c['ロンドン喪失(q<120)'] += 1
        if cw: c['英の内戦あり'] += 1
        if polchg and int(polchg.split('(')[0]) < 120: c['政体の変化(q<120)'] += 1
        if core_end == 0: c['終わりCore0'] += 1
print("\n# 要約（腕ごとの種の数）")
keys = ['種', '1/10未満', 'ロンドン喪失', 'ロンドン喪失(q<120)', '政体の変化(q<120)', '英の内戦あり', '終わりCore0']
print("腕\t" + "\t".join(keys))
for name, c in summary.items(): print(name + "\t" + "\t".join(str(c[k]) for k in keys))
