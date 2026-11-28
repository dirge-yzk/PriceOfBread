# (1438) c154wi 読む線：rg と nx の後の戦争の増えの内訳（記録・コミットしない）。
# 生を読むだけ（エンジンと係数を呼ばない）。使い方: python r1438.py <生のフォルダ> <種の範囲 1-10> <札> <出力のフォルダ>
# 出力: <出力>/q1_<札>.tsv・q3_<札>.tsv・q4_<札>.tsv・q4rows_<札>.tsv
import sys, os, collections

AJT = 40.0  # Coefficients.p04_trade.cs:95 AllyJoinTension の既定（読むだけ・覇権の 0.6 倍と段の 1.5^depth は生に無い＝区分で読む）

def rows_of(p):
    with open(p, 'rb') as f:
        L = f.read().decode('utf-8').splitlines()
    h = L[0].split('\t'); ix = {n: i for i, n in enumerate(h)}
    return ix, [r for r in (l.split('\t') for l in L[1:]) if len(r) == len(h)]

def wexit_ends(p):
    """wexit_s の 1 本の戦争（同じ powerIdA・B で月が続く列）ごとに (組, 最初の月, 最後の月, 終わったか, 接触なし崩壊の型か, 最後の行の接触なし)"""
    ix, rows = rows_of(p)
    lastm = max(int(r[ix['month']]) for r in rows)
    by = collections.defaultdict(list)
    for r in rows: by[(r[ix['powerIdA']], r[ix['powerIdB']])].append(r)
    out = []
    for lst in by.values():
        lst.sort(key=lambda r: int(r[ix['month']]))
        cur = [lst[0]]
        def flush(run):
            last = run[-1]; m = int(last[ix['month']])
            col = last[ix['oldCollapseAFlag']] == '1' or last[ix['oldCollapseBFlag']] == '1'
            nc = last[ix['pushSumPositiveFlag']] == '0'
            out.append((frozenset((last[ix['sideA']], last[ix['sideB']])), int(run[0][ix['month']]), m, m < lastm, col and nc and m < lastm, nc, last[ix['civilFlag']] == '1'))
        for r in lst[1:]:
            if int(r[ix['month']]) == int(cur[-1][ix['month']]) + 1: cur.append(r)
            else: flush(cur); cur = [r]
        flush(cur)
    return out, lastm

