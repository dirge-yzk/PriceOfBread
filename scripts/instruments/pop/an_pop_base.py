#!/usr/bin/env python
"""(361) 段B の道具：1783 年の世界の基準の死の水準を導く（設計 doc §2-4(a)・§3-2 B）。
pop_s<seed>.tsv の N 行から、初期の年（既定 1783〜1789）の全ての国を足し合わせて、
 ・粗出生・粗死亡・自然増（年あたり）
 ・層別の死の率（観測）と、標準のピラミッドの死の重み（Model.cs の MortalityWeights と同じ式）に対する倍率
 ・自然増が目標（年 0.5%＝StdPyramidGrowth）になる死の水準の倍率（観測の出生で）
を出す。段C が〈死の率＝重み×世界の基準の水準1本〉を置くときの水準の候補を、この木の観測から読むための道具。
使い方：PYTHONUTF8=1 python an_pop_base.py <計器の出力フォルダ> [開始年 終了年]
⚠ 回復期の月（平常の死が 0）を含む窓は死の水準を低く見せる＝既定では回復期の月が窓の 25% を超える行を除く。
⚠ 標準のピラミッド・出生率 0.035・増加率 0.005 は SimCore/Model.cs と Coefficients.cs の値の写し（変わったら直す）。
"""
import csv, glob, math, os, sys
import os as _os, sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), '..', 'pop'))
import age_guard as _age_guard
_age_guard.require_bands8_argv()   # (490) 8 層（10 歳刻み）の pop_s だけを前提にする＝11 層の生なら止める

STD = [0.255, 0.195, 0.160, 0.130, 0.105, 0.080, 0.050, 0.025]
GROWTH = 0.005
CBR_STD = 0.035


def std_weights(cbr):
    w, inflow = [], cbr
    for b in range(8):
        outflow = (STD[b] / 120.0) * 12.0 if b < 7 else 0.0   # 加齢の流出（層の人数÷120 の月率を年率へ）
        mu = (inflow - outflow) / STD[b] - GROWTH
        w.append(mu if mu > 0 else 0.0)
        inflow = outflow
    return w


def num(v):
    try:
        return float(v)
    except Exception:
        return float('nan')


def main():
    a = sys.argv[1:]
    if not a:
        print(__doc__); return
    y0 = int(a[1]) if len(a) > 2 else 1783
    y1 = int(a[2]) if len(a) > 2 else 1789
    files = sorted(glob.glob(os.path.join(a[0], 'pop_s*.tsv')))
    births = deaths = pm = 0.0
    dband = [0.0] * 8; eband = [0.0] * 8
    rows = used = 0
    for p in files:
        with open(p, encoding='utf-8') as fh:
            rd = csv.reader(fh, delimiter=chr(9))
            h = next(rd); ix = {k: i for i, k in enumerate(h)}
            for r in rd:
                if len(r) != len(h) or r[ix['scope']] != 'N':
                    continue
                y = int(r[ix['year']])
                if not (y0 <= y <= y1):
                    continue
                rows += 1
                mo = num(r[ix['monthsCount']])
                if not (mo > 0) or num(r[ix['recoveringMonths']]) > 0.25 * mo:
                    continue
                popm = num(r[ix['popMeanK']]) * mo   # 人×月
                bd = [num(r[ix['deathsFrom%dK' % (10 * b)]]) for b in range(8)]
                mu = [num(r[ix['muFrom%dPerYear' % (10 * b)]]) for b in range(8)]
                if any(math.isnan(x) for x in bd + mu) or not (popm > 0):
                    continue
                used += 1
                births += num(r[ix['birthsK']]); deaths += sum(bd) + num(r[ix['deathsOtherK']]); pm += popm
                for b in range(8):
                    dband[b] += bd[b]
                    if mu[b] > 0:
                        eband[b] += bd[b] / mu[b]   # 人年（死÷率）
    print('# 世界の基準の死の水準（%d〜%d 年・N 行 %d のうち使えた %d・ファイル %d）' % (y0, y1, rows, used, len(files)))
    if not (pm > 0):
        print('使える行が無い'); return
    cbr = births / (pm / 12.0); cdr = deaths / (pm / 12.0)
    print('粗出生 %.1f‰・粗死亡 %.1f‰・自然増 %.2f%%/年（標準のピラミッドの前提は出生 %.1f‰・増加 %.1f%%）' % (cbr * 1000, cdr * 1000, (cbr - cdr) * 100, CBR_STD * 1000, GROWTH * 100))
    w = std_weights(CBR_STD)
    ex = sum(eband)
    print('層\t観測の死の率/年\t標準の重み/年\t観測÷重み\t人年の割合\t標準のピラミッドの割合')
    num_obs = den = 0.0
    for b in range(8):
        mu = dband[b] / eband[b] if eband[b] > 0 else float('nan')
        print('%d\t%.4f\t%.4f\t%.2f\t%.3f\t%.3f' % (b, mu, w[b], mu / w[b] if w[b] > 0 else float('nan'), eband[b] / ex, STD[b]))
        if eband[b] > 0:
            num_obs += dband[b]; den += w[b] * eband[b]
    lvl_obs = num_obs / den
    lvl_target = (cbr - GROWTH) / (den / ex)
    print('観測の死の水準（重みに対する倍率・人年で重みづけ）\t%.3f' % lvl_obs)
    print('自然増が %.1f%%/年 になる死の水準（観測の出生 %.1f‰で）\t%.3f  ＝観測の水準の %.2f 倍' % (GROWTH * 100, cbr * 1000, lvl_target, lvl_target / lvl_obs))
    print('⚠ 水準 1.0＝標準のピラミッドが出生 %.1f‰・増加 %.1f%% で動かない死の率' % (CBR_STD * 1000, GROWTH * 100))


if __name__ == '__main__':
    main()
