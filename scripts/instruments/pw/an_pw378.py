#!/usr/bin/env python
# (378) 動力の作り直し：事前登録 measurements/pw_20261017_prereg.txt の線 M1〜M5・L1〜L9 の読み手（読むだけ）。
# 入力＝計器つき 10 種の出力フォルダ（INS=1 bash scripts/instruments/batch.sh run <腕> の <出力>/ins）の power_s<種>.tsv と region_s<種>.tsv。
# 使い方：PYTHONUTF8=1 python an_pw378.py A=<insA>[|<成績表A>] B=<insB>[|<成績表B>] N1=<insN1>[|<成績表N1>] N2=<insN2>[|<成績表N2>]（区切りは縦棒＝ドライブの : と混ざらない）
#   腕の名前は A・B・N1・N2（A と B は必須・N1 と N2 は無くてもよい＝幅は付かない）。成績表（`平均恐慌回数` の行）は L9 だけが読む。
# 出力：線ごとに各腕の値と B の合否（合否は B と A 系〔A・N1・N2〕の比べ）。未評価（入力が無い）は合格に数えない。
# ⚠ 線の幅・規則は全て事前登録の字面のまま。この読み手は線を動かさない。
import csv, glob, os, re, statistics, sys, collections

ENG, FRA, PRU, USA, JPN, CHN, RUS, MAR = 'イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア', 'マラータ同盟'
INDIA_REGIONS = {'中核ベンガル', '内陸ベンガル', 'マドラス', 'ボンベイ'}
IND5 = [ENG, FRA, PRU, USA, JPN]
TAGS = ['hum', 'ani', 'wat', 'stm', 'ele']
# 機械のレシピの台数/能力（PowerMachines.Table と同じ値・追記4 で固定した物。M2 の式が読む）
N_PER_CAP = {'wat': 5.0, 'stm': 4.0, 'ele': 6.0}


def f(v):
    try:
        x = float(v)
    except Exception:
        return float('nan')
    return x


def isnum(x):
    return x == x and abs(x) != float('inf')


def read_tsv(path):
    with open(path, encoding='utf-8') as fh:
        return list(csv.DictReader(fh, delimiter='\t'))


def seeds_of(d):
    out = []
    for p in sorted(glob.glob(os.path.join(d, 'power_s*.tsv'))):
        m = re.search(r'power_s(\d+)\.tsv$', p)
        if m:
            out.append((int(m.group(1)), p))
    return out


def group_of(nation, region, tier):
    """本国の州の群：国の名前（印は英領 4 州の合算・マラータは国）。本国の州＝tier が Core。"""
    if region in INDIA_REGIONS:
        return '印'
    if nation == MAR:
        return MAR
    if tier == 'Core':
        return nation
    return None