def main():
    d, rng, tag, od = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
    s0, s1 = map(int, rng.split('-'))
    Q1 = collections.Counter(); Q1e = collections.Counter(); Q3 = collections.Counter(); Q4 = collections.Counter()
    marg = collections.Counter(); we = collections.defaultdict(float); wen = 0
    q4rows = []
    for s in range(s0, s1 + 1):
        ix, W = rows_of(f'{d}/wars_s{s}.tsv')
        wars = []
        for x in W:
            st = int(x[ix['q']]) * 3
            wars.append((frozenset((x[ix['sideA']], x[ix['sideB']])), st, st + int(x[ix['months']]), x[ix['verdictCode']], x[ix['entryCode']], x[ix['civilWarFlag']], int(x[ix['startYear']]), x[ix['sideA']], x[ix['sideB']], int(x[ix['q']])))
        ends, lastm = wexit_ends(f'{d}/wexit_s{s}.tsv')
        nxe = collections.defaultdict(list)
        for p, fm, lm, ended, nx, nc, cv in ends:
            if nx: nxe[p].append((fm, lm))
        blank = collections.defaultdict(list); anyend = collections.defaultdict(list)
        for p, st, en, v, e, cv, y, a, b, q in wars:
            if v != '-1': anyend[p].append((st, en))
            if v == '0': blank[p].append((st, en))
        # 問い 1：始まりの前 12／60 か月に同じ組の終わり（接触なし崩壊の型・白紙・全て）があったか
        for p, st, en, v, e, cv, y, a, b, q in wars:
            Q1['wars'] += 1
            for lab, src in (('nx', nxe), ('blank', blank), ('any', anyend)):
                gaps = [st - pe for (ps, pe) in src.get(p, []) if ps < st and st - pe >= -2]
                g = min(gaps) if gaps else None
                for k in (12, 60):
                    if g is not None and g <= k:
                        Q1[f'{lab}{k}'] += 1; Q1[f'{lab}{k}_{e}'] += 1
        # 終わった接触なし崩壊の型の戦争そのものの再戦の率（白紙のうち型あり／型なし・勝ち）
        nxset = {(p, lm) for p in nxe for (fm, lm) in nxe[p]}
        for p, st, en, v, e, cv, y, a, b, q in wars:
            if v == '-1': continue
            isnx = v == '0' and any((p, m) in nxset for m in range(en - 3, en + 4))
            k = 'nxblank' if isnx else ('blank' if v == '0' else 'won')
            nxt = [w[1] - en for w in wars if w[0] == p and w[1] >= en - 2 and w[1] > st]
            g = min(nxt) if nxt else None
            Q1e[k + '_ended'] += 1; Q1e[k + '_re12'] += (g is not None and g <= 12); Q1e[k + '_re60'] += (g is not None and g <= 60)
        # 問い 3：入口の札の数・同盟の参戦の緊張の余り（tensionwhy の始まりの四半期の前の行）
        for w in wars: Q3['entry_' + w[4]] += 1
        allyq = {}
        for p, st, en, v, e, cv, y, a, b, q in wars:
            if e == 'ally': allyq[(a, b, q)] = None
        need = {(a, b) for (a, b, q) in allyq} | {(b, a) for (a, b, q) in allyq}
        tix, T = rows_of(f'{d}/tensionwhy_s{s}.tsv')
        tens = {}
        for r in T:
            k = (r[tix['nationA']], r[tix['nationB']])
            if k in need: tens[(k, int(r[tix['q']]))] = float(r[tix['tension']])
        for (a, b, q) in allyq:
            t = None
            for qq in (q - 1, q - 2, q):
                t = tens.get(((a, b), qq), tens.get(((b, a), qq)))
                if t is not None: break
            if t is None: marg['noRow'] += 1; continue
            m = t - AJT
            band = 'lt-16' if m < -16 else 'lt0' if m < 0 else 'lt5' if m < 5 else 'lt20' if m < 20 else 'lt60' if m < 60 else 'ge60'
            marg[band] += 1; marg['n'] += 1; marg['sum'] += m
        pe = f'{d}/warentry_s{s}.tsv'
        eix, E = rows_of(pe) if os.path.exists(pe) else ({}, [])
        for r in E:
            wen += 1
            for c in ('calls', 'evals', 'r_ok', 'r_will', 'r_mil', 'r_atWar', 'r_weak', 'sh_pass', 'sh_will', 'sh_mil', 'sh_calls'):
                we[c] += float(r[eix[c]])
            for c in ('feverNow', 'willNow'):
                v = r[eix[c]]
                if v not in ('nan', ''): we[c] += float(v); we[c + '_n'] += 1
        # 問い 4：最後まで続いた戦争（verdictCode -1）
        ncByPair = {}
        for p, fm, lm, ended, nx, nc, cv in ends:
            if not ended: ncByPair[p] = nc
        for p, st, en, v, e, cv, y, a, b, q in wars:
            if v != '-1': continue
            nc = ncByPair.get(p)
            Q4['n'] += 1
            Q4['start' + ('1949+' if y >= 1949 else '1948' if y == 1948 else '1940_47' if y >= 1940 else 'lt1940')] += 1
            Q4['civil' if cv == '1' else 'intl'] += 1
            Q4['contact' if nc is False else 'nocontact' if nc else 'noWexit'] += 1
            Q4['entry_' + e] += 1
            q4rows.append((s, y, a, b, e, cv, en - st, 'nocontact' if nc else 'contact' if nc is False else 'noWexit'))
    os.makedirs(od, exist_ok=True)
    with open(f'{od}/q1_{tag}.tsv', 'wb') as f:
        f.write(('# ' + d + ' seeds ' + rng + '\n').encode())
        for k in sorted(Q1): f.write(f'{k}\t{Q1[k]}\n'.encode())
        for k in sorted(Q1e): f.write(f'E:{k}\t{Q1e[k]}\n'.encode())
        for k in ('nxblank', 'blank', 'won'):
            n = Q1e[k + '_ended']
            if n: f.write(f'R:{k}\tended={n}\tre12={Q1e[k+"_re12"]/n:.3f}\tre60={Q1e[k+"_re60"]/n:.3f}\n'.encode())
    with open(f'{od}/q3_{tag}.tsv', 'wb') as f:
        f.write(('# ' + d + ' seeds ' + rng + '\n').encode())
        for k in sorted(Q3): f.write(f'{k}\t{Q3[k]}\n'.encode())
        for k in sorted(marg): f.write(f'allyMargin_{k}\t{marg[k]:.1f}\n'.encode())
        if marg['n']: f.write(f'allyMargin_mean\t{marg["sum"]/marg["n"]:.2f}\n'.encode())
        f.write(f'warentry_rows\t{wen}\n'.encode())
        for k in sorted(we):
            if k.endswith('_n'): continue
            v = we[k] / we[k + '_n'] if (k + '_n') in we else we[k]
            f.write(f'we_{k}\t{v:.4f}\n'.encode())
    with open(f'{od}/q4_{tag}.tsv', 'wb') as f:
        f.write(('# ' + d + ' seeds ' + rng + '\n').encode())
        for k in sorted(Q4): f.write(f'{k}\t{Q4[k]}\n'.encode())
    with open(f'{od}/q4rows_{tag}.tsv', 'wb') as f:
        f.write('seed\tstartYear\tsideA\tsideB\tentry\tcivil\tmonths\tcontact\n'.encode())
        for r in q4rows: f.write(('\t'.join(map(str, r)) + '\n').encode())

main()
