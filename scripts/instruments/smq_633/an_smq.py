# (633) 線 smq：枠の採算の率の分子と分母の分解（表 slotnd_s）の読み手（読むだけ）。事前登録 measurements/smq633_20261028_prereg.txt [3][4]。
# 使い方：python -X utf8 an_smq.py <計器の出力フォルダ> <種> [<種> ...]
#   フォルダ直下に slotnd_s<種>.tsv と slot_s<種>.tsv（検算 C3）がある前提。
# 出力：C1〜C3（計器の検算）／表①（分子 RV＝12×v÷年賃金・分母 SY＝枠の値段÷年賃金・資本の費用・rate の幾何平均と区間）／表②（uH の分解）／署名 D1〜D3 の割合。
# 区間：1783〜1789 年・1790 年代〜1850 年代の十年。読む国：英・仏・普・米・日・清（NATIONS の他の国は出さない）。
import csv, glob, os, sys, math

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

SUMS = None
def load(d, seed):
    out = []
    for r in rows(os.path.join(d, f'slotnd_s{seed}.tsv')):
        v = {k: fl(x) for k, x in r.items() if k not in ('nation', 'sector')}
        v['nation'] = r['nation']; v['sector'] = r['sector']
        out.append(v)
    return out

def agg(rs):
    a = {}
    for r in rs:
        for k, x in r.items():
            if k in ('nation', 'sector', 'quarter', 'year'): continue
            if x != x: continue
            if k in ('maxRate', 'maxA'): a[k] = max(a.get(k, 0.0), x)
            else: a[k] = a.get(k, 0.0) + x
    return a

def dv(a, b): return a / b if b and b == b and b != 0 else float('nan')

def f(x, n=3):
    if x != x: return 'nan'
    if abs(x) >= 1000: return f'{x:.0f}'
    return f'{x:.{n}g}'

def checks(d, seeds):
    print('== C1〜C3 計器の検算')
    for s in seeds:
        rs = load(d, s)
        if not rs: print(f'seed {s}: slotnd_s が無い'); continue
        a = agg(rs)
        print(f'seed {s}: 行 {len(rs)}  C1 pathDesire={a.get("pathDesire", 0):.6g}（pathPlan={a.get("pathPlan", 0):.6g}）  C2 rateMismatchN={a.get("rateMismatchN", 0):.6g}  C2′ capMismatchN={a.get("capMismatchN", 0):.6g}  nNonFinite={a.get("nNonFinite", 0):.6g}')
        # 区間の和の恒等：pathDesire+pathPlan＝sumN、rate の区間の和＝sumN
        bad = 0
        for r in rs:
            if abs(r['pathDesire'] + r['pathPlan'] - r['sumN']) > 1e-9 * max(1.0, r['sumN']): bad += 1
            if abs(r['nRate0'] + r['nBin1'] + r['nBin10'] + r['nBin100'] + r['nBin1000'] + r['nBinHi'] - r['sumN']) > 1e-9 * max(1.0, r['sumN']): bad += 1
        print(f'   恒等（経路の和・rate 区間の和＝sumN）が割れた行 {bad}')
        # C3：slot_s の planRate（N 加重）との照合（国×四半期）
        sl = {}
        for r in rows(os.path.join(d, f'slot_s{s}.tsv')):
            sl[(r['nation'], int(fl(r['quarter'])))] = (fl(r['planRate']), fl(r['planN']))
        mine = {}
        for r in rs:
            k = (r['nation'], int(r['quarter']))
            m = mine.setdefault(k, [0.0, 0.0]); m[0] += r['sumRate']; m[1] += r['sumN']
        worst = 0.0; n = 0; first = {}
        for k, (sr, sn) in mine.items():
            if k not in sl: continue
            pr, pn = sl[k]
            # slot_s の planRate は月ごとの N 加重の平均＝Σ(N×rate)÷ΣN（投資家のある州だけ）。こちらは全ての枠のある州。
            if sn > 0 and pr == pr:
                rel = abs(sr / sn - pr) / max(1e-12, abs(pr)); worst = max(worst, rel); n += 1
            if k[1] == min(q for (nn, q) in mine if nn == k[0]): first[k[0]] = (sr / sn if sn else float('nan'), pr)
        print(f'   C3 slot_s.planRate との相対差の最大 {worst:.3g}（{n} 国×四半期）')
        print('   q0 の N 加重 rate（slotnd 合算 / slot_s.planRate）：' + '  '.join(f'{k} {v[0]:.4g}/{v[1]:.4g}' for k, v in first.items() if k in NAT))

