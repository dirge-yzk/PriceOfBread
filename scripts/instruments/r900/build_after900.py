# (900) c77lg：今の docs/機能の台帳.md（木 101d4329）から、(a) §3 の同じ系統の見出しを畳み (b) S19 の節（§2c）を入れて §3 の S19 の仮の行を置き換え
#  (c) 12 列目〈直す／消す／残す〉を全ての台帳の表に足して決めた行を埋め (d) S04-P02 に錨を足した台帳の下書きを作る。
#  （(893) c76lg の build_after893.py を写して直した・読むだけの線＝リポジトリは書かない・合流が写す）
# 使い方: python -X utf8 build_after900.py <今の台帳> <S19 の節> <出力> [<id の表の出力>]
# 畳み方：§3（## 3. から ## 4. の前まで）の ### 見出しの塊を系統の鍵（Sxx）でまとめ、最初に出た位置に置く。
#  塊の中の行は出た順のまま後ろへ続ける（行の中身・id・系統の中の順は変えない）。畳む前と後で id の集合と系統ごとの id の並びが一致することを assert する。
import sys, re, collections
src, sec, out = sys.argv[1:4]
idout = sys.argv[4] if len(sys.argv) > 4 else None
b = open(src, 'rb').read().decode('utf-8')
s = open(sec, 'rb').read().decode('utf-8')
assert '\r' not in b and '\r' not in s
ROW = re.compile(r'^\| (S\d\d-[A-Z]?\d+) \|')
def ids_of(text): return [m.group(1) for l in text.split('\n') for m in [ROW.match(l)] if m]
def by_sys(ids):
    d = collections.defaultdict(list)
    for i in ids: d[i[:3]].append(i)
    return d
def rep(a, c, n=1):
    global b
    assert b.count(a) == n, (b.count(a), a[:60])
    b = b.replace(a, c)
ids0 = ids_of(b)
assert len(ids0) == len(set(ids0)), '畳む前に重複の id がある'
# ---- (a) §3 の見出しを畳む ----
i3 = b.index('\n## 3. '); i4 = b.index('\n## 4. ')
head, body3, tail = b[:i3], b[i3:i4], b[i4:]
L = body3.split('\n')
pre, blocks = [], []
for l in L:
    if l.startswith('### '): blocks.append([l]); continue
    (blocks[-1] if blocks else pre).append(l)
order, merged = [], {}
HDR = '| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |'
SEP = '|---|---|---|---|---|---|---|---|---|---|---|'
dup = collections.Counter()
for blk in blocks:
    key = blk[0].split()[1]
    dup[key] += 1
    rows = [l for l in blk[1:] if ROW.match(l)]
    other = [l for l in blk[1:] if not ROW.match(l) and l not in (HDR, SEP, '')]
    if key not in merged:
        order.append(key); merged[key] = dict(title=blk[0], rows=[], other=[])
    else:
        assert merged[key]['title'] == blk[0], ('同じ鍵で題が違う', merged[key]['title'], blk[0])
    merged[key]['rows'] += rows; merged[key]['other'] += other
folded_from = {k: n for k, n in dup.items() if n > 1}
nb = list(pre)
for k in order:
    m = merged[k]
    nb += [m['title'], '']
    if m['other']: nb += m['other'] + ['']
    if m['rows']: nb += [HDR, SEP] + m['rows'] + ['']
    nb += ['']
while nb and nb[-1] == '': nb.pop()
body3n = '\n'.join(nb) + '\n'
b = head + body3n + tail
ids1 = ids_of(b)
assert sorted(ids0) == sorted(ids1), '畳む前と後で id の集合が違う'
assert by_sys(ids0) == by_sys(ids1), '系統ごとの id の並びが違う'
print('(a) 畳んだ系統（見出しの数）', dict(sorted(folded_from.items())), '行の id', len(ids0), '→', len(ids1), '集合と系統ごとの並びが一致')
# ---- (b) S19 の節を入れて §3 の S19 の仮の行を置き換える ----
rep('**門ごとに割ってあるのは確かめた系統だけ**（今は S16・S15）', '**門ごとに割ってあるのは確かめた系統だけ**（今は S16・S15・S19）')
rep('- 錨の確かめ：`python -X utf8 scripts/instruments/r893/ledger_check.py <repo> <木> docs/機能の台帳.md`＝',
    '- 錨の確かめ：`python -X utf8 scripts/instruments/r900/ledger_check.py <repo> <木> docs/機能の台帳.md`＝(900) で門 7（台帳の表の全ての行と見出しが 12 列）・門 8（§3 の同じ系統の見出しが 1 つだけ）・門 9（〈消す〉の行は読み手の数を持つ）を足し、門ごとに通る／落ちるを印字する。それまでの説明：')
rep('- 確かめ直す行の選び方（AI のプレイテストの前）：`python -X utf8 scripts/instruments/r893/changed_rows.py <repo か worktree> docs/機能の台帳.md`＝',
    '- 確かめ直す行の選び方（AI のプレイテストの前）：`python -X utf8 scripts/instruments/r900/changed_rows.py <repo か worktree> docs/機能の台帳.md`（(900) で 12 列の行を読むようにした＝r893 の版は 11 列の行だけを読み、12 列の台帳では行が 0 になる）＝')
