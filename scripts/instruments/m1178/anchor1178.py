# (1178) 合流：reanchor1116.py が〈写せない〉と出した 1 行（S16-19＝(1173) c116k1 が RegionAvgSuffrageInclusion の引数に係数を足した）の錨を今の木の行へ直し、k1 が働きを変えた行（S16-19・S26-46）の最後の列の末尾に〈(1178)：…〉を足す。
#   python -X utf8 -I anchor1178.py <reanchor の後の台帳> <書き出す台帳>
import sys
src, dst = sys.argv[1:3]
b = open(src, 'rb').read(); nl = '\r\n' if b'\r\n' in b else '\n'
L = b.decode('utf-8').split(nl)
OLD, NEW = 'Politics.cs:1041:RegionAvgSuffrageInclusion(World w, Region r)', 'Politics.cs:1044:RegionAvgSuffrageInclusion(World w, Coefficients c, Region r)'
NOTE = '(1173) c116k1 で旗 SecessionInclusionOnce を 1 にした（包摂は闘争度の目標の 1 回だけ＝分離の圧の :894 の割引は旗 1 で通らない・州の包摂は人の版を非兵士の大きさで重みづけ・植民地格は植民地参政権・引数に係数を足した）・§0-2 ★(1164) ④ の推奨〈F2〉＝作者「しばらくの間、全て推奨で」で推奨を採った・作者の確認待ち'
hit = set()
for i, l in enumerate(L):
    for k in ('S16-19', 'S26-46'):
        if l.startswith('| ' + k + ' |'):
            if k == 'S16-19':
                assert l.count(OLD) == 1, k
                l = l.replace(OLD, NEW)
            l = l.rstrip(); assert l.endswith('|'), k
            L[i] = l[:-1].rstrip() + f'〔(1178)：{NOTE}〕 |'; hit.add(k)
assert hit == {'S16-19', 'S26-46'}, hit
open(dst, 'wb').write(nl.join(L).encode('utf-8'))
print('ok', len(hit))