class Seed:
    """1 種ぶん：群×年の産出・賃金・解禁と、英の州×四半期の雇用。"""

    def __init__(self, path):
        rows = read_tsv(path)
        rpath = path.replace('power_s', 'region_s')
        tiers, emp, pop = {}, collections.defaultdict(list), {}
        if os.path.exists(rpath):
            for x in read_tsv(rpath):
                tiers[(x['quarter'], x['region'])] = x['tier']
                pop[(x['quarter'], x['region'])] = f(x['populationK'])
                if x['nation'] == ENG and x['tier'] == 'Core':
                    emp[int(x['year'])].append(f(x['employmentShare']))
        self.emp = emp
        self.out = collections.defaultdict(lambda: collections.defaultdict(float))   # (群, 年) -> 列 -> 和
        self.wage = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0])               # (群, 年) -> [wageFx×w, wageReal×w, w, n]
        self.fac = collections.defaultdict(float); self.facPop = collections.defaultdict(float)   # 英の facOut と人口（年）
        self.disc = collections.defaultdict(set)                                          # 年 -> 蒸気の行を解禁した国
        self.ani_seen = 0; self.ani_nan = 0; self.ani_pos = 0
        self.m2_ok = 0; self.m2_n = 0
        self.m3_ok = 0; self.m3_n = 0; self.food_off = 0; self.food_n = 0
        self.m4_ok = 0; self.m4_n = 0
        self.m5_pos = 0; self.m5_grow = 0; self._prev_stm = {}
        self.fill_n = 0; self.fill_low = 0
        self.m3b = collections.defaultdict(lambda: collections.defaultdict(list))   # 年 -> 国 -> [(pL÷(cpl×lpm), foodRel)]
        self.nations_with_steam = collections.defaultdict(set)
        for x in rows:
            y = int(x['year']); nat = x['nation']; reg = x['region']
            tier = tiers.get((x['quarter'], reg), 'Core')
            if f(x['discSteam']) == 1:
                self.disc[y].add(nat)
            g = group_of(nat, reg, tier)
            # M1：畜力の列
            if isnum(f(x['aniOut'])):
                self.ani_seen += 1
                if f(x['aniOut']) > 0:
                    self.ani_pos += 1
            else:
                self.ani_nan += 1
            if g is None:
                continue
            for t in TAGS:
                v = f(x[t + 'Out'])
                if isnum(v):
                    self.out[(g, y)][t] += v
            w = f(x['workersK'])
            wf, wr = f(x['wageFx']), f(x['wageReal'])
            if isnum(wf) and isnum(wr) and isnum(w):
                a = self.wage[(g, y)]; a[0] += wf * w; a[1] += wr * w; a[2] += w; a[3] += 1
            if g == ENG:
                v = f(x['facOut'])
                if isnum(v):
                    self.fac[y] += v
                p = pop.get((x['quarter'], reg), float('nan'))
                if isnum(p):
                    self.facPop[y] += p
            # M3：人力の自己費用が 0.35×cpl×lpm×因子 に一致するか（因子は foodFactor 列）
            hs, cpl, lpm, ff = f(x['humSelf']), f(x['cpl']), f(x['lpm']), f(x['foodFactor'])
            if isnum(hs) and isnum(cpl) and isnum(lpm) and isnum(ff):
                self.m3_n += 1
                if abs(hs - 0.35 * cpl * lpm * ff) <= 1e-6 * max(abs(hs), 1e-12):
                    self.m3_ok += 1
            if isnum(ff):
                self.food_n += 1
                if abs(ff - 1.0) > 0.01:
                    self.food_off += 1
            # M4：因子の削除
            fs, fe, fn = f(x['facSteamFill']), f(x['facLabEff']), f(x['facLabNom'])
            if isnum(fs) and isnum(fe) and isnum(fn):
                self.m4_n += 1
                if abs(fs) <= 1e-12 and abs(fe - fn) <= 1e-9 * max(abs(fn), 1e-12):
                    self.m4_ok += 1
            # M2/M5
            for t in ('wat', 'stm', 'ele'):
                gate, cap, sc = f(x[t + 'Gate']), f(x[t + 'Cap']), f(x[t + 'ScaleSup'])   # 追記3 ③：能力を決めた時点の据え付け
                if isnum(gate) and gate > 0 and isnum(cap) and isnum(sc):
                    n = N_PER_CAP[t]
                    self.m2_n += 1
                    if abs(cap - sc / n * gate) <= 1e-6 * max(cap, 1e-12):
                        self.m2_ok += 1
            if isnum(f(x['stmScale'])) and f(x['stmScale']) > 0:
                self.m5_pos += 1
            # M5（追記3 ⑨）：据え付けが前の年より増えた州×年（買った量が損耗を超えた＝支払いが正の代理）
            for t in ('wat', 'stm', 'ele'):
                sc = f(x[t + 'Scale']); key = (nat, reg, t)
                if isnum(sc):
                    pv = self._prev_stm.get(key)
                    if pv is not None and sc > pv + 1e-12:
                        self.m5_grow += 1
                    self._prev_stm[key] = sc
            # R11：fill（注文に対して原資で払えた割合）
            fl = f(x.get('fill', 'nan'))
            if isnum(fl):
                self.fill_n += 1
                if fl < 0.99:
                    self.fill_low += 1
            # M3b（記録）：国×年の横断で pL÷(cpl×lpm) と foodRel
            pLx, cplx, lpmx, fr = f(x['pL']), f(x['cpl']), f(x['lpm']), f(x['foodRel'])
            if isnum(pLx) and isnum(cplx) and isnum(lpmx) and isnum(fr) and pLx > 0 and cplx > 0 and lpmx > 0 and fr > 0:
                self.m3b[y][nat].append((pLx / (cplx * lpmx), fr))

    def share(self, g, y, t='stm'):
        o = self.out.get((g, y))
        if not o:
            return float('nan')
        tot = o['hum'] + o['wat'] + o['stm'] + o['ele']
        return o[t] / tot if tot > 0 else float('nan')

    def year10(self, g):
        ys = sorted(y for (gg, y) in self.out if gg == g)
        for y in ys:
            s = self.share(g, y)
            if isnum(s) and s >= 0.10:
                return y
        return 9999

    def wage_ratio(self, g1, g2, lo, hi, kind):
        vals = []
        for y in range(lo, hi + 1):
            a, b = self.wage.get((g1, y)), self.wage.get((g2, y))
            if not a or not b or a[2] <= 0 or b[2] <= 0:
                continue
            k = 0 if kind == 'fx' else 1
            x1, x2 = a[k] / a[2], b[k] / b[2]
            if x2 > 0:
                vals.append(x1 / x2)
        return statistics.median(vals) if vals else float('nan')


