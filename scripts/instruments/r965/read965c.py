# (965) 読むだけ：read965b.py の <名札>_reg.tsv（種×州）と <名札>_dead.tsv を、0 の続きのある州・輸出の無い州・0 でない植民地格の州で並べる。
#   使い方: python -X utf8 -I read965c.py <出力フォルダ> <名札...>
import sys, os, statistics as st, collections as C
OUT = sys.argv[1]
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
for tag in sys.argv[2:]:
    rows = [l.rstrip('\n').split('\t') for l in open(os.path.join(OUT, f'{tag}_reg.tsv'), encoding='utf-8')]
    h = rows[0]; ix = {n: i for i, n in enumerate(h)}; rows = rows[1:]
    g = lambda r, n: fl(r[ix[n]])
    dead = [r for r in rows if g(r, 'nDeadQ') > 0]; alive = [r for r in rows if g(r, 'nDeadQ') == 0]
    noexp = [r for r in rows if g(r, 'nExpQ') / g(r, 'nq') < 0.2]
    print(f'=== {tag}  種×州 {len(rows)}  0 の続きのある {len(dead)}  無い {len(alive)}  輸出の四半期が 2 割未満 {len(noexp)}（うち 0 の続きのある {sum(1 for r in noexp if g(r, "nDeadQ") > 0)}）')
    print('  0 の続きのある種×州の輸出の四半期の割合の中央', round(med([g(r, 'nExpQ') / g(r, 'nq') for r in dead]), 4), '無い州', round(med([g(r, 'nExpQ') / g(r, 'nq') for r in alive]), 4))
    for nm in ['nPortQ', 'ratioTop1', 'ratioStaple', 'rpl', 'savPc', 'mktPc', 'expPc', 'soldShareExpQ', 'soldShareNoExpQ', 'subRate']:
        f = (lambda r: g(r, nm) / g(r, 'nq')) if nm == 'nPortQ' else (lambda r: g(r, nm))
        print(f'  {nm:16s} 0 の続き {med([f(r) for r in dead]):.4g}\t無い {med([f(r) for r in alive]):.4g}\t輸出 2 割未満で 0 の続きなし {med([f(r) for r in noexp if g(r, "nDeadQ") == 0]):.4g}')
    print('  top1 の種類（0 の続きのある）', C.Counter(r[ix['top1Kind']] for r in dead).most_common(), '（無い）', C.Counter(r[ix['top1Kind']] for r in alive).most_common())
    print('  持ち主（0 の続きのある・州×四半期で重み）', C.Counter(r[ix['nation']] for r in dead for _ in range(int(g(r, 'nDeadQ')))).most_common(6), '港に 1 度でもなった種×州', sum(1 for r in dead if g(r, 'nPortQ') > 0), '/', len(dead), '（無い州）', sum(1 for r in alive if g(r, 'nPortQ') > 0), '/', len(alive))
    ps = C.Counter(); pr = C.defaultdict(list)
    for r in dead: ps[r[0]] += int(g(r, 'nDeadQ')); pr[r[0]].append((int(g(r, 'nDeadQ')), r[1], r[ix['nation']], round(g(r, 'nExpQ') / g(r, 'nq'), 3), int(g(r, 'nPortQ') > 0)))
    print('  種ごとの州×四半期', dict(sorted(ps.items(), key=lambda x: int(x[0]))), '種×州の数', {k: len(v) for k, v in sorted(pr.items(), key=lambda x: int(x[0]))})
    top = max(ps, key=ps.get) if ps else None
    if top: print(f'  最大の種 {top} の州（上位 8：州×四半期・州・持ち主・輸出の割合・港）', sorted(pr[top], reverse=True)[:8])
    dr = [l.rstrip('\n').split('\t') for l in open(os.path.join(OUT, f'{tag}_dead.tsv'), encoding='utf-8')][1:]
    lastq = sorted(int(r[2]) - int(r[5]) for r in dr if int(r[5]) > 0)
    print('  区間の入り口から見た最後の輸出の四半期（絶対の q）の中央', lastq[len(lastq) // 2] if lastq else None, 'q≦4（開幕の直後で止まった）', sum(1 for q in lastq if q <= 4), '/', len(lastq))
