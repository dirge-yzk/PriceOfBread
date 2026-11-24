# (1330) c139sl：止める線〈名目 GDP の 1 四半期 100 倍以上の上がり（前の四半期が正）〉の跳びを、記録の区分で割る台本（読むだけ・旧の台本は変えない）。
#   名乗り：(ii)(iii) は (1326) が m1321・m1328 の盤面を見た後に作った区分＝結果の後に組んだ区分。(i) は r833 の 分母の痩せ（㊳）と同じ定義で、既に止める線の読み手に入っている。
#   記録の列＝採否に使わない・止める線の定義は変えない（新しい止める線を足さない）。新しい区分は次の合流から記録の列として並べる。
#   区分の順（先に当たった物）：(0) 建国の直後（系列の最初の行から K 四半期以内）＞(i) 政府の人口の比が ≧10 か ≦1/10（populationK）＞
#     (ii) 直前 WIN 行（跳びの行を含まない）の中に、人口 ≧ZPOP 千人で名目 GDP ≦0 が ZMIN 行以上連続する区間がある（gdp_zero10 の連続の定義を窓に当てる）＞
#     (iii) 市場の産出が 0 の近く＝州の産出（工場＋採取の参照値）の国の和の比 tot（跳びの四半期÷前の四半期）が ≧NEAR で、名目の比÷tot が [LO,HI]（前の四半期の産出が 0 なら偽＝stoplines1002 の ok3 と同じ・(1337) c140nt で直した）＞どれにも当たらない。
#   窓 WIN・連続 ZMIN・NEAR・LO・HI は 906・1002 の既存の定数（新しい閾値は足さない）。ZPOP は gdp_zero10 の既存の人口の線。
#   使い方: python -I -X utf8 gdp_jump1330.py [--rows] <名前>=<ins フォルダ> ...
import csv, glob, math, os, sys
K = 4; WIN = 8; ZMIN = 4; ZPOP = 10.0; JUMP = 100.0; NEAR = 10.0; LO = 0.1; HI = 10.0; POP_THIN = 10.0
COLS = ('factoryOutputRefValuePerMonth', 'extractionOutputRefValuePerMonth')
KINDS = ('建国の直後', '(i)人口の跳び', '(ii)0の区間の後', '(iii)産出が0の近く', 'どれにも当たらない')
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
def pratio(cur, prv):
    if not (math.isfinite(cur) and math.isfinite(prv)): return float('nan')
    if prv > 0: return cur / prv
    return math.inf if cur > 0 else float('nan')
def thin(r): return (math.isfinite(r) and (r >= POP_THIN or r <= 1.0 / POP_THIN)) or r == math.inf
def zero_run(win):
    run = 0
    for g, p in win:
        run = run + 1 if (g <= 0 and p >= ZPOP) else 0
        if run >= ZMIN: return True
    return False
def out_cause(a, b, ra):
    sa = sum(a[k] for k in sorted(a)); sb = sum(b[k] for k in sorted(b))
    if not (math.isfinite(sa) and math.isfinite(sb)): return False, float('nan')
    if sa <= 0: return False, (math.inf if sb > 0 else float('nan'))   # (1337) 前の産出が 0 なら偽（stoplines1002 の ok3 と同じ）＝本物の跳びが記録の側へ逃げる口を閉じる。tot は表示のためだけ
    tot = sb / sa
    return (tot >= NEAR and LO <= ra / tot <= HI), tot
args = [a for a in sys.argv[1:] if not a.startswith('--')]
rows_out = '--rows' in sys.argv[1:]
print(f'# gdp_jump1330 K={K} WIN={WIN} ZMIN={ZMIN} ZPOP={ZPOP:g} 対象=名目の比 ≧{JUMP:g}（前の四半期が正） 産出の列={"+".join(COLS)} 区分の順={">".join(KINDS)}')
for arg in args:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    ev = []; j1e10 = 0
    for s in seeds:
        sev = []
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            iu, inm, iy, iq, ig, ii, ip = ix['nationUid'], ix['nation'], ix['year'], ix['quarter'], ix['nominalGdpPerYear'], ix['nationIndex'], ix['populationK']
            prev = {}; first = {}; hist = {}
            for x in r:
                g = num(x[ig]); u = x[iu]; q = int(x[iq]); pop = num(x[ip]); cidx = int(x[ii])
                if u not in first: first[u] = q
                if not math.isfinite(g): continue
                hw = hist.setdefault(u, [])
                if u in prev and prev[u][0] > 0:
                    pg, ppop, pq, pidx = prev[u]; ra = g / pg
                    if ra >= 1e10: j1e10 += 1
                    if ra >= JUMP:
                        sev.append(dict(s=s, nm=x[inm], y=int(x[iy]), q=q, age=q - first[u], ra=ra, rp=pratio(pop, ppop), win=list(hw[-WIN:]), pq=pq, pidx=pidx, cidx=cidx, tot=float('nan')))
                hw.append((g, pop)); prev[u] = (g, pop, q, cidx)
        need = {(e['pq'], e['pidx']) for e in sev if e['age'] > K and not thin(e['rp']) and not zero_run(e['win'])} | {(e['q'], e['cidx']) for e in sev if e['age'] > K and not thin(e['rp']) and not zero_run(e['win'])}
        reg = {}
        if need:
            with open(os.path.join(d, f'region_s{s}.tsv'), encoding='utf-8', newline='') as f:
                r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
                ic = [ix[c] for c in COLS]; irg, ini = ix['region'], ix['nationIndex']
                for x in r:
                    k = (int(x[0]), int(x[ini]))
                    if k in need: reg.setdefault(k, {})[x[irg]] = sum(num(x[i]) for i in ic)
        for e in sev:
            if e['age'] <= K: kind = KINDS[0]
            elif thin(e['rp']): kind = KINDS[1]
            elif zero_run(e['win']): kind = KINDS[2]
            else:
                ok, tot = out_cause(reg.get((e['pq'], e['pidx']), {}), reg.get((e['q'], e['cidx']), {}), e['ra']); e['tot'] = tot
                kind = KINDS[3] if ok else KINDS[4]
            e['kind'] = kind; ev.append(e)
    cnt = '・'.join(f'{k} {sum(1 for e in ev if e["kind"] == k)}' for k in KINDS)
    print(f'{name}\t種 {len(seeds)}\t記録（1330）100 倍以上 {len(ev)}（{cnt}）\t1e10 倍以上 {j1e10}')
    if rows_out:
        for e in ev:
            print('   行\t{s}\t{nm}\tq{q}\t{kind}\t名目の比 {ra:.4g}\t人口の比 {rp:.4g}\t産出の比 {tot:.4g}\t最初の行から {age}'.format(**e))
