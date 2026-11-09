# (1006) 合流：c92tg の英 1913 の合計（買い手の行・最初の門・A〜D・値段の比の度数）を tradeskip_s から数え直す（担当の集計の台本はコミットされていない＝反証役の指摘・独立に書いた）。
#   使い方: python -X utf8 -I uk1913_tg.py <計器の出力フォルダ> [国=イギリス] [年=1913]
import sys, csv
d = sys.argv[1]; NAT = sys.argv[2] if len(sys.argv) > 2 else 'イギリス'; YR = sys.argv[3] if len(sys.argv) > 3 else '1913'
K = ['rows','rowIdle','rowNoNeed','rowBudget0','rowScan','rowDealtCross','rowDealtDomOnly','rowNoDeal','xPairs','xSoldOut','xCap','xWedge','xWedgeA','xWedgeB','xWedgeC','xWedgeD','xPass'] + [f'ratioBin{i}' for i in range(10)]
S = dict.fromkeys(K, 0.0); n = 0
for s in range(1, 11):
    with open(f'{d}/tradeskip_s{s}.tsv', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        for row in r:
            if row[ix['nation']] == NAT and row[ix['year']] == YR and row[ix['good']] != 'ALL':
                n += 1
                for k in K: S[k] += float(row[ix[k]])
print(f'{NAT} {YR} 行(国×財×種)={n}')
print('買い手の行', int(S['rows']), '番の標本', int(S['rowIdle']), '需要なし', int(S['rowNoNeed']), '予算0', int(S['rowBudget0']), '走査', int(S['rowScan']), '(越境', int(S['rowDealtCross']), '国内だけ', int(S['rowDealtDomOnly']), '成約なし', int(S['rowNoDeal']), ')')
x = S['xPairs']; print('越境の組', int(x), ' 先の買い手 %.1f%% 容量 %.1f%% 楔 %.1f%% 通過 %.1f%%' % tuple(100*S[k]/x for k in ['xSoldOut','xCap','xWedge','xPass']))
w = S['xWedge']; print('楔の区分 A %.1f%% B %.1f%% C %.1f%% D %.1f%%' % tuple(100*S['xWedge'+c]/w for c in 'ABCD'))
print('値段の比の度数', [int(S[f'ratioBin{i}']) for i in range(10)], '和', int(sum(S[f'ratioBin{i}'] for i in range(10))))
