# (1210) 問い③ 台帳 S06 の数の行を keep の生で数え直す（エンジンの閾値の式で）。読むだけ。
#  S06-09 物価の床：pclamp_s の kind=floor の行（World.cs の PriceLevel = Math.Max(P, PriceLevelFloor) が効いた四半期を計器が書く）÷ nation_s の国×四半期。
#          確かめ：floor の行は priceLevelAfterFiatIndex < 0.4（PriceLevelFloor）かつ priceLevelAfterIndex == 0.4 であること（式と同じ不等式）。
#  S06-19 恐慌の門：nation_s の realGdpPerYear から World.UpdateGdpCycle の式を組み直す（GdpTrend＝EMA 0.18・GdpPeak は上がれば追い、下がれば 0.02 で寄る・
#          Drawdown＝1−Trend÷Peak〔Peak>1 の時〕）。門＝Drawdown > DepressionScrapThreshold 0.20（Simulation.cs MonthSettle の max(0, dd−0.2) が正）。
#          1 人あたりも同じ式（GdpPcDrawdown）で組み、門の開いた中で 1 人あたり≦0.2 の割合を出す。年の単位（年率÷12）は比なので消える。
#          限界：エンジンは GdpPerMonth（月）を四半期ごとに読む・計器の realGdpPerYear が同じ時点の値かは計器の定義に従う＝近似と名乗る。
#  S06-01 失業：labor_s の farmUnemploymentShare・plainUnemploymentShare（計器の列のまま・picked の国の四半期の行と年末の行）の平均と最大。
#  計器が書かない行（空・数でない）は数えない。
# 使い方: python -X utf8 -I s06_recount.py <生> <種0> <種1> <出力>
import sys, csv, math, collections
raw, s0, s1, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
SM, PD, TH, FLOOR = 0.18, 0.02, 0.20, 0.4

def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r: yield ix, x

def fl(s):
    try:
        v = float(s); return v if math.isfinite(v) else None
    except ValueError:
        return None

def toward(a, b, k): return a + (b - a) * k

res = []
tot = collections.Counter()
for s in range(s0, s1 + 1):
    nq = 0; ser = collections.defaultdict(list)
    for ix, x in rows(f'{raw}/nation_s{s}.tsv'):
        nq += 1
        g = fl(x[ix['realGdpPerYear']]); pop = fl(x[ix['totalPopulationK']])
        ser[x[ix['nation']]].append((int(x[ix['quarter']]), g, pop))
    fl_rows = bad_floor = 0
    import os
    pcl = f'{raw}/pclamp_s{s}.tsv'
    # pclamp_s は床か跳びが起きた時だけ開かれる（最初の行で開く）＝ファイルが無い種は床の行 0 と数える（〈計器が書かない〉ではない＝計器は起きた四半期だけを書く型）
    for ix, x in (rows(pcl) if os.path.exists(pcl) else []):
        if x[ix['kind']] != 'floor': continue
        fl_rows += 1
        a, b = fl(x[ix['priceLevelAfterFiatIndex']]), fl(x[ix['priceLevelAfterIndex']])
        if not (a is not None and a < FLOOR and b is not None and abs(b - FLOOR) < 1e-12): bad_floor += 1
    gate = judged = pc_le = 0; top = collections.Counter()
    for nat, lst in ser.items():
        lst.sort(); T = P = Tpc = Ppc = None
        for q, g, pop in lst:
            if g is None: continue
            gm = g / 12.0
            T = gm if not (T is not None and T > 0) else toward(T, gm, SM)
            pc = gm / max(1.0, (pop or 0) * 1000.0)
            Tpc = pc if not (Tpc is not None and Tpc > 0) else toward(Tpc, pc, SM)
            if Ppc is None or Tpc > Ppc: Ppc = Tpc
            else: Ppc = toward(Ppc, Tpc, PD)
            if P is None or T > P: P = T
            else: P = toward(P, T, PD)
            dd = min(1.0, max(0.0, 1.0 - T / P)) if P > 1 else 0.0
            ddpc = min(1.0, max(0.0, 1.0 - Tpc / Ppc)) if (Ppc and Ppc > 0 and P > 1) else 0.0
            judged += 1
            if dd > TH:
                gate += 1; top[nat] += 1
                if ddpc <= TH: pc_le += 1
    lab = collections.defaultdict(list)
    for ix, x in rows(f'{raw}/labor_s{s}.tsv'):
        for k in ('farmUnemploymentShare', 'plainUnemploymentShare'):
            v = fl(x[ix[k]])
            if v is not None: lab[k].append(v)
    res.append((s, nq, fl_rows, bad_floor, judged, gate, pc_le, top.most_common(3),
                {k: (sum(v) / len(v), max(v), len(v)) for k, v in lab.items()}))
    for k, v in (('nq', nq), ('floor', fl_rows), ('badfloor', bad_floor), ('judged', judged), ('gate', gate), ('pcle', pc_le)): tot[k] += v
with open(out, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# raw={raw} seeds={s0}..{s1}\n種\t国×四半期\t床の行\t床の行で式と合わない\t後退度を数えた国×四半期\t門が開いた\t開いた割合\t開いた中で1人あたり≦0.2\t門の多い国\t農業の失業 平均/最大/行\t高度でない労働の失業 平均/最大/行\n')
    for s, nq, f_, bf, j, gt, pl, top, lab in res:
        fu = lab.get('farmUnemploymentShare', (float('nan'),) * 3); pu = lab.get('plainUnemploymentShare', (float('nan'),) * 3)
        o.write(f'{s}\t{nq}\t{f_}\t{bf}\t{j}\t{gt}\t{gt / j if j else float("nan"):.4f}\t{pl / gt if gt else float("nan"):.3f}\t' + ' '.join(f'{n}:{c}' for n, c in top)
                + f'\t{fu[0]:.5f}/{fu[1]:.4f}/{fu[2]}\t{pu[0]:.4f}/{pu[1]:.4f}/{pu[2]}\n')
    o.write(f'# 計 床の行 {tot["floor"]}／国×四半期 {tot["nq"]}（{tot["floor"] / tot["nq"]:.5%}）・式と合わない床の行 {tot["badfloor"]}・門 {tot["gate"]}／{tot["judged"]}（{tot["gate"] / tot["judged"]:.3%}）・開いた中で1人あたり≦0.2 {tot["pcle"]}（{tot["pcle"] / max(1, tot["gate"]):.3%}）\n')
print('ok', out)
