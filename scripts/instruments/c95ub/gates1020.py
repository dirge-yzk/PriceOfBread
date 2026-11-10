# (1020) c95ub 段0″b 遍在の財（石灰石・珪砂・粘土を非産地の 0.4 の外へ）の必要性・紙・門の台本。
# 計器 recipeout_s（級・endowFromProductivity・outputSum・unitCostOutW・scaleSum）と nationgood_s（producedQty・demandQty・shortageQty・crossExportQty・priceMeanGold）を
# 自分で集計する（エンジンの関数を呼ばない・財は名前で読む＝GoodIdGuardTests）。
# 使い方: python -X utf8 -I gates1020.py need  <基線の生>                      <出力>
#         python -X utf8 -I gates1020.py gates <腕の生> <基線の生> <n1> <n2> <種の範囲 例 1-10> <出力>
import sys, os
U3 = ('石灰石', '珪砂', '粘土')
YEARS = ('1870', '1913')
# 下流の財のレシピの投入（Catalog.cs:87・:91・:92・Simulation.cs:1415 の木炭製鉄の写し＝係数は手で置く）
DOWN = {('鉄', '(既定)'): {'石灰石': 0.25}, ('鉄', '木炭製鉄'): {'石灰石': 0.25},
        ('セメント', '(既定)'): {'石灰石': 0.8, '粘土': 0.2}, ('ガラス', '(既定)'): {'珪砂': 0.7, '石灰石': 0.2028}}
EPS_ROWS = 0.02   # 行の数の許し（ヌル腕の行の数の差 ≦0.5% の 4 倍）

def rows(raw, kind, lo, hi):
    for s in range(lo, hi + 1):
        p = os.path.join(raw, f'{kind}_s{s}.tsv')
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                r = line.rstrip('\n').split('\t')
                yield s, ix, r

def fl(x):
    try: return float(x)
    except ValueError: return float('nan')

def summarize(raw, lo, hi):
    S = dict(n3=0, e3=0, s3=0, x3=0, n3_04=0, s3_10=0, e3_40=0, oN=0, oN_04=0, out_old=0.0, out_new=0.0, scale_n=0.0, scale_s=0.0,
             prod=0.0, dem=0.0, short=0.0, xexp=0.0, px={}, pxn={}, cost={}, cw={}, nprod={}, n3_prod=0, keys=set())
    for s, ix, r in rows(raw, 'recipeout', lo, hi):
        g, cls, y = r[ix['good']], r[ix['class']], r[ix['year']]
        e = fl(r[ix['endowFromProductivity']])
        if g in U3:
            S['keys'].add((s, y, r[ix['nation']], g, r[ix['recipe']]))
            if cls == 'N':
                S['n3'] += 1; S['n3_04'] += abs(e - 0.4) <= 1e-9; S['n3_prod'] += int(fl(r[ix['producers']]))
                if y in YEARS: S['out_old'] += fl(r[ix['outputSum']]); S['scale_n'] += fl(r[ix['scaleSum']])
            elif cls == 'S':
                S['s3'] += 1; S['s3_10'] += abs(e - 1.0) <= 1e-9
                if y in YEARS: S['out_new'] += fl(r[ix['outputSum']]); S['scale_s'] += fl(r[ix['scaleSum']])
            elif cls == 'E':
                S['e3'] += 1; S['e3_40'] += abs(e - 4.0) <= 1e-9
            else: S['x3'] += 1
        elif cls == 'N':
            S['oN'] += 1; S['oN_04'] += abs(e - 0.4) <= 1e-9
        k = (g, r[ix['recipe']])
        if k in DOWN and cls == 'F' and y in YEARS:
            o = fl(r[ix['outputSum']]); u = fl(r[ix['unitCostOutW']])
            if o > 0 and u == u:
                S['cost'][k] = S['cost'].get(k, 0.0) + u * o; S['cw'][k] = S['cw'].get(k, 0.0) + o
    for s, ix, r in rows(raw, 'nationgood', lo, hi):
        g, y = r[ix['good']], r[ix['year']]
        if g in U3 and y in YEARS:
            S['prod'] += fl(r[ix['producedQty']]); S['dem'] += fl(r[ix['demandQty']])
            S['short'] += fl(r[ix['shortageQty']]); S['xexp'] += fl(r[ix['crossExportQty']])
            p = fl(r[ix['priceMeanGold']])
            if p == p: S['px'][g] = S['px'].get(g, 0.0) + p; S['pxn'][g] = S['pxn'].get(g, 0) + 1
    return S

