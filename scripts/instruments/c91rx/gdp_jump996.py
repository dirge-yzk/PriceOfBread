# (996) c91rx：名目GDPの 1 四半期 10 倍以上の跳びと落ちを〈州の出入り〉の区分で割る記録の台本（読むだけ・旧の台本は変えない）。
#   名乗り：この区分は (992) が m988 の盤面を見た後に作った物差し。記録の列＝採否に使わない（差し替えは次の合流から）。
#   定義は measurements/m996_20261109_prereg_rx.txt [2]。旧の節（gdp_jump906.py）は呼ばない＝stoplines996.sh が stoplines906.sh の後ろにこの出力を足す。
#   使い方: python -I -X utf8 gdp_jump996.py [--rows] <名前>=<ins フォルダ> ...   （出力の行：1 腕あたり 2 行＋--rows の行）
import csv, glob, math, os, sys
K = 4                    # 建国の直後の幅（gdp_jump906 と同じ）
WIN = 8                  # 0 の続きの窓（同）
ZMIN = 4                 # 0 の続きの下限（同）
JUMP = 100.0             # 旧の止める線の倍率
NEAR = 10.0              # 新しい節の対象（≧10 か ≦1/10）
POP_THIN = 10.0          # 分母の痩せ（旧と同じ）
LO = 0.1                 # 共通の比の区間（両端を含む）
HI = 10.0
COLS = ('factoryOutputRefValuePerMonth', 'extractionOutputRefValuePerMonth')
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
def pratio(cur, prv):
    if not (math.isfinite(cur) and math.isfinite(prv)): return float('nan')
    if prv > 0: return cur / prv
    return math.inf if cur > 0 else float('nan')
def thin(r):
    return math.isfinite(r) and (r >= POP_THIN or r <= 1.0 / POP_THIN) or r == math.inf
def common_ratio(a, b):
    com = sorted(set(a) & set(b))
    ca = sum(a[k] for k in com); cb = sum(b[k] for k in com)
    return (cb / ca if (ca > 0 and math.isfinite(ca) and math.isfinite(cb)) else float('nan')), len(com)
def tot_ratio(a, b):
    sa = sum(a[k] for k in sorted(a)); sb = sum(b[k] for k in sorted(b))
    return sb / sa if (sa > 0 and math.isfinite(sa) and math.isfinite(sb)) else float('nan')
args = [a for a in sys.argv[1:] if not a.startswith('--')]
rows_out = '--rows' in sys.argv[1:]
print(f'# gdp_jump996 K={K} WIN={WIN} ZMIN={ZMIN} 対象=名目の比 ≧{NEAR:g} か ≦1/{NEAR:g} 区間=[{LO:g},{HI:g}] 産出の列={"+".join(COLS)} 区分の優先=建国の直後＞0の続き＞分母の痩せ＞州の出入り＞それ以外')
for arg in args:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    j1e10 = nonf = found100 = 0; ev = []
    for s in seeds:
        sev = []
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            iu, inm, iy, iq, ig, ii = ix['nationUid'], ix['nation'], ix['year'], ix['quarter'], ix['nominalGdpPerYear'], ix['nationIndex']
            ip, ipl = ix['populationK'], ix['priceLevel']
            prev = {}; first = {}; hist = {}
            for x in r:
                g = num(x[ig]); u = x[iu]; q = int(x[iq])
                if u not in first: first[u] = q
                if not math.isfinite(g): nonf += 1; continue
                hw = hist.setdefault(u, [])
                nz = sum(1 for v in hw[-WIN:] if v <= 0)
                hw.append(g)
                pop = num(x[ip]); pl = num(x[ipl]); cidx = int(x[ii])
                if u in prev and prev[u][0] > 0:
                    pg, ppop, pq, pidx, ppl = prev[u]
                    ra = g / pg
                    if ra >= 1e10: j1e10 += 1
                    near = g > 0 and (ra >= NEAR or ra <= 1.0 / NEAR)
                    if near:
                        if q - first[u] <= K:
                            if ra >= JUMP: found100 += 1
                        else:
                            sev.append(dict(s=s, nm=x[inm], y=int(x[iy]), q=q, age=q - first[u], ra=ra, rp=pratio(pop, ppop), nz=nz,
                                            pq=pq, pidx=pidx, cidx=cidx, pr=(pl / ppl if (math.isfinite(pl) and math.isfinite(ppl) and ppl > 0) else float('nan'))))
                prev[u] = (g, pop, q, cidx, pl)
        if not sev: continue
        need = {(e['pq'], e['pidx']) for e in sev} | {(e['q'], e['cidx']) for e in sev}
        reg = {}
        with open(os.path.join(d, f'region_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            ic = [ix[c] for c in COLS]; irg, ini = ix['region'], ix['nationIndex']
            for x in r:
                k = (int(x[0]), int(x[ini]))
                if k in need: reg.setdefault(k, {})[x[irg]] = sum(num(x[i]) for i in ic)
        for e in sev:
            a = reg.get((e['pq'], e['pidx']), {})
            b = reg.get((e['q'], e['cidx']), {})
            cr, ncom = common_ratio(a, b); tot = tot_ratio(a, b)
            moved = (set(a) != set(b)) and math.isfinite(cr) and LO <= cr <= HI
            if e['ra'] >= JUMP and e['nz'] >= ZMIN: kind = '建国の後の0の続き'
            elif thin(e['rp']): kind = '分母の痩せ'
            elif moved: kind = '州の出入り'
            else: kind = 'それ以外'
            e.update(kind=kind, cr=cr, tot=tot, ncom=ncom, na=len(a), nb=len(b), big=(e['ra'] >= JUMP or e['ra'] <= 1.0 / JUMP), up=e['ra'] > 1)
            ev.append(e)
    def cnt(sel, kinds):
        return '・'.join(f'{k} {sum(1 for e in ev if sel(e) and e["kind"] == k)}' for k in kinds)
    up100 = lambda e: e['big'] and e['up']
    dn100 = lambda e: e['big'] and not e['up']
    mid = lambda e: not e['big']
    K3 = ('分母の痩せ', '州の出入り', 'それ以外')
    print(f'{name}\t種 {len(seeds)}\t記録（996）上がり≧100 {found100 + sum(1 for e in ev if up100(e))}（建国の直後 {found100}・建国の後の0の続き {sum(1 for e in ev if up100(e) and e["kind"] == "建国の後の0の続き")}・{cnt(up100, K3)}）\t1e10 倍以上 {j1e10}\t非有限 {nonf}')
    print(f'{name}\t記録（996）下がり≦1/100 {sum(1 for e in ev if dn100(e))}（{cnt(dn100, K3)}）\t10〜100 倍の跳びと落ち {sum(1 for e in ev if mid(e))}（{cnt(mid, K3)}）\t〈それ以外〉≧100（上がり・下がり） {sum(1 for e in ev if e["big"] and e["kind"] == "それ以外")}')
    if rows_out:
        for e in ev:
            print('   行\t{s}\t{nm}\tq{q}\t{kind}\t{dir}{bg}\t名目の比 {ra:.4g}\t人口の比 {rp:.4g}\t共通の比 {cr:.4g}\t州の和の比 {tot:.4g}\t物価の比 {pr:.4g}\t名目÷共通 {nc:.4g}\t共通の州 {ncom}/{na}→{nb}'.format(
                dir='上がり' if e['up'] else '下がり', bg='≧100' if e['big'] else '10〜100', nc=(e['ra'] / e['cr'] if e['cr'] == e['cr'] and e['cr'] != 0 else float('nan')), **e))
