#!/usr/bin/env python
# (438) 動力の束（PowerRebuild）＋設備からの能力（FactoryCapacityFromPlant）：事前登録 measurements/pv_20261019_prereg.txt の狙いの数字 G1〜G8 の読み手（読むだけ）。
# 入力＝計器つき 10 種の出力フォルダ（INS=1 bash scripts/instruments/batch.sh run <腕> の <出力>/ins）。
#   B   ＝腕 B（必須）：power_s<種>.tsv・class_s<種>.tsv・factory_s<種>.tsv・region_s<種>.tsv
#   A   ＝比べる側（必須）：m436 の計器つき 10 種の生（D:/PoB_work/keep_436/m436_ins）。class_s・factory_s・region_s を読む（power_s は無くてよい）。
#   AP  ＝（必須）A の世界の power_s を持つフォルダ（同じ木の旗 0 の計器つき・全長）。G1・G3 の対照と R9 の A 側。
# 使い方：PYTHONUTF8=1 python -X utf8 an_pv.py B=<insB> A=<m436_ins> AP=<insA_power>
# 出力：線ごとに値と合否。入力が無い物は未評価（合格に数えない）。
# ⚠ 線の幅・規則は全て事前登録の字面のまま。この読み手は線を動かさない。
import csv, glob, os, re, statistics, sys, collections

ENG, FRA, PRU, USA, JPN, CHN, RUS = 'イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア'
SIX = [ENG, FRA, PRU, USA, JPN, CHN]            # class_s（G4・G8 前半）：class_s は全ての国が出る
SIX_F = [ENG, FRA, PRU, RUS, JPN, CHN]          # factory_s（G5・G8 後半）：m436 の計器つき 10 種の factory_s は 8 国（米を含まない）＝米をロシアに替える（事前登録 追記3）
SIX_ADOPT = [ENG, FRA, PRU, USA, JPN, RUS]      # power_s（G6）：B と AP は NATIONS に米を足して回す（追記3）
TAGS = ['hum', 'ani', 'wat', 'stm', 'ele']
N_PER_CAP = {'wat': 15.0, 'stm': 4.0, 'ele': 6.0}   # PowerMachines.Table と同じ値（追記4 で固定した物）


def f(v):
    try:
        return float(v)
    except Exception:
        return float('nan')


def isnum(x):
    return x == x and abs(x) != float('inf')


def med(xs):
    xs = [x for x in xs if isnum(x)]
    return statistics.median(xs) if xs else float('nan')


def read_tsv(path):
    with open(path, encoding='utf-8') as fh:
        return list(csv.DictReader(fh, delimiter='\t'))


def seeds_of(d, stem):
    out = {}
    for p in sorted(glob.glob(os.path.join(d, stem + '_s*.tsv'))):
        m = re.search(stem + r'_s(\d+)\.tsv$', p)
        if m:
            out[int(m.group(1))] = p
    return out


# ---------------------------------------------------------------- class_s（工場労働者の階級 classId 2）
def class_stats(d):
    """seed -> {(nation, year): (share, relIncome, sizeK)}（四半期 q%4==3 の行）。"""
    res = {}
    for s, p in seeds_of(d, 'class').items():
        by = collections.defaultdict(lambda: collections.defaultdict(lambda: [0.0, 0.0]))
        with open(p, encoding='utf-8') as fh:
            r = csv.reader(fh, delimiter='\t'); next(r)
            for row in r:
                if not row[0].startswith('q'):
                    continue
                q = int(row[0][1:])
                if q % 4 != 3:
                    continue
                n = row[1]
                if n not in SIX:
                    continue
                size, e = f(row[5]), f(row[8])
                if not (isnum(size) and isnum(e)):
                    continue
                y = int(row[2]); c = int(row[3])
                by[(n, y)][c][0] += size; by[(n, y)][c][1] += e
        out = {}
        for (n, y), d2 in by.items():
            tot = sum(v[0] for v in d2.values()); te = sum(v[1] for v in d2.values())
            if 2 in d2 and d2[2][0] > 0 and tot > 0 and te > 0:
                out[(n, y)] = (d2[2][0] / tot, (d2[2][1] / d2[2][0]) / (te / tot), d2[2][0])
        res[s] = out
    return res


