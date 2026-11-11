# 読むだけ：機能の台帳（docs/機能の台帳.md の下書き）を組む。
# 入力＝文書 docs/ゲーム概要と実装機能.md・系統の表 systems.tsv・錨つきの見出し feats_anch.json・S16 の門の行 s16/rows_s16.tsv・系統の付表 sysmeta.tsv
# 使い方: python -X utf8 build_ledger.py <repo> <head> <workdir> <出力 md> <文書 md> <錨つきの見出し json> <系統の表 tsv> <S16 の行 tsv> <S16 の注 md> <旧→新の行の対応 json>
#   文書は overview.patch を当てた後の物（行の参照はその文書の行）。行の id は系統の中の見出しの連番（Sxx-Hnnn）＝文書の行番号に結ばない。
import json, sys, subprocess, re, collections, os, csv
repo, head, wd, outp, docp, featsp, sysp, s16p, notesp, mapp = sys.argv[1:11]
doc = open(docp, 'rb').read().decode('utf-8-sig').split('\n')
feats = json.load(open(featsp, encoding='utf-8'))
lmap = {int(k): v for k, v in json.load(open(mapp, encoding='utf-8')).items()}
systems = []
for l in open(sysp, encoding='utf-8').read().splitlines()[1:]:
    sid, name, rng, sec = l.split('\t'); systems.append((sid, name, rng, sec))
meta = {}
with open(os.path.join(wd, 'sysmeta.tsv'), encoding='utf-8', newline='') as fh:
    for r in csv.DictReader(fh, delimiter='\t'): meta[r['系統']] = r
cnt_main = {}; cnt_sens = {}; own_main = {}
for l in open(os.path.join(wd, 'syscount.txt'), encoding='utf-8').read().splitlines():
    p = l.split('\t')
    if len(p) == 3 and p[0].startswith('S'): cnt_main[p[0]] = int(p[1]); own_main[p[0]] = p[2]
for l in open(os.path.join(wd, 'syscount_sens.txt'), encoding='utf-8').read().splitlines():
    p = l.split(' ')
    if p and re.match(r'^S\d\d$', p[0]): cnt_sens[p[0]] = int(p[1])
# 木のファイルと SimCore の本文
tracked = [p for p in subprocess.run(['git', '-C', repo, 'ls-tree', '-r', '--name-only', head], capture_output=True, text=True).stdout.split('\n') if p]
simcs = [p for p in tracked if p.startswith('PriceOfBread.SimCore/') and p.endswith('.cs')]
byname = collections.defaultdict(list)
for p in tracked: byname[p.split('/')[-1]].append(p)
src = {p: subprocess.run(['git', '-C', repo, 'show', f'{head}:{p}'], capture_output=True).stdout.decode('utf-8-sig', errors='replace').split('\n') for p in simcs}
decl_cache = {}
def decl(name):
    if name in decl_cache: return decl_cache[name]
    best = None
    order = ['PriceOfBread.SimCore/Coefficients.cs'] + [p for p in simcs if p != 'PriceOfBread.SimCore/Coefficients.cs']
    rx = re.compile(r'\b(public|internal|private|protected|const|static|readonly)\b[^=;(]*\b' + re.escape(name) + r'\b\s*(=|\{|\(|;|=>)')
    for p in order:
        for i, l in enumerate(src[p], 1):
            if rx.search(l.split('//')[0]): best = (p, i); break
        if best: break
    decl_cache[name] = best; return best
reader_cache = {}
def readers(name):
    if name in reader_cache: return reader_cache[name]
    rx = re.compile(r'\.' + re.escape(name) + r'\b')
    fs = sorted({p.split('/')[-1] for p in simcs if p != 'PriceOfBread.SimCore/Coefficients.cs' and not p.split('/')[-1].startswith('Instruments') and any(rx.search(l.split('//')[0]) for l in src[p])})
    reader_cache[name] = fs; return fs
def body(line):
    out = []
    for l in doc[line:line + 40]:
        if l.startswith('#'): break
        out.append(l)
    return '\n'.join(out)
def norm(t):
    t = re.sub(r'^[●○◐◇]\s*', '', t.replace('~~', '').strip())
    t = re.sub(r'（★.*$', '', t)
    return t.strip()
# 残りタスクと (b) の割り当て（行の単位）
b_rows = {}   # 文書の行 → 線
for k, v in [(824, '(891) c75ds'), (829, '(891) c75ds'), (1042, '(891) c75ds'), (3856, '(891) c75ds'), (3299, '(891) c75ds'),
             (2243, '(890) c75ts'), (6507, '(890) c75ts'), (909, '(890) c75ts')]:
    b_rows[lmap[k]] = v
b_sys = {'S07': '(887) c75ip', 'S27': '(888) c75ce', 'S17': '(889) c75cp', 'S04': '(890) c75ts'}
s16 = []
with open(s16p, encoding='utf-8', newline='') as fh:
    for r in csv.DictReader(fh, delimiter='\t'): s16.append(r)
