#!/usr/bin/env python
"""(417) 線 dc の止める線の読み手（事前登録 追記2）：この線に固有の物差し ⑥⑴⑵ と、運動体の門の記録（R6・R8）。読むだけ。
使い方：python an2_stop.py <A の ins フォルダ（5 種）> <B の ins フォルダ（10 種）> <m412_ins のフォルダ（10 種・既存の表の比べ先）>
  ⑥⑴ 運動体の常駐：挙げた 8 国（アメリカを除く＝毎四半期の行がある国）の dcont_s の行で factionsCount＞20 の割合（A・B）。B が 10% を超えたら当たり。
  ⑥⑵ 反乱・内戦の頻度の桁（2 本・どちらか当たりで当たり）：
      (a) 運動体の蜂起 factionRevoltsCum（種×国の最大の和）＝A の 5 種と B の同じ 5 種を対にして B÷A が 0.5 未満か 2 超。
      (b) 内戦のある国×四半期（nation_s の civilWarMonths＞0）＝B の 10 種の種あたりと m412_ins の 10 種の種あたりの比が 0.5 未満か 2 超。
  記録：頭数の分解（partOldK→partMilK→partNewK）・運動体の MilSum（milSumOld/Mil/NewK）・運動体の数・結束の門の通過（facCohGateCount・facCohMutCount・facStrongCount・strengthRatioMaxIndex）。
採否の最終判定は報告（確証の区分×バランス破壊）。この読み手は数字を出すだけ。
"""
import csv, glob, sys, statistics as st

sys.stdout.reconfigure(encoding='utf-8')
LISTED = ['清', 'フランス', 'イギリス', 'ロシア', 'オスマン', 'プロイセン', 'オーストリア', '日本']


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def load(d, pat, seeds=None):
    rows = []
    for f in sorted(glob.glob(d + '/' + pat)):
        seed = int(f.split('_s')[-1].split('.')[0])
        if seeds is not None and seed not in seeds:
            continue
        with open(f, encoding='utf-8') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                r['seed'] = seed
                rows.append(r)
    return rows


def seeds_of(rows):
    return sorted(set(r['seed'] for r in rows))


def revolts(rows):
    best = {}
    for r in rows:
        k = (r['seed'], r['nation'])
        v = fl(r['factionRevoltsCum'])
        if v == v and v > best.get(k, 0):
            best[k] = v
    return sum(best.values())


def civil_rows(rows):
    return sum(1 for r in rows if fl(r['civilWarMonths']) > 0)


def col_sum(rows, k):
    return sum(v for v in (fl(r[k]) for r in rows) if v == v)


def p(a, q):
    a = sorted(v for v in a if v == v)
    return a[min(len(a) - 1, int(len(a) * q))] if a else float('nan')


