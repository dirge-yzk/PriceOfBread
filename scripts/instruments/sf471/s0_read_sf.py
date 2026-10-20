#!/usr/bin/env python
"""(471) 線 sf の sf_s の段 0 の読み手（読むだけ）。使い方：python s0_read_sf.py <計器の出力フォルダ(ins)> [seed...]
事前登録 measurements/sf471_20261020_prereg.txt [3][8] の場面の量を国ごとに要約する：
  G0-1  soldierElder60K ÷ (elder60K + soldierElder60K)（年金の法がある四半期・Q>=100 の平均）
  G0-2  cheapGoodCount>0 の国×四半期の数
  G0-3  1 - voteWeightScoped ÷ voteWeightAll（1850 年以後の平均）
  G4    budgetNegCumAmount・budgetNegCumPopMonthK の最終行（累計）
  累計   pensionToSoldiersCum・cheapSubsidyPaidCum の最終行
"""
import csv, sys, glob, os, collections, statistics

def fl(x):
    try: return float(x)
    except Exception: return float('nan')

def read(path):
    with open(path, encoding='utf-8', errors='replace') as f:
        rows = list(csv.DictReader(f, delimiter='\t'))
    return rows

def main():
    d = sys.argv[1]
    seeds = sys.argv[2:] or sorted({os.path.basename(p)[4:-4] for p in glob.glob(os.path.join(d, 'sf_s*.tsv'))})
    for sd in seeds:
        p = os.path.join(d, f'sf_s{sd}.tsv')
        if not os.path.exists(p):
            print('no file', p); continue
        rows = read(p)
        by = collections.defaultdict(list)
        for r in rows:
            by[r['nation']].append(r)
        print(f'== seed {sd}  rows {len(rows)}  nations {len(by)}')
        print('nation\tG0-1 soldierElderShare\tpenQ\tG0-2 cheapNQ\tG0-3 excludedShare\tpenToSoldCum\tcheapPaidCum\tbudNegAmt\tbudNegPopMonthK')
        cheap_total = 0
        for n, rs in sorted(by.items()):
            sh = []; pq = 0
            for r in rs:
                q = int(fl(r['quarter']))
                lv = fl(r['pensionLevel']); e = fl(r['elder60K']); s = fl(r['soldierElder60K'])
                if q >= 100 and lv > 0 and e + s > 0:
                    sh.append(s / (e + s)); pq += 1
            cq = sum(1 for r in rs if fl(r['cheapGoodCount']) > 0)
            cheap_total += cq
            ex = []
            for r in rs:
                if int(fl(r['year'])) >= 1850:
                    a = fl(r['voteWeightAll']); b = fl(r['voteWeightScoped'])
                    if a > 0: ex.append(1 - b / a)
            last = rs[-1]
            print('\t'.join([n,
                f'{statistics.fmean(sh):.5f}' if sh else '-', str(pq), str(cq),
                f'{statistics.fmean(ex):.4f}' if ex else '-',
                f"{fl(last['pensionToSoldiersCum']):.4g}", f"{fl(last['cheapSubsidyPaidCum']):.4g}",
                f"{fl(last['budgetNegCumAmount']):.4g}", f"{fl(last['budgetNegCumPopMonthK']):.4g}"]))
        print('cheapGood nation-quarters total', cheap_total)

main()
