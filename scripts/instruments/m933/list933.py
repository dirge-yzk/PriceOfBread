# (933) 合流：00一覧（行 (927)〜(933)・§0-2 ★(933)・§00〈次の波（波82〜）〉・採用率の表）と CURRENT §1 の先頭と実装ログの追記（バイトで読み書き）。
#   python -X utf8 list933.py <repo>
import sys, os
R = sys.argv[1]
L = R + '/docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
C = R + '/docs/CURRENT.md'
G = R + '/docs/202609/99_実装ログ_20260903.md'
H = os.path.dirname(os.path.abspath(__file__))
b = open(L, 'rb').read().decode('utf-8')
lines = b.split('\n')
def idx(prefix):
    h = [i for i, l in enumerate(lines) if l.startswith(prefix)]
    assert len(h) == 1, (prefix, len(h))
    return h[0]

rows = [
'| **★(927) 恒等の線 c81sm 小物の束 (i)〜(iv)（波81・ident c81sm〈判定: 恒等〉・完了〔★(933) で main へ〕）** | 2026-11-07：(i) mut_all908.sh の LsMax の TierRule の項目を今の木で回した＝LandShareTests.T7f が赤（予言どおり・試験の追加は不要）(ii) heirDebtSettledPerQuarter を Bank.Lend の前後の債務の差（実際に移った額）で数える＋HeirDebtSettledMovedTests (iii) EmptyPopDebtFirstTests.cs:154・CearnTradeColumnsTests.cs:199 を EndsWith へ戻した (iv) 走査の番人 OnDoubleReadScanTests（2 回以上読む 33 関数の表）。反証役：heir_diff.txt の〈相対差 1e-6 超 0 行〉は誤り（804,777 本・旧が正で新が 0 の床の型が 802,768）・走査の鍵の名が注釈の語になる型（次の掃除の線）・mut920.py の M1 の旧の文字列。 | 実装ログ (927) |',
'| **★(928) 恒等の線 c81im 矯正講和の計器 imp_s と crownpc_s の crownRegisterSourceCode（波81・ident c81im〈判定: 恒等〉・完了〔★(933) で main へ〕）** | 2026-11-07：imp_s（16 列・同じ月の順番の列つき）と crownpc_s の末尾 1 列（0 開幕・1 政体変更・2 矯正講和・3 蜂起の決着・4 廃位・5 幕府の終わり・6 不明）。10 種で imp_s 456 行・順番の分布 1:382・2:55・3:16・4:2・5:1。反証役：**符号 0 は開幕と建国を畳む**（途中で生まれた 154 国の最初の行は全て 0＝注の〈6＝建国〉は誤り・F1 では最初の行の quarter＞0 で分ける）・突然変異の表の穴（year・absScoreIndex・勝者と押し付けの政体の取り違え・符号 2・5・6 の口）・同じ敗者が同じ月に何度も矯正される（種 7・1806 年の露は 5 回）＝F1 の材料。 | 実装ログ (928) |',
'| **★(929) 挙動の線 c81cw 内戦の軍需の 2 回目（波81・旗 CivilWarArmsDemandUnified 既定 1・★(933) で採った）** | 2026-11-07：74abe7a7 から腕 B と S11 だけ。門 G1〜G3・G1f 全て緑（a_c 腕 B 10.714・S11 10.829・newRuleMismatch 0）・止める線 0。合流の M・S11 でも全て緑（a_c 10.796・10.786）。3 回の規則＝腕つきの 2 回目で採った。反証役：共和制フランス（種 1）の GDP 0 の区間は本国格になった後も 0 に近い（線が原因かは未確定・記録）・〈★赤 異常 2 件〉の名乗り漏れ。 | 実装ログ (929)・(933) |',
'| **★(930) 挙動の線 c81la B-2a の再走（波81・名簿 7 国の landOwnership・★(933) で採った）** | 2026-11-07：d590fc9f のデータを main の頭へ・StateFactoryTests の RGO の期待を 4 枠へ・注の直し 2 件・opening_bake.json の焼き直し。門 G-B1〜G-B3 全て通る（Δ÷P 腕 B 0.9775〜1.0125）・止める線 0・合流の M・S11 で門の行は線とバイトで同じ。3 回の規則＝腕つきの 1 回目で採った。反証役：reads930.sh の S11 の枝は BASE1 を渡さない（★(933) の read933.sh は渡す）・フルスイートの木の sha256 は再現できない（傍証で支える）。 | 実装ログ (930)・(933) |',
'| **★(931) 読む線 c81co 植民地格の州が人口を保ったまま産出 0 を続ける形（波81・コミット 0〔★(933) が写した〕・完了）** | 2026-11-07：`docs/202611/20261107/20261107_植民地格の州の産出0の続き_読み_下書き.md`・`scripts/instruments/r931/`・`measurements/m931_20261107/`。m919 で 98 区間・4,126 州×四半期（全て植民地格・人は抜けない）・人頭税が州のお金を吸い切る・再点火の発注だけが持ち主の現金で払う形＝推奨〈計器 zerop_s（恒等）→ 案 1〉。反証役：区間の中では 95% の州×四半期で州の貯蓄が 1e-12 未満（〈4 桁上で届かない〉は入り口だけの数）・G1 は種ごとの中央で・G2 は生産者の数で。 | 実装ログ (931) |',
'| **★(932) 設計の材料の線 c81ld 地主の政治の導き直し C-0′＋D-0（盤面 m919・波81・コミット 0〔★(933) が写した〕・完了）** | 2026-11-07：`docs/202611/20261107/20261107_地主の政治の導き直しの段C-0′＋D-0_盤面m919_材料.md`・`_下書き.md`・`scripts/instruments/r932/`・`measurements/m932_20261107/`。corncf_s 90 列・lordstat_s 28 列・試験 T1〜T17・L1〜L11・新しい状態 0・C-1 の着値は (a′)。反証役：G1 の〈通る側〉は紙で示せていない（C-0′ の生で初めて）・割譲の〈約 8 割〉は割譲だけなら 96〜97%・§4 の史実の型は出典なし。 | 実装ログ (932) |',
'| **★(933) 合流（波81・挙動の線 c81cw・c81la を採った新しい基線 m933・恒等 c81sm・c81im・読む c81co・設計の材料 c81ld・完了）** | 2026-11-07：予言 `measurements/m933_20261107_prereg_merge.txt`・**腕 Z の ident m933z は〈判定: 不一致〉＝道具の限界**（c81sm の宣言の列と c81im の Politics.cs の +1 行の経路が同じ cearn_s に重なる＝写しの道を通らない・独自の検算 zcheck933 で違いは宣言の列 809,504 欄と経路の写し 586 欄だけ・その他 0）。アブレーションの成績表は線の腕 B と diff 0 行（素の 10 種で確かめた）。M・S11 で両方の線の門が緑・止める線 0・空洞化 1/10 未満 0・推定 212.2 秒。新しい基線 m933（`b10_20261107_m933_ins`・NG 93〔ng933〕・ヌル腕 N1 211.1・N2 212.0・S11 211.9 秒・生 keep_933）。ident.cfg（INSRAW keep_933/base_ins・BASE_COMMIT 1af3e5d7・TESTS_NONFULL 2418）。 | 実装ログ (933) |',
]
i = idx('| **★(926) 合流（波80')
lines[i + 1:i + 1] = rows