rep('- 列：行・系統・機能・期待する動き（文書の出典の行）・錨（file:line:名＝木で引ける宣言か式の行）・持ち主のファイル（錨の係数を読むファイルと文書が名指すファイル・Coefficients.cs と計器は除く）・判定の型・最後に確かめたコミット・判定・根拠・残りタスク。',
    '- 列：行・系統・機能・期待する動き（文書の出典の行）・錨（file:line:名＝木で引ける宣言か式の行）・持ち主のファイル（錨の係数を読むファイルと文書が名指すファイル・Coefficients.cs と計器は除く）・判定の型・最後に確かめたコミット・判定・根拠・残りタスク・**直す／消す／残す**（(900) で足した 12 列目）。\n'
    '- 12 列目〈直す／消す／残す〉：確かめた系統で判定が〈働かない・一部だけ働く〉の行と休眠（係数 0・旗 0）の行に、推奨と理由を 1 行（他の行は〈—〉）。影響の小ささは読み手の数と量の桁で読む（計測しない・9-26 の基準 ②③）。〈消す〉の型＝(i) 読み手が無い・係数 0 や旗 0 で何も変えない（恒等の線）(ii) 量が小さく他の帯に届かない (iii) 既存の機構から出るべき物を別の式で足している（思想⑨）（(ii)(iii) は挙動の線）。残す例外＝点火の予定がある物・作者の原文で頼まれた物。〈消す〉は読み手の grep の数を書く（門 9）。AI の推奨＝作者「全て推奨で」で推奨を採った（作者 2026-11-06「動作していないロジックのうち、ほとんどゲームに影響を与えないもの、わざわざロジックを実装するまでもない（既存の機能から自然に出るべき）ものは、削除することも検討してください。」）。\n'
    '- §3 の見出し（(900) で畳んだ）：文書の分野の行の範囲が飛ぶ系統（S10・S13・S17・S19・S20・S22・S26・S27）は、生成の時に範囲ごとに見出しが立っていた＝(900) の build_after900.py が系統ごとに最初の位置へ畳んだ（行の中身・id・系統の中の順は変えない）。')
rep('- 生の名：R1＝D:/PoB_work/keep_885/base_ins', '- 生の名：R2＝D:/PoB_work/keep_899/base_ins（合わせた木 ea7fcad2 の腕 Z・計器つき 10 種・m873 と全行一致＝(900) の S19 が読んだ）。R1＝D:/PoB_work/keep_885/base_ins')
rep('- **錨の座標**（(899) の合流）：', '- **changed_rows の限界**（(900) の下書き・(893) 反証役〈拾う範囲〉）：係数の変更で拾うのは〈錨の行に出る c.X・Coef.X・coef.X の X〉と〈Coefficients.cs の錨の名〉の宣言の行（名の直後に = か { か ;）だけ。関数の別の行や別名の変数（`var c = w.Coef` 以外の名・引数で渡した係数）で読む係数・Coefficients.cs の宣言でない行（初期化子の式・プロパティの本体）の変更は拾わない。持ち主のファイルの変更は全て拾う（ファイルの単位）。錨が〈—〉の行は係数の側を拾わず、持ち主のファイルだけで拾う。⚠ 錨の行番号は〈錨を引き直した木〉の行（(899) の reanchor で S15 の行は 101d4329 系の行）だが、changed_rows は錨の行を〈最後に確かめたコミット〉の木で読む（line_at）＝二つの木が違う行（S15 の 731d3de1・S04-P02 の 11db605f ほか）では古い木の同じ番号の行を読み、名の一致を確かめない＝係数名の拾いが黙って外れうる。S04-P02 に (900) で足した錨（EconomyStep.cs:128）は門 6 と持ち主のファイルの追跡のためで、係数の拾いのためではない。\n- **錨の座標**（(899) の合流）：')
rep('| S19 | 法の成立 | 3400-3626,3666-3694,3750-3801 | 45 | Contraband.cs Policy.cs | 15 | 73 | なし | 未確かめ（対象外の候補） |',
    '| S19 | 法の成立 | 3400-3626,3666-3694,3750-3801 | 45 | Contraband.cs Policy.cs | 15 | 73 | なし | (900) で確かめた（§2c・木 101d4329） |')
