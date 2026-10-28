# (641) 線 smq2：枠を 1 つ足したときの産出の増え（私的な限界）と a＝0 の山の読み手（読むだけ）。事前登録 measurements/smq641_20261028_prereg.txt [4] と追記 1 [A4]〜[A6]。
# 使い方：python -X utf8 an_smq2.py <計器の出力フォルダ> <種> [<種> ...]
#   フォルダ直下に slotmg_s<種>.tsv・slotmg0_s<種>.tsv・slotnd_s<種>.tsv がある前提。
# 出力：検算 C1〜C6／表①（平均と限界）／署名 M1〜M5 のセル数／表②（a＝0 の山の理由と財）／表③（時系列の符号の一致）。
# 区間：1783〜1789 年・1790 年代〜1850 年代の十年。読む国：英・仏・普・米・日・清。国ごとの数は種の幅（最小〜最大）と並べる。
# 署名の閾値は追記 1 [A6] のとおり（結果の後に動かさない）。M1 のセルの判定＝pf＜1 の部分集合で (binRhoHp0÷nHp)・(binRho1Hp0÷nHp)・(binG0÷nG) が全て 0.9 以上。
import csv, os, sys, math, collections

NAT = ["イギリス", "フランス", "プロイセン", "アメリカ", "日本", "清"]
SECT = ["textile", "metalchem", "other"]

def fl(x):
    try: return float(x)
    except Exception: return float('nan')

def rows(p):
    if not os.path.exists(p): return
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r, None)
        if h is None: return
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))

def period(y):
    if y <= 1789: return '1783-89'
    return f'{y // 10 * 10}s'

def div(a, b): return a / b if b and b == b and b != 0 else float('nan')
def f3(x): return 'nan' if x != x else f'{x:.3f}'
def f4(x): return 'nan' if x != x else f'{x:.4f}'

