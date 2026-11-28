"""(1430) 問い 1：西部（アメリカ）の流入÷受け入れの上限（月の値）。読むだけ・コミットしない。
python -I q1_west_cap.py <計器の出力ディレクトリ> <種,種,...> [州の名=西部]
入力：migcap_s<seed>.tsv（domPlanNewKPerMonth＝上限を掛ける前の国内の計画の流入〔1 か月分・四半期の末の盤面〕・
      domCapKPerMonth＝0.012/3×人口×AbsorbCap〔計器が自分で組んだ ReceiveScale の上限〕）と
      region_s<seed>.tsv（migrationInKPerQuarter＝実際の国内の流入の四半期の和）。
出力：10 年ごと（その 10 年の四半期の平均）に 人口・計画/上限・実際の流入(月)/上限・上限を超えた四半期の数。"""
import sys, os, collections
d = sys.argv[1]; seeds = [int(s) for s in sys.argv[2].split(',')]; REG = sys.argv[3] if len(sys.argv) > 3 else '西部'
def rows(p):
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for line in fh:
            v = line.rstrip('\n').split('\t')
            if len(v) > 4 and REG in v[:6]:
                yield dict(zip(h, v))
for s in seeds:
    mc = {}
    for r in rows(os.path.join(d, f'migcap_s{s}.tsv')):
        if r['region'] != REG: continue
        mc[int(r['quarter'])] = (float(r['year']), float(r['populationK']), float(r['domPlanNewKPerMonth']), float(r['domCapKPerMonth']), r['nation'])
    rin = {}
    for r in rows(os.path.join(d, f'region_s{s}.tsv')):
        if r['region'] != REG: continue
        rin[int(r['quarter'])] = (float(r['migrationInKPerQuarter']), float(r['crossBorderInKPerQuarter']), float(r['arableLand']), float(r['ruralPopulationK']))
    dec = collections.OrderedDict()
    for q in sorted(mc):
        yr, pop, plan, cap, nat = mc[q]
        mi, xi, ar, ru = rin.get(q, (float('nan'),) * 4)
        k = int(yr) // 10 * 10
        a = dec.setdefault(k, [0, 0.0, 0.0, 0.0, 0.0, 0, 0.0, 0.0, 0.0, set()])
        a[0] += 1; a[1] += pop; a[2] += plan; a[3] += cap; a[4] += mi / 3.0; a[5] += 1 if plan > cap else 0; a[6] += xi / 3.0; a[7] = ar; a[8] = ru; a[9].add(nat)
    print(f'## 種 {s}・{REG}・10 年ごとの四半期の平均（千人・月）')
    print('年代\t人口\t農村\t耕地\t計画(月)\t上限(月)\t計画÷上限\t実の国内流入(月)\t実÷上限\t上限を超えた四半期\t越境の流入(月)\t持ち主')
    tq = tb = 0
    for k, a in dec.items():
        n = a[0]; plan = a[2] / n; cap = a[3] / n; mi = a[4] / n
        tq += n; tb += a[5]
        print(f'{k}\t{a[1]/n:.1f}\t{a[8]:.1f}\t{a[7]:.0f}\t{plan:.3g}\t{cap:.3g}\t{(plan/cap if cap>0 else float("nan")):.3g}\t{mi:.3g}\t{(mi/cap if cap>0 else float("nan")):.3g}\t{a[5]}/{n}\t{a[6]/n:.3g}\t{"・".join(sorted(a[9]))}')
    print(f'## 種 {s}：上限を超えた四半期 {tb}/{tq}')
