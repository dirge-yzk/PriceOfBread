# r788：プローブの表で、英が敗者の講和（1784〜1815）を 事象（同じ月）に畳み、崩壊か・戦線の数・連鎖の根の数を種ごとに出す。
import sys, os, csv, collections, statistics as st
d = sys.argv[1]; s0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1; UK = 'イギリス'
def F(x):
    try: return float(x)
    except: return float('nan')
out = []
for s in range(s0, s0 + 10):
    rows = [r for r in csv.DictReader(open(os.path.join(d, f'r788_s{s}.tsv'), encoding='utf-8'), delimiter='\t') if r['hit'] == '1']
    ev = collections.defaultdict(list)
    for r in rows:
        aw = F(r['score']) > 0; lo = r['B'] if aw else r['A']
        if lo != UK or not (1784 <= int(r['year']) <= 1815): continue
        r['lcol'] = (r['bCol'] if aw else r['aCol']) == '1'; r['fr'] = int(r['frontsB'] if aw else r['frontsA'])
        r['ukAttacked'] = (r['B'] == UK)   # 英が B 側＝相手が開戦した側（根の戦争なら相手が宣戦・連鎖なら相手が参戦）
        ev[r['month']].append(r)
    peaces = sum(len(v) for v in ev.values())
    col_ev = sum(1 for v in ev.values() if any(r['lcol'] for r in v))
    fr = [max(r['fr'] for r in v) for v in ev.values()]
    roots = [len(set(r['rootIdx'] for r in v)) for v in ev.values()]
    rep = sum(F(r['rep']) for v in ev.values() for r in v)
    rep_multi = sum(F(r['rep']) for v in ev.values() if len(v) >= 2 for r in v)
    out.append((s, peaces, len(ev), col_ev, max(fr) if fr else 0, st.median(fr) if fr else 0, sum(roots), rep, rep_multi))
print('seed\t講和\t事象(月)\t崩壊の事象\t戦線の最大\t戦線の中央\t根の数の和\t賠償\t複数の月の賠償')
for o in out: print('\t'.join(str(round(x, 1)) if isinstance(x, float) else str(x) for x in o))
print('中央\t' + '\t'.join(str(st.median([o[i] for o in out])) for i in range(1, 7)) + f'\t額の割合(複数の月) {sum(o[8] for o in out)/max(1e-9,sum(o[7] for o in out)):.2f}')