# ---------------------------------------------------------------- factory_s・region_s（工場の産出÷人口・閉鎖の割合）
def factory_stats(d):
    """seed -> {(nation, year): (outPerCapita, bankruptShare)}。"""
    res = {}
    for s, p in seeds_of(d, 'factory').items():
        rp = seeds_of(d, 'region').get(s)
        if rp is None:
            continue
        pop = collections.defaultdict(float)
        for x in read_tsv(rp):
            if x['activeFlag'] != '1':
                continue
            q = int(x['quarter'])
            if q % 4 != 3:
                continue
            pop[(q, x['nation'])] += f(x['populationK'])
        out = {}
        for x in read_tsv(p):
            q = int(x['quarter'])
            if q % 4 != 3:
                continue
            n = x['nation']
            if n not in SIX_F:
                continue
            pp = pop.get((q, n), 0.0)
            if pp > 0:
                cnt = f(x['factoryCount']); bk = f(x['factoryBankruptCount'])
                out[(n, int(x['year']))] = (f(x['outputRefValuePerMonth']) / pp, (bk / cnt) if isnum(cnt) and cnt > 0 and isnum(bk) else float('nan'))
        res[s] = out
    return res


def med_over_seeds(per_seed, key, idx):
    return med([per_seed[s][key][idx] for s in per_seed if key in per_seed[s]])


# ---------------------------------------------------------------- power_s
class PowerSeed:
    def __init__(self, path, tiers):
        rows = read_tsv(path)
        self.out = collections.defaultdict(lambda: collections.defaultdict(float))   # (国, 年) -> 列 -> 和（本国の州）
        self.disc = {}                                                                # 国 -> 蒸気の行を解禁した最初の年
        self.ani_seen = self.ani_pos = self.ani_nan = 0
        self.g2_n = self.g2_ok = 0
        self.fac_n = self.fac_plant = 0
        self.g3_n = self.g3_steam_bad = self.g3_lab_bad = self.g3_lab_less = 0
        self.cost = collections.defaultdict(list)                                     # 年 -> [英の州の最安の実質の費用]
        self.wage = collections.defaultdict(lambda: [0.0, 0.0, 0.0])                 # (国, 年) -> [wageFx×w, wageReal×w, w]
        self.unit = collections.defaultdict(lambda: collections.defaultdict(list))    # (国, 年) -> タグ -> [実質の費用]
        self.prov = collections.defaultdict(list)                                     # 年 -> [(主な動力, workerIncomePerHead)]（四半期 q%4==3・Core・本国の州。R9）
        self.hs = collections.defaultdict(list)                                       # (国, 年) -> [humUnit÷stmUnit]（実質の費用の比。G6 の人件費の差の読み）
        for x in rows:
            y = int(x['year']); nat = x['nation']; reg = x['region']
            tier = tiers.get((x['quarter'], reg), 'Core')
            if f(x['discSteam']) == 1 and nat not in self.disc:
                self.disc[nat] = y
            a = f(x['aniOut'])
            if isnum(a):
                self.ani_seen += 1
                if a > 0:
                    self.ani_pos += 1
            else:
                self.ani_nan += 1
            # G3
            sf = f(x['facSteamFill']); nom = f(x['facLabNom']); eff = f(x['facLabEff'])
            if isnum(sf) and isnum(nom) and isnum(eff):
                self.g3_n += 1
                if abs(sf) > 0:
                    self.g3_steam_bad += 1
                if abs(eff - nom) > 1e-9 * max(1.0, abs(nom)):
                    self.g3_lab_bad += 1
                if eff < nom * (1 - 1e-9):
                    self.g3_lab_less += 1
            # G2a：機械のレシピの能力＝据え付け÷台数/能力×門
            for t, n_ in N_PER_CAP.items():
                gate = f(x[t + 'Gate']); cap = f(x[t + 'Cap']); sup = f(x[t + 'ScaleSup'])
                if isnum(gate) and gate > 0 and isnum(cap) and isnum(sup):
                    self.g2_n += 1
                    if abs(cap - sup / n_ * gate) <= 1e-6 * max(abs(cap), 1e-12):
                        self.g2_ok += 1
            # G2b：設備が頭数を超えた州×四半期（工場のある州）
            fo = f(x['facOut']); pk = f(x['plantK']); wk = f(x['workersK'])
            if isnum(fo) and fo > 0 and isnum(pk) and isnum(wk) and wk > 0:
                self.fac_n += 1
                if pk > 1.001 * wk:
                    self.fac_plant += 1
            if tier != 'Core':
                continue
            for t in TAGS:
                v = f(x[t + 'Out'])
                if isnum(v):
                    self.out[(nat, y)][t] += v
            w = f(x['workersK']); wf = f(x['wageFx']); wr = f(x['wageReal'])
            if isnum(w) and isnum(wf) and isnum(wr):
                a2 = self.wage[(nat, y)]; a2[0] += wf * w; a2[1] += wr * w; a2[2] += w
            if int(x['quarter']) % 4 == 3:
                ho, wo, so, eo = f(x['humOut']), f(x['watOut']), f(x['stmOut']), f(x['eleOut'])
                wi = f(x['workerIncomePerHead'])
                if all(isnum(v) for v in (ho, wo, so, eo, wi)) and ho + wo + so + eo > 0:
                    tot_ = ho + wo + so + eo
                    kind = 'ヒト' if ho >= 0.5 * tot_ else ('水車' if wo >= so + eo else '蒸気電動')
                    self.prov[y].append((kind, wi))
                hu_, su_ = f(x['humUnit']), f(x['stmUnit'])
                if isnum(hu_) and isnum(su_) and su_ > 0 and hu_ > 0:
                    self.hs[(nat, y)].append(hu_ / su_)
            cpl = f(x['cpl']); lpm = f(x['lpm'])
            if isnum(cpl) and isnum(lpm) and cpl > 0 and lpm > 0:
                best = float('inf')
                for t in ['hum', 'wat', 'stm', 'ele']:
                    u = f(x[t + 'Unit']); g = f(x[t + 'Gate'])
                    if not isnum(u):
                        continue
                    real = u / (cpl * lpm)
                    self.unit[(nat, y)][t].append(real)
                    if t == 'hum' or (isnum(g) and g > 0):
                        best = min(best, real)
                if best < float('inf') and nat == ENG:
                    self.cost[y].append(best)

    def year50(self, nat):
        ys = sorted({y for (n, y) in self.out if n == nat})
        for y in ys:
            o = self.out[(nat, y)]
            tot = o['hum'] + o['wat'] + o['stm'] + o['ele']
            if tot > 0 and (o['stm'] + o['ele']) / tot >= 0.5:   # (438 追記3) 導入＝蒸気＋電動が過半（水車は蒸気の解禁と関係なく開くので数えない）
                return y
        return 9999

    def share(self, nat, y, tag):
        o = self.out.get((nat, y))
        if not o:
            return float('nan')
        tot = o['hum'] + o['wat'] + o['stm'] + o['ele']
        return o[tag] / tot if tot > 0 else float('nan')


