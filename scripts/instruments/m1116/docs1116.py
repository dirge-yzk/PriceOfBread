# (1116) 合流：00一覧・CURRENT・CURRENT_書庫・README・実装ログを書き換える（m1110/docs1110.py の型・バイトで読み書き・CR の数と BOM を保つ）。
#   python -X utf8 docs1116.py <木> <合わせた木の短いコミット> <本文の置き場（m1116_work）>
#   本文は置き場の row_s00.txt・row_s02.txt・row_next_head.txt・row_next0.txt・row_cur.txt・row_readme.txt・log_lines.md（合流の記録）から読む。
import sys, os
T, H, W = sys.argv[1:4]
def rd(p): return open(p, 'rb').read()
def wr(p, b): open(p, 'wb').write(b)
def body(n): return rd(os.path.join(W, n)).decode('utf-8').strip('\n')

# 00一覧
p = os.path.join(T, 'docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md')
s = rd(p).decode('utf-8'); L = s.split('\n')
i1110 = [i for i, l in enumerate(L) if l.startswith('| **★(1110) 合流（波106')]
assert len(i1110) == 1
L.insert(i1110[0] + 1, body('row_s00.txt'))
inx = [i for i, l in enumerate(L) if l.startswith('| **次の波（波107〜・(1110) の推奨')]
assert len(inx) == 1
old = L[inx[0]]; parts = old.split('<br>')
assert parts[1].startswith('**0. 波107（恒等・作者の 4 本の残り）**'), parts[1][:40]
assert parts[3].startswith('**2. 挙動（波108〜・3 本まで）**'), parts[3][:40]
assert parts[6].startswith('**波107 の計画へ**'), parts[6][:40]
strike = ('| ~~**次の波（波107〜・(1110) の推奨・AI・作者の承認は未確認）**~~〔2026-11-14 ★(1116) で波107 完了＝挙動 0・恒等 4/4（c107di・c107ha・c107hb・c107hc 入れた＝作者の 4 本 ⑤⑥⑦⑧ が全て入った）・'
          '設計の材料 c107op は docs と measurements へ・文書の線 c107ds は届かず〕 | 中身は ★(1116) の行と下の〈次の波（波108〜）〉へ運んだ | 実装ログ (1110) |')
p3 = parts[3]
old4 = '材料＝`measurements/m1108_20261113/result.txt`〔末尾に反証役 v〕・門 G1〜G4〔G1 は旗 0 で赤〕・**開幕の四半期の空白〔アメリカ〕の扱いを事前登録で決める**・置き換える条件の候補が NonElectiveSeatsTests の盤面で立つかを先に確かめる・答え待ちではない）'
assert old4 in p3, 'p3'
p3 = p3.replace(old4, '材料＝`measurements/m1108_20261113/result.txt`〔末尾に反証役 v〕と **(1115) の材料と下書き `docs/202611/20261114/20261114_開院の空白_材料.md`・`_下書き.md`〔末尾に反証役 v の 11 件〕**・'
                '案 1〈Parliament.cs だけ・開院の条件を席が空・思想が有る・選挙政体へ・開幕の選挙政体の国は議会の前に UpdateShares を 1 回〉（AI の案・確証なし・初期値が先の向きは案 2＝§0-2 ★(1116) ①）・'
                '持ち主に InstrumentsMon.cs の monlaw_s の末尾 3 列〔parlHeadCode・seatSumEnd・seatConsEnd〕・門 G1・G1′・G2〜G4b〔母集合は parlHeadCode で＝距離 0 を入れない・反証役 v〕＋UpdateShares の上の側の門＋内戦明けの最初の四半期〔旧≠新の第 3 の類〕を予言に・'
                '対照は MinimumWageTarget（lawGateOpen:MinimumWage は常に 1）・答え待ちではない）')
