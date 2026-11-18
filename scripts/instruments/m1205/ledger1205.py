# (1205) 合流：台帳 docs/機能の台帳.md の S21 を (1203) c120rh の 35 行へ差し替える（r1203/mk_testmd_s21.py の形・バイトで読み書き）。
#  §3 の S21-H の仮の行 20 本を消す（### S21 の見出しと空の表の頭は S12 と同じく残す）・§4-6 の ### 4-6-S21 の見出しと表の頭と 11-12 の 32 行を消す・
#  §2 に 2n S21 の節（35 行＋反証役 v の訂正）を足す・§1 の S21 の状態と残りタスクの列（⑩⑪ は済み）を直す。
import sys, re
R = sys.argv[1]
p = R + '/docs/機能の台帳.md'
md = open(p, 'rb').read().decode('utf-8')
nl = '\r\n' if '\r\n' in md else '\n'
L = md.split(nl)
ls = open(R + '/measurements/m1203_20261118/q3/ledger_S21_2026-11-18.tsv', encoding='utf-8').read().split('\n')
rows = ['| ' + ' | '.join(l.split('\t')) + ' |' for l in ls[1:] if l.strip()]
out = []; dh = dold = 0; skip46 = False
for l in L:
    if l.startswith('### 4-6-S21'): skip46 = True; continue
    if skip46:
        if l.startswith('### ') or l.startswith('## '): skip46 = False
        else:
            if re.match(r'^\| S21-\d\d \|', l): dold += 1
            continue
    if re.match(r'^\| S21-H\d\d\d \|', l): dh += 1; continue
    if re.match(r'^\| S21 \|', l):
        c = l.split('|')
        c[-3] = ' なし（⑩ 利札の残り＝S22 の側で入っている・⑪ InsolvencyQuarters＝grep 0 件で済み＝(1203)） '
        c[-2] = ' (1203) c120rh で確かめた（木 c68e09bf・写し 24 行は 7bb40c77 の判定・§2n） '
        l = '|'.join(c)
    out.append(l)
i3 = out.index(next(x for x in out if x.startswith('## 3. ')))
hdr = '| ' + ' | '.join(ls[0].split('\t')) + ' |'
sep = '|' + '---|' * 12
ins = ['## 2n. S21 国債と破産（(1203) c120rh で確かめた・木 c68e09bf・(1205) の合流が写した＝measurements/m1203_20261118/q3/ledger_S21_2026-11-18.tsv）', '', hdr, sep] + rows + ['',
 '### 2n-1. 反証役 v の訂正（合流 (1205) が足した・表の行は書き換えていない）', '',
 '- S21-05・K77 の〈国債の金利の最小 M 9 国 0.0162〉は台本 mk_tsv_s21.py の文に書いてあるだけだった＝反証役が keep_1198/base_ins の fiscal_s（9 国）と nation_s（全ての国）で数え直し、どちらも最小 0.0162・0.04/3 未満は 0 行（主張は正しい）。数え直しの台本は r1203/s21_recount.py。',
 '- S21-33 の英の国債の不履行の数は〈件〉と〈種〉を分けて読む：S11 は 3 件・2 種（種 11 に 2 件・種 15 に 1 件）＝(1198) の記録〈S11 2/10〉は種の数。',
 '- 12 列目の K77〜K82（dl_table_S21_2026-11-18.tsv）は消す候補＝〈消すと決まった〉ではない（AI の推奨・作者「全て推奨で」の下の候補）。', '']
out = out[:i3] + ins + out[i3:]
open(p, 'wb').write(nl.join(out).encode('utf-8'))
print('消した仮の行', dh, '消した 11-12 の行', dold, '足した行', len(rows))
