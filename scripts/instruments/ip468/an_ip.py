#!/usr/bin/env python
# (468) 工業化度の削除と動力の束（旗 PowerRebuild・FactoryCapacityFromPlant・FactoryBuffsOff・IndustrializationOff）：事前登録 measurements/ip_20261020_prereg.txt の追加の線の読み手（読むだけ）。
# 入力＝腕 B の計器つき 10 種の出力フォルダ（INS=1 bash scripts/instruments/batch.sh run <腕> の <出力>/ins）の power_s<種>.tsv（NATIONS に米を足す）。比べる側 A は scripts/instruments/pv/an_pv.py が読む（G1〜G9・T1〜T6 の本体）。
# 使い方：PYTHONUTF8=1 python -X utf8 an_ip.py B=<insB> [A=<m466_ins>] [AP=<insA_power>]
#   A ＝比べる側の計器つき 10 種の生（D:/PoB_work/keep_466/m466_ins）：factory_s の industrializationMean と modsig の industrialFrontier を読む（T8c）。
#   AP＝A の世界の power_s（腕 A の計器つき 5 種）：積み増しの累計 cumPrivateInd・cumStateInd（G10b・記録）。
# 出力：T8（発散の守り）・G10（導出量と設備の一致）・T7（天井が外れた確かめの近似）・R-ip1（清÷英の労働の値段・年別）。入力が無い物は未評価（合格に数えない）。
# ⚠ 線の幅・規則は全て事前登録の字面のまま（[追記1] の緩め・[追記2] の定義の置き直しを含む）。この読み手は線を動かさない。
# ⚠ T8a・T8b・G10 は腕 B（PowerRebuild・IndustrializationOff が入った世界）の出力にだけ意味がある（A の power_s は旧のレシピの能力を読むので T8a は発散して見える）。
import csv, glob, math, os, re, statistics, sys, collections

ENG, CHN = 'イギリス', '清'
GAIN, SCALE = 1.2, 2.5   # IndProductivityGain・IndProductivityScale の既定（曲線の上限＝Gain×Scale＝3.0）


def f(v):
    try:
        return float(v)
    except Exception:
        return float('nan')


def ok(x):
    return x == x and abs(x) != float('inf')


def read_power(d):
    rows = []
    for p in sorted(glob.glob(os.path.join(d, 'power_s*.tsv'))):
        m = re.search(r'power_s(\d+)\.tsv$', p)
        seed = int(m.group(1)) if m else 0
        with open(p, encoding='utf-8') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                r['_seed'] = seed
                rows.append(r)
    return rows


def pct(xs, q):
    xs = sorted(x for x in xs if ok(x))
    if not xs:
        return float('nan')
    k = min(len(xs) - 1, max(0, int(math.ceil(q * len(xs))) - 1))
    return xs[k]


def index_of(x):
    return SCALE * x / max(1e-12, SCALE * GAIN - x) if x > 0 else 0.0


