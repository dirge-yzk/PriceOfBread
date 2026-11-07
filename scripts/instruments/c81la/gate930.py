# (930) c81la：(909) gate909.py の写し。変えたのは種の範囲だけ＝環境変数 SEED_START（既定 1）で腕と基線の種を SEED_START〜+9 に・ヌル腕 N1・N2 は常に種 1〜10（S11 の幅 W は種 1〜10 の幅で代える近似＝事前登録 [4]）。門の式・閾値・表 ROSTER は 909 のまま。
# (909) c78ls 土地の持ち分の段 B-2a の門の集計（紙の台本と腕の門の両方）。読むだけ。
# 使い方:
#   python gate909.py paper <基線の生> <N1 の生> <N2 の生>            … 紙の数（P・e・U・ヌル腕の幅・旗 0 の盤面での門の値）
#   python gate909.py gate  <腕の生> <基線の生> <N1 の生> <N2 の生>    … 腕の門 G-B1・G-B2・G-B3
# 生のフォルダには landshare_s<種>.tsv と nation_s<種>.tsv が要る。名簿と値は world.json から読まず、この台本の手書きの表 ROSTER（事前登録 [1] の写し）。
# G-B1 の 3 つ目は world.json を自分で読む（読み手が world.json から数える＝表 ROSTER との一致も別に確かめる）。
import sys, os, json, statistics

ROSTER = {   # 事前登録 [1]：枠の順＝貴族・教会・農民の自作・市民の地主（国家の枠を貴族へ畳んだ値・3 桁）
    'フランス':   [0.265, 0.087, 0.378, 0.270],
    'イギリス':   [0.745, 0.045, 0.175, 0.035],
    'スペイン':   [0.488, 0.152, 0.228, 0.132],
    'プロイセン': [0.904, 0.020, 0.076, 0.0],
    'デンマーク': [0.905, 0.020, 0.075, 0.0],
    '日本':       [0.987, 0.013, 0.0, 0.0],
    'ポーランド': [0.872, 0.128, 0.0, 0.0],
}
CLS = ['Noble', 'Clergy', 'Peasant', 'Capitalist', 'Other']
SB = int(os.environ.get('SEED_START', '1'))
SEEDS = range(SB, SB + 10)      # 腕と基線
NULL_SEEDS = range(1, 11)       # ヌル腕（種 11〜20 のヌル腕は無い）

def read_tsv(p):
    with open(p, 'rb') as f:
        lines = f.read().decode('utf-8').splitlines()
    h = lines[0].split('\t')
    return [dict(zip(h, l.split('\t'))) for l in lines[1:] if l]

def q0(folder, seed):
    """名簿の国ごとの q0 の landshare_s の行と nation_s の名目 GDP（年）。"""
    ls = {r['nation']: r for r in read_tsv(os.path.join(folder, f'landshare_s{seed}.tsv')) if r['quarter'] == '0'}
    gdp = {}
    for r in read_tsv(os.path.join(folder, f'nation_s{seed}.tsv')):
        if r['quarter'] == '0':
            gdp[r['nation']] = float(r['nominalGdpPerYear'])
    return ls, gdp

def f(r, k): return float(r[k])

def norm(v):
    s = sum(v); return [x / s for x in v]

def moved(r, gdp, wn):
    """表の重みが正の枠（教会・農民・市民のうち）の階級の LandRent（四半期）の和を年へ直して名目 GDP（年）で割った量（日・ポは教会だけ＝重み 0 の枠の雑音を混ぜない）。"""
    return 4.0 * sum(f(r, f'landRentActual{c}PerQuarter') for k, c in ((1, 'Clergy'), (2, 'Peasant'), (3, 'Capitalist')) if wn[k] > 0) / gdp

def per_arm(folder, seeds=None):
    out = {n: {'Q': [], 'P': [], 'e': [], 'ne': [], 'row': []} for n in ROSTER}
    for s in (seeds if seeds is not None else SEEDS):
        ls, gdp = q0(folder, s)
        for n, w in ROSTER.items():
            r = ls[n]; g = gdp[n]
            wn = norm(w)
            out[n]['Q'].append(moved(r, g, wn))
            out[n]['P'].append(4.0 * f(r, 'rentNonEndowByOwnerClassNoblePerQuarter') * (wn[1] + wn[2] + wn[3]) / g)
            ne, en = f(r, 'rentNonEndowPerQuarter'), f(r, 'rentEndowPerQuarter')
            out[n]['e'].append(en / (ne + en) if ne + en > 0 else 0.0)
            out[n]['row'].append(r)
    return out

def med(x): return statistics.median(x)

def worldjson_shares(path):
    with open(path, 'rb') as fh:
        d = json.loads(fh.read().decode('utf-8'))
    o = {}
    for n in d['nations']:
        if 'landOwnership' in n: o[n['name']] = norm(n['landOwnership'])
    return o

def gb1(r, wj):
    """wj＝手書きの表 ROSTER の正規化（gate のときは world.json との一致も門に入れる）。G-B1：1 つ目 maxRelDiffDataVsField・2 つ目 maxRelDiffSlotVsActual・3 つ目＝world.json の割合と実際の割合の相対差の最大（教会・農民・市民）。
    実際の割合＝(landRentActual[c] − 持ち主が c の非賦存の地代〔c＝市民は送金も〕)÷持ち主が貴族の非賦存の地代。"""
    a = f(r, 'maxRelDiffDataVsField'); b = f(r, 'maxRelDiffSlotVsActual')
    den = f(r, 'rentNonEndowByOwnerClassNoblePerQuarter')
    worst = 0.0
    for k, c in ((1, 'Clergy'), (2, 'Peasant'), (3, 'Capitalist')):
        act = f(r, f'landRentActual{c}PerQuarter') - f(r, f'rentNonEndowByOwnerClass{c}PerQuarter') - (f(r, 'colonialRemitPerQuarter') if c == 'Capitalist' else 0.0)
        share = act / den if den > 0 else float('nan')
        want = wj[k]
        if want == 0.0: d = abs(share)
        else: d = abs(share - want) / want
        if not (d <= worst): worst = d if d == d else float('inf')
    return a, b, worst

