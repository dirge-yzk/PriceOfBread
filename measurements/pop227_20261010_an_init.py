# -*- coding: utf-8 -*-
"""initdump の TSV（国・量・時点・値・単位）から (227) の読み：植民地の州の人口・英÷仏の GDP の比・本国だけの1人あたり GDP・南アジアの和。
python an_init.py <腕=tsv> [<腕=tsv> ...]   （最初の腕が比べる基準）"""
import sys, csv, collections

REF1820 = {  # Maddison 2010 の 1820 年 1人あたり GDP の英を1とした比【推・記憶】
    'ネーデルラント': 1.08, 'フランス': 0.67, 'プロイセン': 0.63, 'アメリカ': 0.74, '日本': 0.39, '清': 0.35, 'スペイン': 0.59, 'オーストリア': 0.62, 'ロシア': 0.48, 'マラータ同盟': 0.31,
}
SOUTH_ASIA = ['マラータ同盟', 'マイソール', 'ハイデラバード', 'デリー', 'アワド', 'シク連邦', 'ネパール']


def load(path):
    d = collections.defaultdict(dict)   # (nation, time) -> quantity -> value
    for r in csv.reader(open(path, encoding='utf-8'), delimiter='\t'):
        if len(r) < 4 or r[0] == 'nation':
            continue
        nat, q, tm, v = r[0], r[1], r[2], r[3]
        try:
            d[(nat, tm)][q] = float(v)
        except ValueError:
            d[(nat, tm)][q] = v
    return d


def regions(d, nat, tm):
    q = d.get((nat, tm), {})
    names = sorted({k.split('/')[1] for k in q if k.startswith('州/') and k.endswith('/人口')})
    return [(n, q['州/%s/人口' % n], q.get('州/%s/格' % n, ''), q.get('州/%s/産出額(参照価格・全生産者)' % n, 0.0)) for n in names]


def home_pc(d, nat, tm):
    rs = [r for r in regions(d, nat, tm) if r[2] != 'Colonial']
    pop = sum(r[1] for r in rs); out = sum(r[3] for r in rs)
    return (out * 12 / pop if pop > 0 else float('nan')), pop


def main():
    arms = [a.split('=', 1) for a in sys.argv[1:]]
    data = {n: load(p) for n, p in arms}
    base = arms[0][0]
    nations = sorted({k[0] for k in data[base]})
    for tm in ('開幕', '助走後'):
        print('=== %s ===' % tm)
        # 英÷仏
        for n, d in data.items():
            g = lambda nat, key: d.get((nat, tm), {}).get(key, float('nan'))
            ratio_nom = g('イギリス', 'GDP/名目(年換算)') / g('フランス', 'GDP/名目(年換算)')
            ratio_real = g('イギリス', 'GDP/実質(年換算)') / g('フランス', 'GDP/実質(年換算)')
            pc = {nat: home_pc(d, nat, tm) for nat in ['イギリス', 'フランス', 'ネーデルラント', 'プロイセン', 'アメリカ', '日本', '清', 'スペイン', 'オーストリア', 'ロシア']}
            ukpc = pc['イギリス'][0]
            col = {nat: sum(r[1] for r in regions(d, nat, tm) if r[2] == 'Colonial') for nat in ['イギリス', 'フランス', 'スペイン', 'ポルトガル', 'ネーデルラント', 'ロシア']}
            sa = sum(d.get((nat, tm), {}).get('人口/総数', 0) for nat in SOUTH_ASIA)
            sa_col = col['イギリス']
            print('[%s] 英÷仏 GDP 名目 %.3f 実質 %.3f ｜英の本国人口 %.1f百万 植民地格 %.1f百万 ｜仏 本国 %.1f 植民地 %.1f ｜南アジア（独立国の和）%.1f百万＋英領の州 %.1f百万' % (
                n, ratio_nom, ratio_real, pc['イギリス'][1] / 1e6, col['イギリス'] / 1e6, pc['フランス'][1] / 1e6, col['フランス'] / 1e6, sa / 1e6, sa_col / 1e6))
            hg = lambda nat: sum(r[3] for r in regions(d, nat, tm) if r[2] != 'Colonial')
            print('    本国だけの産出額（参照価格×量・州の和）の 英÷仏 %.3f（植民地込みの国の GDP 比 %.3f）' % (hg('イギリス') / hg('フランス'), ratio_real))
            print('    本国の1人あたり GDP（参照価格×量/年・英=1）：' + '　'.join('%s %.2f(史実%.2f)' % (nat, pc[nat][0] / ukpc, REF1820.get(nat, float('nan'))) for nat in pc if nat != 'イギリス'))
            print('    植民地格の州の人口(百万)：' + '　'.join('%s %.1f' % (k, v / 1e6) for k, v in col.items()))
            tot = sum(sum(r[1] for r in regions(d, nat, tm) if r[2] == 'Colonial') for nat in {k[0] for k in d})
            print('    世界の植民地格の州の人口の和 %.1f百万' % (tot / 1e6))
            ud = d.get(('イギリス', tm), {}).get('植民地/質量の単位(千人)')
            if ud is not None:
                print('    質量の単位 %.1f 千人・英の植民地の質量 %.2f' % (ud, d[('イギリス', tm)].get('植民地/植民地の質量', float('nan'))))
    # 植民地の州の表（開幕）
    print('=== 植民地格の州の人口（開幕・千人）旧→各腕 ===')
    names = []
    for nat in ['フランス', 'イギリス', 'ロシア', 'ネーデルラント', 'スペイン', 'オマーン', 'デンマーク', 'ポルトガル']:
        for r in regions(data[base], nat, '開幕'):
            if r[2] == 'Colonial':
                names.append((nat, r[0]))
    for nat, nm in names:
        row = [nat, nm]
        for n, d in data.items():
            v = [r[1] for r in regions(d, nat, '開幕') if r[0] == nm]
            row.append('%.1f' % (v[0] / 1000) if v else '-')
        print('\t'.join(row))


if __name__ == '__main__':
    main()
