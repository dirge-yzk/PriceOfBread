# (1437) 問い2 (a) と問い5 の二重の記帳（読むだけ）
# (a) sellr の 線路・機関車 の lastDemand（四半期の末の標本＝その月の EconomyStep の totalDemand＋Transport の調達の want＋他の州の国の中の買いの take）を、
#     trrail の procureWantUnitsPerYear÷monthsCount÷2（州の調達の 1 財×1 月の平均）と並べる。sellr はその州に線路か機関車の生産者がある行だけ＝無い州は〈数えられない〉。
# (c) 二重の記帳：ProcureNational は売り手の州で Procure を呼ぶ＝take を売り手の lastDemand へ足す（買い手の州は want の全てを既に足している）。
#     take の和は natGot 以上・natWant 以下（take＝min(deficit, 貯蓄÷届け値)・在庫で削られた分だけ got＜take）。年×国で natGot と natWant と州の want の和を出す。
# 入力：<raw>/sellr_s*.tsv・trrail_s*.tsv・<out>/q1c_rows.tsv。出力：<out>/<tag>_q2a.txt・<out>/<tag>_q2dbl.tsv
import sys, csv, collections, statistics as st
raw, out, tag = sys.argv[1], sys.argv[2], sys.argv[3]
roster = set()
with open(f"{out}/q1c_rows.tsv", encoding='utf-8') as f:
    for r in csv.DictReader(f, delimiter='\t'): roster.add((int(r['seed']), r['region']))
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except Exception: return 0.0
ratios = collections.defaultdict(list); rosterRows = []; dbl = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0])
cover = collections.Counter()
for s in range(1, 11):
    tw = {}
    with open(f"{raw}/trrail_s{s}.tsv", newline='', encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for row in rd:
            y = int(row[ix['year']]); mc = fl(row[ix['monthsCount']])
            want = fl(row[ix['procureWantUnitsPerYear']])
            natW = sum(fl(row[ix['natWantPhi' + b]]) for b in ('Low', 'Mid', 'High')); natG = sum(fl(row[ix['natGotPhi' + b]]) for b in ('Low', 'Mid', 'High'))
            tw[(y, row[ix['region']])] = (want / mc / 2 if mc > 0 else 0.0, want, natW, natG)
            d = dbl[(s, y, row[ix['nation']])]; d[0] += want; d[1] += natW; d[2] += natG; d[3] += fl(row[ix['procureGotUnitsPerYear']])
    seen = set()
    with open(f"{raw}/sellr_s{s}.tsv", newline='', encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for row in rd:
            g = row[ix['good']]
            if g not in ('線路', '機関車'): continue
            y = int(row[ix['year']]); rg = row[ix['region']]
            k = (y, rg, g)
            if k in seen: continue
            seen.add(k)
            if (y, rg) not in tw: continue
            wpm = tw[(y, rg)][0]
            if not wpm > 0: continue
            ld = fl(row[ix['lastDemand']]); rt = ld / wpm
            band = 'yr<1870' if y < 1870 else 'yr>=1870'
            ratios[(g, band)].append(rt)
            if (s, rg) in roster: rosterRows.append((s, y, rg, row[ix['ownerNation']], g, ld, wpm, rt, fl(row[ix['stock']]), fl(row[ix['prevImportAll']]), fl(row[ix['lastProduced']]), fl(row[ix['price']])))
    for (y, rg) in tw:
        if tw[(y, rg)][0] > 0 and y % 10 == 0: cover['want>0 の州×年'] += 1
    for (y, rg, g) in seen:
        if y % 10 == 0 and (y, rg) in tw and tw[(y, rg)][0] > 0 and g == '線路': cover['うち sellr に線路の行'] += 1
with open(f"{out}/{tag}_q2a.txt", 'w', encoding='utf-8') as f:
    f.write(f"sellr の覆い（10 年ごとの年）：{dict(cover)}\n")
    f.write("財\t期\t行\tlastDemand÷(want÷月÷2) の 10%点\t中央\t90%点\t1未満の割合\n")
    for (g, b), l in sorted(ratios.items()):
        l.sort(); n = len(l)
        f.write(f"{g}\t{b}\t{n}\t{l[n // 10]:.3f}\t{l[n // 2]:.3f}\t{l[9 * n // 10]:.3f}\t{sum(1 for x in l if x < 0.999) / n:.3f}\n")
    f.write("\n名簿の州（q1c の止まった州×種）で sellr に行がある物：seed year region owner good lastDemand wantPerGoodMonth ratio stock prevImport lastProduced price\n")
    for r in sorted(rosterRows): f.write('\t'.join(f"{x:.4g}" if isinstance(x, float) else str(x) for x in r) + '\n')
with open(f"{out}/{tag}_q2dbl.tsv", 'w', encoding='utf-8') as f:
    f.write("seed\tyear\tnation\twantSum\tnatWantSum\tnatGotSum(=take の下限)\tprocureGotSum\n")
    for (s, y, n), d in sorted(dbl.items()):
        if d[0] > 0: f.write(f"{s}\t{y}\t{n}\t{d[0]:.6g}\t{d[1]:.6g}\t{d[2]:.6g}\t{d[3]:.6g}\n")
# 要約：全ての種×年×国の和
T = [0.0] * 4
for d in dbl.values():
    for i in range(4): T[i] += d[i]
with open(f"{out}/{tag}_q2a.txt", 'a', encoding='utf-8') as f:
    f.write(f"\n二重の記帳の和（10 種・全ての年と国）：州の調達の want {T[0]:.6g}・国の中の買いの want（deficit）{T[1]:.6g}・natGot（take の下限）{T[2]:.6g}・州の在庫からの got（procureGot＝国の中の買いを含まない・Transport.cs の rm.Procured は国の中の買いの前）{T[3]:.6g}\n")
    f.write(f"  take の下限÷want {T[2] / T[0]:.4f}・上限÷want {T[1] / T[0]:.4f}・natGot÷(got＋natGot) {T[2] / (T[3] + T[2]):.4f}\n")
print('ok')
