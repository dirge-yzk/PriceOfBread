#!/usr/bin/env python
"""(471) 線 sf の読み手（読むだけ）。使い方：
    python an_sf471.py <B の ins> <A′ の ins> [--seeds=1..10] [--costB=<.cost>] [--costA=<.cost>]
事前登録 measurements/sf471_20261020_prereg.txt の [8]（本文）＋追記 2 の狙いの数字を、10 種それぞれについて満たすかを数え、
7 種以上＝当たり・4〜6＝部分・3 以下＝外れ で判定する。読むだけ（採否の最終判断は報告に書く）。
読む表：sf_s（B と A′）・fiscal_s（claimTotalPerMonth）・goods_s（priceMean）・ce_s（negativeDemandPerMonth）。
"""
import csv, sys, os, statistics, collections, math, re

def fl(x):
    try: return float(x)
    except Exception: return float('nan')

def read(path):
    with open(path, encoding='utf-8', errors='replace', newline='') as f:
        return list(csv.DictReader(f, delimiter='\t'))

def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')

def sf_rows(d, sd):
    p = os.path.join(d, f'sf_s{sd}.tsv')
    return read(p) if os.path.exists(p) else None

def by_nation(rows):
    o = collections.defaultdict(list)
    for r in rows: o[r['nation']].append(r)
    return o

def verdict(n):
    return '当たり' if n >= 7 else '部分' if n >= 4 else '外れ'