# 見出しの行（S16 以外）
seen_title = {}; rows = []; dup = 0; anch_rows = 0; hno = collections.Counter()
fold = collections.defaultdict(list)
for f in feats:
    t = norm(f['title'])
    if t in seen_title: fold[seen_title[t]].append((f['line'], f['system'])); dup += 1; continue
    seen_title[t] = f['line']
for f in feats:
    t = norm(f['title'])
    if seen_title[t] != f['line'] or f['system'] == 'S16': continue
    anchors = list(f['anchors'][:4])
    owners = set()
    for a in f['anchors']:
        owners.update(readers(a.split(':')[2]))
    if not anchors:
        b = body(f['line'])
        ids = []
        for seg in re.findall(r'`([^`]+)`', b):
            for m in re.findall(r'(?:[A-Z][A-Za-z0-9]*\.)?([A-Z][A-Za-z0-9]{3,})', seg):
                if m.endswith('cs') or m in ids: continue
                ids.append(m)
        for m in ids:
            d = decl(m)
            if d: anchors.append(f'{d[0].split("/")[-1]}:{d[1]}:{m}'); owners.add(d[0].split('/')[-1])
            if len(anchors) >= 3: break
    for fn in f['files']:
        if any(p.startswith('PriceOfBread.SimCore/') for p in byname.get(fn, [])) and fn != 'Coefficients.cs': owners.add(fn)
    if anchors: anch_rows += 1
    ex = '；同じ題 ' + '・'.join(f'L{l}（{s}）' for l, s in fold[f['line']]) if fold[f['line']] else ''
    struck = '（廃止の見出し）' if f['struck'] else ''
    sid = f['system']
    who = b_rows.get(f['line']) or b_sys.get(sid)
    judge = f'この波の線が埋める {who}' if who else '未確かめ'
    gone = ('；消えた係数 ' + '・'.join(f['vars_missing'])) if f['vars_missing'] else ''
    hno[sid] += 1
    rows.append(dict(id=f'{sid}-H{hno[sid]:03d}', sys=sid, title=t + struck,
                     expect=f'文書 L{f["line"]}（粒＝見出し・未分割）{ex}',
                     anchor=';'.join(anchors) if anchors else '—', owner=' '.join(sorted(owners)) if owners else '—',
                     kind='—', last='—', judge=judge, basis=f'文書の記号 {f["sym"]}（9-26・写さない）{gone}',
                     task=meta[sid]['残りタスク'] if meta[sid]['残りタスク'] else '—'))
