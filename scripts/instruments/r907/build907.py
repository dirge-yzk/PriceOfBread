# (907) c78lg：機能の台帳に S25 外交の節（§2d）を入れ、§3 の S25 の仮の行を置き換え、S09・S24 の重なりの行へ〈S25-xx で確かめた〉を書き、
#  S15-10 の判定（(900) 反証役の残り (a)）と 12 列目に読み手の数の無い 5 行（(c)）を埋めた下書きを作る（読むだけの線＝リポジトリは書かない・合流が写す）。
# 使い方: python -X utf8 build907.py <repo> <木> <今の台帳> <s25_rows.py> <出力の台帳> <S25 の節の出力> <id の差の出力>
import sys, re, subprocess, collections, importlib.util
repo, tree, src, spec, out, secout, idout = sys.argv[1:8]
sp = importlib.util.spec_from_file_location('rows', spec); M = importlib.util.module_from_spec(sp); sp.loader.exec_module(M)
b = open(src, 'rb').read().decode('utf-8')
assert '\r' not in b
import os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_anchor; build_anchor.init(repo, tree); resolve = build_anchor.resolve
rows = []
own_skip = {'Coefficients.cs'}
for (rid, L0, name, expect, anchors, kind, judge, basis, rest, fix, over) in M.ROWS:
    an = [resolve(a) for a in anchors]
    owners = []
    for a in an:
        f = a.split(':')[0]
        if f in own_skip or f.endswith('Tests.cs') or f in owners: continue
        owners.append(f)
    for c in (name, expect, basis, rest, fix): assert '|' not in c, (rid, c[:40])
    rows.append(dict(id=rid, L=L0, name=name, expect=expect, an=';'.join(an), own=' '.join(owners) or '—', kind=kind, judge=judge, basis=basis, rest=rest, fix=fix, over=over))
def rowline(r):
    return f"| {r['id']} | S25 | {r['name']} | {r['expect']}（L{r['L']}） | {r['an']} | {r['own']} | {r['kind']} | {tree} | {r['judge']} | {r['basis']} | {r['rest']} | {r['fix']} |"