def main():
    args = dict(a.split('=', 1) for a in sys.argv[1:] if '=' in a)
    B = args.get('B')
    if not B:
        print('usage: an_ip.py B=<ins dir>')
        return
    rows = read_power(B)
    if not rows:
        print('power_s が無い＝未評価')
        return
    act = [r for r in rows if r.get('active') == '1']
    print(f'行数 {len(rows)}（active {len(act)}）・種 {len(set(r["_seed"] for r in rows))}')

    # T8a 機械の能力÷労働の需要 ≦ 1.2 の州×年末の四半期（power_s は年末の四半期の行だけを出す＝事前登録の〈州×四半期〉を〈州×年末の行〉と読む・[追記3]）が 99.9% 以上。労働の需要が全ての能力の和の 1% 未満の行（比が発散する）は数えず、数えなかった行の数を印字する（[追記2]）
    ratios = []; skipped = 0
    for r in act:
        caps = [f(r.get(t + 'Cap', 'nan')) for t in ('hum', 'ani', 'wat', 'stm', 'ele')]
        total = sum(c for c in caps if ok(c))
        mach = sum(f(r.get(t + 'Cap', 'nan')) for t in ('wat', 'stm', 'ele') if ok(f(r.get(t + 'Cap', 'nan'))))
        dem = f(r['lDemand'])
        if dem > 0 and total > 0 and dem >= 0.01 * total:
            ratios.append(mach / dem)
        else:
            skipped += 1
    if ratios:
        le = sum(1 for x in ratios if x <= 1.2) / len(ratios)
        print(f'T8a 機械の能力÷労働の需要 ≦ 1.2：{le * 100:.3f}%（行 {len(ratios)}・数えなかった行 {skipped}・最大 {max(ratios):.3f}）→ {"合格" if le >= 0.999 else "不合格"}（99.9% 以上）')
    # T8b 担い手の頭数あたりの資本の部分（強度 x・capitalPerHead）の最大 < Gain×Scale（3.0）＝設備÷頭数 < 4.0 の言い換え（[追記2]：年末の頭数で割った設備÷頭数は頭数の減りで 4.0 を超えて見える）。参考に設備÷頭数（読む時点の頭数）の p99.9 も印字
    cph = [f(r.get('capitalPerHead', 'nan')) for r in act]
    cph = [x for x in cph if ok(x)]
    if cph:
        mx = max(cph)
        print(f'T8b 資本の部分の強度 x の最大 = {mx:.3f}（p99.9 {pct(cph, 0.999):.3f}）→ {"合格" if mx < GAIN * SCALE else "不合格"}（{GAIN * SCALE:.1f} 未満）')
    kr = []
    for r in act:
        w, k = f(r['workersK']), f(r['plantK'])
        if w > 0 and ok(k):
            kr.append(k / w)
    if kr:
        print(f'  （参考・記録）設備÷読む時点の頭数 p99.9 = {pct(kr, 0.999):.3f}（最大 {max(kr):.3f}）')

    # G10（追記1 の緩め）導出量 indDerived と 設備から取り直した指数 index_of(capitalPerHead)：投資・摩耗は同じ月の中で設備だけを動かす＝月の積み増し分だけずれる
    dif = []
    for r in act:
        d, c = f(r.get('indDerived', 'nan')), f(r.get('capitalPerHead', 'nan'))
        if ok(d) and ok(c):
            idx = index_of(c)
            dif.append(abs(d - idx) / max(0.05, abs(d)))
    if dif:
        within = sum(1 for x in dif if x <= 0.10) / len(dif)
        print(f'G10 導出量と設備の指数の一致（相対 10% 以内）：{within * 100:.2f}%（行 {len(dif)}・p99 {pct(dif, 0.99):.3f}）→ {"合格" if within >= 0.99 else "不合格"}（99% 以上・厳密な一致は番人 IndustrializationOffTests の月の更新）')

    # T7（近似・記録）英 1850 年：機械の割合 実際 ÷ 旧の天井 1.2 m²（m ≈ 労働の値段÷最安の機械の単位費用 − 1・平滑でなく年末の値）
    t7 = collections.defaultdict(list)
    for r in act:
        if r['nation'] != ENG or r['year'] != '1850':
            continue
        cap = sum(f(r.get(t + 'Cap', 'nan')) for t in ('wat', 'stm', 'ele') if ok(f(r.get(t + 'Cap', 'nan'))))
        dem, pL = f(r['lDemand']), f(r['pL'])
        units = [f(r.get(t + 'Unit', 'nan')) for t in ('wat', 'stm', 'ele')]
        units = [u for u in units if ok(u) and u > 0]
        if dem > 0 and units and pL > 0:
            m = max(0.0, pL / min(units) - 1.0)
            old = 1.2 * m * m
            t7[r['_seed']].append((cap / dem) / old if old > 1e-9 else float('nan'))
    meds = [statistics.median([x for x in v if ok(x)]) for v in t7.values() if any(ok(x) for x in v)]
    if meds:
        print(f'T7（記録）英 1850 の 実際÷1.2m² の種の中央値 {statistics.median(meds):.2f}（種 {len(meds)}・線 2 以上は事前登録の読み・m は近似）')

    # R-ip1 清÷英の労働の値段（実質＝pL÷(cpl×lpm)・州の中央値の国×年の種の中央値）
    byy = collections.defaultdict(lambda: collections.defaultdict(list))
    for r in act:
        if r['nation'] in (ENG, CHN) and int(r['year']) % 10 == 0:
            cpl, lpm, pL = f(r['cpl']), f(r['lpm']), f(r['pL'])
            if cpl > 0 and lpm > 0 and pL > 0:
                byy[(r['year'], r['_seed'])][r['nation']].append(pL / (cpl * lpm))
    out = collections.defaultdict(list)
    for (y, s), d in byy.items():
        if ENG in d and CHN in d:
            out[y].append(statistics.median(d[CHN]) / statistics.median(d[ENG]))
    if out:
        print('R-ip1 清÷英の労働の値段（実質・十年ごと・種の中央値）：' + '  '.join(f'{y}:{statistics.median(v):.2f}' for y, v in sorted(out.items())))

    # T8c（追記3・新）工業化度の導出量 I の発散の線：B の国平均 I（factory_s の industrializationMean）と最前線（modsig の industrialFrontier）の最大（種ごとの最大の中央値）÷ A の同じ量（m466 の計器つき 10 種）。
    #   ≦ 2.0 で合格（採る条件・投資が飽和して積み上がらない）／≧ 10.0 は発散（止める線）。比べる国は A の factory_s にある国（B の米は除く）。
    A = args.get('A')
    if A:
        def maxes(d, stem, col, nations=None):
            out = []
            for p in sorted(glob.glob(os.path.join(d, stem + '_s*.tsv'))):
                m = 0.0
                with open(p, encoding='utf-8') as fh:
                    for r in csv.DictReader(fh, delimiter='	'):
                        if nations is not None and r.get('nation') not in nations:
                            continue
                        v = f(r.get(col, 'nan'))
                        if ok(v):
                            m = max(m, v)
                out.append(m)
            return out
        a_nat = set()
        for p in sorted(glob.glob(os.path.join(A, 'factory_s*.tsv')))[:1]:
            with open(p, encoding='utf-8') as fh:
                for r in csv.DictReader(fh, delimiter='	'):
                    a_nat.add(r['nation'])
        for label, stem, col, nat in (('国平均 I', 'factory', 'industrializationMean', a_nat), ('最前線 I', 'modsig', 'industrialFrontier', None)):
            ma, mb = maxes(A, stem, col, nat), maxes(B, stem, col, nat)
            if ma and mb:
                ra = statistics.median(ma); rb = statistics.median(mb)
                ratio = rb / ra if ra > 0 else float('nan')
                verdict = '発散（止める線）' if ratio >= 10.0 else ('合格' if ratio <= 2.0 else '不合格（採る条件を満たさない・止める線ではない）')
                print(f'T8c {label} の最大（種の中央値）B {rb:.2f} ÷ A {ra:.2f} = {ratio:.2f} → {verdict}（≦2.0 合格・≧10.0 発散）')
    # 州の I の分布（記録）：p50・p90・p99・最大と、5 超・10 超の割合（A の州の上限 5.0・私的の netReturn の自然な止まり 約 7.4）
    iv = [f(r.get('indDerived', 'nan')) for r in act]
    iv = [x for x in iv if ok(x)]
    if iv:
        print(f'（記録）州の I：p50 {pct(iv, 0.5):.2f}・p90 {pct(iv, 0.9):.2f}・p99 {pct(iv, 0.99):.2f}・最大 {max(iv):.2f}・5 超 {sum(1 for x in iv if x > 5) / len(iv) * 100:.2f}%・10 超 {sum(1 for x in iv if x > 10) / len(iv) * 100:.2f}%')
    # G10b（記録）積み増しの累計：国ごとの州の合計（cumPrivateInd＋cumStateInd・1900 年以前の最後の行）B ÷ AP。A は州の上限 5.0 があるので B が大きく出る（上限の廃止の効き）。
    AP = args.get('AP')
    def cum_by_nation(rows):
        last = {}
        for r in rows:
            if int(r['year']) > 1900:
                continue
            k = (r['_seed'], r['nation'], r['region'])
            last[k] = (int(r['quarter']), f(r.get('cumPrivateInd', 'nan')) + f(r.get('cumStateInd', 'nan')))
        agg = collections.defaultdict(float)
        for (sd, nat, reg), (_, v) in last.items():
            if ok(v):
                agg[(sd, nat)] += v
        per = collections.defaultdict(list)
        for (sd, nat), v in agg.items():
            per[nat].append(v)
        return {n: statistics.median(v) for n, v in per.items()}
    if AP and 'cumPrivateInd' in (rows[0] if rows else {}):
        prow = read_power(AP)
        if prow and 'cumPrivateInd' in prow[0]:
            cb, ca = cum_by_nation(rows), cum_by_nation(prow)
            outl = []
            for n in ('イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', 'ロシア', '清'):
                if n in cb and n in ca and ca[n] > 0:
                    outl.append(f'{n} {cb[n] / ca[n]:.2f}')
            if outl:
                print('G10b（記録）1900 年までの積み増しの累計 B÷A（国ごとの州の合計・種の中央値）：' + '  '.join(outl))

if __name__ == '__main__':
    main()