def table1(d, seeds):
    print('\n== 表① 分子 RV（枠 1 つの年の得 12×v÷年賃金）・分母 SY（枠の値段÷年賃金）・CC（資本の費用・年）・rate の幾何平均（GM）と N の割合 [rate<10 / 10〜100 / 100〜1000 / 1000〜]・idle・稼働の余り')
    for s in seeds:
        rs = load(d, s)
        print(f'-- seed {s}')
        for nat in NAT:
            for sec in SECT:
                sub = [r for r in rs if r['nation'] == nat and r['sector'] == sec]
                if not sub: continue
                pers = {}
                for r in sub: pers.setdefault(period(int(r['year'])), []).append(r)
                cells = []
                for p in sorted(pers):
                    a = agg(pers[p])
                    wage = dv(a.get('sumWageYear', 0), a.get('nWage', 0))
                    rv = dv(12 * dv(a.get('sumV', 0), a['sumN']), wage); sy = dv(dv(a.get('sumSlotCost', 0), a['sumN']), wage)
                    cc = dv(a.get('sumCapCost', 0), a['sumN']); gm = 10 ** dv(a.get('sumLogRate', 0), a.get('nLogRate', 0)) if a.get('nLogRate', 0) else float('nan')
                    lo = dv(a.get('nBin1', 0) + a.get('nBin10', 0) + a.get('nRate0', 0), a['sumN']); b2 = dv(a.get('nBin100', 0), a['sumN']); b3 = dv(a.get('nBin1000', 0), a['sumN']); b4 = dv(a.get('nBinHi', 0), a['sumN'])
                    idle = dv(a.get('sumIdle', 0), a.get('nCap', 0))
                    cells.append(f'{p}: RV {f(rv)} SY {f(sy)} CC {f(cc)} GM {f(gm)} [{f(lo, 2)}/{f(b2, 2)}/{f(b3, 2)}/{f(b4, 2)}] idle {f(idle, 2)}')
                print(f'{nat} {sec}'); [print('   ' + c) for c in cells]

def table2(d, seeds):
    print('\n== 表② uH の分解（plan の N 加重の平均・月あたりの貨幣）と RH＝12×uH÷年賃金・v÷uH・機械が占め手の最大の割合・uH が非正の割合・稼働')
    for s in seeds:
        rs = load(d, s)
        print(f'-- seed {s}')
        for nat in NAT:
            for sec in SECT:
                sub = [r for r in rs if r['nation'] == nat and r['sector'] == sec]
                if not sub: continue
                pers = {}
                for r in sub: pers.setdefault(period(int(r['year'])), []).append(r)
                print(f'{nat} {sec}')
                for p in sorted(pers):
                    a = agg(pers[p]); pp = a.get('pathPlan', 0)
                    if not pp: continue
                    wage = dv(a.get('sumWageYear', 0), a.get('nWage', 0))
                    m = lambda k: dv(a.get(k, 0), pp)
                    print(f'   {p}: price {f(dv(a.get("sumPrice", 0), a["sumN"]))} self {f(dv(a.get("sumSelf", 0), a["sumN"]))} nonLabor {f(m("sumNonLabor"))} w0 {f(m("sumW0"))} lam {f(m("sumLam"))} w0*lam {f(m("sumW0Lam"))} a {f(m("sumA"))} X(N加重) {f(dv(a.get("sumX", 0), pp))}'
                          f' uH {f(dv(a.get("sumUH", 0), a["sumN"]))} bestMach {f(m("sumBestMach"))} v {f(dv(a.get("sumV", 0), a["sumN"]))} RH {f(dv(12 * dv(a.get("sumUH", 0), a["sumN"]), wage))}'
                          f' winMach {f(dv(a.get("nWinMachine", 0), pp), 2)} uH<=0 {f(dv(a.get("nUHneg", 0), a["sumN"]), 2)} fill {f(dv(a.get("sumFill", 0), a["sumN"]), 2)} out/N {f(dv(a.get("sumOut", 0), a["sumN"]))} maxA {f(a.get("maxA", 0))}')
                    nb = a.get('nBestAnimal', 0) + a.get('nBestWater', 0) + a.get('nBestSteam', 0) + a.get('nBestElectric', 0)
                    print(f'       machine occupant (positive best): share of plan N {f(dv(nb, pp), 2)}  gross {f(dv(a.get("sumBestGross", 0), nb))} capital charge {f(dv(a.get("sumBestCharge", 0), nb))}  kinds animal/water/steam/electric '
                          f'{f(dv(a.get("nBestAnimal", 0), nb), 2)}/{f(dv(a.get("nBestWater", 0), nb), 2)}/{f(dv(a.get("nBestSteam", 0), nb), 2)}/{f(dv(a.get("nBestElectric", 0), nb), 2)}')
                    print(f'       denominators: slotCost {f(dv(a.get("sumSlotCost", 0), a["sumN"]))} bond {f(dv(a.get("sumBond", 0), a["sumN"]), 3)} wear12 {f(dv(a.get("sumWear12", 0), a["sumN"]), 3)} crowd {f(dv(a.get("sumCrowd", 0), a["sumN"]), 3)} heat {f(dv(a.get("sumHeat", 0), a["sumN"]), 3)} floorShare {f(dv(a.get("nFloor", 0), a["sumN"]), 2)}')

