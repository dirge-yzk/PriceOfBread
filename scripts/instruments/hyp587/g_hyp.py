#!/usr/bin/env python
"""(587) 線 hyp：切り下げの月の上限の緩め（旗 DevaluationCapReliefExp）の腕 B の読み手（事前登録 追記 4 (e)(g)(i)）。
使い方：PYTHONUTF8=1 python g_hyp.py <基線の ins（m584_ins）> <腕 B の ins> [-v]
  H4：腕 B の devcap_s<seed>.tsv（全ての国・旧の切りに当たった月だけ）の最初の行の時刻 t_c（w＜q の順・w0…→q0…）の前まで、基線と B の money_s（time の順位が t_c より小さい行）と
      nation_s（quarter が t_c の q 番号より小さい行・t_c が w なら比べる行なし）をバイトで比べる。devcap_s が無い・空の種は 3 つの表（money_s・nation_s・fiscal_s）を全行比べる。
  記録（採否に使わない）：devcap_s の行数・国・最大の room÷cap／B の money_s・nation_s（9 国）で P＞100 の (種, 国)・最大 P・最初の時刻と、直前 8 四半期の戦時・内戦・債務不履行の有無／平時の P≧10 の行。
"""
import sys, re, glob, os, csv, math, collections


def trank(t):
    m = re.fullmatch(r'([wq])(\d+)', t)
    return (0 if m.group(1) == 'w' else 1, int(m.group(2))) if m else (9, 0)


def rows(path):
    if not os.path.exists(path):
        return None
    with open(path, 'rb') as fh:
        L = fh.read().split(b'\n')
    return L[0], [l for l in L[1:] if l]


def f(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def devcap_first(path):
    r = rows(path)
    if r is None or not r[1]:
        return None, 0, {}, 0.0
    head = r[0].decode('utf-8').rstrip('\r').split('\t')
    iT, iN, iR = head.index('time'), head.index('nation'), head.index('roomOverCapRatio')
    best = None; nat = collections.Counter(); mx = 0.0
    for ln in r[1]:
        c = ln.decode('utf-8').rstrip('\r').split('\t')
        k = trank(c[iT])
        if best is None or k < best[0]:
            best = (k, c[iT])
        nat[c[iN]] += 1; mx = max(mx, f(c[iR]))
    return best, len(r[1]), nat, mx


def compare(a, b, keep):
    A, B = rows(a), rows(b)
    if A is None or B is None:
        return None
    if A[0] != B[0]:
        return (0, 1, 'ヘッダー')
    h = A[0].decode('utf-8').rstrip('\r').split('\t')
    ra = [l for l in A[1] if keep(l.decode('utf-8').rstrip('\r').split('\t'), h)]
    rb = [l for l in B[1] if keep(l.decode('utf-8').rstrip('\r').split('\t'), h)]
    n = min(len(ra), len(rb)); diff = abs(len(ra) - len(rb)); first = None
    for i in range(n):
        if ra[i] != rb[i]:
            diff += 1
            if first is None:
                first = i
    return n, diff, first


def main(base, arm, verbose):
    seeds = sorted(int(re.search(r'money_s(\d+)', p).group(1)) for p in glob.glob(f'{arm}/money_s*.tsv'))
    bad = 0; hits = 0
    for s in seeds:
        tc, n, nat, mx = devcap_first(f'{arm}/devcap_s{s}.tsv')
        if tc is None:
            keep = lambda c, h: True
            label = 'devcap_s 空（t_c＝∞）'
            tabs = ['money_s', 'nation_s', 'fiscal_s']
            res = {t: compare(f'{base}/{t}{s}.tsv', f'{arm}/{t}{s}.tsv', keep) for t in tabs}
        else:
            hits += 1
            label = f't_c＝{tc[1]}・devcap_s {n} 行・国 {dict(nat)}・最大 room÷cap {mx:.3f}'
            def km(c, h, tc=tc):
                return trank(c[h.index('time')]) < tc[0]
            def kn(c, h, tc=tc):
                return False if tc[0][0] == 0 else int(c[h.index('quarter')]) < tc[0][1]
            res = {'money_s': compare(f'{base}/money_s{s}.tsv', f'{arm}/money_s{s}.tsv', km),
                   'nation_s': compare(f'{base}/nation_s{s}.tsv', f'{arm}/nation_s{s}.tsv', kn)}
        d = sum(v[1] for v in res.values() if v)
        miss = [k for k, v in res.items() if v is None]
        if d or miss:
            bad += 1
        print(f'種{s}: {label}  ' + '  '.join(f'{k}: 比べた{v[0]}行 食い違い{v[1]}' if v else f'{k}: 無し' for k, v in res.items())
              + (f'  最初の食い違い {({k: v[2] for k, v in res.items() if v and v[1]})}' if verbose and d else ''))
    print(f'H4 外れた種（0）={bad}／{len(seeds)}・切りに当たった種（devcap_s が空でない）={hits}')
    # 記録：P>100 と文脈
    over = []; peace10 = []
    for s in seeds:
        war = {}; dflt = {}
        nsp = f'{arm}/nation_s{s}.tsv'
        if os.path.exists(nsp):
            prev = {}
            for r in csv.DictReader(open(nsp, encoding='utf-8'), delimiter='\t'):
                q = int(r['quarter']); nm = r['nation']
                war[(nm, q)] = f(r['atWarMonths']) > 0 or f(r['civilWarMonths']) > 0
                dc = f(r['sovereignDefaultCumulativeCount'])
                dflt[(nm, q)] = (dc > prev.get(nm, dc)); prev[nm] = dc
        seen = {}
        for r in csv.DictReader(open(f'{arm}/money_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
            if r['event'] != 'update':
                continue
            pa = f(r['priceLevelAfterIndex'])
            mt = re.fullmatch(r'q(\d+)', r['time'])
            ctx = False
            if mt:
                q = int(mt.group(1))
                ctx = any(war.get((r['nation'], qq), False) or dflt.get((r['nation'], qq), False) for qq in range(max(0, q - 8), q + 1))
            if pa > 100:
                k = (s, r['nation'])
                v = seen.setdefault(k, [0, 0.0, r['time'], ctx])
                v[0] += 1; v[1] = max(v[1], pa)
            if pa >= 10 and mt and not ctx and war:
                peace10.append((s, r['nation'], r['time'], pa))
        over += [(k, v) for k, v in seen.items()]
    print(f'記録 (e)：P＞100 の (種,国)＝{len(over)}（直前 8 四半期に戦時・内戦・債務不履行あり={sum(1 for _, v in over if v[3])}・平時={sum(1 for _, v in over if not v[3])}）／平時に P≧10 の行={len(peace10)}')
    for k, v in sorted(over):
        print(f'  P>100：種{k[0]} {k[1]} 行{v[0]} 最大{v[1]:.1f} 最初{v[2]} 文脈あり={v[3]}')
    for x in peace10[:10]:
        print(f'  平時 P≧10：{x}')


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], '-v' in sys.argv)
