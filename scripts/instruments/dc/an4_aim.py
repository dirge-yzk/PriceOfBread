#!/usr/bin/env python
"""(431) 線 dq の〈機構が狙いどおり働いたか〉の読み手：計器の表 dcont_s<種>.tsv から事前登録 [4] の T1〜T3 を式のとおりに出す（読むだけ）。
使い方：python an4_aim.py <腕 B の計器の出力フォルダ（…/ins）> [<腕 A の…/ins>]
  1 つ渡す＝その腕（B）の T1〜T3。2 つ渡す＝B（先）と A（後）を並べる（A では T3 は対照＝0 でないのが正しい）。
事前登録 measurements/dq_20261019_prereg.txt [4]：
  T1 U ＝ Σ(partNewK − partMilK) ÷ Σ partMilK（挙げた 8 国×全行×全種）。狙い U ≥ 5%（枝 dc の B は 0.6%）。0.6%〜5% は〈部分的に働いた〉。
  T2 D ＝ 8 国の全行の dGtMShare の単純平均。狙い D ＞ 0.12。
  T3 B の全行（8 国の毎四半期＋他の全ての国の年末）の milLivingMeanShare が厳密に 0。
⚠限界（この読み手が自己申告する）：dcont_s は四半期末の断面で、不満度は今の盤面から再計算した値（思想の更新・参加の述語の瞬間とは LivingRef の更新分〈ギャップの 3%〉だけずれる）／
  partMilK・partNewK は同じ世界の同じ瞬間の 2 通りの数え方（B の世界では実際の参加の重みは partNewK）／8 国以外の国は年末の行だけ（T3 の対象には入る・T1 T2 には入れない）／
  列の値の書式は有効数字 10 桁（T3 の 0 判定は書式が 0 のときだけ）。採否の判定は止める線と合わせて報告で行う（この読み手は数字を出すだけ）。
"""
import csv, glob, sys, math

sys.stdout.reconfigure(encoding='utf-8')
NATIONS = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本']


def load(d):
    rows = []
    files = sorted(glob.glob(d + '/dcont_s*.tsv'))
    for f in files:
        with open(f, encoding='utf-8') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                rows.append(r)
    return rows, len(files)


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def p90(a):
    a = sorted(v for v in a if v == v)
    return a[min(len(a) - 1, int(len(a) * 0.9))] if a else float('nan')


def aim(tag, d):
    rows, nfiles = load(d)
    print('==', tag, d, '（dcont_s の表 %d 本・行 %d）' % (nfiles, len(rows)))
    big = [r for r in rows if r['nation'] in NATIONS]
    print('   8 国の行 %d' % len(big))
    # T1
    sNew = sMil = sOld = 0.0
    msNew = msMil = 0.0
    per = {}
    for r in big:
        pn, pm, po = fl(r['partNewK']), fl(r['partMilK']), fl(r['partOldK'])
        if pn != pn or pm != pm:
            continue
        sNew += pn; sMil += pm; sOld += po
        msNew += fl(r['milSumNewK']); msMil += fl(r['milSumMilK'])
        a = per.setdefault(r['nation'], [0.0, 0.0])
        a[0] += pn; a[1] += pm
    U = (sNew - sMil) / sMil if sMil > 0 else float('nan')
    print('T1 U＝Σ(partNewK−partMilK)÷Σ partMilK ＝ %.4f（%.2f%%）  [狙い ≥ 5%%・枝 dc の B は 0.6%%]' % (U, U * 100))
    print('   内訳：二値→連続 (partMilK÷partOldK) ＝ %.3f 倍／不満度が先行 (partNewK÷partMilK) ＝ %.4f 倍／MilSum の同じ比 ＝ %.4f' % (
        sMil / sOld if sOld > 0 else float('nan'), sNew / sMil if sMil > 0 else float('nan'), msNew / msMil if msMil > 0 else float('nan')))
    for n in NATIONS:
        if n in per and per[n][1] > 0:
            print('   %-8s U＝%.4f' % (n, (per[n][0] - per[n][1]) / per[n][1]))
    # T2
    dg = [fl(r['dGtMShare']) for r in big]
    dg_ok = [v for v in dg if v == v]
    D = sum(dg_ok) / len(dg_ok) if dg_ok else float('nan')
    print('T2 D＝8 国の全行の dGtMShare の単純平均 ＝ %.4f  [狙い ＞ 0.12]' % D)
    for n in NATIONS:
        v = [fl(r['dGtMShare']) for r in big if r['nation'] == n]
        v = [x for x in v if x == x]
        if v:
            print('   %-8s 平均 %.4f・p90 %.4f' % (n, sum(v) / len(v), p90(v)))
    # T3
    nz = [r for r in rows if fl(r['milLivingMeanShare']) != 0.0]
    mx = max((abs(fl(r['milLivingMeanShare'])) for r in rows), default=float('nan'))
    print('T3 milLivingMeanShare が 0 でない行 ＝ %d／%d（最大の絶対値 %.3g）  [B の狙い＝0 件。A では 0 件でないのが正しい]' % (len(nz), len(rows), mx))
    t1 = 'PASS' if U >= 0.05 else ('PARTIAL' if U > 0.006 else 'MISS')
    print('判定の素（報告で止める線と合わせる）：T1=%s・T2=%s・T3=%s' % (t1, 'PASS' if D > 0.12 else 'MISS', 'PASS' if not nz else 'MISS'))
    return U, D, len(nz)


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print(__doc__); sys.exit(1)
    print(__doc__.split('事前登録')[0].strip().splitlines()[0])
    aim('B', sys.argv[1])
    if len(sys.argv) > 2:
        aim('A', sys.argv[2])
