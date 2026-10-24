#!/usr/bin/env python
"""(417) 線 dc の記録の線の読み手：計器の表 dcont_s<種>.tsv を読み、国ごとに事前登録 R4〜R8 の要約を出す（読むだけ）。
使い方：python an1_dcont.py <計器の出力フォルダ（…/ins）> [<別の腕のフォルダ>]
  1 つ渡す＝その腕の表。2 つ渡す＝A（先）と B（後）を並べて差を出す。
採否に使う物は無い（記録の線）。事前登録 measurements/dc_20261019_prereg.txt [5]。
"""
import csv, glob, sys, statistics as st

sys.stdout.reconfigure(encoding='utf-8')


def load(d):
    rows = []
    for f in sorted(glob.glob(d + '/dcont_s*.tsv')):
        seed = int(f.split('_s')[-1].split('.')[0])
        with open(f, encoding='utf-8') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                r['seed'] = seed
                rows.append(r)
    return rows


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def q(a, p):
    a = sorted(v for v in a if v == v)
    return a[min(len(a) - 1, int(len(a) * p))] if a else float('nan')


def summarize(rows, nations):
    out = {}
    for n in nations:
        rr = [r for r in rows if r['nation'] == n]
        if not rr:
            continue
        col = lambda k: [fl(r[k]) for r in rr]
        hi = sum(1 for v in col('highSatHighMilShare') if v >= 0.05)
        out[n] = dict(
            rows=len(rr),
            sat=st.median([v for v in col('satMeanShare') if v == v] or [float('nan')]),
            dis=st.median([v for v in col('discontentMeanShare') if v == v] or [float('nan')]),
            mil=st.median([v for v in col('militancyMeanShare') if v == v] or [float('nan')]),
            liv=st.median([v for v in [fl(r.get('milLivingMeanShare', 'nan')) for r in rr] if v == v] or [float('nan')]),   # (551) 生活の列は削除済み＝新しい生には無い（旧い生だけ値が出る）
            voi=st.median([v for v in col('milVoiceMeanShare') if v == v] or [float('nan')]),
            war=st.median([v for v in col('milWarMeanShare') if v == v] or [float('nan')]),
            nat=st.median([v for v in col('milNationMeanShare') if v == v] or [float('nan')]),
            fam=st.median([v for v in col('milFamineMeanShare') if v == v] or [float('nan')]),
            chronic=st.median([v for v in col('chronicShortageMeanShare') if v == v] or [float('nan')]),
            drop=st.median([v for v in col('livingDropMeanIndex') if v == v] or [float('nan')]),
            hihi5=hi,
            partOldMax=max(col('partOldK')), partNewMax=max(col('partNewK')),
            dgtm_p90=q(col('dGtMShare'), .9),
            clOld_p90=q(col('clusterCountOld'), .9), clNew_p90=q(col('clusterCountNew'), .9),
            avgOld=st.median([v for v in col('clusterAvgMilOldIndex') if v == v and v > 0] or [float('nan')]),
            avgNew=st.median([v for v in col('clusterAvgMilNewIndex') if v == v and v > 0] or [float('nan')]),
            fac_max=max(col('factionsCount')), mut_max=max(col('factionsMutatedCount')),
            revolts=sum(max(fl(r['factionRevoltsCum']) for r in rr if r['seed'] == s) for s in set(r['seed'] for r in rr)),
        )
    return out


def show(tag, s):
    print('==', tag)
    hdr = 'nation        rows   sat   dis   mil | 5内訳 生活 政治 戦争 民族 飢餓 | 慢性 落差 | hiHi>=5% | 頭数最大(旧/新)K | D>M p90 | 運動体数p90(旧/新) | 平均闘争度(旧/新) | 永続運動体max/変異max/蜂起累計'
    print(hdr)
    for n, v in s.items():
        print('%-10s %6d %5.3f %5.3f %5.3f |  %5.3f %5.3f %5.3f %5.3f %5.3f | %5.3f %6.3f | %5d | %9.0f/%9.0f | %5.3f | %4.0f/%4.0f | %5.3f/%5.3f | %3.0f/%3.0f/%d' % (
            n, v['rows'], v['sat'], v['dis'], v['mil'], v['liv'], v['voi'], v['war'], v['nat'], v['fam'], v['chronic'], v['drop'], v['hihi5'],
            v['partOldMax'], v['partNewMax'], v['dgtm_p90'], v['clOld_p90'], v['clNew_p90'], v['avgOld'], v['avgNew'], v['fac_max'], v['mut_max'], v['revolts']))


if __name__ == '__main__':
    dirs = sys.argv[1:]
    if not dirs:
        print(__doc__); sys.exit(1)
    nations = ['清', 'フランス', 'アメリカ', 'イギリス', 'ロシア', 'オスマン', 'プロイセン', 'オーストリア', '日本']
    for d in dirs:
        show(d, summarize(load(d), nations))
