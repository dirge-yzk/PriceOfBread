# (909) c78ls 紙の突然変異の表（㉚）：基線の生（keep_905/base_ins）の q0 の行に各変異の規則を当てて〈腕の行〉を作り、gate909 の門の関数で赤か緑かを数える。読むだけ。
#   使い方: python mutpaper909.py <基線の生> <N1 の生> <N2 の生>
# 合成の規則（紙の近似）：名目 GDP・持ち主が貴族の非賦存の地代 NE・賦存の地代 E・送金は基線の値のまま（軌道の動きは無いと見なす）。
#   階級の並び＝Noble・Clergy・Peasant・Capitalist・Other。枠 k の受け手の階級＝k（0 貴族・1 教会・2 農民・3 市民）。基線は 貴族＝NE＋E・市民＝送金（keep_905 の q0 で確かめた）。
import sys, os, statistics
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gate909 as G

C5 = ['Noble', 'Clergy', 'Peasant', 'Capitalist', 'Other']

def vec(r, pre):
    return [G.f(r, f'{pre}{c}PerQuarter') for c in C5]

def reldiff(a, b):
    d = sum(abs(x - y) for x, y in zip(a, b)); t = sum(b)
    return d / t if t > 0 else (0.0 if d == 0 else float('inf'))

def synth(r, w, mut):
    """基線の行 r と表の正規化 w から、変異 mut の腕の行（門が読む列だけ）を作る。"""
    NE = G.f(r, 'rentNonEndowByOwnerClassNoblePerQuarter'); E = G.f(r, 'rentEndowPerQuarter')
    base = vec(r, 'landRentActual')
    def plus(amount, ws):
        v = list(base); v[0] -= amount
        for k in range(4): v[k] += amount * ws[k]
        return v
    correct = plus(NE, w)
    wj = w
    fieldEndow = fieldCol = 0
    if mut == 'M0 正しい形':            act = exp = dat = correct
    elif mut == 'M1 欄を読まない（構築で写さない）': act = exp = list(base); dat = correct
    elif mut == 'M2 割り方で欄を読まない（構築は写す）': act = list(base); exp = dat = correct
    elif mut == 'M3 賦存にも当てる':     act = exp = plus(NE + E, w); dat = correct; fieldEndow = 1
    elif mut == 'M4 植民地格にも当てる':  act = exp = dat = correct; fieldCol = 1
    elif mut == 'M5 割合を 2 度掛ける':   act = list(base); act[0] -= NE; act = [a + (NE * w[k] * w[k] if k < 4 else 0) for k, a in enumerate(act)]; exp = dat = correct
    elif mut == 'M6 正規化しない':        act = exp = dat = correct   # 表の和は 3 桁で 1＝この盤面では区別できない（B-0 の LandShareTests.T5 の和 10 の欄で拾う）
    elif mut == 'M7 教会の枠を貴族の受け手へ（割り方）':
        act = list(correct); act[0] += NE * w[1]; act[1] -= NE * w[1]; exp = dat = correct
    elif mut == 'M7b 教会の値を貴族へ写す（データ）':
        wj = [w[0] + w[1], 0.0, w[2], w[3]]; act = exp = dat = plus(NE, wj)
    elif mut == 'M8 名簿の外に当てる':    act = exp = dat = correct
    else: raise ValueError(mut)
    o = dict(r)
    for c, v in zip(C5, act): o[f'landRentActual{c}PerQuarter'] = repr(v)
    o['maxRelDiffSlotVsActual'] = repr(reldiff(exp, act)); o['maxRelDiffDataVsField'] = repr(reldiff(dat, exp))
    o['rgoFieldEndowCount'] = str(fieldEndow); o['rgoFieldColonialCount'] = str(fieldCol)
    return o, wj

MUTS = ['M0 正しい形', 'M1 欄を読まない（構築で写さない）', 'M2 割り方で欄を読まない（構築は写す）', 'M3 賦存にも当てる', 'M4 植民地格にも当てる',
        'M5 割合を 2 度掛ける', 'M6 正規化しない', 'M7 教会の枠を貴族の受け手へ（割り方）', 'M7b 教会の値を貴族へ写す（データ）', 'M8 名簿の外に当てる']

def main():
    base, n1, n2 = sys.argv[1:4]
    A = {k: G.per_arm(k) for k in (base, n1, n2)}
    GDP = {}
    for s in G.SEEDS:
        g = G.q0(base, s)[1]
        for n in G.ROSTER: GDP[(s, n)] = g[n]
    print(f'# mutpaper909 base={base} n1={n1} n2={n2}（q0・紙の近似＝軌道の動きなし）')
    print('# 門：G-B1a maxRelDiffDataVsField≦1e-12・G-B1b maxRelDiffSlotVsActual≦1e-12・G-B1c 表（world.json と同じ）の割合と実際の割合が相対 1e-6（重み 0 の枠は絶対 1e-6）・表と world.json の一致・G-B2 0.8≦Δ/P≦U かつ Δ>W・G-B3 植民地格と賦存の欄 0・名簿の外の q0 の欄 0')
    print('mutation\tG-B1a\tG-B1b\tG-B1c\ttable\tG-B2\tG-B3\t赤の国（G-B2）\tΔ/P の範囲')
    for m in MUTS:
        red = {'a': [], 'b': [], 'c': [], 't': [], '2': [], '3': []}
        ratios = []
        for n in G.ROSTER:
            w = G.norm(G.ROSTER[n])
            P = G.med(A[base][n]['P']); e = G.med(A[base][n]['e']); U = (1 + 1 / (1 - e)) / 2
            Qb, Q1, Q2 = (G.med(A[x][n]['Q']) for x in (base, n1, n2)); W = max(Qb, Q1, Q2) - min(Qb, Q1, Q2)
            Qa = []; wjn = w
            for s_i, r in enumerate(A[base][n]['row']):
                o, wjn = synth(r, w, m)
                a, b, c = G.gb1(o, w)
                if a > 1e-12: red['a'].append(n)
                if b > 1e-12: red['b'].append(n)
                if c > 1e-6: red['c'].append(n)
                gdp = GDP[(s_i + 1, n)]
                Qa.append(G.moved(o, gdp, w))
                if int(o['rgoFieldEndowCount']) > 0 or int(o['rgoFieldColonialCount']) > 0: red['3'].append(n)
            if max(abs(x - y) for x, y in zip(wjn, w)) > 1e-12: red['t'].append(n)
            D = G.med(Qa) - Qb; ratio = D / P if P > 0 else float('nan'); ratios.append(ratio)
            if not ((0.8 <= ratio <= U) and (D > W)): red['2'].append(n)
        if m.startswith('M8'): red['3'].append('名簿の外')
        u = lambda k: '赤' if red[k] else '緑'
        print(f'{m}\t{u("a")}\t{u("b")}\t{u("c")}\t{u("t")}\t{u("2")}\t{u("3")}\t{",".join(dict.fromkeys(red["2"])) or "-"}\t{min(ratios):.3f}〜{max(ratios):.3f}')

if __name__ == '__main__':
    main()