def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    opt = dict(a[2:].split('=', 1) for a in sys.argv[1:] if a.startswith('--') and '=' in a)
    dB, dA = args[0], args[1]
    seeds = list(range(1, 11))
    if 'seeds' in opt:
        a, b = opt['seeds'].split('..'); seeds = list(range(int(a), int(b) + 1))
    res = collections.defaultdict(dict)   # gate -> seed -> (ok, detail)
    extra = collections.defaultdict(dict)
    for sd in seeds:
        B = sf_rows(dB, sd); A = sf_rows(dA, sd)
        if B is None or A is None:
            print(f'seed {sd}: 表が無い（B={B is not None} A={A is not None}）'); continue
        bn, an = by_nation(B), by_nation(A)
        # ---- G0-1 場面（A′）
        sh = []
        for n, rs in an.items():
            for r in rs:
                q = int(fl(r['quarter'])); lv = fl(r['pensionLevel']); e = fl(r['elder60K']); s = fl(r['soldierElder60K'])
                if q >= 100 and lv > 0 and e + s > 0: sh.append(s / (e + s))
        g01 = med(sh) if sh else float('nan')
        res['G0-1'][sd] = (g01 >= 0.001, f'median soldierElderShare={g01:.5f} (n={len(sh)})')
        # ---- G1 形（B > 0 の国・A′ は全国 0）
        bpos = sum(1 for n, rs in bn.items() if fl(rs[-1]['pensionToSoldiersCum']) > 0)
        apos = sum(1 for n, rs in an.items() if fl(rs[-1]['pensionToSoldiersCum']) > 0)
        res['G1'][sd] = (bpos >= 1 and apos == 0, f'B>0 nations={bpos} A>0 nations={apos}')
        # ---- G0-2 場面
        cq = sum(1 for rs in an.values() for r in rs if fl(r['cheapGoodCount']) > 0)
        res['G0-2'][sd] = (cq >= 100, f'A cheap nation-quarters={cq}')
        # ---- G2 形：-1 が立つ期の輸出率の和どうしの比 B/A
        def rate(rs):
            ex = sum(fl(r['cheapExportValue']) for r in rs if fl(r['cheapGoodCount']) > 0)
            pr = sum(fl(r['cheapProducedRefValue']) for r in rs if fl(r['cheapGoodCount']) > 0)
            return ex / pr if pr > 0 else float('nan')
        ratios = {}
        for n in bn:
            if n in an:
                rb, ra = rate(bn[n]), rate(an[n])
                if rb == rb and ra == ra and ra > 0: ratios[n] = rb / ra
        allB = rate([r for rs in bn.values() for r in rs]); allA = rate([r for rs in an.values() for r in rs])
        poolratio = allB / allA if allA and allA == allA else float('nan')
        paid = sum(1 for rs in bn.values() if fl(rs[-1]['cheapSubsidyPaidCum']) > 0)
        paidA = sum(1 for rs in an.values() if fl(rs[-1]['cheapSubsidyPaidCum']) > 0)
        n105 = sum(1 for v in ratios.values() if v >= 1.05)
        res['G2'][sd] = (poolratio >= 1.0 and n105 >= 1 and paid > 0 and paidA == 0,
                         f'pooled B/A={poolratio:.3f} nations>=1.05: {n105}/{len(ratios)} paidNations B={paid} A={paidA}')
        # ---- G2' 負担の上限：fiscal_s の支出の和に対する補助の累計（挙げた 8 国）
        fB = os.path.join(dB, f'fiscal_s{sd}.tsv')
        spend = collections.defaultdict(float)
        if os.path.exists(fB):
            for r in read(fB): spend[r['nation']] += fl(r['claimTotalPerMonth'])
        rr = []
        for n, rs in bn.items():
            if n in spend and spend[n] > 0 and len(rs) > 300:   # 挙げた国（毎四半期の行がある国）
                rr.append(fl(rs[-1]['cheapSubsidyPaidCum']) / spend[n])
        g2p = med(rr)
        if not rr: res["G2'"][sd] = (False, '未測（fiscal_s が無い＝挙げた国の指定なしの回）')
        else: res["G2'"][sd] = (g2p <= 0.05, f'median paid/spend over {len(rr)} nations={g2p:.4f} max={max(rr) if rr else float("nan"):.4f}')
        # ---- G0-3' 場面：植民地格の重みの割合（1850 年以後・A′）
        cnt = 0; vals = {}
        for n, rs in an.items():
            ex = [fl(r['colonialVoteWeight']) / fl(r['voteWeightAll']) for r in rs if int(fl(r['year'])) >= 1850 and fl(r['voteWeightAll']) > 0]
            if ex:
                vals[n] = statistics.fmean(ex)
                if vals[n] >= 0.01: cnt += 1
        res["G0-3'"][sd] = (cnt >= 3, f'nations with colonialShare>=0.01: {cnt}  ' + ' '.join(f'{k}={v:.3f}' for k, v in vals.items() if k in ('清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本')))
        # ---- G3' 形：重みの一致の割合
        def okshare(rs, den):
            tot = ok = 0
            for r in rs:
                d_ = fl(r[den]); u = fl(r['voteWeightUsed'])
                if d_ > 0 and u == u:
                    tot += 1
                    if 0.97 <= u / d_ <= 1.03: ok += 1
            return 100.0 * ok / tot if tot else float('nan')
        sB = okshare([r for rs in bn.values() for r in rs], 'voteWeightScopedColonialOnly')
        sA = okshare([r for rs in an.values() for r in rs], 'voteWeightAll')
        res["G3'"][sd] = (sB >= sA - 3.0, f'B {sB:.1f}% vs A {sA:.1f}%')
        # ---- G5 AI の口：植民地参政権の段が動いた国
        moved = [n for n, rs in bn.items() if len({fl(r['colonialSuffrageLaw']) for r in rs}) > 1]
        movedA = [n for n, rs in an.items() if len({fl(r['colonialSuffrageLaw']) for r in rs}) > 1]
        mm = [n for n, rs in bn.items() if len({fl(r['migrantNativeSuffrageLaw']) for r in rs}) > 1]
        res['G5'][sd] = (len(moved) >= 1, f'colonialSuffrage moved nations B={len(moved)} A={len(movedA)} migrantNative moved B={len(mm)}')
        # ---- 議会の量（記録）
        def seat(rs):
            rr_ = [r for r in rs if int(fl(r['year'])) >= 1850]
            if not rr_: return (float('nan'),) * 3
            return tuple(statistics.fmean(fl(r[c]) for r in rr_) for c in ('seatCons', 'seatLib', 'seatSoc'))
        parl = {}
        for n in ('清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本'):
            if n in bn and n in an:
                sb, sa = seat(bn[n]), seat(an[n])
                gb = collections.Counter(int(fl(r['governingParty'])) for r in bn[n] if int(fl(r['year'])) >= 1850)
                ga = collections.Counter(int(fl(r['governingParty'])) for r in an[n] if int(fl(r['year'])) >= 1850)
                parl[n] = (tuple(b - a for b, a in zip(sb, sa)), gb, ga)
        extra['parl'][sd] = parl
        # ---- 往復の計器（記録）
        def fin(rs, c): return fl(rs[-1][c])
        sw = {n: (fin(bn[n], 'biasSwitchCum'), fin(an[n], 'biasSwitchCum'), fin(bn[n], 'stressFoldCum'), fin(an[n], 'stressFoldCum')) for n in bn if n in an and len(bn[n]) > 40}
        ov = {n: (statistics.fmean(fl(r['cheapWithNegTariffCount']) for r in bn[n]), statistics.fmean(fl(r['cheapGoodCount']) for r in bn[n])) for n in bn if len(bn[n]) > 40}
        extra['switch'][sd] = (sw, ov)
        # ---- G4 / 価格（B・A′）
        negB = negA = float('nan')
        for tag, d in (('B', dB), ('A', dA)):
            p = os.path.join(d, f'ce_s{sd}.tsv')
            if os.path.exists(p):
                s = 0.0
                for r in read(p): s += fl(r['negativeDemandPerMonth']) if r['negativeDemandPerMonth'] not in ('', 'nan') else 0.0
                if tag == 'B': negB = s
                else: negA = s
        extra['neg'][sd] = (negB, negA)
        gp = {}
        for tag, d in (('B', dB), ('A', dA)):
            p = os.path.join(d, f'goods_s{sd}.tsv')
            acc = collections.defaultdict(list)
            if os.path.exists(p):
                for r in read(p):
                    v = fl(r['priceMean'])
                    if v == v and v > 0: acc[(r['nation'], r['goodName'], int(fl(r['year'])) // 50)].append(v)
            gp[tag] = {k: statistics.fmean(v) for k, v in acc.items()}
        rat = [gp['B'][k] / gp['A'][k] for k in gp['B'] if k in gp['A']]
        res['価格比'][sd] = (0.99 <= med(rat) <= 1.01, f'median B/A price (nation,good,half-century)={med(rat):.4f} n={len(rat)}')
    print('=' * 100)
    for g in ('G0-1', 'G1', 'G0-2', 'G2', "G2'", "G0-3'", "G3'", 'G5', '価格比'):
        d = res.get(g, {})
        n = sum(1 for ok, _ in d.values() if ok)
        print(f'{g}: {n}/{len(d)} 種が満たす → {verdict(n)}')
        for sd in sorted(d): print(f'    seed {sd}: {"○" if d[sd][0] else "×"} {d[sd][1]}')
    print('=' * 100)
    print('G4（ce_s negativeDemandPerMonth の和：B は厳密に 0 のはず・A′ は塵）')
    for sd in sorted(extra['neg']): print(f'    seed {sd}: B={extra["neg"][sd][0]:.3g} A={extra["neg"][sd][1]:.3g}')
    print('議会の量（1850 年以後・8 国・B−A′ の議席シェアの平均差〔保守,自由,社会〕と与党の族の月数 B/A）')
    for sd in sorted(extra['parl']):
        print(f'  seed {sd}')
        for n, (dd, gb, ga) in extra['parl'][sd].items():
            print(f'    {n}: dSeat=({dd[0]:+.3f},{dd[1]:+.3f},{dd[2]:+.3f}) gov B={dict(sorted(gb.items()))} A={dict(sorted(ga.items()))}')
    print('売買方針の往復（国ごと：biasSwitchCum B/A・stressFoldCum B/A 〔最終行〕・B の cheapWithNegTariff 平均/cheapGood 平均）')
    for sd in sorted(extra['switch']):
        sw, ov = extra['switch'][sd]
        ks = [k for k in ('清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本') if k in sw]
        print(f'  seed {sd} 全国の中央値: switch B/A {med([v[0] for v in sw.values()]):.0f}/{med([v[1] for v in sw.values()]):.0f} fold B/A {med([v[2] for v in sw.values()]):.0f}/{med([v[3] for v in sw.values()]):.0f} negtar/cheap {med([v[0] for v in ov.values()]):.1f}/{med([v[1] for v in ov.values()]):.1f}')
        print(f'  seed {sd}: ' + ' | '.join(f'{k} sw {sw[k][0]:.0f}/{sw[k][1]:.0f} fold {sw[k][2]:.0f}/{sw[k][3]:.0f} negtar {ov[k][0]:.1f}/{ov[k][1]:.1f}' for k in ks))
    for tag in ('costB', 'costA'):
        if tag in opt and os.path.exists(opt[tag]):
            txt = open(opt[tag], encoding='utf-8', errors='replace').read()
            m = re.search(r'推定の計算量（1試合）: ([0-9]+[.][0-9]+)', txt)
            print(tag, opt[tag], '推定', m.group(1) if m else txt[:200])

main()
