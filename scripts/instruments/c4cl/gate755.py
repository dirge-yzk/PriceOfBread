"""(755) c4cl：事前登録 m755 の門 C1・C2 と記録を計器 dfac_s の出力から読む（読むだけ）。
python -X utf8 gate755.py <計器の出力ディレクトリ> [--ref <旗 0 の計器つきの出力ディレクトリ>] [--seeds 1-10]
  C1 ＝全行で desiredUsedSum が desiredNewSum と相対差 1e-9 以内（最大の相対差と行数を出す）。旗 0 の出力では desiredUsed＝desiredOld を数える（突然変異の確かめ用に --old）。
  C2 ＝lowUsedSum÷lowOldSum（全国×層 1〜4・全四半期の和）の種の中央値 ≦0.85。
  記録＝highUsed÷highOld（行が無い種は nan）・下の挟みに縛られた必要量の割合・層ごとの比・国ごとの下の比・欲しい量÷買えた量・--ref があれば boughtUsedSum の層ごとの 新÷旧（国×四半期で対応する行の和）。
dfac_s は POB_INSTRUMENTS_NATIONS の国だけの行。"""
import sys, os, statistics as st, collections, glob, re

args = sys.argv[1:]
old_mode = '--old' in args
if old_mode: args.remove('--old')
ref = None
if '--ref' in args:
    i = args.index('--ref'); ref = args[i + 1]; del args[i:i + 2]
seeds_arg = None
if '--seeds' in args:
    i = args.index('--seeds'); seeds_arg = args[i + 1]; del args[i:i + 2]
d = args[0]

def seeds_in(dr):
    return sorted(int(re.search(r'dfac_s(\d+)\.tsv$', f).group(1)) for f in glob.glob(os.path.join(dr, 'dfac_s*.tsv')))
seeds = seeds_in(d)
if seeds_arg:
    a, b = seeds_arg.split('-'); seeds = [s for s in seeds if int(a) <= s <= int(b)]

def rows(dr, sd):
    with open(os.path.join(dr, f'dfac_s{sd}.tsv'), encoding='utf-8') as fh:
        hdr = next(fh).rstrip('\n').split('\t')
        for ln in fh:
            c = ln.rstrip('\n').split('\t')
            if len(c) != len(hdr): continue
            yield {h: (v if h == 'nation' else float(v)) for h, v in zip(hdr, c)}

def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')

maxrel, nrows, bad = 0.0, 0, 0
C2, HI, LOWSHARE = [], [], []
tier_low = collections.defaultdict(lambda: [[], []])    # 層 -> [lowUsed の種ごとの和, lowOld の種ごとの和]
nat_low = collections.defaultdict(lambda: [0.0, 0.0])
want_buy = collections.defaultdict(lambda: [0.0, 0.0])  # 層 -> [desiredUsed, bought]
for sd in seeds:
    lu = lo = hu = ho = lb = nd = 0.0
    tl = collections.defaultdict(lambda: [0.0, 0.0])
    for r in rows(d, sd):
        nrows += 1
        tgt = r['desiredOldSum'] if old_mode else r['desiredNewSum']
        rel = abs(r['desiredUsedSum'] - tgt) / max(1.0, abs(tgt))
        maxrel = max(maxrel, rel)
        if rel > 1e-9: bad += 1
        lu += r['lowUsedSum']; lo += r['lowOldSum']; hu += r['highUsedSum']; ho += r['highOldSum']; lb += r['lowBindNeedSum']; nd += r['needSum']
        t = int(r['tier']); tl[t][0] += r['lowUsedSum']; tl[t][1] += r['lowOldSum']
        nat_low[r['nation']][0] += r['lowUsedSum']; nat_low[r['nation']][1] += r['lowOldSum']
        want_buy[t][0] += r['desiredUsedSum']; want_buy[t][1] += r['boughtUsedSum']
    C2.append(lu / lo if lo > 0 else float('nan'))
    HI.append(hu / ho if ho > 0 else float('nan'))
    LOWSHARE.append(lb / nd if nd > 0 else float('nan'))
    for t, (u, o) in tl.items():
        tier_low[t][0].append(u); tier_low[t][1].append(o)
    print(f'seed {sd}\tC2(lowUsed/lowOld)={C2[-1]:.4f}\thigh={HI[-1]:.4f}\tlowBindNeedShare={LOWSHARE[-1]:.4f}')
mC2 = med(C2)
print(f'行数={nrows}\tC1 最大の相対差={maxrel:.3e}\t1e-9 超の行={bad}\t{"C1 合格" if bad == 0 and nrows > 0 else "C1 不合格"}')
print(f'C2 種の中央値={mC2:.4f}（門 ≦0.85）\t{"C2 合格" if mC2 <= 0.85 else "C2 不合格"}')
print(f'記録\thighUsed/highOld の中央値={med(HI):.4f}\tlowBindNeedShare の中央値={med(LOWSHARE):.4f}')
for t in sorted(tier_low):
    u = sum(tier_low[t][0]); o = sum(tier_low[t][1])
    print(f'記録\t層{t}\tlowUsed/lowOld={u / o if o > 0 else float("nan"):.4f}\tdesiredUsed/bought={want_buy[t][0] / want_buy[t][1] if want_buy[t][1] > 0 else float("nan"):.4f}')
for n in sorted(nat_low):
    u, o = nat_low[n]
    print(f'記録\t国 {n}\tlowUsed/lowOld={u / o if o > 0 else float("nan"):.4f}')
if ref:
    rseeds = [s for s in seeds_in(ref) if s in seeds]
    ratios = collections.defaultdict(list)
    for sd in rseeds:
        a = collections.defaultdict(float); b = collections.defaultdict(float)
        for r in rows(d, sd): a[int(r['tier'])] += r['boughtUsedSum']
        for r in rows(ref, sd): b[int(r['tier'])] += r['boughtUsedSum']
        for t in a:
            if b[t] > 0: ratios[t].append(a[t] / b[t])
    for t in sorted(ratios):
        print(f'記録\t層{t}\tboughtUsed 新÷旧（旗 1 の軌道÷旗 0 の軌道）の種の中央値={med(ratios[t]):.4f}\t種={len(ratios[t])}')
