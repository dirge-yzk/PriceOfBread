# (1087) 合流：機能の台帳の更新（バイトで読み書き・LF）。
#   python -X utf8 ledger1087.py <測った木の短いコミット> <出力 md>
#   ① ld (1084) の写し ledger_dryrun_a.md（紙の段 9 系統 436 行＋観戦 13 行・案 a）を土台にする（main の台帳 decb50fa5 から作った物＝main の台帳はその後動いていない）。
#   ② 反証役 v（ld）の指摘で S09-07・S09-10・S09-27 の〈最後に確かめた〉を fc9aeb4c へ戻し、根拠の先頭に〈錨は 7bb40c77 の座標〉を書く。
#   ③ s18 (1085) の 31 行を §2f として §3 の前に足す（§3 の S18 の仮の行は残す＝(1078) の S23 と同じ形）＋反証役 v の訂正 §2f-1。
#   ④ 挙動を変えた線の行：dm＝S05-H014・S05-H015（巨大建造物）・cr＝S29-98（データの札の空欄）の型・最後に確かめた・判定・根拠・12 列目。
#   ⑤ §0 の判定の型の語彙に〈頻度・不変条件・番人〉を足す（s18 と観戦の行が使う・ld の申し送り）。
import sys, csv, io
H, OUT = sys.argv[1], sys.argv[2]
R = 'D:/PoB_work/wt_m1087/'
def rd(p): return open(p, 'rb').read().decode('utf-8')
s = rd('D:/PoB_work/r1084_work/ledger_dryrun_a.md')
assert '\r' not in s
L = s.split('\n')
def row(i): return L[i].split(' | ')
def find(pref):
    r = [i for i, l in enumerate(L) if l.startswith(pref)]; assert len(r) == 1, (pref, len(r)); return r[0]
def setcols(rid, upd):
    i = find('| ' + rid + ' |')
    c = L[i][2:-2].split(' | ')
    assert len(c) == 12, (rid, len(c))
    for k, v in upd.items(): c[k] = v(c[k]) if callable(v) else v
    L[i] = '| ' + ' | '.join(c) + ' |'
n_fix = 0
# ② S09 の 3 行（列 7＝最後に確かめた・列 9＝根拠）
for rid in ('S09-07', 'S09-10', 'S09-27'):
    setcols(rid, {7: 'fc9aeb4c', 9: lambda x: '錨は 7bb40c77 の座標・判定は fc9aeb4c の S25 の行から引き継いだ（(1087) 合流が反証役 v の指摘で〈最後に確かめた〉を fc9aeb4c へ戻した）。' + x}); n_fix += 1
# ④ dm の 2 行
dmwhy = ('(1081) c103dm の DM-D′ で威信と巨大建造物を物理で消した（係数 11 本・megaprojects.json・州の 3 欄・計器の 8 列）＝(1087) 合流で main へ（S11 で採った）。'
         '残る口＝型 Megaproject・Diplomacy.Megaprojects（常に空）・Megaprojects.CanStart／TryStart（常に false）・RequirementMet と運河の表・TreasuryCat.Megaproject と帳簿の列 ledgerMegaprojectPerMonth（0）。'
         '門＝flow_s の Megaprojects.cs の行 0・ledgerMegaprojectPerMonth の 0 でない行 0（合わせた木の M・S11）。文書 L1055・L1060 は古い（消した機能を書く）')
mp = rd(R + 'PriceOfBread.SimCore/Megaprojects.cs').split('\n')
cs = [n + 1 for n, l in enumerate(mp) if 'CanStart(' in l and 'static' in l]; assert len(cs) == 1, cs
for rid in ('S05-H014', 'S05-H015'):
    setcols(rid, {4: f'Megaprojects.cs:{cs[0]}:CanStart', 6: '読まれない欄', 7: H, 8: '働かない（消した＝(1081)・口だけ残る）', 9: dmwhy,
                  11: '消す（口を消す線 DM-P＝App・Display・Runner に触れてよい回・文書の見出しも直す）・残る口の読み手 SimCore 5 ファイル 7 行（Megaprojects.cs の外）・App 2 ファイル 13 行・Runner 1 ファイル 9 行（64e20d396 で git grep Megaproject）'}); n_fix += 1
