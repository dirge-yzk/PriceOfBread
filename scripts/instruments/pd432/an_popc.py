#!/usr/bin/env python
"""(425) 人口の段C の計器 popc_s と (361) の pop_s の読み手（読むだけ）。
使い方：PYTHONUTF8=1 python an_popc.py <計器の出力フォルダ> [--q0 N] [--q1 N] [-n 国名,国名,...] [--eras]
・既定は国×窓（四半期 q0〜q1・既定 12〜40＝開幕の補償死〔Q0〜12〕の後の窓）の (種×四半期) の中央値を主要国ごとに出す（死の水準 L を置く canary 用）。
・--eras は時代（1783-1799・1800-1849・1850-1899・1900-1950）ごとの表（事前登録 [7] の T1〜T4・T11 を読む）。
・出すもの：粗出生‰・粗死亡‰・自然増%／年・合計出生率の代理・初婚の年齢・結婚している割合 m・夫婦の中の制限 c・知識 K・子の価値 v・15 歳の余命・死の水準・手取り÷籠。
・⚠ pop_s は旗 1 でも DemoPop を呼ぶので窓の和は有効。popc_s は旗 1 の走行にしか無い。人口加重の平均（popK）で国をまとめる世界の値を最後に出す。
"""
import csv, glob, math, os, statistics, sys, collections
import os as _os, sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), '..', 'pop'))
import age_guard as _age_guard
_age_guard.require_bands8_argv()   # (490) 8 層（10 歳刻み）の pop_s だけを前提にする＝11 層の生なら止める

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

MAJOR = ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', '日本', '清', 'ネーデルラント']
ERAS = [(1783, 1800), (1800, 1850), (1850, 1900), (1900, 1951)]


def num(v):
    try:
        return float(v)
    except Exception:
        return float('nan')


def med(xs):
    xs = [x for x in xs if x == x and not math.isinf(x)]
    return statistics.median(xs) if xs else float('nan')


def load(d, name):
    out = {}
    for f in sorted(glob.glob(os.path.join(d, name + '_s*.tsv'))):
        seed = int(''.join(ch for ch in os.path.basename(f) if ch.isdigit()) or 0)
        with open(f, encoding='utf-8') as fh:
            rd = csv.DictReader(fh, delimiter='\t')
            out[seed] = list(rd)
    return out


def main():
    a = sys.argv[1:]
    if not a:
        print(__doc__); return
    d = a[0]
    q0, q1, nations, eras = 12, 40, MAJOR, False
    i = 1
    while i < len(a):
        if a[i] == '--q0': q0 = int(a[i + 1]); i += 2
        elif a[i] == '--q1': q1 = int(a[i + 1]); i += 2
        elif a[i] == '-n': nations = a[i + 1].split(','); i += 2
        elif a[i] == '--eras': eras = True; i += 1
        else: i += 1
    pop = load(d, 'pop'); popc = load(d, 'popc')
    seeds = sorted(set(pop) | set(popc))
    print(f'# 出力 {d}  種 {seeds}  窓 q{q0}〜q{q1}  popc_s の種 {sorted(popc)}')
    # (nation, seed, quarter) -> dict
    P = collections.defaultdict(dict)
    for s, rows in pop.items():
        for r in rows:
            if r['scope'] != 'N': continue
            P[(r['nation'], s, int(r['quarter']))].update(
                year=int(r['year']), popK=num(r['popK']), cbr=num(r['cbrPerMille']), cdr=num(r['cdrPerMille']), tfr=num(r['tfrProxy']),
                mu0=num(r['muFrom0PerYear']), s10=num(r['s10Share']), months=num(r['monthsCount']), fertW=num(r['fertileWomenShare']))
    for s, rows in popc.items():
        for r in rows:
            P[(r['nation'], s, int(r['quarter']))].update(
                age=num(r['marriageAgeYears']), m=num(r['marriedShare']), c=num(r['cLimitShare']), k=num(r['knowledgeShare']), v=num(r['childValueIndex']),
                y=num(r['yAdultPerMonth']), b=num(r['basketPerMonth']), r=num(r['realIncomeIndex']), inh=num(r['inheritanceShare']), wc=num(r['womenCostIndex']),
                e15=num(r['lifeExpectancy15Years']), lvl=num(r['deathLevelIndex']), pi=num(r['pensionPiShare']))
    cols = ['cbr', 'cdr', 'nat%', 'tfr', 'age', 'm', 'c', 'k', 'v', 'e15', 'lvl', 'r(手取り÷籠)']
    print('国\t窓\t行\t' + '\t'.join(cols))

    def row(label, win, keys):
        rs = [P[k] for k in keys]
        if not rs: return None
        cbr = med([x.get('cbr', float('nan')) for x in rs]); cdr = med([x.get('cdr', float('nan')) for x in rs])
        vals = [cbr, cdr, (cbr - cdr) / 10.0, med([x.get('tfr', float('nan')) for x in rs]),
                med([x.get('age', float('nan')) for x in rs]), med([x.get('m', float('nan')) for x in rs]), med([x.get('c', float('nan')) for x in rs]),
                med([x.get('k', float('nan')) for x in rs]), med([x.get('v', float('nan')) for x in rs]), med([x.get('e15', float('nan')) for x in rs]),
                med([x.get('lvl', float('nan')) for x in rs]), med([x.get('r', float('nan')) for x in rs])]
        print(f'{label}\t{win}\t{len(rs)}\t' + '\t'.join(f'{v:.3f}' if v == v else 'nan' for v in vals))
        return vals

    allkeys = list(P.keys())
    world = {}
    if not eras:
        for n in nations:
            keys = [k for k in allkeys if k[0] == n and q0 <= k[2] <= q1 and P[k].get('months', 0) > 0]
            v = row(n, f'q{q0}-{q1}', keys)
            if v: world[n] = v
        if world:
            # 主要国の中央（国ごとの中央値の中央値）
            m = [med([world[n][j] for n in world]) for j in range(len(cols))]
            print('主要国の中央\t' + f'q{q0}-{q1}\t{len(world)}\t' + '\t'.join(f'{x:.3f}' if x == x else 'nan' for x in m))
            nat = [(n, world[n][2]) for n in world]
            print('# 自然増%／年（出生−死亡）：' + '  '.join(f'{n} {x:+.2f}' for n, x in nat))
    else:
        for n in nations:
            for lo, hi in ERAS:
                keys = [k for k in allkeys if k[0] == n and lo <= P[k]['year'] < hi and P[k].get('months', 0) > 0]
                row(n, f'{lo}-{hi - 1}', keys)


if __name__ == '__main__':
    main()
