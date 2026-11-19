# (1236) c125b1 段 0：全体の世界のプローブ（probe_Program.cs.txt の出力）を数える（エンジンの関数を呼ばない）。
# 使い方: python -X utf8 -I probe_sum.py <f0.tsv> <f1.tsv> [州の名=ジャマイカ]
# ① 州の不在地主の大農園の地代＝Σ(LastRevenue−LastCost)（月ごと・種ごと）の符号の数（月 1〜12）。
# ② 財ごと（コーヒー・サトウキビほか）の損益の和（月 1〜12 の和・種の中央）と、損益が負の月の数。
# ③ 規模の伸び＝Scale の月 0→月 12（財ごと・種の中央・最小・最大）と、月 12 の産出÷月 1 の産出。
# ④ 英仏の植民地格の州の全体：地代の和が正の州×月の割合（月 1〜12）。
import sys, csv, collections, statistics as st
def load(p):
    rows = []
    with open(p, encoding='utf-8', newline='') as f:
        hdr = f.readline().rstrip('\n')
        rd = csv.DictReader(f, delimiter='\t')
        for r in rd: rows.append(r)
    return hdr, rows
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
REG = sys.argv[3] if len(sys.argv) > 3 else 'ジャマイカ'
for tag, path in (('旗0', sys.argv[1]), ('旗1', sys.argv[2])):
    hdr, rows = load(path)
    print(f'## {tag} {hdr}')
    rent = collections.defaultdict(float)          # (seed, month) -> 州 REG の地代
    good = collections.defaultdict(float)          # (seed, month, good) -> 損益
    scale = {}                                     # (seed, month, good) -> scale（同じ財の大農園の和）
    outp = collections.defaultdict(float)
    allreg = collections.defaultdict(float)        # (seed, month, nation, region) -> 地代
    for r in rows:
        s, m = int(r['seed']), int(r['month'])
        pl = fl(r['lastRevenue']) - fl(r['lastCost'])
        allreg[(s, m, r['nation'], r['region'])] += pl
        if r['region'] != REG: continue
        g = r['good']
        rent[(s, m)] += pl; good[(s, m, g)] += pl
        scale[(s, m, g)] = scale.get((s, m, g), 0.0) + fl(r['scale']); outp[(s, m, g)] += fl(r['out'])
    seeds = sorted({k[0] for k in rent})
    if not seeds: print(f'  州 {REG} の行が無い'); continue
    pos = sum(1 for k, v in rent.items() if k[1] >= 1 and v > 0); neg = sum(1 for k, v in rent.items() if k[1] >= 1 and v < 0)
    print(f'① {REG} の地代（月 1〜12×種 {len(seeds)}）：正 {pos}・負 {neg}・0 {len(seeds)*12-pos-neg}')
    print('   種ごとの月 1〜12 の和：' + ' '.join(f's{s}={sum(rent[(s, m)] for m in range(1, 13)):.2f}' for s in seeds))
    print('   種ごとの月 1 の値：' + ' '.join(f's{s}={rent[(s, 1)]:.2f}' for s in seeds))
    goods = sorted({k[2] for k in good})
    for g in goods:
        sums = [sum(good[(s, m, g)] for m in range(1, 13)) for s in seeds]
        negm = sum(1 for s in seeds for m in range(1, 13) if good[(s, m, g)] < 0)
        gr = [scale[(s, 12, g)] / scale[(s, 0, g)] for s in seeds if scale.get((s, 0, g), 0) > 0 and (s, 12, g) in scale]
        sc0 = [scale[(s, 0, g)] for s in seeds if (s, 0, g) in scale]; sc12 = [scale[(s, 12, g)] for s in seeds if (s, 12, g) in scale]
        og = [outp[(s, 12, g)] / outp[(s, 1, g)] for s in seeds if outp[(s, 1, g)] > 0]
        print(f'② {g}：損益の和（月 1〜12）中央 {st.median(sums):.3f} 最小 {min(sums):.3f} 最大 {max(sums):.3f}・負の月 {negm}/{len(seeds)*12}')
        if gr: print(f'③ {g}：規模 月0 中央 {st.median(sc0):.4f}→月12 中央 {st.median(sc12):.4f}・伸び 中央 {st.median(gr):.4f} 最小 {min(gr):.4f} 最大 {max(gr):.4f}・産出 月12÷月1 中央 {st.median(og) if og else float("nan"):.3f}')
    tot = sum(1 for k in allreg if k[1] >= 1); p = sum(1 for k, v in allreg.items() if k[1] >= 1 and v > 0)
    print(f'④ 英仏の植民地格の州×月（月 1〜12・種 {len(seeds)}）：地代の和が正 {p}/{tot}')
