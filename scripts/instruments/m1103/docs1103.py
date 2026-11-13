# (1103) 合流：docs の生きた文書へ書く（実装ログ・00一覧・README・CURRENT）。バイトで読み書き（CR の数と BOM を保つ）。
#   python -X utf8 docs1103.py <木の根> <作業フォルダ>
import sys, os
T, W = sys.argv[1], sys.argv[2]
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
def one(s, old, new):
    assert s.count(old) == 1, (old[:80], s.count(old))
    return s.replace(old, new)

# 1. 実装ログ：末尾に線の 6 本と合流の行
L = os.path.join(T, 'docs/202609/99_実装ログ_20260903.md')
s = rd(L)
lines = rd(os.path.join(W, 'log_lines.md'))
lines = lines.replace('@@RR@@', '採った〔★(1103)・S11〕').replace('@@GP@@', '採った〔★(1103)・S11〕')
lines = lines.replace('@@RRMERGE@@', '採った（★(1103)・M と S11 で G1・G1′・G2・G3・G7 が通った・A_rr≡腕 B）。v2 の高は attrib ② と申し送り 1 を取り下げて晴らした（φ＜1 の主な原因は州の取り分の揺れ〔未確定〕）。G4 は腕では読まず機構の試験だけ・G3 は払いの 72% の照合と名乗った。')
lines = lines.replace('@@GPMERGE@@', '採った（★(1103)・M と S11 で G1〜G4 が通った・A_gp≡腕 B）。最低賃金法の施行の行の増え（M 42,703・新しい N1 46,885・N2 39,712・基線 m1087 28,830）は記録・経路を読む線を次の波へ。')
merge = rd(os.path.join(W, 'log_merge.md'))
merge = merge.replace('@@LINES@@', 'SimCore 13 ファイル ＋645／−50・Tests 22 ファイル ＋721／−23・App 0・Runner 0・scripts 39 ファイル ＋2,660／−0。')
merge = merge.replace('@@ORPHAN@@', open(os.path.join(W, 'orphan.txt'), encoding='utf-8').read().strip())
if not s.endswith('\n'): s += '\n'
s = s + lines + merge
wr(L, s)

# 2. 00一覧
F = os.path.join(T, 'docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md')
s = rd(F)
row1096 = [l for l in s.split('\n') if l.startswith('| **★(1096) 合流（波104・挙動の線 0 本＝基線は m1087 のまま）** |')]
assert len(row1096) == 1
new_row = ('| **★(1103) 合流（波105・挙動の線 c105rr・c105gp を採った＝新しい基線 m1103）** | 2026-11-13：挙動 2/3（c105rr〔(1097) 支配の力の段 R・州の線路×支配度・旗 RailByRegion 既定 1〕・c105gp〔(1099) s18gp・非選挙の議席を空に・旗 NonElectiveSeatsEmpty 既定 1〕を S11 で採った・c105mc〔(1098) MC1〕は旗 1 のフルスイートの予言に無い赤 3 本で腕 B の前に止めた＝旗 0 の器だけ入れた）・恒等 1/1（c105ix〔(1100) armsfill_s・DiscoveryClockTests の集まり・画面の試験 2 本・T6〕）・読む c105s08〔(1101) S08 の台帳 41 行＝§2h〕・設計の材料 c105cq〔(1102) 段 C′ の事前登録の材料〕。'
           '腕 Z（合わせた木＋旗 0 の使い捨ての木）〈判定: 恒等〉＝m1087 と全行一致・A_rr≡rr の腕 B・A_gp≡gp の腕 B（成績表と共通の表）・止める線 0（M・S11・新しい N1・N2）・空洞化 1/10 未満 0・推定 M 230.3 秒・名目の余り 約 3.1 秒・テスト 2808＋3・WriterRatchet item 11・34 を ★(749) で取り直した。'
           '段 R の反証役 v2 の高（attrib ②〈φ＜1 は脅威の項〉）は取り下げた＝φ＜1 の主な原因は州の取り分の揺れ（未確定）。記録：最低賃金法の施行の行が線の後に約 1.5 倍（新しいヌル腕も同じ水準）。基線の生 keep_1103。作者に聞く物は §0-2 ★(1103)。次の波は〈次の波（波106〜）〉。 | 実装ログ (1097)〜(1103) |')
s = one(s, row1096[0] + '\n', row1096[0] + '\n' + new_row + '\n')
# 次の波：前の行を打ち消して畳み、新しい行を足す
old = [l for l in s.split('\n') if l.startswith('| **次の波（波105〜・(1096) の推奨・AI・作者の承認は未確認）**')]
assert len(old) == 1
o = old[0]
carry0 = o.index('<br>**(1096) の足し**'); carry1 = o.index('<br>**この次の波の推奨の組**')
carry = o[carry0:carry1]
struck = ('| ~~**次の波（波105〜・(1096) の推奨・AI・作者の承認は未確認）**~~〔2026-11-13 ★(1103) で波105 完了＝挙動 2/3（c105rr・c105gp 採った＝新しい基線 m1103・c105mc は旗 0 の器だけ）・恒等 1/1（c105ix 入れた）・読む c105s08 と設計の材料 c105cq は docs と measurements へ〕 | 中身は ★(1103) の行と下の〈次の波（波106〜）〉へ運んだ | 実装ログ (1096) |')
nxt = rd(os.path.join(W, 'next106.md')).strip('\n')
nxt = nxt.replace('@@CARRY@@', carry)
s = one(s, o + '\n', struck + '\n' + nxt + '\n')
# 採用率の表
a = '・設計の材料 1（c104rc〔段 R と C′ の素案〕）・計画 0**（★(1096)）。'
s = one(s, a, a[:-1] + ' → **波105 挙動 2/3（c105rr〔段 R〕・c105gp〔s18gp〕採った＝新しい基線 m1103・c105mc は旗 1 の予言に無い赤で腕 B の前に止めた＝旗 0 の器だけ）・恒等 1/1（c105ix）・読む 1（c105s08〔S08 輸送と地理〕）・設計の材料 1（c105cq〔段 C′ の事前登録〕）・計画 0**（★(1103)）。')
# §0-2
q0 = [l for l in s.split('\n') if l.startswith('| **★(1096) 作者に聞く物（波104 の合流・本当に迷う物だけ・推奨つき）** |')]
assert len(q0) == 1
q = rd(os.path.join(W, 'q1103.md')).strip('\n')
s = one(s, q0[0] + '\n', q + '\n' + q0[0] + '\n')
wr(F, s)

# 3. README：★(1096) の行の後に ★(1103)
R = os.path.join(T, 'docs/README.md')
s = rd(R)
r0 = [l for l in s.split('\n') if l.startswith('- ★(1096)：')]
assert len(r0) == 1
s = one(s, r0[0] + '\n', r0[0] + '\n' + rd(os.path.join(W, 'readme1103.md')).strip('\n') + '\n')
wr(R, s)

# 4. CURRENT §1：最終更新の行を前の更新へ下げ、新しい最終更新を置く
C = os.path.join(T, 'docs/CURRENT.md')
s = rd(C)
c0 = [l for l in s.split('\n') if l.startswith('| ★ **最終更新（main へ合流済み）** |')][:1]
assert len(c0) == 1
prev = c0[0].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(1096)） |', 1)
cur = rd(os.path.join(W, 'current1103.md')).strip('\n')
s = s.replace(c0[0] + '\n', cur + '\n' + prev + '\n', 1)
wr(C, s)
print('ok')
