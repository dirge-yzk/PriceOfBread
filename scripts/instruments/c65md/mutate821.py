# (821) c65md 突然変異を使い捨ての木の Diplomacy.cs へ当てる（バイトで読み書き＝CRLF と BOM を保つ）。
#   使い方: python -I -X utf8 mutate821.py <木> <M1|M2|M3|M4|M7>
#   M1 歩の分けを外す／M2 調停の戦争の崩壊を reach なしの満額で上書き／M3 前走査の除外だけを外す／M4 前走査で調停の戦争を持つ国の戦争を全て数えない／M7 分けを調停でない戦争にも当てる
import sys, os
tree, m = sys.argv[1], sys.argv[2]
p = os.path.join(tree, 'PriceOfBread.SimCore', 'Diplomacy.cs')
b = open(p, 'rb').read()
G = 'if ((aCollapse || bCollapse) && !(fpBlank && war.ForcedPeace))'.encode('utf-8')
P = 'if (fpBlank && cw.ForcedPeace) continue;'.encode('utf-8')
I = 'double impose = CollapseScore * (1.0 - (1.0 - reach));'.encode('utf-8')
subs = {
    'M1': [(G, 'if (aCollapse || bCollapse)'.encode('utf-8'))],
    'M2': [(G, 'if (aCollapse || bCollapse)'.encode('utf-8')),
           (I, 'double impose = (fpBlank && war.ForcedPeace) ? CollapseScore : CollapseScore * (1.0 - (1.0 - reach));'.encode('utf-8'))],
    'M3': [(P, '/* M3 */'.encode('utf-8'))],
    'M4': [(P, 'if (fpBlank && (cw.ForcedPeace || Wars.Exists(x => x.ForcedPeace && (x.A == cw.A || x.B == cw.A || x.A == cw.B || x.B == cw.B)))) continue;'.encode('utf-8'))],
    'M7': [(G, 'if ((aCollapse || bCollapse) && !fpBlank)'.encode('utf-8'))],
}[m]
for old, new in subs:
    n = b.count(old)
    if n != 1: sys.exit(f'{m}: 置き換え元が {n} 個（1 個でない）')
    b = b.replace(old, new)
open(p, 'wb').write(b)
print(f'{m}: {len(subs)} か所を置き換えた {p}')
