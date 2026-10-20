#!/usr/bin/env python
"""(481) N2 の診断の読み手（読むだけ）：labor_s の追記 6 列を読み、四半期末に床×0.90 未満の州の能力を持つ行を原因に分ける。
使い方：PYTHONUTF8=1 python an_n2.py <計器の出力フォルダ> [種の数=10]
 - N2 の定義（mw469 事前登録 §12）＝施行行のうち enactedBelowFloorShare > 0.05 の行。
 - 列の意味は SimCore/InstrumentsLw.cs の注。belowNoStep／belowPriceMoved／belowFloorMoved の和は enactedBelowFloorShare と一致する。
 - 限界：印 Fresh は折り込み（その国の四半期）で下ろす＝挙げた国は毎四半期・他の国は年末だけの行（年末の行は前の折り込みから 4 四半期ぶんの歩を見る）。
"""
import sys, os, math, collections


def rows(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for l in f:
            yield dict(zip(h, l.rstrip('\n').split('\t')))


def num(x):
    try:
        return float(x)
    except ValueError:
        return math.nan


def main():
    d = sys.argv[1]
    n = int(sys.argv[2]) if len(sys.argv) > 2 else 10
    tot = enacted = n2 = 0
    cause = collections.Counter()
    sumshare = collections.Counter()
    maxstep = 0.0
    ex = []
    byn = collections.Counter()
    for s in range(1, n + 1):
        for r in rows(os.path.join(d, 'labor_s%d.tsv' % s)):
            if 'stepBelowShare' not in r:
                print('列が無い（旧の計器の出力）'); return
            tot += 1
            if num(r['lawCode']) <= 0:
                continue
            enacted += 1
            b = num(r['enactedBelowFloorShare'])
            maxstep = max(maxstep, num(r['stepBelowShare']))
            if not (b > 0.05):
                continue
            n2 += 1
            parts = {'歩なし': num(r['belowNoStepShare']), '値段が歩の後に落ちた': num(r['belowPriceMovedShare']), '床が歩の後に上がった': num(r['belowFloorMovedShare'])}
            k = max(parts, key=lambda x: parts[x])
            cause[k] += 1
            for kk, v in parts.items():
                sumshare[kk] += v
            byn[r['nation']] += 1
            if len(ex) < 12:
                ex.append((s, r['quarter'], r['year'], r['nation'], b, parts, num(r['floorEndOverStepIndex']), num(r['priceEndOverStepIndex'])))
    print('== 行 %d（法が施行された行 %d）・N2（enactedBelowFloorShare>0.05）の行 %d' % (tot, enacted, n2))
    print('== stepBelowShare の最大（歩の直後に床の下だった割合・機構が床を守れていれば 0）  %.6f' % maxstep)
    print('== N2 の行の主因（最大の原因）：', dict(cause))
    if n2:
        print('== N2 の行の原因の割合の平均：', {k: round(v / n2, 4) for k, v in sumshare.items()})
    print('== N2 の行の国別（上位）：', byn.most_common(8))
    for e in ex:
        print('   種%d 四半期%s 年%s %s below=%.3f %s floorEnd/step=%.3f priceEnd/step=%.3f' % (e[0], e[1], e[2], e[3], e[4], {k: round(v, 3) for k, v in e[5].items()}, e[6], e[7]))


main()