def signatures(d, seeds):
    print('\n== 署名 D1〜D3（国×区分×四半期の行のうち 1783〜1830 年で成り立つ割合・N で重み付けない行の数）')
    for s in seeds:
        rs = [r for r in load(d, s) if r['nation'] in NAT and 1783 <= r['year'] <= 1830]
        print(f'-- seed {s}')
        for nat in NAT:
            sub = [r for r in rs if r['nation'] == nat and r['sumN'] > 0]
            if not sub: continue
            d1 = d2 = d3 = 0
            for r in sub:
                idle = dv(r['sumIdle'], r['nCap']); hi = dv(r['nBin100'] + r['nBin1000'] + r['nBinHi'], r['sumN'])   # rate≧10（事前登録 [4] D1）。区間の名前は上端：nBin1＝(0,1)・nBin10＝[1,10)・nBin100＝[10,100)・nBin1000＝[100,1000)・nBinHi＝[1000,∞)
                D1 = idle == idle and idle >= 0.3 and hi >= 0.5
                fl_ = dv(r['nFloor'], r['sumN']); heat = dv(r['sumHeat'], r['sumBond'] + r['sumWear12'])
                D2 = (fl_ == fl_ and fl_ >= 0.1) or (heat == heat and heat >= 0.5)
                wage = dv(r['sumWageYear'], r['nWage']); sy = dv(dv(r['sumSlotCost'], r['sumN']), wage)
                D3 = sy == sy and sy < 1 and not D1 and not D2
                d1 += D1; d2 += D2; d3 += D3
            n = len(sub)
            print(f'{nat}: 行 {n}  D1 {d1 / n:.2f}  D2 {d2 / n:.2f}  D3 {d3 / n:.2f}')

