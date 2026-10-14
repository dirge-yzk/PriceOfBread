# -*- coding: utf-8 -*-
"""(333) initdump の TSV から、南アジアの開幕の人口と〈人口に連れて動く量〉を腕どうしで比べる（読むだけ）。
python sa333_20261014_an_init.py A=<tsv> B1=<tsv> [B2=<tsv> ...]   （最初の腕が基準）"""
import sys, csv, collections, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sa333_20261014_spec as spec

SA = spec.INDEPENDENTS
OUT = ['ネパール']


def load(path):
    d = collections.defaultdict(dict)
    for r in csv.reader(open(path, encoding='utf-8'), delimiter='\t'):
        if len(r) < 4 or r[0] == 'nation':
            continue
        try:
            d[(r[0], r[2])][r[1]] = float(r[3])
        except ValueError:
            d[(r[0], r[2])][r[1]] = r[3]
    return d


def regpop(d, nat, tm, names):
    q = d.get((nat, tm), {})
    return sum(q.get('州/%s/人口' % n, 0.0) for n in names)


def sa_total(d, tm):
    ind = sum(d[(n, tm)].get('人口/総数', 0.0) for n in SA)
    col = sum(regpop(d, own, tm, [c]) for own, c in spec.COLONIES)
    return ind, col


def main():
    arms = [a.split('=', 1) for a in sys.argv[1:]]
    data = {n: load(p) for n, p in arms}
    base = arms[0][0]
    for tm in ('開幕', '助走後', '最初の四半期の後'):
        print('=== %s ===' % tm)
        for n, d in data.items():
            ind, col = sa_total(d, tm)
            neph = d[('ネパール', tm)].get('人口/総数', 0)
            cn = d[('清', tm)].get('人口/総数', 0)
            print('[%s] 独立6国 %.1f百万＋英仏領の州 %.1f百万＝範囲内の和 %.1f百万（BCG 1783 補間 %.1f・窓 %.0f〜%.0f）｜ネパール %.1f（範囲外）｜清÷範囲内の和 %.2f' % (
                n, ind / 1e6, col / 1e6, (ind + col) / 1e6, spec.TARGET_K / 1e3, spec.WINDOW_K[0] / 1e3, spec.WINDOW_K[1] / 1e3, neph / 1e6, cn / (ind + col)))
        keys = [('人口/総数', 1e-3, '千人'), ('人口/兵士', 1e-3, '千人'), ('GDP/実質(年換算)', 1.0, ''), ('GDP/1人あたり(実質・年)', 1.0, ''), ('財政/国庫', 1.0, ''),
                ('財政/国庫÷年名目GDP', 1.0, ''), ('貨幣/広義(全POPの貯蓄)', 1.0, ''), ('貨幣/貯蓄÷年名目GDP', 1.0, ''), ('軍/総合ゲージ', 1.0, ''), ('設備/産地(RGO)の能力の和', 1.0, ''),
                ('設備/工場の能力の和', 1.0, ''), ('投資家/貯蓄の和', 1.0, ''), ('制度/技術水準', 1.0, '')]
        for nat in SA:
            print('  -- %s' % nat)
            for k, sc, u in keys:
                vals = []
                for n, d in data.items():
                    v = d.get((nat, tm), {}).get(k)
                    vals.append('%s=%s' % (n, ('%.4g' % (v * sc)) if isinstance(v, float) else v))
                b = data[base].get((nat, tm), {}).get(k)
                ratio = ''
                if len(data) > 1 and isinstance(b, float) and b != 0:
                    ratio = '  比(' + '/'.join('%.3f' % (data[n].get((nat, tm), {}).get(k, float('nan')) / b) for n in list(data)[1:]) + ')'
                print('     %s%s: %s%s' % (k, ('(' + u + ')') if u else '', '  '.join(vals), ratio))
            rk = [data[n].get((nat, tm), {}).get('軍/国力ランク') for n in data]
            print('     国力ランク: ' + ' / '.join(str(x) for x in rk))
    # 耕地：州ごとの arableLand は initdump に無い→ 人口(農民)に比例して置かれる（Simulation.cs:925 初期農民×LandHeadroom）


if __name__ == '__main__':
    main()