def arm_summary(tag, dc_rows):
    q = [r for r in dc_rows if r['nation'] in LISTED]
    n = len(q)
    big = sum(1 for r in q if fl(r['factionsCount']) > 20)
    print('--', tag, ' 種', seeds_of(dc_rows), ' 挙げた 8 国の行', n)
    print('   ⑥⑴ factionsCount＞20 の行 %d / %d = %.2f%%（当たり＝10%% 超）・p50 %.0f p90 %.0f max %.0f'
          % (big, n, 100.0 * big / max(1, n), p([fl(r['factionsCount']) for r in q], .5), p([fl(r['factionsCount']) for r in q], .9), max([fl(r['factionsCount']) for r in q] or [0])))
    po, pm, pn = col_sum(q, 'partOldK'), col_sum(q, 'partMilK'), col_sum(q, 'partNewK')
    print('   頭数の和（行の和・千人）旧 %.0f → 闘争度だけの連続 %.0f（×%.2f）→ 不満度も読む %.0f（×%.3f）' % (po, pm, pm / po if po else float('nan'), pn, pn / pm if pm else float('nan')))
    so, sm, sn = col_sum(q, 'milSumOldK'), col_sum(q, 'milSumMilK'), col_sum(q, 'milSumNewK')
    print('   運動体の MilSum の和 旧 %.0f → 闘争度だけ %.0f（×%.2f）→ 不満度も %.0f（×%.3f）' % (so, sm, sm / so if so else float('nan'), sn, sn / sm if sm else float('nan')))
    print('   運動体の数（足切り後）の平均 旧 %.2f・闘争度だけ %.2f・新 %.2f'
          % (st.mean([fl(r['clusterCountOld']) for r in q] or [0]), st.mean([fl(r['clusterCountMil']) for r in q] or [0]), st.mean([fl(r['clusterCountNew']) for r in q] or [0])))
    g = [fl(r['facCohGateCount']) for r in q]
    m3 = [fl(r['facCohMutCount']) for r in q]
    sg = [fl(r['facStrongCount']) for r in q]
    rr = [fl(r['strengthRatioMaxIndex']) for r in q]
    print('   結束の門 ≧0.8 の運動体が居る行 %d（延べ %d）・≧0.3 の居る行 %d（延べ %d）・戦力が保持力＋郷勇を超える運動体の居る行 %d（延べ %d）・戦力比の最大 p99 %.2f max %.2f'
          % (sum(1 for v in g if v > 0), sum(g), sum(1 for v in m3 if v > 0), sum(m3), sum(1 for v in sg if v > 0), sum(sg), p(rr, .99), max(rr or [0])))
    print('   永続した運動体の変異 max %.0f・結束 max %.3f' % (max([fl(r['factionsMutatedCount']) for r in q] or [0]), max([fl(r['factionsMaxCohesionIndex']) for r in q] or [0])))


if __name__ == '__main__':
    if len(sys.argv) < 4:
        print(__doc__); sys.exit(1)
    dA, dB, dM = sys.argv[1:4]
    A = load(dA, 'dcont_s*.tsv'); B = load(dB, 'dcont_s*.tsv')
    arm_summary('A', A); arm_summary('B', B)
    sA = set(seeds_of(A))
    B5 = [r for r in B if r['seed'] in sA]
    print('== ⑥⑵(a) 運動体の蜂起 factionRevoltsCum（種×国の最大の和・対にする種 %s）' % sorted(sA))
    ra, rb5, rb = revolts(A), revolts(B5), revolts(B)
    ratio = rb5 / ra if ra else float('inf')
    print('   A %.0f・B（同じ種）%.0f → B÷A = %.2f %s ／ B 全 10 種の種あたり %.1f（A の種あたり %.1f）'
          % (ra, rb5, ratio, '【当たり】' if (ratio < 0.5 or ratio > 2) else '（範囲内）', rb / max(1, len(seeds_of(B))), ra / max(1, len(sA))))
    print('== ⑥⑵(b) 内戦のある国×四半期（nation_s の civilWarMonths＞0・種あたり）')
    Mn = load(dM, 'nation_s*.tsv'); Bn = load(dB, 'nation_s*.tsv')
    if not Bn:
        print('   （B の ins に nation_s が無い）')
    else:
        cm, cb = civil_rows(Mn) / max(1, len(seeds_of(Mn))), civil_rows(Bn) / max(1, len(seeds_of(Bn)))
        r2 = cb / cm if cm else float('inf')
        print('   m412_ins %.1f 行/種・B %.1f 行/種 → B÷m412 = %.2f %s' % (cm, cb, r2, '【当たり】' if (r2 < 0.5 or r2 > 2) else '（範囲内）'))
        sB5 = sorted(sA)
        cm5 = civil_rows([r for r in Mn if r['seed'] in sA]) / max(1, len(sB5))
        cb5 = civil_rows([r for r in Bn if r['seed'] in sA]) / max(1, len(sB5))
        print('   同じ 5 種：m412_ins %.1f・B %.1f' % (cm5, cb5))