def esc(s): return str(s).replace('|', '／').replace('\n', ' ')
L = []
A = L.append
A('# 機能の台帳（チェックシート）')
A('')
A(f'〈改修の度の点検〉と〈一括の棚卸し〉を 1 つの表でつなぐ生きた台帳。作者 2026-11-06（原文「最終的には、全ての機能が正しく機能しているかをチェックする必要がある」「二度同じ機能をチェックするのはバカらしい」・つなぐ形は作者「判断して」で推奨を採った）。運用は docs/README.md ★(885)。骨は (886) c75lg（木 {head[:8]}）。')
A('')
A('## 0. 読み方と運用')
A('')
A('- 行の粒＝〈1 つの門か式＋狙いの動き〉。**門ごとに割ってあるのは確かめた系統だけ**（今は S16）。他の系統の行は文書の見出し 1 件＝1 行の仮の行（期待する動きの列に〈粒＝見出し・未分割〉）＝その系統を確かめる線が門ごとに割る。1 本の読む線で 7151 行を直さないための範囲の切り方。')
A('- 文書（docs/ゲーム概要と実装機能.md）は同じ機構を複数の分野に載せる。同じ題の見出しは 1 行に畳み、出典の列に他の行番号を書いた。')
A('- 列：行・系統・機能・期待する動き（文書の出典の行）・錨（file:line:名＝木で引ける宣言か式の行）・持ち主のファイル（錨の係数を読むファイルと文書が名指すファイル・Coefficients.cs と計器は除く）・判定の型・最後に確かめたコミット・判定・根拠・残りタスク。')
A('- 判定の型＝届かない門／読まれない欄／天井の錠／片側だけの門／両側の門（両側が生で出るか）／効き。判定＝未確かめ／働く／一部だけ働く／働かない／文書が古い／この波の線が埋める。')
A('- **9-26 の棚卸し（docs/202609/20260926/20260926_機能の棚卸し.md）の判定は写さない**（規則違反の物差し）。文書の見出しの記号（●○◐◇＝9-26 の判定）は根拠の列に〈写さない〉と名乗って置くだけ＝判定ではない。')
A('- 錨の取り方：文書の係数名（変数の括弧）を木の SimCore で引き、宣言の行を取る（Coefficients.cs が先）。係数名の無い見出しは文書のバッククォートの識別子を引く。どちらも引けない見出しは錨が〈—〉（その系統を確かめる線が取る）。文書の古い file:line は写さない（9-26 の座標で、24 本のファイルは木に無い）。')
A('- 錨の確かめ：`python -X utf8 scripts/instruments/r886/ledger_check.py <repo> <木> docs/機能の台帳.md`＝各錨の行に名が含まれるか。')
A('- 確かめ直す行の選び方（AI のプレイテストの前）：`python -X utf8 scripts/instruments/r886/changed_rows.py <repo か worktree> docs/機能の台帳.md`＝行の〈最後に確かめたコミット〉から、持ち主のファイルが変わった行（`git diff --name-only <コミット>`）と、錨の係数の宣言の行が変わった行（Coefficients.cs の差分にその名がある）だけを出す。最後に確かめたコミットが〈—〉の行は未確かめの数として別に数える。')
A('- 周辺の点検（README ★(885)）は該当の行を門ごとに割って埋め、最後に確かめたコミットを書く。残りタスクの対象外の系統は波ごとに読む線 1 本で 1 系統ずつ。')
A('- **行の参照（L の番号）は overview.patch を当てた後の docs/ゲーム概要と実装機能.md の行**。行の id（Sxx-Hnnn＝系統の中の見出しの連番・S16 は門の連番 S16-nn）は文書の行番号に結ばない。')
A('- 生の名：R1＝D:/PoB_work/keep_885/base_ins（合わせた木 aef07f2a の腕 Z・計器つき 10 種・盤面 m873 と全行一致）。')
A('')
A('## 1. 系統の表')
A('')
A('〈持ち主のファイル〉は、文書が名指すファイルを最も多く名指す系統へ振った物（8 系統以上に出る拠点のファイル 16 本＝Simulation.cs・World.cs・EconomyStep.cs・MultiNationSim.cs・SelfPlay.cs・Model.cs・Politics.cs・Diplomacy.cs・Fiscal.cs・BilateralMarket.cs・Coefficients.cs・Government.cs・world.json・Catalog.cs・Territory.cs・PolicyPreferences.cs は除いた・同数は全ての系統へ）。コミットの数＝`git log --since=2026-09-26 --format=%h ' + head[:8] + ' -- <持ち主のファイル> | wc -l`。感度の列は拠点のファイルも最も多く名指す系統へ入れた数。')
A('')
A('| 系統 | 名 | 文書の行 | 見出し | 持ち主のファイル（主の規則） | コミット（主） | （感度） | 残りタスクの対象（00一覧） | 状態 |')
A('|---|---|---|---|---|---|---|---|---|')
nhead = collections.Counter(f['system'] for f in feats)
for sid, name, rng, sec in systems:
    m = meta[sid]
    A(f'| {sid} | {name} | {rng} | {nhead[sid]} | {esc(' '.join(dict.fromkeys((own_main.get(sid, '') or '—').split())))} | {cnt_main.get(sid, 0)} | {cnt_sens.get(sid, 0)} | {esc(m["残りタスク"] or "なし")} | {esc(m["状態"])} |')
A('')
A('### 1-1. (886) が確かめる 1 系統の選び方')
A('')
for l in open(os.path.join(wd, 'selection.md'), encoding='utf-8').read().splitlines(): A(l)
A('')
A('## 2. S16 蜂起と内戦と運動（(886) で確かめた・木 ' + head[:8] + '）')
A('')
A('| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |')
A('|---|---|---|---|---|---|---|---|---|---|---|')
for r in s16:
    A(f'| {r["id"]} | S16 | {esc(r["機能"])} | {esc(r["期待する動き（出典）"])} | {esc(r["錨"])} | {esc(r["持ち主"])} | {esc(r["判定の型"])} | {head[:8]} | {esc(r["判定"])} | {esc(r["根拠"])} | {esc(r["残りタスク"])} |')
A('')
for l in open(notesp, encoding='utf-8').read().splitlines(): A(l)
A('')
A('## 3. 他の系統の仮の行（見出し 1 件＝1 行・判定は未確かめ）')
A('')
cur = None
for r in rows:
    if r['sys'] != cur:
        cur = r['sys']; nm = [n for s, n, _, _ in systems if s == cur][0]
        A(''); A(f'### {cur} {nm}'); A('')
        A('| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |')
        A('|---|---|---|---|---|---|---|---|---|---|---|')
    A(f'| {r["id"]} | {r["sys"]} | {esc(r["title"])} | {esc(r["expect"])} | {esc(r["anchor"])} | {esc(r["owner"])} | {r["kind"]} | {r["last"]} | {esc(r["judge"])} | {esc(r["basis"])} | {esc(r["task"])} |')
open(outp, 'wb').write(('\n'.join(L) + '\n').encode('utf-8'))
print('s16_rows', len(s16), 'skeleton_rows', len(rows), 'folded_dups', dup, 'skeleton_with_anchor', anch_rows)