HDR = '| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク | 直す／消す／残す |'
SEP = '|---|---|---|---|---|---|---|---|---|---|---|---|'
jc = collections.Counter(r['judge'] for r in rows)
head = f"""## 2d. S25 外交（(907) c78lg で確かめた・木 {tree}）

- 選び方：主の規則（その系統だけの持ち主のファイルのコミットの数・`git log --since '2026-09-26 00:00:00' --format=%h {tree} -- PriceOfBread.SimCore/Landmass.cs`）で対象外に残る唯一の系統 S25 は 2（b7fd1c8f・f7120308）。感度の規則（拠点の Diplomacy.cs を入れる）では {tree} で 130（同じ書き方で Landmass.cs＋Diplomacy.cs・§1 の表の 127 は 11db605f の数・11db605f で数え直すと時刻なし 124・時刻あり 129＝127 は再現しない＝記録）。どちらの規則でも S25。
- 範囲：文書 §3-8 外交（節は L5348・見出し L5398〜L5869 の 85 件・次の節 L5873＝grep で取り直した・§1 の表の 5398-5872 は見出しの範囲として今も合う）。
- 錨は木 {tree}（SOURCE-HASH aaecd9e3b5cce1b8・SimCore は main 258a4543 と同じ＝`git diff --stat {tree} 258a4543` は WriterRatchetTests.cs の 1 行だけ）で引いた。最後に確かめたは {tree}。
- 生の名：**R905**＝D:/PoB_work/keep_905/base_ins（m905 の腕 Z・計器つき 10 種）。ヌル腕 N1・N2＝D:/PoB_work/keep_905/n1・n2（同じ台本の出力 s25/raw_N1.txt・raw_N2.txt を並べた・判定には使っていない）。数えた台本と出力は D:/PoB_work/r907_work/s25/（合流が measurements/m907_<日付>/s25/ へ写す・s25_static.py・s25_raw.py・s25_raw2.py・s25_raw3.py・s25_rows.py）。成績表は measurements/b10_20261106_m905_ins（124・125 行）。
- 主な生の表：wars（戦争ごと）・invade（侵略の試みごと・屈服の印と足した緊張）・cess（割譲の州ごと）・cpeace（崩壊の講和ごと・repFactors＝戦争ごとの 会議の削り:条約港の倍率:支払い率）・rule（州×四半期の条約港の印と持ち主）・opium（国の行の禁令と緊張）・score（rankInForce）・scalars（defeatShock・warExhaustion）・monlaw（blockadeRiskShare）。**組ごとの緊張・理由の内訳・同盟・従属・禁輸の在庫の表は無い**＝緊張の理由ごとの行は〈未確かめ〉と名乗り §2d-4 に計器の仕様を書いた。
- 重なり：S09-H006・H008・H032、S24-H008・H010・H017・H057・H058・H074 は S25 の行（重なりの列）で確かめ、相手の行は〈S25-xx で確かめた〉と判定を写した（二度確かめない）。L5696 条約の批准権は S17-P07・S19-31 の行。
- 判定の〈働く〉は、生で門の両側か効きが出た行だけ。生に列が無い門は〈未確かめ〉と名乗る。判定の数：{'・'.join(f'{k} {v}' for k, v in sorted(jc.items()))}（{len(rows)} 行）。

{HDR}
{SEP}
""" + '\n'.join(rowline(r) for r in rows) + '\n'
tail = open(spec.replace('s25_rows.py', 's25_tail.md'), 'rb').read().decode('utf-8')
section = head + '\n' + tail
assert '\r' not in section
open(secout, 'wb').write(section.encode('utf-8'))
# ---- 台帳へ入れる ----
ROW = re.compile(r'^\| (S\d\d-[A-Z]?\d+) \|', re.M)
ids0 = [m.group(1) for m in ROW.finditer(b)]
i3 = b.index('\n## 3. ')
b = b[:i3] + '\n' + section.rstrip('\n') + '\n' + b[i3:]
k = b.index('\n### S25 外交\n'); e = b.index('\n### ', k + 5)
b = b[:k] + '\n### S25 外交\n\n- (907) c78lg が門ごとに割った＝§2d（仮の行 S25-H001〜S25-H075 は §2d の S25-01〜S25-77 へ置き換えた・行にしなかった見出しは §2d-1）。\n' + b[e:]
a = '| S25 | 外交 | 5398-5872 | 85 | Landmass.cs | 1 | 127 | なし | 未確かめ（対象外の候補） |'
assert b.count(a) == 1
b = b.replace(a, f'| S25 | 外交 | 5398-5872 | 85 | Landmass.cs | 1 | 127 | なし | (907) で確かめた（§2d・木 {tree}） |')
def cells_of(line): return [c.strip() for c in line.strip().strip('|').split('|')]
def set_row(rid, fn):
    global b
    m = re.search(r'^\| ' + re.escape(rid) + r' \|.*$', b, re.M)
    assert m, rid
    c = cells_of(m.group(0)); assert len(c) == 12, (rid, len(c))
    fn(c)
    for x in c: assert '|' not in x, (rid, x[:30])
    b = b[:m.start()] + '| ' + ' | '.join(c) + ' |' + b[m.end():]
JUNK = 'MultiNationSim.cs:12:Diplomacy'
byid = {r['id']: r for r in rows}
for r in rows:
    for o in r['over']:
        def f(c, r=r):
            if c[4] in ('—', '') or c[4] == JUNK or c[4].startswith(JUNK + ';'):
                c[4] = r['an']; c[5] = r['own']
            c[6] = r['kind']; c[7] = tree; c[8] = r['judge']
            c[9] = f"{r['id']} で確かめた（二度確かめない＝判定と根拠は {r['id']}・(907)）；" + c[9]
            if c[11] in ('—', ''): c[11] = f"{r['id']} を見る" if r['fix'] not in ('—', '') else '—'
        set_row(o, f)