def main():
    d = sys.argv[1]; seeds = sys.argv[2:]
    SUM = {}   # (seed, nation, sector, period) -> {col: sum}
    QS = {}    # (seed, nation, sector) -> {quarter: {col: val}}
    chk = collections.defaultdict(float)
    for sd in seeds:
        for r in rows(os.path.join(d, f'slotmg_s{sd}.tsv')):
            y = int(r['year']); q = int(r['quarter']); nat = r['nation']; sec = r['sector']
            for c in ['nPathDesire', 'nRestoreBad', 'nT0Bad', 'nMcBad', 'nZeroBad', 'nDhBad', 'nBindMismatch', 'nPfEdge']:
                chk[(sd, c)] += fl(r[c])
            chk[(sd, 'rate0gap')] = max(chk[(sd, 'rate0gap')], abs(fl(r['nRate0']) - fl(r['nV0']) - fl(r['nCostBad'])) / max(1.0, fl(r['nRate0'])))
            if nat not in NAT: continue
            k = (sd, nat, sec, period(y)); a = SUM.setdefault(k, collections.defaultdict(float))
            for c, v in r.items():
                if c in ('quarter', 'year', 'nation', 'sector'): continue
                a[c] += fl(v)
            QS.setdefault((sd, nat, sec), {})[q] = {c: fl(v) for c, v in r.items() if c not in ('nation', 'sector')}
    print('=== 検算（種ごと・全 NATIONS の行の和）===')
    for sd in seeds:
        print(f'種 {sd}: C1 nPathDesire={chk[(sd,"nPathDesire")]:.3g}  C2a nRestoreBad={chk[(sd,"nRestoreBad")]:.3g}  C2b nT0Bad={chk[(sd,"nT0Bad")]:.3g}  C2c nMcBad={chk[(sd,"nMcBad")]:.3g}  C2d nZeroBad={chk[(sd,"nZeroBad")]:.3g}  C3 nDhBad={chk[(sd,"nDhBad")]:.3g}  C6 nBindMismatch={chk[(sd,"nBindMismatch")]:.3g}  nPfEdge={chk[(sd,"nPfEdge")]:.3g}  C5 rate0 の分割の最大の相対ずれ={chk[(sd,"rate0gap")]:.2g}')
    # C4：slotnd との共通の列
    common = ["months", "sumN", "sumA", "sumX", "sumFill", "sumIdle", "sumOut", "sumRate", "nRate0", "nBin1", "nBin10", "nBin100", "nBin1000", "nBinHi"]
    for sd in seeds:
        mg = {(r['quarter'], r['nation'], r['sector']): r for r in rows(os.path.join(d, f'slotmg_s{sd}.tsv'))}
        nd = list(rows(os.path.join(d, f'slotnd_s{sd}.tsv')))
        bad = 0; miss = 0
        for r in nd:
            m = mg.get((r['quarter'], r['nation'], r['sector']))
            if m is None: miss += 1; continue
            if any(r[c] != m[c] for c in common): bad += 1
        print(f'種 {sd}: C4 slotnd の行 {len(nd)} 件のうち共通の列が不一致 {bad}・slotmg に無い {miss}')
    periods = ['1783-89'] + [f'{y}s' for y in range(1790, 1860, 10)]
    def cell(sd, nat, sec, per): return SUM.get((sd, nat, sec, per))
    def pooled(nat, sec, per, fn):
        v = [fn(cell(sd, nat, sec, per)) for sd in seeds if cell(sd, nat, sec, per)]
        v = [x for x in v if x == x]
        if not v: return 'nan'
        return f'{f3(sum(v)/len(v))}[{f3(min(v))}–{f3(max(v))}]'
    def gm(c, ln, n, neg=None):
        m = c[n] - (c[neg] if neg else 0.0)
        return math.exp(c[ln] / m) if m > 0 else float('nan')
    print('\n=== 表①（種の平均[最小–最大]・N 加重）===')
    cols = [
        ('sumN(千)', lambda c: c['sumN'] / 1000.0),
        ('a', lambda c: div(c['sumA'], c['sumN'])),
        ('aT/a', lambda c: div(c['sumAT'], c['sumA'])),
        ('g幾何', lambda c: gm(c, 'sumLnG', 'nG')),
        ('g_bin0', lambda c: div(c['binG0'], c['nG'])),
        ('g被覆', lambda c: div(c['nG'], c['sumN'])),
        ('pf<1', lambda c: div(c['nPfLt1'], c['sumN'])),
        ('pf平均', lambda c: div(c['sumPf'], c['sumN'])),
        ('fit<1', lambda c: div(c['nFitLt1'], c['sumN'])),
        ('idle>=.1', lambda c: div(c['nIdleGe10'], c['sumN'])),
        ('dH平均', lambda c: div(c['sumDH'], c['sumN'])),
        ('nOut0/All', lambda c: div(c['nOut0'], c['sumNAll'])),
        ('nCap0/All', lambda c: div(c['nCap0'], c['sumNAll'])),
        ('tail/N', lambda c: div(c['nTail'], c['sumN'])),
    ]
    for nat in NAT:
        for sec in SECT:
            print(f'-- {nat} {sec}')
            print('  期間      ' + ' '.join(f'{c[0]:>20}' for c in cols))
            for per in periods:
                if not any(cell(sd, nat, sec, per) for sd in seeds): continue
                print(f'  {per:<9} ' + ' '.join(f'{pooled(nat, sec, per, (lambda fn: (lambda c: fn(c)))(c[1])):>20}' for c in cols))
    print('\n=== 表①-η′とρ（H＝人の占め手・母数 nH／pf<1 の部分集合 nHp）===')
    cols2 = [
        ('ηH幾何', lambda c: gm(c, 'sumLnEtaH', 'nH', 'nEtaHNeg')),
        ('ηHp_bin0', lambda c: div(c['binEtaHp0'], c['nHp'])),
        ('ρH幾何', lambda c: gm(c, 'sumLnRhoH', 'nH', 'nRhoHNeg')),
        ('ρHp_bin0', lambda c: div(c['binRhoHp0'], c['nHp'])),
        ('ρ1H幾何', lambda c: gm(c, 'sumLnRho1H', 'nH', 'nRho1HNeg')),
        ('ρ1Hp_bin0', lambda c: div(c['binRho1Hp0'], c['nHp'])),
        ('ρH_bin2', lambda c: div(c['binRhoH2'], c['nH'])),
        ('ρHneg', lambda c: div(c['nRhoHNeg'], c['nH'])),
        ('ηB幾何', lambda c: gm(c, 'sumLnEtaB', 'nB', 'nEtaBNeg')),
        ('ρB幾何', lambda c: gm(c, 'sumLnRhoB', 'nB', 'nRhoBNeg')),
        ('ρB_bin0', lambda c: div(c['binRhoB0'], c['nB'])),
        ('nB/N', lambda c: div(c['nB'], c['sumN'])),
    ]
    for nat in NAT:
        for sec in SECT:
            print(f'-- {nat} {sec}')
            print('  期間      ' + ' '.join(f'{c[0]:>20}' for c in cols2))
            for per in periods:
                if not any(cell(sd, nat, sec, per) for sd in seeds): continue
                print(f'  {per:<9} ' + ' '.join(f'{pooled(nat, sec, per, (lambda fn: (lambda c: fn(c)))(c[1])):>20}' for c in cols2))
    # 署名
    print('\n=== 署名（セル＝種×国×族×期間）===')
    cnt = collections.Counter(); tot = 0; totN = collections.Counter(); sumNAllc = 0.0
    hold = collections.defaultdict(list)
    for (sd, nat, sec, per), c in sorted(SUM.items()):
        if c['sumN'] <= 0: continue
        tot += 1; w = c['sumN']; sumNAllc += w
        m1 = c['nHp'] > 0 and div(c['binRhoHp0'], c['nHp']) >= 0.9 and div(c['binRho1Hp0'], c['nHp']) >= 0.9 and c['nG'] > 0 and div(c['binG0'], c['nG']) >= 0.9
        m2 = c['nG'] > 0 and div(c['binG1'] + c['binG2'], c['nG']) >= 0.5
        m3 = c['nHp'] > 0 and div(c['binEtaHp1'] + c['binEtaHp2'], c['nHp']) >= 0.25
        m4 = div(c['nIdleGe10'], c['sumN']) >= 0.25
        for nm, v in (('M1', m1), ('M2', m2), ('M3', m3), ('M4', m4)):
            if v: cnt[nm] += 1; totN[nm] += w; hold[nm].append((sd, nat, sec, per))
    for nm in ('M1', 'M2', 'M3', 'M4'):
        print(f'{nm}: 成り立つセル {cnt[nm]}／{tot}（N 加重の割合 {div(totN[nm], sumNAllc):.3f}）')
    # M2 の開幕 40 四半期
    m2open = collections.Counter(); m2openTot = collections.Counter()
    for (sd, nat, sec), qd in QS.items():
        ng = sum(v['nG'] for q, v in qd.items() if q < 40); g12 = sum(v['binG1'] + v['binG2'] for q, v in qd.items() if q < 40)
        if ng > 0: m2openTot[nat] += 1; m2open[nat] += 1 if g12 / ng >= 0.5 else 0
    print('M2（開幕 40 四半期で |log10 g|≧0.05 が N の 50% 以上）：' + '  '.join(f'{n}:{m2open[n]}/{m2openTot[n]}' for n in NAT))
    # M5
    m5 = []
    for (sd, nat, sec), qd in QS.items():
        run = 0; best = 0
        for q in sorted(qd):
            v = qd[q]; frac = div(v['nOut0'] + v['nCap0'], v['sumNAll'])
            run = run + 1 if frac >= 0.1 else 0; best = max(best, run)
        m5.append((best >= 8, best, sd, nat, sec))
    print('M5（(nOut0+nCap0)/sumNAll≧0.1 が連続 8 四半期以上）：' + str(sum(1 for x in m5 if x[0])) + '／' + str(len(m5)) + ' セル（種×国×族）')
    for nat in NAT:
        print(f'   {nat}: ' + ' '.join(f'{sec}={sum(1 for x in m5 if x[0] and x[3]==nat and x[4]==sec)}/{sum(1 for x in m5 if x[3]==nat and x[4]==sec)}' for sec in SECT) + ' 最長連続 ' + str(max([x[1] for x in m5 if x[3] == nat] or [0])))
    # 表②
    print('\n=== 表②（a＝0 の山の理由・N 加重・国ごと・全期間・種の合計）===')
    reasons = collections.defaultdict(lambda: collections.defaultdict(float))
    goods = collections.defaultdict(lambda: collections.defaultdict(float))
    provs = collections.defaultdict(list)
    for sd in seeds:
        for r in rows(os.path.join(d, f'slotmg0_s{sd}.tsv')):
            nat = r['nation']
            if nat not in NAT: continue
            reasons[nat][r['reason']] += fl(r['sumN'])
            goods[(nat, r['reason'])][r['good']] += fl(r['sumN'])
            if r['reason'] != 'dropped': provs[(nat, r['reason'])].append(fl(r['topShare']))
    for nat in NAT:
        tot_ = sum(reasons[nat].values())
        print(f'-- {nat}（N 合計 {tot_:.4g}）: ' + '  '.join(f'{k}={v/tot_:.3f}' for k, v in sorted(reasons[nat].items(), key=lambda kv: -kv[1])) if tot_ > 0 else f'-- {nat}: 行なし')
        for rs, _ in sorted(reasons[nat].items(), key=lambda kv: -kv[1])[:3]:
            if rs == 'dropped': continue
            gs = sorted(goods[(nat, rs)].items(), key=lambda kv: -kv[1])[:4]
            ts = provs[(nat, rs)]
            print(f'     {rs}: 上位の財 ' + ' '.join(f'{g}={v/tot_:.3f}' for g, v in gs) + (f'・topShare の中央値 {sorted(ts)[len(ts)//2]:.2f}' if ts else ''))
    # 表③
    print('\n=== 表③（ΣN の伸び Δln ΣN と各量の変化の符号の一致＝遅れ 0〜8 四半期・交絡する＝因果の主張に使わない）===')
    meas = [('fill', lambda v: div(v['sumFill'], v['sumN'])), ('pf', lambda v: div(v['sumPf'], v['sumN'])), ('inv', lambda v: div(v['sumInv'], v['sumN'])),
            ('価格/mc', lambda v: div(v['sumPriceOverMc'], v['sumN'])), ('労働律速', lambda v: div(v['nBindLabor'], v['sumN'])), ('材料律速', lambda v: div(v['nBindMaterial'], v['sumN']))]
    for nat in ['イギリス', '清']:
        for sec in SECT:
            agg = collections.defaultdict(lambda: collections.defaultdict(float))
            for sd in seeds:
                for q, v in QS.get((sd, nat, sec), {}).items():
                    for c in ('sumN', 'sumFill', 'sumPf', 'sumInv', 'sumPriceOverMc', 'nBindLabor', 'nBindMaterial'): agg[q][c] += v[c]
            qs = sorted(agg)
            if len(qs) < 20: continue
            lnN = [math.log(agg[q]['sumN']) if agg[q]['sumN'] > 0 else float('nan') for q in qs]
            dN = [lnN[i] - lnN[i - 1] for i in range(1, len(qs))]
            print(f'-- {nat} {sec}（四半期 {len(qs)}）')
            for nm, fn in meas:
                ser = [fn(agg[q]) for q in qs]; dS = [ser[i] - ser[i - 1] for i in range(1, len(qs))]
                out = []
                for lag in range(0, 9):
                    ok = n = 0
                    for i in range(len(dN) - lag):
                        a_, b_ = dN[i], dS[i + lag]
                        if a_ == a_ and b_ == b_ and abs(a_) > 1e-6 and abs(b_) > 1e-9: n += 1; ok += 1 if (a_ > 0) == (b_ > 0) else 0
                    out.append(f'{ok/n:.2f}' if n else 'nan')
                print(f'   {nm:<8} ' + ' '.join(out))

if __name__ == '__main__': main()
