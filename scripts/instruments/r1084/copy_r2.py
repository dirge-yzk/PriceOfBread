# (1084) round2 の 9 系統の TSV を写せる形に直した写し（判定の語彙・門 9 の読み手の数）を作り、
# 台帳の写し（作業フォルダの中だけ）へ当てて本物の ledger_check.py を回す（読むだけ・main の台帳は触らない）
import re, os, subprocess, collections, sys
REPO = 'C:/Users/dirge/Documents/PriceOfBread'
S = 'C:/Users/dirge/AppData/Local/Temp/claude/ee79ebbd2c0731cc/9b9a86e7-43dc-4b00-9074-443f5c41f431/scratchpad'
W = 'D:/PoB_work/r1084_work'
LED = REPO + '/docs/機能の台帳.md'
SYS = ['S03', 'S04', 'S06', 'S09', 'S13', 'S21', 'S26', 'S28', 'S29']
VOC = ('未確かめ', '働く', '一部だけ働く', '働かない', '文書が古い')
# SimCore のコード行（注を除く）で名を数える
src = {}
for dp, dn, fn in os.walk(REPO + '/PriceOfBread.SimCore'):
    if '\\bin' in dp or '/bin' in dp or '\\obj' in dp or '/obj' in dp:
        continue
    for f in fn:
        if f.endswith('.cs'):
            src[os.path.join(dp, f)] = open(os.path.join(dp, f), 'rb').read().decode('utf-8', 'replace').split('\n')
def readers(name):
    pat = re.compile(r'\b' + re.escape(name) + r'\b')
    n = 0
    for p, ls in src.items():
        for L in ls:
            code = L.split('//')[0]
            if pat.search(code) and not re.search(r'public\s+[\w<>\[\]]+\s+' + re.escape(name) + r'\b', code):
                n += 1
    return n
fixlog = open(W + '/round2_fixlog.tsv', 'w', encoding='utf-8', newline='\n')
fixlog.write('行\t直した列\t前\t後\n')
fixed = {}
for sy in SYS:
    T = open(f'{W}/orig/ledger_{sy}_2026-11-12.tsv', 'rb').read().decode('utf-8').split('\n')
    hdr = T[0]
    out = [hdr]
    for t in T[1:]:
        if not t.strip():
            continue
        c = t.split('\t')
        if re.search(r'(^|;)\s*Program\.cs:', c[4]):
            new4 = re.sub(r'(^|;)(\s*)Program\.cs:', r'\1\2PriceOfBread.Runner/Program.cs:', c[4])
            fixlog.write(f'{c[0]}\t錨\tProgram.cs:…\tPriceOfBread.Runner/Program.cs:…（名だけでは 3 本の Program.cs から選べない）\n')
            c[4] = new4
        if c[7] == 'fc9aeb4c' and sy == 'S09':
            fixlog.write(f'{c[0]}\t最後に確かめた\tfc9aeb4c\t7bb40c77（錨の座標は 7bb40c77 の物）\n')
            c[7] = '7bb40c77'
        if c[6].strip() in ('—', '') and c[8] not in ('未確かめ',) and not c[8].startswith('この波の線が埋める'):
            fixlog.write(f'{c[0]}\t判定の型\t{c[6]}\t読まれない欄\n')
            c[6] = '読まれない欄'
            c[9] = '〈判定の型は (1084) が入れた（紙は —）〉；' + c[9]
        if c[11].startswith('消す') and not re.search(r'読み手[^0-9０-９]{0,12}[0-9]', c[11]):
            names = [a.split(':')[-1].strip() for a in c[4].split(';') if a.count(':') >= 2]
            names = [x for x in names if re.match(r'^[A-Za-z_]\w*$', x)]
            if names:
                rs = [(x, readers(x)) for x in names[:3]]
                cnt = '・'.join(f'{x} {r}' for x, r in rs)
                add = f'（読み手 {sum(r for _, r in rs)}＝SimCore のコード行 {cnt}・decb50fa5・(1084) が数えた）'
            else:
                add = '（読み手：錨に識別子が無い＝数えられない・(1084)）'
            fixlog.write(f'{c[0]}\t12列目\t{c[11][:40]}…\t…{add}\n')
            c[11] = c[11] + add
        out.append('\t'.join(c))
    fixed[sy] = out
    open(f'{W}/ledger_{sy}_r1084fix.tsv', 'w', encoding='utf-8', newline='\n').write('\n'.join(out) + '\n')
fixlog.close()
# 台帳の写し（案 a＝仮の行の判定は書き換えず、根拠の先頭に割った先を書く）
H2NEW = collections.defaultdict(list)
for t in open(W + '/round2_rows.tsv', encoding='utf-8').read().split('\n')[1:]:
    if not t.strip():
        continue
    c = t.split('\t')
    for h in re.findall(r'(S\d\d-H\d+)@\d+', c[6]):
        H2NEW[h].append(c[1])
L = open(LED, 'rb').read().decode('utf-8').split('\n')
newL = []
for s in L:
    m = re.match(r'\| (S\d\d-H\d+) \|', s)
    if m and m.group(1) in H2NEW:
        c = s.strip().strip('|').split('|')
        ids = H2NEW[m.group(1)]
        c[9] = ' 2026-11-12 紙の段で門ごとに割った＝' + '・'.join(ids) + '（§4-6）；' + c[9].strip() + ' '
        s = '|' + '|'.join(c) + '|'
    newL.append(s)
sec = ['', '## 4-6. 紙の段の 9 系統（2026-11-12 の紙の段・(1084) が写せる形に直した写し・合流が写す）', '']
for sy in SYS:
    sec.append(f'### 4-6-{sy}')
    sec.append('')
    rows = fixed[sy]
    sec.append('| ' + ' | '.join(rows[0].split('\t')) + ' |')
    sec.append('|' + '---|' * 12)
    for r in rows[1:]:
        sec.append('| ' + ' | '.join(x.strip() for x in r.split('\t')) + ' |')
    sec.append('')
sec += ['## 4-7. 観戦の指摘から足す行（2026-11-12 の観戦・(1084) の写しの表 spectate15.tsv の 13 行・錨は decb50fa5）', '']
SP = [l.split('\t') for l in open(W + '/spectate15.tsv', encoding='utf-8').read().split('\n')[1:] if l.strip()]
sec.append('| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク | 直す／消す／残す |')
sec.append('|' + '---|' * 12)
for c in SP:
    if c[0] == '—':
        continue
    c = c[:12]
    c[7] = 'decb50fa5'
    sec.append('| ' + ' | '.join(x.strip() for x in c) + ' |')
sec.append('')
open(W + '/ledger_dryrun_a.md', 'wb').write(('\n'.join(newL).rstrip('\n') + '\n' + '\n'.join(sec)).encode('utf-8'))
print('ok')