def gb3(folder, seed):
    """G-B3：全ての四半期の rgoFieldColonialCount・rgoFieldEndowCount の最大と、q0 の名簿の外の rgoFieldCount の和（後の四半期の名簿の外は記録）。"""
    mc = me = 0; out0 = 0; outLater = 0; outLaterNations = set()
    for r in read_tsv(os.path.join(folder, f'landshare_s{seed}.tsv')):
        mc = max(mc, int(r['rgoFieldColonialCount'])); me = max(me, int(r['rgoFieldEndowCount']))
        if r['nation'] not in ROSTER and int(r['rgoFieldCount']) > 0:
            if r['quarter'] == '0': out0 += int(r['rgoFieldCount'])
            else: outLater += 1; outLaterNations.add(r['nation'])
    return mc, me, out0, outLater, sorted(outLaterNations)

def main():
    mode = sys.argv[1]
    if mode == 'paper':
        base, n1, n2 = sys.argv[2:5]; arm = base
    else:
        arm, base, n1, n2 = sys.argv[2:6]
    A = {k: per_arm(k) for k in dict.fromkeys([arm, base])}
    for k in (n1, n2):
        if k not in A: A[k] = per_arm(k, NULL_SEEDS)
    wjp = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..', 'PriceOfBread.SimCore', 'world.json')
    wj = worldjson_shares(wjp)
    print(f'# gate930 {mode}  arm={arm}  base={base}  n1={n1}  n2={n2}  種 {SB}〜{SB+9}（ヌル腕は種 1〜10）')
    print(f'# world.json の landOwnership の国: {sorted(wj)}')
    tbl_ok = all(n in wj and max(abs(a - b) for a, b in zip(wj[n], norm(ROSTER[n]))) < 1e-12 for n in ROSTER) and set(wj) == set(ROSTER)
    print(f'# world.json と手書きの表 ROSTER の一致（正規化の後 1e-12・国の集合も）: {tbl_ok}')
    allpass = True
    recs = []
    print('nation\tP_med\te_med\tU\tQarm\tQbase\tQn1\tQn2\tW\tDelta\tDelta/P\tGB2\tGB1_a_max\tGB1_b_max\tGB1_c_max\tGB1')
    for n in ROSTER:
        P = med(A[base][n]['P']); e = med(A[base][n]['e']); U = (1 + 1 / (1 - e)) / 2
        Qa, Qb, Q1, Q2 = (med(A[x][n]['Q']) for x in (arm, base, n1, n2))
        W = max(Qb, Q1, Q2) - min(Qb, Q1, Q2)
        D = Qa - Qb
        ratio = D / P if P > 0 else float('nan')
        Pa = med(A[arm][n]['P'])   # 腕の盤面の P（腕自身の持ち主が貴族の非賦存の地代×(K+P+B)÷腕の GDP）＝軌道の動きを抜いた比の分母
        same = D / Pa if Pa > 0 else float('nan')
        # 事前登録 [4] G-B2：下限と幅は基線の P（指示の形）・上限 U は同じ盤面の比（U が拾う誤り〈賦存にも当てる〉は同じ盤面で 1÷(1−e)＝軌道の動きで落ちないように）
        g2 = (0.8 <= ratio) and (same <= U) and (D > W)
        recs.append(f'{n}	P_arm={Pa:.6g}	Delta/P_arm={same:.4f}	U={U:.4f}	P_arm/P_base={Pa / P if P > 0 else float("nan"):.4f}	記録：Delta/P_base≦U={"真" if ratio <= U else "偽"}')
        ga = gbm = gc = 0.0
        for r in A[arm][n]['row']:
            a, b, c = gb1(r, norm(ROSTER[n]))
            ga, gbm, gc = max(ga, a), max(gbm, b), max(gc, c)
        g1 = ga <= 1e-12 and gbm <= 1e-12 and gc <= 1e-6
        allpass &= g1 and g2
        print(f'{n}\t{P:.6g}\t{e:.4f}\t{U:.4f}\t{Qa:.6g}\t{Qb:.6g}\t{Q1:.6g}\t{Q2:.6g}\t{W:.3g}\t{D:.6g}\t{ratio:.4f}\t{"通る" if g2 else "落ちる"}\t{ga:.3g}\t{gbm:.3g}\t{gc:.3g}\t{"通る" if g1 else "落ちる"}')
    print('# 記録（門ではない）：同じ盤面の比')
    for x in recs: print('REC	' + x)
    g3all = True
    for s in SEEDS:
        mc, me, o0, ol, oln = gb3(arm, s)
        ok = mc == 0 and me == 0 and o0 == 0
        g3all &= ok
        print(f'GB3 seed {s}: colonialMax={mc} endowMax={me} outsideRosterAtQ0={o0} {"通る" if ok else "落ちる"}  （記録：q1 以後の名簿の外で欄を持つ行={ol} 国={",".join(oln)}）')
    if mode == 'gate': allpass &= tbl_ok
    print(f'判定: G-B1 と G-B2 {"全て通る" if allpass else "落ちる"}・G-B3 {"通る" if g3all else "落ちる"}')

if __name__ == '__main__':
    main()