def tiers_of(d, s):
    rp = seeds_of(d, 'region').get(s)
    t = {}
    if rp:
        for x in read_tsv(rp):
            t[(x['quarter'], x['region'])] = x['tier']
    return t


def window_mean(series, lo, hi):
    v = [statistics.median(series[y]) for y in range(lo, hi + 1) if y in series and series[y]]
    return sum(v) / len(v) if v else float('nan')


def verdict(ok):
    return '合格' if ok is True else ('不合格' if ok is False else '未評価')


def main():
    args = dict(a.split('=', 1) for a in sys.argv[1:] if '=' in a)
    B, A, AP = args.get('B'), args.get('A'), args.get('AP')
    if not B or not A or not AP:
        print('使い方：an_pv.py B=<insB> A=<m436_ins> AP=<insA_power>（AP は必須＝G1・G3 の対照と R9 の A 側。無いと〈何もしない機構〉が合格に見える）'); sys.exit(2)
    lines = {}

    # --- power_s（B）
    bseeds = seeds_of(B, 'power')
    pb = {s: PowerSeed(p, tiers_of(B, s)) for s, p in bseeds.items()}
    print(f'B の power_s：{len(pb)} 種 {sorted(pb)}')

    # G1
    if pb:
        seen = sum(p.ani_seen for p in pb.values()); pos = sum(p.ani_pos for p in pb.values())
        ok1 = (seen == 0 and pos == 0)
        ap = {s: PowerSeed(p, tiers_of(AP, s)) for s, p in seeds_of(AP, 'power').items()}
        cpos = sum(p.ani_pos for p in ap.values())
        ctrl = f'・対照 A の aniOut＞0 の行 {cpos}'
        if not ap or cpos <= 0:
            ok1 = False
        lines['G1'] = (ok1, f'〔作りの確かめ〕B の aniOut が数の行 {seen}（正 {pos}）・nan の行 {sum(p.ani_nan for p in pb.values())}{ctrl}')
        # G2
        n = sum(p.g2_n for p in pb.values()); ok = sum(p.g2_ok for p in pb.values())
        share_a = ok / n if n else float('nan')
        fn = sum(p.fac_n for p in pb.values()); fp = sum(p.fac_plant for p in pb.values())
        share_b = fp / fn if fn else float('nan')
        lines['G2'] = ((n > 0 and share_a >= 0.99 and isnum(share_b) and share_b >= 0.02),
                       f'〔作りの確かめ・効きの判定ではない〕機械の行 {n} 中 能力＝据え付け÷台数×門 の行 {share_a:.4f}（線 ≥0.99）・工場のある州×四半期 {fn} 中 設備＞頭数×1.001 の割合 {share_b:.4f}（線 ≥0.02）')
        # G3
        n3 = sum(p.g3_n for p in pb.values())
        bad = sum(p.g3_steam_bad + p.g3_lab_bad for p in pb.values())
        ctrl3 = ''
        ok3 = n3 > 0 and bad == 0
        less = sum(p.g3_lab_less for p in ap.values())
        ctrl3 = f'・対照 A の facLabEff＜facLabNom の行 {less}'
        if not ap or less <= 0:
            ok3 = False
        lines['G3'] = (ok3, f'〔作りの確かめ〕B の行 {n3} 中 facSteamFill≠0 または facLabEff≠facLabNom の行 {bad}{ctrl3}')
        # G6
        # ★(443) 反証役 pv〔bv〕中#1：9999（導入しない種の印）を中央値に混ぜない＝導入した種の数と、導入した種だけの中央値を別に出す（旧は 9999 と実際の年を混ぜて清 4188 のような意味の無い値になった）
        y50_all = {nat: [float(p.year50(nat)) for p in pb.values()] for nat in SIX_ADOPT + [CHN]}
        n_adopt = {nat: sum(1 for y in y50_all[nat] if y < 9999) for nat in y50_all}
        y50 = {nat: (med([y for y in y50_all[nat] if y < 9999]) if n_adopt[nat] > 0 else 9999.0) for nat in y50_all}
        disc = {nat: med([float(p.disc.get(nat, float('nan'))) for p in pb.values()]) for nat in SIX_ADOPT + [CHN]}
        lag = {nat: (y50[nat] - disc[nat]) if isnum(y50[nat]) and isnum(disc[nat]) else float('nan') for nat in SIX_ADOPT + [CHN]}
        adopt = sum(1 for nat in SIX_ADOPT if n_adopt[nat] * 2 >= len(y50_all[nat]) and isnum(y50[nat]) and y50[nat] <= 1950)   # ★(443) 種の半分以上で導入した国
        d_lag = (lag[CHN] - lag[ENG]) if isnum(lag[CHN]) and isnum(lag[ENG]) else float('nan')
        chn_never = n_adopt[CHN] == 0
        ok6 = (isnum(y50[ENG]) and y50[ENG] <= 1900 and (chn_never or (isnum(d_lag) and d_lag >= 25)) and adopt >= 4)
        wr = {}
        for nat in (ENG, CHN):
            fx, rl = [], []
            for p in pb.values():
                a_, b_, w_ = 0.0, 0.0, 0.0
                for y in range(1800, 1851):
                    v = p.wage.get((nat, y))
                    if v and v[2] > 0:
                        a_ += v[0]; b_ += v[1]; w_ += v[2]
                if w_ > 0:
                    fx.append(a_ / w_); rl.append(b_ / w_)
            wr[nat] = (med(fx), med(rl))
        ratio_fx = wr[ENG][0] / wr[CHN][0] if isnum(wr[ENG][0]) and isnum(wr[CHN][0]) and wr[CHN][0] > 0 else float('nan')
        ratio_rl = wr[ENG][1] / wr[CHN][1] if isnum(wr[ENG][1]) and isnum(wr[CHN][1]) and wr[CHN][1] > 0 else float('nan')
        lines['G6'] = (ok6, '導入の年（蒸気＋電動が過半・種の中央値・水車は数えない）' + '・'.join(f'{n}{y50[n]:.0f}〔{n_adopt[n]}/{len(y50_all[n])} 種〕' for n in SIX_ADOPT + [CHN]) +
                       '／解禁年 ' + '・'.join(f'{n}{disc[n]:.0f}' for n in SIX_ADOPT) +
                       f'／遅れ 英{lag[ENG]:.0f}・清{lag[CHN]:.0f}（差 {d_lag:.0f}・線 ≥25 または清が導入しない）・1950 までに導入した国 {adopt}/6（線 ≥4）'
                       f'／記録 英÷清 wageFx {ratio_fx:.2f}・wageReal {ratio_rl:.2f}')
        # G7
        ratios = []
        for p in pb.values():
            a_ = window_mean(p.cost, 1818, 1822); b_ = window_mean(p.cost, 1898, 1902)
            if isnum(a_) and isnum(b_) and a_ > 0:
                ratios.append(b_ / a_)
        r7 = med(ratios)
        lines['G7'] = ((isnum(r7) and r7 <= 0.9), f'英の最安の実質の動力費 1900 年÷1820 年（種の中央値）{r7:.3f}（線 ≤0.9・{len(ratios)} 種）')
        # R1：レシピ別の実質の費用の時系列（英・清・米・日の 10 年おき・種の中央値）
        print('R1 実質の動力費（種の中央値・hum/wat/stm/ele）')
        for nat in (ENG, CHN, USA, JPN):
            row = []
            for y in range(1790, 1951, 20):
                cells = []
                for t in ['hum', 'wat', 'stm', 'ele']:
                    v = med([statistics.median(p.unit[(nat, y)][t]) for p in pb.values() if p.unit.get((nat, y)) and p.unit[(nat, y)].get(t)])
                    cells.append(f'{v:.3f}' if isnum(v) else 'nan')
                row.append(f'{y}:' + '/'.join(cells))
            print(f'  {nat} ' + ' '.join(row))
        # R2：産出の割合（hum/wat/stm/ele・本国の州の和・種の中央値）。水車が主役になっていないか（追記3・点検の高 1）
        def shares(pp, nat, y):
            return '/'.join(f'{med([p.share(nat, y, t) for p in pp.values()]):.2f}' for t in ['hum', 'wat', 'stm', 'ele'])
        print('R2 労働力の財の産出の割合（hum/wat/stm/ele）B | A')
        for nat in (ENG, CHN, USA, JPN):
            print(f'  {nat} ' + ' '.join(f'{y}:{shares(pb, nat, y)}|{shares(ap, nat, y)}' for y in range(1800, 1951, 30)))
        # G6 の人件費の差の読み：humUnit÷stmUnit（実質の費用の比・小さいほど人力が相対的に安い）
        def hs(pp, nat):
            v = [p.hs.get((nat, y), []) for p in pp.values() for y in range(1800, 1851)]
            return med([x for xs in v for x in xs])
        print('R-hs humUnit÷stmUnit（1800〜1850・中央値）：' + '・'.join(f'{nat} B {hs(pb, nat):.3f}/A {hs(ap, nat):.3f}' for nat in (ENG, CHN, FRA, USA, JPN)) + '（清が英より低い＝人力が相対的に安い＝導入が遅れる向き）')
        # R9：1900 年の支配的な動力で州を分けた workerIncomePerHead の中央値（点検の高 1(b)＝機械のレシピ別に所得の落ち方を見る）
        print('R9 州（本国・q%4==3）の主な動力別 workerIncomePerHead（中央値[件数]）B | A')
        for y in (1850, 1900, 1950):
            def grp(pp):
                d = collections.defaultdict(list)
                for p in pp.values():
                    for k, wi in p.prov.get(y, []):
                        d[k].append(wi)
                return ' '.join(f'{k}:{med(d[k]):.3f}[{len(d[k])}]' for k in ('ヒト', '水車', '蒸気電動') if d.get(k))
            print(f'  {y} B {grp(pb)} | A {grp(ap)}')
    else:
        print('B に power_s が無い：G1〜G3・G6・G7 は未評価')

    # --- class_s・factory_s・region_s（B と A）
    cb, ca = class_stats(B), class_stats(A)
    fb, fa = factory_stats(B), factory_stats(A)
    # G4
    if cb and ca:
        passed, detail = 0, []
        for n in SIX:
            oks = []
            for y in (1900, 1950):
                for idx in (0, 1):
                    vb = med_over_seeds(cb, (n, y), idx); va = med_over_seeds(ca, (n, y), idx)
                    oks.append(isnum(vb) and isnum(va) and va > 0 and vb >= 0.7 * va)
                    detail.append((n, y, idx, vb, va))
            if all(oks):
                passed += 1
        lines['G4'] = (passed >= 5, f'6 国のうち 工場労働者の人口割合と相対の所得が 1900・1950 で A の 0.7 倍以上の国 {passed}（線 ≥5）')
        print('G4 詳細（国・年・量〔0＝人口割合・1＝相対の所得〕・B・A・比）')
        for n, y, idx, vb, va in detail:
            print(f'  {n} {y} {idx} B={vb:.3f} A={va:.3f} 比={vb / va if isnum(vb) and isnum(va) and va else float("nan"):.3f}')
        # G8 前半：階級の人口の 1950÷1850
        ok8, cnt8 = 0, []
        for n in SIX:
            rb = [cb[s][(n, 1950)][2] / cb[s][(n, 1850)][2] for s in cb if (n, 1950) in cb[s] and (n, 1850) in cb[s] and cb[s][(n, 1850)][2] > 0]
            ra = [ca[s][(n, 1950)][2] / ca[s][(n, 1850)][2] for s in ca if (n, 1950) in ca[s] and (n, 1850) in ca[s] and ca[s][(n, 1850)][2] > 0]
            mb, ma = med(rb), med(ra)
            cnt8.append((n, mb, ma))
            if isnum(mb) and isnum(ma) and ma > 0 and mb >= 0.7 * ma:
                ok8 += 1
        g8a = ok8 >= 5
    else:
        lines['G4'] = (None, 'class_s が B か A に無い')
        g8a = None; cnt8 = []
    # G5・G8 後半
    if fb and fa:
        ok5, d5 = 0, []
        for n in SIX_F:
            rb = [fb[s][(n, 1950)][0] / fb[s][(n, 1850)][0] for s in fb if (n, 1950) in fb[s] and (n, 1850) in fb[s] and fb[s][(n, 1850)][0] > 0]
            ra = [fa[s][(n, 1950)][0] / fa[s][(n, 1850)][0] for s in fa if (n, 1950) in fa[s] and (n, 1850) in fa[s] and fa[s][(n, 1850)][0] > 0]
            mb, ma = med(rb), med(ra)
            d5.append((n, mb, ma))
            if isnum(mb) and isnum(ma) and ma > 0 and mb >= 0.6 * ma:
                ok5 += 1
        lines['G5'] = (ok5 >= 5, f'6 国のうち 工場の産出÷人口の 1950÷1850 が A の 0.6 倍以上の国 {ok5}（線 ≥5）：' + '・'.join(f'{n} B{mb:.2f}/A{ma:.2f}' for n, mb, ma in d5))
        # 閉鎖の割合の最大（1850 以後・国×種）
        def maxbk(fs):
            v = []
            for s in fs:
                for n in SIX_F:
                    xs = [fs[s][(n, y)][1] for y in range(1850, 1951) if (n, y) in fs[s] and isnum(fs[s][(n, y)][1])]
                    if xs:
                        v.append(max(xs))
            return med(v)
        mb, ma = maxbk(fb), maxbk(fa)
        g8b = (isnum(mb) and isnum(ma) and mb <= 2 * ma) if isnum(mb) and isnum(ma) else None
        if g8a is None or g8b is None:
            lines['G8'] = (None, '入力が足りない')
        else:
            lines['G8'] = (g8a and g8b, '階級の人口 1950÷1850 が A の 0.7 倍以上の国 ' + str(ok8 if cnt8 else '?') + '（線 ≥5）：' + '・'.join(f'{n} B{b_:.2f}/A{a_:.2f}' for n, b_, a_ in cnt8) + f'／閉鎖の割合の最大（国×種の中央値）B {mb:.3f}・A {ma:.3f}（線 B ≤ 2×A）')
    else:
        lines['G5'] = (None, 'factory_s か region_s が B か A に無い'); lines['G8'] = (None, '入力が足りない')

    print()
    for k in ['G1', 'G2', 'G3', 'G4', 'G5', 'G6', 'G7', 'G8']:
        if k in lines:
            ok, msg = lines[k]
            print(f'{k} {verdict(ok)}：{msg}')
        else:
            print(f'{k} 未評価')
    gate_keys = ['G1', 'G2', 'G3', 'G4', 'G5', 'G8']
    st = [lines.get(k, (None, ''))[0] for k in gate_keys]
    print()
    if all(x is True for x in st):
        print('採る条件（G1〜G5・G8）：全て合格（G6・G7 は出た／出ないを報告に書く）')
    else:
        print('採る条件（G1〜G5・G8）：', ' '.join(f'{k}={verdict(x)}' for k, x in zip(gate_keys, st)))


if __name__ == '__main__':
    main()