def summary(d, seeds):
    print('\n== 要約（国の全区分の合算・区間ごと）：RV 12×v÷年賃金／RH 12×uH÷年賃金／SY 枠の値段÷年賃金／CC 資本の費用／GM rate の幾何平均／rate>=10・rate>=1＝その rate の N の割合／idle・fill／uH≦0 の割合／機械が最大の割合／機械の得÷機械の資本の費用')
    for s in seeds:
        rs = [r for r in load(d, s) if r['nation'] in NAT]
        print(f'-- seed {s}')
        for nat in NAT:
            sub = [r for r in rs if r['nation'] == nat]
            pers = {}
            for r in sub: pers.setdefault(period(int(r['year'])), []).append(r)
            for p in ['1783-89', '1810s', '1850s']:
                if p not in pers: continue
                a = agg(pers[p]); n = a['sumN']; pp = a.get('pathPlan', 0)
                wage = dv(a.get('sumWageYear', 0), a.get('nWage', 0))
                nb = a.get('nBestAnimal', 0) + a.get('nBestWater', 0) + a.get('nBestSteam', 0) + a.get('nBestElectric', 0)
                hi = dv(a.get('nBin100', 0) + a.get('nBin1000', 0) + a.get('nBinHi', 0), n); ge1 = dv(a.get('nBin10', 0) + a.get('nBin100', 0) + a.get('nBin1000', 0) + a.get('nBinHi', 0), n)
                print(f'{nat} {p}: RV {f(dv(12 * dv(a.get("sumV", 0), n), wage))} RH {f(dv(12 * dv(a.get("sumUH", 0), n), wage))} SY {f(dv(dv(a.get("sumSlotCost", 0), n), wage))} CC {f(dv(a.get("sumCapCost", 0), n))} '
                      f'GM {f(10 ** dv(a.get("sumLogRate", 0), a.get("nLogRate", 0)))} rate>=10 {f(hi, 2)} rate>=1 {f(ge1, 2)} idle {f(dv(a.get("sumIdle", 0), a.get("nCap", 0)), 2)} fill {f(dv(a.get("sumFill", 0), n), 2)} '
                      f'uH<=0 {f(dv(a.get("nUHneg", 0), n), 2)} winMach {f(dv(a.get("nWinMachine", 0), pp), 2)} gross/charge {f(dv(a.get("sumBestGross", 0), a.get("sumBestCharge", 0)))} maxA {f(a.get("maxA", 0))} floor {f(dv(a.get("nFloor", 0), n), 2)} heat/(bond+wear) {f(dv(a.get("sumHeat", 0), a.get("sumBond", 0) + a.get("sumWear12", 0)), 2)}')

def sens(d, seeds):
    # 追記 4（点検への応答）：署名の閾値への敏感さ。後付けの読み＝採否に使わない。D2 の熱の比の分布・SY<1 の割合（D1・D2 と独立）・SY を 3.5 年（出典なし〈推〉）に揃えたときの rate の幾何平均の算術。
    print('\n== 追記 4 敏感さ（1783〜1830 年・国×区分×四半期の行・N で重み付けない）：heat＝sumHeat÷(sumBond＋sumWear12) の中央値・[0.4,0.6) の割合・>=0.5 の割合／SY<1 の割合／1783-89 の SY0 と GM0 と GM0×SY0÷3.5')
    for s in seeds:
        rs = [r for r in load(d, s) if r['nation'] in NAT and 1783 <= r['year'] <= 1830 and r['sumN'] > 0]
        print(f'-- seed {s}')
        for nat in NAT:
            sub = [r for r in rs if r['nation'] == nat]
            if not sub: continue
            hs = sorted(h for h in (dv(r['sumHeat'], r['sumBond'] + r['sumWear12']) for r in sub) if h == h)
            med = hs[len(hs) // 2] if hs else float('nan')
            mid = sum(1 for h in hs if 0.4 <= h < 0.6) / len(hs) if hs else float('nan')
            ge5 = sum(1 for h in hs if h >= 0.5) / len(hs) if hs else float('nan')
            sy_lt1 = 0; n = 0
            for r in sub:
                wage = dv(r['sumWageYear'], r['nWage']); sy = dv(dv(r['sumSlotCost'], r['sumN']), wage)
                if sy == sy: n += 1; sy_lt1 += sy < 1
            op = agg([r for r in sub if r['year'] <= 1789])
            wage0 = dv(op.get('sumWageYear', 0), op.get('nWage', 0)); sy0 = dv(dv(op.get('sumSlotCost', 0), op.get('sumN', 0)), wage0)
            gm0 = 10 ** dv(op.get('sumLogRate', 0), op.get('nLogRate', 0)) if op.get('nLogRate', 0) else float('nan')
            print(f'{nat}: 行 {len(sub)}  heat 中央値 {f(med, 2)} [0.4,0.6) {f(mid, 2)} >=0.5 {f(ge5, 2)}  SY<1 {f(dv(sy_lt1, n), 2)}  SY0 {f(sy0)} GM0 {f(gm0)} GM0×SY0÷3.5 {f(gm0 * sy0 / 3.5)}')

if __name__ == '__main__':
    d = sys.argv[1]; seeds = sys.argv[2:]
    checks(d, seeds); summary(d, seeds); table1(d, seeds); table2(d, seeds); signatures(d, seeds); sens(d, seeds)
