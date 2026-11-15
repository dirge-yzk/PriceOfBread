# (1163) 直す形の紙の当て（事前登録の下書き §3・静的・1 歩の目標）：CTL 四半期 40 の rulepoweralloc_s と rulepower_s から、案ごとに外国の力を組み直す。
#   今   ：配り＝O×w÷Σw_候補・F＝配り×ρ_f（エンジンと突き合わせた形）
#   案 2 ：自国の州も同じ重み w＝Y×(1−b)（s＝0）で配りの候補に入る（配り切らない先が自国）。自国へ落ちた分は持ち主の力に足す（持ち主の O の自国の分も同じ）
#   案 4 ：(1−b) を重みから力へ移す（配り＝O×Y′(1−s)÷Σ・F＝配り×ρ_f×(1−b)）＝思想⑨の点検が要る形（比べのために並べる）
#   自国の州の Y は、四半期 40 にどこかの国の候補の行に出た州だけ分かる＝自国の重みの和は下限＝案 2 の外国の力は上側の目安。
import sys, os, collections, statistics as st, math, pickle
D = 'D:/PoB_work/keep_1157_rej/c113cr_mut/CTL/ins'
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
recs = pickle.load(open('D:/PoB_work/r1163_work/out/decomp.txt.pkl', 'rb'))
out = []
P = lambda *x: out.append(' '.join(str(y) for y in x))
for s in (1, 2):
    rp = {}; natO = {}; regs = collections.defaultdict(list)
    with open(os.path.join(D, f'rulepower_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if int(c[0]) != 40: continue
            n = c[ix['nation']]; rg = c[ix['region']]
            rp[rg] = (n, fl(c[ix['ownerWeightShare']]), fl(c[ix['rhoEngIndex']]))
            natO.setdefault(n, fl(c[ix['oEngNationIndex']])); regs[n].append(rg)
    Y = {}; cand = collections.defaultdict(list); wok = wbad = 0; rows40 = []
    with open(os.path.join(D, f'rulepoweralloc_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if int(c[0]) != 40: continue
            y = fl(c[ix['yIndex']]); Y[c[ix['region']]] = y
            if c[ix['candEngFlag']] != '1': continue
            kind = c[ix['candDKind']]; sh = fl(c[ix['sShare']]); b = fl(c[ix['bShare']]); w = fl(c[ix['wEngIndex']])
            cy = y if kind == '3' else y * sh
            rows40.append((c[ix['region']], cy, sh, w))
            cand[c[ix['nation']]].append((c[ix['region']], cy, sh, b, w, fl(c[ix['rhoFDIndex']])))
    # 列 bShare は計器の影の b（エンジンの b と違う）＝エンジンの (1−b) は w÷(Y′(1−s)) で読み、rulepower_s の ownerWeightShare（エンジンの Ow）と突き合わせる
    # rulepower_s の ownerWeightShare もエンジンの Ow と合わなかった（一致 0）＝エンジンの (1−b) を w÷(Y′(1−s)) で州ごとに読み（OMB）、別の国の候補の行どうしで同じ州の値が一致するかで確かめる
    OMB = {}
    for rg, cy, sh, w in rows40:
        if not (cy * (1 - min(1.0, sh)) > 0): continue
        v = w / (cy * (1 - min(1.0, sh)))
        if rg in OMB:
            if abs(OMB[rg] - v) <= 1e-9 * max(1.0, abs(v)): wok += 1
            else: wbad += 1
        else: OMB[rg] = v
    P(f'## 種 {s}：エンジンの (1−b)＝w÷(Y′(1−s)) の州の数 {len(OMB)}・別の国の行どうしの一致 {wok}・不一致 {wbad}')
    def home(n):
        rs = regs.get(n, []); known = [r for r in rs if r in Y]
        known = [r for r in known if r in OMB]
        wsum = sum(Y[r] * OMB[r] for r in known)
        return wsum, len(known), len(rs)
    P('  州\t持ち主\t外国\tt 今\tt 案2\tt 案4\t外国の自国の州（Y が分かる/全て）\t自国の重み÷候補の重み')
    t_now, t2s, t4s = [], [], []
    for (ss, rg), r in sorted(recs.items()):
        if ss != s: continue
        fk = r['fk']; O = r['O']; cs = cand[fk]; swc = sum(x[4] for x in cs)
        hw, hk, hn = home(fk)
        a2 = O * r['w'] / (swc + hw); F2 = a2 * r['rf']
        own = r['own']; Oo = natO.get(own, 0.0); R = 0.0
        if Oo > 0:
            owc = sum(x[4] for x in cand.get(own, [])); ohw, _, _ = home(own)
            wr = Y.get(rg, 0.0) * OMB.get(rg, 0.0)
            R = Oo * wr / (owc + ohw) * rp[rg][2] if owc + ohw > 0 else 0.0
        t2 = F2 / (r['tot'] - r['F'] + F2 + R)
        s4 = sum(x[1] * (1 - min(1.0, x[2])) for x in cs)
        a4 = O * r['y'] * (1 - min(1.0, r['s'])) / s4 if s4 > 0 else 0.0; F4 = a4 * r['rf'] * OMB[rg]
        t4 = F4 / (r['tot'] - r['F'] + F4)
        tn = r['F'] / r['tot']
        t_now.append(tn); t2s.append(t2); t4s.append(t4)
        P(f"  {rg}\t{own}\t{fk}\t{tn:.3f}\t{t2:.3f}\t{t4:.3f}\t{hk}/{hn}\t{hw / swc if swc > 0 else float('nan'):.3g}")
    if t_now:
        P(f'  中央：今 {st.median(t_now):.3f}・案2 {st.median(t2s):.3f}・案4 {st.median(t4s):.3f}')
with open(sys.argv[1], 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