# 錨は名で引けるように宣言の行番号を後で reanchor ではなく実の行で入れる（下）
# ④ dm で古くなった他の行（合流の grep：FormationLegitimacy・灌漑・Megaproject の錨）
setcols('S27-H057', {4: '—', 6: '読まれない欄', 7: H, 8: '働かない（消した＝(1081)）',
                     9: '(1081) c103dm の DM-D′ で係数 FormationLegitimacyBoost を消した（Formations.cs の変名の後の正統性への一回加算）＝(1087) 合流で main へ（S11 で採った）。DormantIdentityTests の RetiredFields が名の戻りを赤にする。文書 L6349 は古い',
                     11: '消す（文書の見出しを直す・残る口は無い）・読み手 SimCore 0（係数ごと消えた）'}); n_fix += 1
for rid in ('S28-H022', 'S28-23'):
    setcols(rid, {4: f'Megaprojects.cs:{cs[0]}:CanStart', 6: '読まれない欄', 7: H, 8: '働かない（消した＝(1081)・灌漑ごと）',
                  9: '(1081) c103dm で巨大建造物を灌漑ごと物理で消した（megaprojects.json の灌漑・干拓の行と耕地を足す ApplyEffect）＝(1087) 合流で main へ（S11 で採った）。実行時に今ある州の耕地を広げる道は 0 本（紙 Q-DM1）＝〈耕地を投資の結果として広げる〉は鉱山と畑の上限 ③ の線と S05-H004 の残りタスク。基線は requires の無い灌漑が 1800 年より前に全て完成していた（反証役 v2）',
                  11: '消す（口を消す線 DM-P・文書の見出しを直す）・読み手 SimCore 5 ファイル 7 行（Megaprojects.cs の外・残る口）'}); n_fix += 1
# ④ cr の 1 行
setcols('S29-98', {7: H, 8: '働く（空文字の州 0・文化の空は名指しの許し 12 州だけ・宗教の例外 0）',
                   9: '(1082) c103cr＝world.json の 21 州に宗教（今ある名だけ）・9 州に文化・残る 12 州は文化を空のまま名指しの許し（理由つき）・番人 RegionLabelInvariantTests（構築した World.Regions を読む・開幕に昇格した植民地 81 州を含む・3 本）＝(1087) 合流で main へ（S11 で採った）。名が既にある名の集合に入るかを見る試験は無い（反証役 v・次の線）',
                   11: '残す（許しの 12 州は文化の 2 段 CU の線で小文化の名を置き、許しの行を消す）'}); n_fix += 1
# ⑤ §0 の語彙
k = find('- 判定の型＝届かない門／読まれない欄／天井の錠／片側だけの門／両側の門（両側が生で出るか）／効き。')
L[k] = L[k].replace('／効き。', '／効き／頻度（作者 2026-10-29〈頻発するのが問題〉の監視の型・(1085) s18）／不変条件／番人（観戦の行）＝(1087) 合流で足した。', 1)
# ③ §2f S18
rows = list(csv.reader(io.StringIO(rd('D:/PoB_work/r1085_work/ledger_S18_2026-11-12.tsv').replace('\r\n', '\n')), delimiter='\t'))
assert all(len(r) == 12 for r in rows if r), [len(r) for r in rows]
rows = [r for r in rows if r]
for r in rows:
    if r[0] == 'S18-17' and r[11] == '消す候補 K1':
        r[11] = '消す候補 K1・読み手 SimCore 0（w.Parties は Parliament.cs の外に 0）・App 1（GameViewModel.Politics.cs:349 の与党名）・試験 1（数は根拠の列から＝(1087) 合流が門 9 のために足した）'; n_fix += 1
tab = ['| ' + ' | '.join(c.replace('|', '／').replace('\n', ' ') for c in r) + ' |' for r in rows]
tab.insert(1, '|' + '---|' * len(rows[0]))
sec = rd(R + 'scripts/instruments/m1087/ledger_s18_sec.md').replace('\r\n', '\n').rstrip('\n').split('\n')
j = sec.index('@@TABLE@@'); sec[j:j + 1] = tab
i3 = find('## 3. 他の系統の仮の行')
L[i3:i3] = sec + ['']
open(OUT, 'wb').write('\n'.join(L).encode('utf-8'))
print('ok', 'S18 行', len(rows) - 1, '直した行', n_fix, '全行', len(L))