b = b.replace('\n## 3. 他の系統の仮の行（見出し 1 件＝1 行・判定は未確かめ）\n', '\n' + s.rstrip('\n') + '\n\n## 3. 他の系統の仮の行（見出し 1 件＝1 行・判定は未確かめ）\n', 1)
i = b.index('\n### S19 法の成立\n', b.index('\n## 3. '))
j = b.index('\n### ', i + 1)
s19h = [x for x in ids_of(b[i:j]) if x.startswith('S19-H')]
b = b[:i] + '\n### S19 法の成立\n\n- (900) c77lg が門ごとに割った＝§2c（仮の行 S19-H001〜S19-H042 は §2c の S19-01〜S19-43 へ置き換えた）。\n' + b[j:]
assert b.count('\n### S19 法の成立\n') == 1
print('(b) 外した S19 の仮の行', len(s19h), '足した S19 の行', len([x for x in ids_of(s) if x.startswith('S19-')]))
# ---- (d) S04-P02 に錨を足す（P04・P05 は 1 行に絞れる式が無い＝足さない） ----
rep('| S04-P02 | S04 | 農民の失業（b′） | 自給の必要な労働を超えた農民が失業する | — | EconomyStep.cs |',
    '| S04-P02 | S04 | 農民の失業（b′） | 自給の必要な労働を超えた農民が失業する | EconomyStep.cs:128:private static double FarmUnemploymentStage2( | EconomyStep.cs |')
# ---- (c) 12 列目を足す ----
FIX = {
 'S15-17': '残す：床が噛むのは稀（R1′ で 15 行）・読み手は正統性の 1 本＝消す根拠が無い（床の形が設計どおりかは国民意識の列が要る＝§2b-4）',
 'S15-23': '直す（読んでから）：引力点が立憲・共和・ブル独の 3 つだけ＝S15-37・S15-38・S19-24・S19-25 の届かなさの根（読む線 attract_s・00一覧 ⑦）',
 'S15-32': '消す（i・読み手 0＝届かない枝・試験の呼び手 1＝ConservationTests.cs:682）：経路 6 と実行部の violent＝偽の枝（恒等の線 ⑤(c) r886i・NaN の道の向きを事前登録に書く）',
 'S15-38': '残す：上流（S15-23）の下流・プロ独と無政府に届けば働く・RepudiateDebt は不履行（Fiscal.cs）でも使う',
 'S15-42': '残す：点火の予定（10-18 精査 政7＝導出は必要・③-4・思想⑩）・読み手 SimCore 2 行',
 'S16-32': '直す：床（RevoltVoiceFloor）は思想⑧に反する形＝点火のときに床でない形へ（10-18 精査 政1〈RevoltVoiceFloor は修正〉）・読み手 SimCore 1 行',
 'S16-33': '残す：点火の予定（10-18 精査 政1＝必要・③-1・枝 rev）・読み手 SimCore 3 行',
 'S16-37': '残す：点火の予定（10-18 精査 政1＝必要・③-1）・同じ主フラグの読み手 SimCore 9 行',
 'S16-38': '残す：点火の予定（10-18 精査 政1＝必要・③-1）・読み手 SimCore 5 行',
 'S19-02': '直す（文書）：施行は 1 段ずつで害が無い＝文書を〈照会の側の門〉と書き直す（overview_s19.patch）',
 'S19-13': '残す：作者 2026-10-11「これは残す。特権はすべて所得に繋がる訳ではない」',
 'S19-18': '残す（上流待ち）：効きは常に 0 だが上流の封鎖の危険の書き手（S09 R-a）が残りタスク。書き手を入れないと決まれば消す（ii・挙動の線・SimCore の行 15＝宣言と AI と要求を含む）',
 'S19-21': '残す：点火の予定（10-18 精査 政1＝必要・③-1）・読み手 SimCore 9 行',
 'S19-24': '残す：上流（S15-23）の下流・政体の属性の例外（Government.cs:741）',
 'S19-25': '残す：『全て』の政体の門は docs/202608/20260814_台本を創発へ_部分完了.md が残すと決めた・鉱山・軍需・石油が動かないのは AI の目標の側（未確かめ）',
 'S19-29': '残す：点火の予定（10-18 精査 政11＝必要（後回し）・③-3 研究と高等教育）・選好の読み手 1 行',
 'S19-30': '残す：点火の予定（10-18 精査 政4＝修正・③-2 特権の束）・読み手 SimCore 11 行（計器 1 を含む）。GuildMobilityDrag だけは精査が〈消してよい〉と書く（S19 の錨の外）',
}
nh = b.count(HDR); assert nh >= 1
b = b.replace(HDR + '\n' + SEP + '\n', HDR + ' 直す／消す／残す |\n' + SEP + '---|\n')
assert b.count(HDR + ' 直す／消す／残す |') == nh and b.count(SEP + '---|') == nh
outl, used = [], set()
for l in b.split('\n'):
    m = ROW.match(l)
    if m:
        n = len(l.strip().strip('|').split('|'))
        assert n == 11, (m.group(1), n)
        v = FIX.get(m.group(1), '—'); used.add(m.group(1)) if m.group(1) in FIX else None
        assert '|' not in v
        l = l.rstrip() + ' ' + v + ' |'
    outl.append(l)
assert used == set(FIX), set(FIX) - used
b = '\n'.join(outl)
ids2 = ids_of(b)
print('(c) 台帳の表', nh, '本に 12 列目を足した・埋めた行', len(used), '・行の id', len(ids2))
open(out, 'wb').write(b.encode('utf-8'))
if idout:
    open(idout, 'wb').write(('\n'.join(f'{x}' for x in ids0) + '\n').encode('utf-8'))
print('書いた', out, len(b.split('\n')), '行')