def med(xs):
    xs = [x for x in xs if isnum(x)]
    return statistics.median(xs) if xs else float('nan')


def mean(xs):
    xs = [x for x in xs if isnum(x)]
    return sum(xs) / len(xs) if xs else float('nan')


def band_crisis(path):
    if not path or not os.path.exists(path):
        return float('nan')
    for line in open(path, encoding='utf-8'):
        m = re.search(r'平均恐慌回数\s*([0-9.]+)', line)
        if m:
            return float(m.group(1))
    return float('nan')


def main():
    arms = {}
    for a in sys.argv[1:]:
        name, rest = a.split('=', 1)
        d, _, band = rest.partition('|')
        arms[name] = (d, band)
    data = {}
    for name, (d, band) in arms.items():
        data[name] = [Seed(p) for _, p in seeds_of(d)]
        print(f'# {name}: {len(data[name])} 種 ({d})')
    nulls = [n for n in ('A', 'N1', 'N2') if n in data]
    res = {}

    def per_arm(fn):
        return {n: fn(data[n]) for n in data}

    # M1〜M5
    print('\n【機構の確かめ】')
    for n in data:
        ss = data[n]
        ani = sum(s.ani_pos for s in ss); nan = sum(s.ani_nan for s in ss)
        m2 = sum(s.m2_ok for s in ss) / max(1, sum(s.m2_n for s in ss))
        m3 = sum(s.m3_ok for s in ss) / max(1, sum(s.m3_n for s in ss))
        foodoff = sum(s.food_off for s in ss) / max(1, sum(s.food_n for s in ss))
        m4 = sum(s.m4_ok for s in ss) / max(1, sum(s.m4_n for s in ss))
        m5 = sum(s.m5_pos for s in ss)
        print(f'  {n}: M1 aniOut>0 の行 {ani}・nan の行 {nan} | M2 据え付け=能力（ScaleSup） {m2:.4f} | M3 自己費用の式の恒等 {m3:.4f}・因子が 1% 離れる行 {foodoff:.3f} | M4 機構の有無（構築上緑になりうる） {m4:.4f} | M5 stmScale>0 の行 {m5}・据え付けが前の年より増えた州×年 {sum(s.m5_grow for s in ss)} | R11 fill<0.99 の割合 {sum(s.fill_low for s in ss)/max(1,sum(s.fill_n for s in ss)):.3f}（行 {sum(s.fill_n for s in ss)}）')
    if 'B' in data:
        ss = data['B']
        ok1 = sum(s.ani_seen for s in ss) == 0
        ok2 = sum(s.m2_ok for s in ss) / max(1, sum(s.m2_n for s in ss)) >= 0.99
        ok3 = sum(s.m3_ok for s in ss) / max(1, sum(s.m3_n for s in ss)) >= 0.99 and sum(s.food_off for s in ss) / max(1, sum(s.food_n for s in ss)) >= 0.30
        ok4 = sum(s.m4_ok for s in ss) / max(1, sum(s.m4_n for s in ss)) >= 0.99
        ok5 = sum(s.m5_pos for s in ss) > 0 and sum(s.m5_grow for s in ss) > 0
        ctrl = all(sum(s.ani_pos for s in data[n]) > 0 for n in nulls) if nulls else False
        print(f'  B 合否: M1 {ok1}（A系で aniOut が正の対照 {ctrl}）・M2 {ok2}・M3 {ok3}・M4 {ok4}・M5 {ok5}')
    # M3b（記録）：国×年の横断の相関 corr(log(pL÷(cpl×lpm)), log foodRel) の年平均
    import math
    def corr(xs, ys):
        n = len(xs)
        if n < 5:
            return float('nan')
        mx, my = sum(xs) / n, sum(ys) / n
        sxx = sum((a - mx) ** 2 for a in xs); syy = sum((b - my) ** 2 for b in ys)
        sxy = sum((a - mx) * (b - my) for a, b in zip(xs, ys))
        return sxy / math.sqrt(sxx * syy) if sxx > 0 and syy > 0 else float('nan')
    for n in data:
        cs = []
        for s in data[n]:
            for y, d in s.m3b.items():
                if y < 1790:
                    continue
                xs, ys = [], []
                for nat, lst in d.items():
                    xs.append(math.log(statistics.median(a for a, _ in lst))); ys.append(math.log(statistics.median(b for _, b in lst)))
                c = corr(xs, ys)
                if isnum(c):
                    cs.append(c)
        print(f'  M3b(記録) {n}: 国×年の横断の相関 corr(log 賃金÷(cpl×lpm), log foodRel) の平均 {mean(cs):.3f}（{len(cs)} 年×種）')

    print('\n【挙動の線】')
    # L1
    def l1(ss):
        cross = []; ratio = []
        for s in ss:
            c = 9999
            for y in sorted(y for (g, y) in s.out if g == ENG):
                o = s.out[(ENG, y)]
                if o['stm'] > o['wat']:
                    c = y; break
            cross.append(c)
            o = s.out.get((ENG, 1870)) or s.out.get((ENG, 1871)) or s.out.get((ENG, 1869))
            ratio.append(o['stm'] / o['wat'] if o and o['wat'] > 0 else (float('inf') if o and o['stm'] > 0 else float('nan')))
        return med(cross), med(ratio)
    r = per_arm(l1); res['L1'] = r
    for n, v in r.items():
        print(f'  L1 {n}: 英の蒸気が水車を抜く年（中央値）{v[0]}・1870 の蒸気÷水車（中央値）{v[1]:.2f}')
    if 'B' in r:
        print(f'  L1 B 合否: {1815 <= r["B"][0] <= 1850 and r["B"][1] >= 3.0}')
    # L2
    def l2(ss):
        return {g: med([s.year10(g) for s in ss]) for g in (ENG, CHN, '印', MAR, FRA, PRU, USA, JPN)}
    r = per_arm(l2); res['L2'] = r
    for n, v in r.items():
        print(f'  L2 {n}: Year10 ' + '・'.join(f'{g}={int(x) if isnum(x) else x}' for g, x in v.items()))
    if 'B' in r:
        v = r['B']
        ok = v[ENG] <= 1900 and (v[CHN] - v[ENG] >= 50 or v[CHN] == 9999) and v['印'] <= v[CHN] + 10 and v[MAR] <= v[CHN] + 10
        print(f'  L2 B 合否: {ok}')
    # L3
    def l3(ss):
        return med([s.wage_ratio(ENG, CHN, 1800, 1850, 'fx') for s in ss]), med([s.wage_ratio(ENG, CHN, 1800, 1850, 'real') for s in ss])
    r = per_arm(l3); res['L3'] = r
    for n, v in r.items():
        print(f'  L3 {n}: 英÷清の為替建ての賃金（1800〜1850 中央値）{v[0]:.2f}・食料建て {v[1]:.2f}')
    if 'B' in r:
        print(f'  L3 B 合否: {isnum(r["B"][0]) and r["B"][0] >= 2.0 and isnum(r["B"][1]) and 0.7 <= r["B"][1] <= 1.5}')
    # L4
    def l4(ss):
        cnt = []
        for s in ss:
            ys = [y for y in s.disc if y <= 1792]
            nat = set()
            for y in ys:
                nat |= s.disc[y]
            nat.discard(ENG)
            cnt.append(len(nat))
        return mean(cnt)
    r = per_arm(l4); res['L4'] = r
    for n, v in r.items():
        print(f'  L4 {n}: 1792 年末までに蒸気の行を解禁した国（英を除く・10 種平均）{v:.2f}')
    if 'B' in r and nulls:
        print(f'  L4 B 合否: {r["B"] <= min(r[n] for n in nulls)}')
    # L6
    print('  L6 工業 5 国の Year10（中央値）')
    if 'B' in data and 'A' in data:
        a = res['L2']['A']; b = res['L2']['B']
        ok = True
        for g in IND5:
            if a[g] <= 1920 and not b[g] <= a[g] + 25:
                ok = False
            print(f'     {g}: A {a[g]} B {b[g]}')
        print(f'  L6 B 合否: {ok}')
    # L7
    def l7(ss):
        vals = []
        for s in ss:
            def per(y):
                p = s.facPop.get(y, 0)
                return s.fac.get(y, 0) / p if p > 0 else float('nan')
            a, b = per(1830), per(1870)
            vals.append(b / a if isnum(a) and a > 0 else float('nan'))
        return med(vals)
    r = per_arm(l7); res['L7'] = r
    for n, v in r.items():
        print(f'  L7 {n}: 英の1人あたり工場の産出 1870÷1830（中央値）{v:.3f}')
    if 'B' in r and nulls:
        lo = min(r[n] for n in nulls)
        print(f'  L7 B 合否: {r["B"] >= 1.0 and r["B"] >= 0.5 * lo}')
    # L8
    def l8(ss):
        vals = []
        for s in ss:
            xs = [x for y, v in s.emp.items() if 1800 <= y <= 1900 for x in v if isnum(x)]
            vals.append(mean(xs))
        return mean(vals)
    r = per_arm(l8); res['L8'] = r
    for n, v in r.items():
        print(f'  L8 {n}: 英の本国の州の employmentShare（1800〜1900 平均）{v:.3f}')
    if 'B' in r and 'A' in r:
        print(f'  L8 B 合否: {r["B"] >= r["A"] - 0.2}')
    # L9
    r = {n: band_crisis(arms[n][1]) for n in data}
    for n, v in r.items():
        print(f'  L9 {n}: 平均恐慌回数 {v}')
    if 'B' in r and nulls and all(isnum(r[n]) for n in nulls) and isnum(r['B']):
        print(f'  L9 B 合否: {r["B"] <= 1.5 * max(r[n] for n in nulls)}')


if __name__ == '__main__':
    main()
