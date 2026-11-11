# (938) c82fg 読む線：建国（プレイ中に生まれた国）ごとの組成。読むだけ。
#   使い方: python -X utf8 -I read938.py <出力 tsv> 名前=ins フォルダ ...
#   建国＝nation_s の nationUid が q0 より後に初めて現れる国。
#   州の産出＝region_s の factoryOutputRefValuePerMonth＋extractionOutputRefValuePerMonth（名目 GDP と同じ RefPrice の量）。
#   〈建国前に 0〉＝建国の州の全てが、建国の前の四半期（親の下）で産出 0 以下。
#   建国から続く 0＝系列の最初の行から数えて名目 GDP が 0 以下の行の続き（人口の条件なし・gdp_zero906 --founding は人口 10 千人以上かつ最初の 4 行に始まり 4 行以上）。
#   型（名前からの推）：内戦の相手の側（wars の sideB＝この国・civilWarFlag 1）の sideA の名前を含む＝政体戦か要求戦／含まない＝分離独立。
import csv, glob, os, sys, math
sys.stdout.reconfigure(encoding='utf-8')
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
out = open(sys.argv[1], 'w', encoding='utf-8', newline='')
w = csv.writer(out, delimiter='\t', lineterminator='\n')
w.writerow(['arm', 'seed', 'nation', 'uid', 'q0', 'year0', 'lastQ', 'rows', 'type', 'parent', 'pop0K', 'nReg', 'tiers0', 'colonialOnly0',
            'preZeroAll', 'preOut', 'out0', 'regSav0', 'zeroRunFromStart', 'zeroRunEnd', 'tierAtRunEnd', 'gdpPerKAtRunEnd+4', 'parentGdpPerK0', 'endHow'])
summary = []
for arg in sys.argv[2:]:
    arm, d = arg.split('=', 1)
    for f in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv')), key=lambda p: int(os.path.basename(p)[8:-4])):
        s = int(os.path.basename(f)[8:-4])
        rows = {}; firstq = None; namegdp = {}
        with open(f, encoding='utf-8', newline='') as fh:
            r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                q = int(x[ix['quarter']])
                if firstq is None: firstq = q
                u = x[ix['nationUid']]
                rows.setdefault(u, []).append((q, int(x[ix['year']]), x[ix['nation']], num(x[ix['nominalGdpPerYear']]), num(x[ix['populationK']])))
                namegdp[(x[ix['nation']], q)] = (num(x[ix['nominalGdpPerYear']]), num(x[ix['populationK']]))
        maxq = max(v[-1][0] for v in rows.values())
        # 州：quarter -> region -> (nation, tier, out, pop)
        reg = {}; regsav = {}
        rf = os.path.join(d, f'region_s{s}.tsv')
        with open(rf, encoding='utf-8', newline='') as fh:
            r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                q = int(x[ix['quarter']])
                o = num(x[ix['factoryOutputRefValuePerMonth']]) + num(x[ix['extractionOutputRefValuePerMonth']])
                reg.setdefault(q, {})[x[ix['region']]] = (x[ix['nation']], x[ix['tier']], o, num(x[ix['populationK']]))
        cf = os.path.join(d, f'cearn_s{s}.tsv')
        if os.path.exists(cf):
            with open(cf, encoding='utf-8', newline='') as fh:
                r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
                for x in r:
                    regsav[(int(x[ix['quarter']]), x[ix['region']])] = num(x[ix['savings']])
        wars = []
        wf = os.path.join(d, f'wars_s{s}.tsv')
        if os.path.exists(wf):
            with open(wf, encoding='utf-8', newline='') as fh:
                r = csv.reader(fh, delimiter='\t'); next(r)
                for x in r:
                    if x[1] == '1': wars.append((int(x[0]), x[2], x[3]))
        for u, rs in rows.items():
            q0, y0, nm = rs[0][0], rs[0][1], rs[0][2]
            if q0 == firstq: continue
            par = next((a for (yy, a, b) in wars if b == nm and abs(yy - y0) <= 1), '')
            typ = ('政体戦か要求戦' if par and par in nm else '分離独立') if par else '不明'
            regs0 = [(rn, v) for rn, v in reg.get(q0, {}).items() if v[0] == nm]
            tiers = sorted(set(v[1] for _, v in regs0))
            pre = [reg.get(q0 - 1, {}).get(rn) for rn, _ in regs0]
            preout = sum(p[2] for p in pre if p)
            prezero = bool(pre) and all(p and not (p[2] > 0) for p in pre)
            out0 = sum(v[2] for _, v in regs0)
            sav0 = sum(regsav.get((q0, rn), float('nan')) for rn, _ in regs0)
            n0 = 0
            for (_, _, _, g, _) in rs:
                if not (g > 0): n0 += 1
                else: break
            qend = rs[n0 - 1][0] if n0 else ''
            tend = '/'.join(sorted(set(v[1] for rn, v in reg.get(qend, {}).items() if v[0] == nm))) if n0 else ''
            j = min(len(rs) - 1, n0 + 4)
            gpk = rs[j][3] / rs[j][4] if rs[j][4] > 0 else float('nan')
            pg = namegdp.get((par, q0 - 1)) if par else None
            ppk = pg[0] / pg[1] if pg and pg[1] > 0 else float('nan')
            last = rs[-1][0]
            endhow = '最後まで続いた' if n0 == len(rs) else ('正へ' if n0 else '0なし')
            if n0 == len(rs) and last < maxq: endhow = '0のまま国が消えた'
            w.writerow([arm, s, nm, u, q0, y0, last, len(rs), typ, par, round(rs[0][4], 1), len(regs0), '/'.join(tiers),
                        int(tiers == ['Colonial']), int(prezero), f'{preout:.4g}', f'{out0:.4g}', f'{sav0:.3g}', n0, qend, tend, f'{gpk:.4g}', f'{ppk:.4g}', endhow])
    print('done', arm, flush=True)
out.close()