# (a) S15-10
def f15(c):
    c[6] = '両側の門'; c[7] = tree; c[8] = '働く'
    c[9] = ('S19-06（生 R2＝keep_899/base_ins・木 101d4329）の数を引いた＝二度数えない：純支持で否決（openFrontStates 符号 1）参政権 17,431・秘密投票 160／通った一歩 参政権 10・秘密投票 159＝同じ門（MandateMargin の憲法級の閾値 0）の両側が出る・非憲法級の軸は符号 1 が 0（閾値 +∞）。'
            '植民地参政権・移民原住民参政権・女性の選挙権の段は戦線が 1・1・0＝門まで来ない（門の働きではなく上流）。Government.cs は 101d4329〜' + tree + ' で変わらない（git diff 0 行）。(907) で判定を埋めた（(900) 反証役の残り (a)）')
set_row('S15-10', f15)
def f1906(c):
    c[9] = c[9].replace('S15-10（未確かめ）の〈否決の理由ごとの数〉はこの列で読める（§2b は書き換えない・合流の判断）', 'S15-10 の〈否決の理由ごとの数〉はこの列で読める（(907) で S15-10 を〈働く〉に埋めた）')
set_row('S19-06', f1906)
# (c) 12 列目に読み手の数
ADD = {
 'S15-23': '（読み手：Transition の呼び手 コード 1＝Politics.cs:503・試験 9／PolityInertia コード 5＝Politics.cs:609・CrownParliament.cs:120・:144・GameViewModel.Politics.cs:325・Program.cs:2438・試験 1）',
 'S15-38': '（読み手：RepudiateDebt の呼び手 コード 2＝Fiscal.cs:362・Politics.cs:715・試験 0）',
 'S19-02': '（読み手：StagedAxis の呼び手 コード 1＝Government.cs:634・試験 0）',
 'S19-24': '（読み手：StateAtheism コード 5＝Government.cs:143・:741・Policy.cs:94・Politics.cs:380・Privileges.cs:37・試験 2）',
 'S19-25': '（読み手：NationalizationAll コード 14・試験 1／NationalizeAll コード 14・試験 2＝LawCatalog.cs:129・Government.cs:177・:178・:722・:727・Parliament.cs:382 ほか）',
}
for rid, add in ADD.items():
    def fa(c, add=add):
        assert not re.search(r'読み手[^0-9０-９]{0,12}[0-9]', c[11]), c[11][:60]
        c[11] = c[11] + add
    set_row(rid, fa)
ids1 = [m.group(1) for m in ROW.finditer(b)]
assert len(ids1) == len(set(ids1)), [x for x, n in collections.Counter(ids1).items() if n > 1]
gone = sorted(set(ids0) - set(ids1)); new = sorted(set(ids1) - set(ids0))
exp_gone = [f'S25-H{i:03d}' for i in range(1, 76)]
exp_new = [r['id'] for r in rows]
ok = gone == exp_gone and new == sorted(exp_new) and (set(ids0) - set(gone)) == (set(ids1) - set(new))
open(idout, 'wb').write((f'前 {len(ids0)} 行・後 {len(ids1)} 行・消えた {len(gone)}（S25-H001〜H075 と一致 {gone == exp_gone}）・足した {len(new)}（S25-01〜S25-{len(rows):02d} と一致 {new == sorted(exp_new)}）・他の id の集合が同じ {(set(ids0) - set(gone)) == (set(ids1) - set(new))}\n'
                        + '消えた ' + ' '.join(gone) + '\n足した ' + ' '.join(new) + '\n').encode('utf-8'))
assert ok
open(out, 'wb').write(b.encode('utf-8'))
print('書いた', out, '行', len(ids1), '判定', dict(jc))