p6 = '**波108 の計画へ**：c107ds は届かなかった＝短い README は直る前の版のまま＝止める線・★(743)・★(711) は元の README の行で読むよう、計画が指示文に書く（(1110) の行のまま）。'
new = [body('row_next_head.txt'), body('row_next0.txt')] + parts[2:3] + [p3] + parts[4:6] + [body('row_next_add.txt'), p6] + parts[7:]
L[inx[0]] = strike
L.insert(inx[0] + 1, '<br>'.join(new))
i02 = [i for i, l in enumerate(L) if l.startswith('| **★(1110) 作者に聞く物（波106 の合流')]
assert len(i02) == 1
L.insert(i02[0] + 1, body('row_s02.txt'))
# 採用率の表：最後の〈（★(1110)）。〉の後に 1 列
ia = [i for i, l in enumerate(L) if l.startswith('| **採用率の表（★(685)')]
assert len(ia) == 1
a = '・計画 0**（★(1110)）。'
assert L[ia[0]].count(a) == 1
L[ia[0]] = L[ia[0]].replace(a, '・計画 0**（★(1110)） → **波107 挙動 0・恒等 4/4（c107di〔表示の層の別アセンブリ〕・c107ha〔注を履歴へ 組 A〕・c107hb〔組 B〕・c107hc〔係数の注〕入れた）・設計の材料 1（c107op〔開院の空白〕）・文書 0（c107ds 届かず）・計画 0**（★(1116)）。')
wr(p, '\n'.join(L).encode('utf-8'))

# CURRENT（最上段に 1 行・前の更新が 3 本を超えた分を書庫の見出しの次へ）
p = os.path.join(T, 'docs/CURRENT.md'); b = rd(p); bom = b.startswith(b'\xef\xbb\xbf')
s = b.decode('utf-8-sig'); L = s.split('\n')
ic = [i for i, l in enumerate(L) if l.startswith('| ★ **最終更新（main へ合流済み）**')]
assert len(ic) == 1
cur = L[ic[0]]
L[ic[0]] = body('row_cur.txt')
L.insert(ic[0] + 1, cur.replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(1110)） |', 1))
prev = [i for i, l in enumerate(L) if l.startswith('| 前の更新（')]
moved = []
while len(prev) > 3:
    moved.append(L.pop(prev[-1])); prev = prev[:-1]
wr(p, (b'\xef\xbb\xbf' if bom else b'') + '\n'.join(L).encode('utf-8'))
if moved:
    p2 = os.path.join(T, 'docs/CURRENT_書庫.md'); b2 = rd(p2); bom2 = b2.startswith(b'\xef\xbb\xbf')
    L2 = b2.decode('utf-8-sig').split('\n')
    ih = [i for i, l in enumerate(L2) if l.startswith('| 前の更新（')]
    k = ih[0] if ih else 1
    for m in reversed(moved): L2.insert(k, m)
    wr(p2, (b'\xef\xbb\xbf' if bom2 else b'') + '\n'.join(L2).encode('utf-8'))
print('CURRENT moved', len(moved))

# README：★(1110) の行の次に ★(1116)・§2 に docs/history の行
p = os.path.join(T, 'docs/README.md'); b = rd(p); bom = b.startswith(b'\xef\xbb\xbf')
L = b.decode('utf-8-sig').split('\n')
ir = [i for i, l in enumerate(L) if l.startswith('- ★(1110)：')]
assert len(ir) == 1
L.insert(ir[0] + 1, body('row_readme.txt'))
i2 = [i for i, l in enumerate(L) if l.startswith('| ★**表示の層と試験の置き場**（(1111)')]
assert len(i2) == 1
L.insert(i2[0] + 1, body('row_readme_s2.txt'))
wr(p, (b'\xef\xbb\xbf' if bom else b'') + '\n'.join(L).encode('utf-8'))

# 実装ログ（末尾に (1111)〜(1116)）
p = os.path.join(T, 'docs/202609/99_実装ログ_20260903.md'); b = rd(p)
add = rd(os.path.join(W, 'log_lines.md')).decode('utf-8')
if not b.endswith(b'\n'): b += b'\n'
wr(p, b + add.lstrip('\n').encode('utf-8') if b.endswith(b'\n\n') else b + add.encode('utf-8'))
print('ok')