nw = open(H + '/next933.md', 'rb').read().decode('utf-8').strip('\n')
i = idx('| **次の波（波81〜・(926) の推奨')
lines[i] = nw

q = open(H + '/ask933.md', 'rb').read().decode('utf-8').strip('\n')
i = idx('| **★(926) 作者に聞く物（波80 の合流')
lines[i:i] = [q]

i = idx('| **採用率の表（★(685)')
old = '・計画 0**（★(926)）。'
assert lines[i].count(old) == 1
lines[i] = lines[i].replace(old, old[:-1] + ' → **波81 挙動 2/2（c81cw〔内戦の軍需の 2 回目・採った〕・c81la〔B-2a の再走・腕つきの 1 回目・採った〕）・恒等 2（c81sm〔小物の束〕・c81im〔imp_s〕＝腕 Z は道具の限界で〈不一致〉・検算で入れた）・読む 1（c81co〔植民地格の州の産出 0〕）・設計の材料 1（c81ld〔地主の政治 C-0′＋D-0〕）・計画 0**（★(933)）。')
open(L, 'wb').write('\n'.join(lines).encode('utf-8'))
print('00一覧 ok')

c = open(C, 'rb').read().decode('utf-8')
k0 = c.index('| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-07（(926) 合流（波80）')
k1 = c.index('\n', k0)
cur = c[k0:k1]
seg = cur.split(' | ')
assert len(seg) == 3, len(seg)
new_val = ('⚠ **2026-11-07（(933) 合流（波81）＝挙動の線 c81cw〔(929) 内戦の軍需・旗 CivilWarArmsDemandUnified 既定 1〕と c81la〔(930) B-2a の名簿 7 国の landOwnership〕を採った＝新しい基線 m933**。恒等の線 c81sm〔(927) 小物の束〕・c81im〔(928) 計器 imp_s と crownpc_s の crownRegisterSourceCode〕を入れた。'
           '**腕 Z の ident m933z は〈判定: 不一致〉＝道具の限界**（宣言の列と経路の行の移動が同じ cearn_s に重なる・独自の検算 zcheck933 で違いは宣言の列と経路の写しだけ＝成績表 2 枚・注視の表・NG・テストの数・savetest・leakhunt は一致）。'
           'M・S11 で両方の線の門が緑・止める線 0・推定 212.2 秒（名目の余り 約 36.3 秒）。読む線 c81co〔(931) 植民地格の州の産出 0〕と設計の材料 c81ld〔(932) 地主の政治 C-0′＋D-0〕は docs と measurements へ写した。'
           '表の基準は `b10_20261107_m933_ins`（exact 45・near 86・NG 93〔ng933〕・ヌル腕 `_n1_ins`・`_n2_ins`・S11 `b10s11_20261107_m933_ins`・生 `D:/PoB_work/keep_933`）・ident.cfg（INSRAW keep_933/base_ins・BASE_COMMIT 1af3e5d7・TESTS_NONFULL 2418）。'
           '前の行：(926) 合流（波80）＝挙動の線 0 本・基線 m919。')
seg[1] = new_val
c = c[:k0] + ' | '.join(seg) + c[k1:]
open(C, 'wb').write(c.encode('utf-8'))
print('CURRENT ok')

g = open(G, 'rb').read()
add = open('D:/PoB_work/m933/log_lines.md', 'rb').read() + open('D:/PoB_work/m933/log_933.md', 'rb').read()
if not g.endswith(b'\n'):
    g += b'\n'
open(G, 'wb').write(g + add)
print('log ok')
