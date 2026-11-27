# (1424) 問い2：終わった戦争の後、同じ組（順は問わない）が 12 か月以内・60 か月以内に再び戦う件の数（読むだけ）。
# 道 A（wexit_s がある腕）：wexit の組の最後の行の終わり方（lk_gate.endpath）ごと＝崩壊・決定的・疲れ果て・調停・他。
# 道 B（全ての腕・wars_s）：終わりの四半期 q と月数から月を近似（終わり＝q×3〔±2 か月〕・始め＝終わり−月数）・判定の種類（白紙 0・A 勝ち 1・B 勝ち 2）ごと。
import sys, os, collections, csv
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lk_gate import load, groups, endpath
d = sys.argv[1]; s0, s1 = map(int, sys.argv[2].split('-'))
A = collections.defaultdict(collections.Counter); B = collections.defaultdict(collections.Counter)
def count(tab, key, gap):
    tab[key]['ended'] += 1
    if gap is not None and gap <= 12: tab[key]['re12'] += 1
    if gap is not None and gap <= 60: tab[key]['re60'] += 1
for s in range(s0, s1 + 1):
    if os.path.exists(f'{d}/wexit_s{s}.tsv'):
        ix, rows = load(f'{d}/wexit_s{s}.tsv'); ws = []
        for g, rs in groups(ix, rows).items():
            ws.append((frozenset((g[0], g[1])), int(rs[0][ix['month']]), int(rs[-1][ix['month']]), endpath(ix, rs[-1])))
        for k, (p, st, en, ep) in enumerate(ws):
            if en >= 2003: continue
            nxt = [w[1] - en for w in ws if w[0] == p and w[1] > en]
            count(A, ep, min(nxt) if nxt else None); count(A, 'ALL', min(nxt) if nxt else None)
    r = csv.reader(open(f'{d}/wars_s{s}.tsv', encoding='utf-8'), delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
    ws = []
    for x in r:
        en = int(x[ix['q']]) * 3; ws.append((frozenset((x[ix['sideA']], x[ix['sideB']])), en - int(x[ix['months']]), en, x[ix['verdictCode']], x[ix['civilWarFlag']]))
    for p, st, en, v, cv in ws:
        if v == '-1': continue
        nxt = [w[1] - en for w in ws if w[0] == p and w[1] >= en - 2 and w[2] > en]
        key = 'blank' if v == '0' else 'won'
        count(B, key, min(nxt) if nxt else None); count(B, 'ALL', min(nxt) if nxt else None); count(B, 'civil' if cv == '1' else 'foreign', min(nxt) if nxt else None)
print(f'# {d} 種 {s0}-{s1}')
for nm, tab in (('A(wexit・終わり方)', A), ('B(wars_s・判定)', B)):
    for k in sorted(tab): t = tab[k]; print(f'{nm} {k}: ended={t["ended"]} re12={t["re12"]} re60={t["re60"]} re12share={t["re12"]/max(1,t["ended"]):.3f} re60share={t["re60"]/max(1,t["ended"]):.3f}')
