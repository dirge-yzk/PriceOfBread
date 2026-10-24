#!/usr/bin/env python
"""(431) 線 dq の〈機構が狙いどおり働いたか〉の読み手：計器の表 dcont_s<種>.tsv から事前登録 [4] の T1〜T3 を式のとおりに出す（読むだけ）。
使い方：python an4_aim.py <腕 B の計器の出力フォルダ（…/ins）> [<腕 A の…/ins>]
  1 つ渡す＝その腕（B）の T1〜T3。2 つ渡す＝B（先）と A（後）を並べる（A では T3 は対照＝0 でないのが正しい）。
事前登録 measurements/dq_20261019_prereg.txt [4]：
  T1 U ＝ Σ(partNewK − partMilK) ÷ Σ partMilK（挙げた 8 国×全行×全種）。狙い U ≥ 5%（枝 dc の B は 0.6%）。0.6%〜5% は〈部分的に働いた〉。
  T2（追記2）D ＝ 枝 dc の小国 3 国（ハワイ・ダホメ・コンゴ）の年末の行の dGtMShare の単純平均（3 国まとめて）。狙い D ＞ 0.121（dc の B の最大）。8 国の平均・人口で重みづけた割合は記録。
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
    # T2（追記2）：枝 dc の小国（ハワイ王国・ダホメ王国・コンゴ王国）の年末の行の dGtMShare の行の単純平均（dc の B は 0.112・0.121・0.108）
    SMALL = {'ハワイ王国': 0.112, 'ダホメ王国': 0.121, 'コンゴ王国': 0.108}
    sm_all = []
    print('T2 小国（枝 dc と同じ国・同じ物差し＝全ての年末の行の dGtMShare の単純平均）  [狙い：3 国まとめて ＞ 0.121＝dc の B の最大]')
    for n, ref in SMALL.items():
        v = [fl(r['dGtMShare']) for r in rows if n in r['nation']]
        v = [x for x in v if x == x]
        sm_all += v
        print('   %-8s 行 %5d  平均 %.4f（dc の B は %.3f）' % (n, len(v), sum(v) / len(v) if v else float('nan'), ref))
    D = sum(sm_all) / len(sm_all) if sm_all else float('nan')
    print('   3 国まとめて D ＝ %.4f' % D)
    # 記録：大国 8 国の平均（行の単純平均）と人口で重みづけた割合（partDenK がある表だけ）
    dg = [fl(r['dGtMShare']) for r in big]
    dg_ok = [v for v in dg if v == v]
    print('   記録：8 国の全行の dGtMShare の単純平均 ＝ %.4f' % (sum(dg_ok) / len(dg_ok) if dg_ok else float('nan')))
    if big and 'partDenK' in big[0]:
        num = den = 0.0
        for r in big:
            g, w_ = fl(r['dGtMShare']), fl(r['partDenK'])
            if g == g and w_ == w_:
                num += g * w_; den += w_
        print('   記録：8 国の人口で重みづけた割合 Σ(dGtMShare×partDenK)÷ΣpartDenK ＝ %.4f' % (num / den if den > 0 else float('nan')))
    for n in NATIONS:
        v = [fl(r['dGtMShare']) for r in big if r['nation'] == n]
        v = [x for x in v if x == x]
        if v:
            print('   %-8s 平均 %.4f・p90 %.4f' % (n, sum(v) / len(v), p90(v)))
    # T3
    nz = [r for r in rows if fl(r.get('milLivingMeanShare', '0')) != 0.0]   # (551) 列は削除済み＝新しい生では 0 件
    mx = max((abs(fl(r.get('milLivingMeanShare', '0'))) for r in rows), default=float('nan'))
    print('T3（結線の確認＝旗を配線すれば必ず通る・機構が働いた証拠ではない。採否は T1・T2）milLivingMeanShare が 0 でない行 ＝ %d／%d（最大の絶対値 %.3g）  [B の狙い＝0 件。A では 0 件でないのが正しい]' % (len(nz), len(rows), mx))
    # 追記3 の記録（採否に使わない）：先の力学に届いたか・分母の別の取り方・罷業・T3 は結線の確認
    nclMil = sum(fl(r['clusterCountMil']) for r in big if fl(r['clusterCountMil']) == fl(r['clusterCountMil']))
    nclNew = sum(fl(r['clusterCountNew']) for r in big if fl(r['clusterCountNew']) == fl(r['clusterCountNew']))
    print('記録：運動体の候補の数 clusterCountNew÷clusterCountMil ＝ %.4f／MilSum の比 milSumNewK÷milSumMilK ＝ %.4f（頭数の比 %.4f）' % (
        nclNew / nclMil if nclMil > 0 else float('nan'), msNew / msMil if msMil > 0 else float('nan'), sNew / sMil if sMil > 0 else float('nan')))
    if big and 'partDenK' in big[0]:
        pdg = 0.0
        for r in big:
            g, w_ = fl(r['dGtMShare']), fl(r['partDenK'])
            if g == g and w_ == w_:
                pdg += g * w_
        print('記録：T1 の別の分母 Σ(partNewK−partMilK)÷Σ(dGtMShare×partDenK)（不満度が闘争度を上回る POP の人口を分母）＝ %.4f' % ((sNew - sMil) / pdg if pdg > 0 else float('nan')))
    if big and 'strikeRegions' in big[0]:
        srg = [fl(r['strikeRegions']) for r in big]; sis = [fl(r['strikeIntensitySum']) for r in big]
        print('記録：罷業の導出 World.TechProductivity と同じ式（8 国の毎四半期・地域の闘争度が門 0.5 超）：行あたりの地域数の平均 ＝ %.4f／強さの和の行平均 ＝ %.4f／1 地域でも超えた行の割合 ＝ %.4f' % (
            sum(srg) / len(srg), sum(sis) / len(sis), sum(1 for v in srg if v > 0) / len(srg)))
    t1 = 'PASS' if U >= 0.05 else ('PARTIAL' if U > 0.006 else 'MISS')
    print('判定の素（報告で止める線と合わせる）：T1=%s・T2=%s・T3=%s' % (t1, 'PASS' if D > 0.121 else 'MISS', 'PASS' if not nz else 'MISS'))
    return U, D, len(nz)


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print(__doc__); sys.exit(1)
    print(__doc__.split('事前登録')[0].strip().splitlines()[0])
    aim('B', sys.argv[1])
    if len(sys.argv) > 2:
        aim('A', sys.argv[2])