def outN(S): return S['out_old'] + S['out_new']   # 旧の N と新の S の和（基線は S＝0・腕は N＝0 のはず）
def short(S): return S['short'] / S['dem'] if S['dem'] > 0 else float('nan')
def ucost(S, k): return S['cost'].get(k, 0.0) / S['cw'][k] if S['cw'].get(k, 0) > 0 else float('nan')
def px(S, g): return S['px'][g] / S['pxn'][g] if S['pxn'].get(g) else float('nan')

def floors(B):
    # G3 の床：投入の値段が級 N の単位費用まで落ちきった場合の、下流の単位費用の下がりの上限（係数×(値段−RGO の単位費用)÷下流の単位費用）
    rgo = {'石灰石': 0.3531, '珪砂': 0.3536, '粘土': 0.3526}   # 基線 keep_1017/base_ins 1870 の級 N の unitCostOutW（産出の重み・need の出力から手で写した）
    out = {}
    for k, inp in DOWN.items():
        u = ucost(B, k)
        if not (u > 0): continue
        drop = sum(a * max(0.0, px(B, g) - rgo[g]) for g, a in inp.items())
        out[k] = 1.0 - drop / u
    return out

def main():
    mode = sys.argv[1]
    if mode == 'need':
        raw, outp = sys.argv[2], sys.argv[3]
        B = summarize(raw, 1, 10); L = []
        L.append(f'# need1020 raw={raw} seeds=1-10')
        L.append(f'3 財の級 N の行 {B["n3"]}（endow=0.4 は {B["n3_04"]}）・producers の和 {B["n3_prod"]}・級 E の行 {B["e3"]}（4.0 は {B["e3_40"]}）・級 S {B["s3"]}・他の級 {B["x3"]}')
        L.append(f'3 財の級 N の outputSum（1870＋1913）{B["out_old"]:.1f}・scaleSum {B["scale_n"]:.3f}')
        L.append(f'他の財の級 N の行 {B["oN"]}（0.4 は {B["oN_04"]}）')
        L.append(f'nationgood（3 財・1870＋1913）：prod {B["prod"]:.0f}・dem {B["dem"]:.0f}・不足÷需要 {short(B):.4f}・越境の輸出÷産出 {B["xexp"]/B["prod"]:.5f}')
        L.append('値段の平均（nationgood の priceMeanGold・行の平均）：' + '・'.join(f'{g} {px(B,g):.4f}' for g in U3))
        L.append('下流の単位費用（級 F・産出の重み）：' + '・'.join(f'{k[0]}{k[1]} {ucost(B,k):.4f}' for k in DOWN))
        L.append('G3 の床（1−投入の値段の落ち幅の上限÷下流の単位費用）：' + '・'.join(f'{k[0]}{k[1]} {v:.4f}' for k, v in floors(B).items()))
        L.append('判定：' + ('級 N の行が有る＝旗は何かをする＝腕を回す' if B['n3'] > 0 else '級 N の行が 0＝形を退ける'))
        open(outp, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n'); print('\n'.join(L)); return
    arm, base, n1, n2, rng, outp = sys.argv[2:8]
    lo, hi = map(int, rng.split('-'))
    A, B, N1, N2 = (summarize(x, lo, hi) for x in (arm, base, n1, n2))
    L = [f'# gates1020 arm={arm} base={base} n1={n1} n2={n2} seeds={rng}']
    res = []
    def gate(name, ok, txt): res.append((name, ok)); L.append(f'{name} {"通る" if ok else "落ちる"}：{txt}')
    # G1（機構・下側）
    g1a = A['n3'] == 0
    g1b = A['s3'] > 0 and A['s3_10'] == A['s3'] and A['s3'] >= B['n3'] * (1 - EPS_ROWS)
    gate('G1', g1a and g1b, f'3 財の級 N の行 {A["n3"]}（門 0）・級 S の行 {A["s3"]}（endow=1.0 は {A["s3_10"]}・門 全て）・旧の N の行 {B["n3"]}×(1−{EPS_ROWS})={B["n3"]*(1-EPS_ROWS):.0f} 以上')
    # G1′（形の誤り・上側）
    g1pa = A['oN'] > 0 and A['oN_04'] == A['oN'] and A['oN'] >= B['oN'] * (1 - EPS_ROWS)
    g1pb = A['e3'] > 0 and A['e3_40'] == A['e3'] and A['e3'] >= B['e3'] * (1 - EPS_ROWS)
    gate("G1'", g1pa and g1pb, f'他の財の級 N {A["oN"]}（0.4 は {A["oN_04"]}・基線 {B["oN"]}×0.98 以上）・3 財の級 E {A["e3"]}（4.0 は {A["e3_40"]}・基線 {B["e3"]}×0.98 以上）')
    # G2（狙いの量）
    r = outN(A) / outN(B); r1 = outN(N1) / outN(B); r2 = outN(N2) / outN(B)
    gate('G2', 1.05 <= r <= 2.5 and r > max(r1, r2), f'3 財の非産地の産出（旧 N→新 S・1870＋1913・和÷和）新÷旧 {r:.4f}（門 1.05〜2.5・ヌル腕 n1 {r1:.4f}・n2 {r2:.4f} の外）')
    # G2b（不足÷需要）
    sa, sb, s1, s2 = short(A), short(B), short(N1), short(N2)
    q = sa / sb
    gate('G2b', q <= 0.90 and q < min(s1, s2) / sb, f'3 財の不足÷需要（1870＋1913）新 {sa:.4f}÷旧 {sb:.4f}＝{q:.4f}（門 ≦0.90・ヌル腕 {s1/sb:.4f}・{s2/sb:.4f} の外）')
    # G3（入力の費用の下がりの上限）
    fb = floors(B); ok3 = True; t3 = []
    for k in DOWN:
        if k not in fb: continue
        v = ucost(A, k) / ucost(B, k); v1 = ucost(N1, k) / ucost(B, k); v2 = ucost(N2, k) / ucost(B, k)
        lim = fb[k] * min(1.0, v1, v2); ok3 = ok3 and v >= lim
        t3.append(f'{k[0]}{k[1]} {v:.4f}（床 {lim:.4f}＝{fb[k]:.4f}×ヌル腕の下端・n1 {v1:.4f}・n2 {v2:.4f}）')
    gate('G3', ok3, '・'.join(t3))
    # G4（越境）：基線の越境÷産出が床 0.02 未満なら記録
    xb = B['xexp'] / B['prod']; xa = A['xexp'] / A['prod'] if A['prod'] > 0 else float('nan')
    L.append(f'G4 {"記録（基線 " + f"{xb:.5f}" + " ＜床 0.02）" if xb < 0.02 else "門"}：越境の輸出÷産出 新 {xa:.5f}・旧 {xb:.5f}・新÷旧 {xa/xb:.4f}・n1 {N1["xexp"]/N1["prod"]:.5f}・n2 {N2["xexp"]/N2["prod"]:.5f}')
    # 記録（同じ盤面の新÷旧＝全ての列）
    L.append('記録 新÷旧（n1・n2 の比を括弧）：')
    def rec(name, fa, fb_, f1, f2): L.append(f'  {name} {fa/fb_:.4f}（n1 {f1/fb_:.4f}・n2 {f2/fb_:.4f}）')
    rec('3 財の非産地の scaleSum（N→S）', A['scale_n'] + A['scale_s'], B['scale_n'] + B['scale_s'], N1['scale_n'] + N1['scale_s'], N2['scale_n'] + N2['scale_s'])
    rec('3 財の産出（nationgood・全ての級）', A['prod'], B['prod'], N1['prod'], N2['prod'])
    rec('3 財の需要（nationgood）', A['dem'], B['dem'], N1['dem'], N2['dem'])
    for g in U3: rec(f'{g} の値段の平均', px(A, g), px(B, g), px(N1, g), px(N2, g))
    rec('3 財の級 N＋S の行の数', A['n3'] + A['s3'], B['n3'] + B['s3'], N1['n3'] + N1['s3'], N2['n3'] + N2['s3'])
    ov = len(A['keys'] & B['keys']) / max(1, len(B['keys'])); o1 = len(N1['keys'] & B['keys']) / max(1, len(B['keys']))
    L.append(f'  3 財の (種,年,国,財,レシピ) の鍵の重なり 腕 {ov:.4f}・n1 {o1:.4f}')
    L.append('判定：' + ' '.join(f'{n}={"通" if ok else "落"}' for n, ok in res))
    open(outp, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n'); print('\n'.join(L))

main()
