# (1199) 注：(1193) で欄（国民意識）が消えた・旧の生でだけ動く（中身は変えていない）
# 読むだけ (1182) c117rs：k1（SecessionInclusionOnce）の〈英以外で余り＜0 が減る〉の帰属。
#  余り＝holdShareIndex − pressureIndex（gates_k1.py の G1 と同じ）・G1 の母集合＝tier Colonial かつ coreShareMeanShare＞1e-9。
#  同じ種・同じ (四半期, 国, 州) で腕と基線を並べ、基線で余り＜0 → 腕で ≧0（消えた）と逆（増えた）を数える。
#  エンジンの式（Politics.CheckSecession）で列から組み直す：
#   保持 H＝(正統性の項 Lt＋強制の項 Ct)×構造 S・Lt＝legitimacy÷(1＋totalPopK×SecessionHoldPerPop 5e-6)・Ct＝SecessionHoldCoercion 1×SecessionThreshold 1.43×coerce÷(coerce＋PoliticalReferencePop 1000)
#    S＝holdShareIndex÷holdBaseIndex（距離の投影÷共有の軍の分母）。
#   圧 P＝闘争度 mil×残り R（関税の差・文化の距離・中核の民族の項＝(tariffTerm＋ethnicTerm)÷mil）×意識 consF×包摂 suffF×大衆 crowdF。
#    包摂の割引を圧に掛けるかは、組み直しが pressureIndex に合う方を行ごとに選ぶ（旗 1 の腕は掛けない）。合わない行は帰属に使わず数だけ書く。
#  一つずつ入れ替え：基線の行の 8 因子のうち 1 つだけ腕の値にして余りの符号が変わるかを数える（因子＝Lt・Ct・S・mil・R・consF・suffF・crowdF）。
# 使い方: python -X utf8 -I k1_attr.py <基線の生> <腕の生> <名> [種の数=10]
import sys, os, csv, collections
BASE, ARM, TAG = sys.argv[1:4]
NS = int(sys.argv[4]) if len(sys.argv) > 4 else 10
HPP, HC, THR, REF = 5e-6, 1.0, 1.43, 1000.0
UK = 'イギリス'
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def load(d, s):
    p = os.path.join(d, f'secession_s{s}.tsv')
    if not os.path.exists(p): return None
    res = {}
    with open(p, encoding='utf-8', newline='') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['tier'] != 'Colonial': continue
            core = fl(r['coreShareMeanShare'])
            k = (int(r['quarter']), r['nation'], r['region'])
            L, tp, co = fl(r['legitimacyIndex']), fl(r['totalPopK']), fl(r['coerceIndex'])
            Lt = L / (1 + tp * HPP); Ct = HC * THR * (co / (co + REF))
            hb = fl(r['holdBaseIndex']); hs = fl(r['holdShareIndex']); pr = fl(r['pressureIndex'])
            mil = fl(r['militancyMeanIndex']); tt = fl(r['tariffTermIndex']); et = fl(r['ethnicTermIndex'])
            cf, sf, wf = fl(r['consciousnessFactorShare']), fl(r['suffrageFactorShare']), fl(r['crowdFactorShare'])
            v_old = (tt + et) * cf * sf * wf; v_once = (tt + et) * cf * wf
            def ok(a, b): return abs(a - b) <= 1e-9 * max(1.0, abs(b))
            use = 'old' if ok(v_old, pr) else 'once' if ok(v_once, pr) else None
            res[k] = dict(core=core, Lt=Lt, Ct=Ct, hbok=ok(Lt + Ct, hb), S=(hs / hb if hb != 0 else 1.0), H=hs, P=pr,
                          mil=mil, R=((tt + et) / mil if mil != 0 else 0.0), cf=cf, sf=(sf if use == 'old' else 1.0), sfcol=sf, wf=wf, use=use,
                          neg=(hs - pr < 0))
    return res
FAC = ['Lt', 'Ct', 'S', 'mil', 'R', 'cf', 'sf', 'wf']
def margin(d):
    return (d['Lt'] + d['Ct']) * d['S'] - d['mil'] * d['R'] * d['cf'] * d['sf'] * d['wf']
C = collections.Counter(); natd = collections.Counter(); flip = collections.Counter(); win = collections.Counter(); agree = collections.Counter()
seeds = 0
for s in range(1, NS + 1):
    b, a = load(BASE, s), load(ARM, s)
    if b is None or a is None: continue
    seeds += 1
    for side, D in (('基線', b), ('腕', a)):
        for k, d in D.items():
            agree[side, 'hb', d['hbok']] += 1; agree[side, 'use', d['use']] += 1
            if d['core'] > 1e-9 and d['neg']:
                C[side, 'G1', k[1] == UK] += 1
    for k in set(b) | set(a):
        db, da = b.get(k), a.get(k)
        uk = k[1] == UK
        if db is None or da is None:
            d = db or da
            if d['core'] > 1e-9 and d['neg']: C['片側だけ', '基線' if db else '腕', uk] += 1
            continue
        gb, ga = db['core'] > 1e-9 and db['neg'], da['core'] > 1e-9 and da['neg']
        if gb == ga: C['同じ', gb, uk] += 1; continue
        kind = '消えた' if gb else '増えた'
        C[kind, uk] += 1; natd[kind, k[1]] += 1
        if uk: continue
        if db['use'] is None or da['use'] is None or not db['hbok'] or not da['hbok']:
            C[kind, '組み直しが合わない'] += 1; continue
        m0 = margin(db); best = None
        for f in FAC:
            x = dict(db); x[f] = da[f]; m1 = margin(x)
            if (m0 < 0) != (m1 < 0): flip[kind, f] += 1
            dm = abs(m1 - m0)
            if best is None or dm > best[1]: best = (f, dm)
        win[kind, best[0]] += 1
        if (m0 < 0) == (margin(da) < 0): C[kind, '組み直しの符号が生と違う'] += 1
print(f'# {TAG} 基線={BASE} 腕={ARM} 種 {seeds}/{NS}')
print('行の一致（Lt＋Ct＝holdBaseIndex）：' + ' '.join(f'{k[0]}{"合う" if k[2] else "合わない"}:{v}' for k, v in sorted(agree.items(), key=str) if k[1] == 'hb'))
print('圧の組み直し（包摂の割引あり old／なし once／どちらも合わない None）：' + ' '.join(f'{k[0]}{k[2]}:{v}' for k, v in sorted(agree.items(), key=str) if k[1] == 'use'))
for side in ('基線', '腕'):
    print(f'G1（植民地格・中核＞0・余り＜0）{side}：英 {C[side, "G1", True]}・英以外 {C[side, "G1", False]}・計 {C[side, "G1", True] + C[side, "G1", False]}')
print(f'片側だけの行（G1）：基線だけ 英 {C["片側だけ", "基線", True]}・英以外 {C["片側だけ", "基線", False]}／腕だけ 英 {C["片側だけ", "腕", True]}・英以外 {C["片側だけ", "腕", False]}')
print(f'並べた行：同じ（G1 のまま）英 {C["同じ", True, True]}・英以外 {C["同じ", True, False]}・消えた 英 {C["消えた", True]}・英以外 {C["消えた", False]}・増えた 英 {C["増えた", True]}・英以外 {C["増えた", False]}')
for kind in ('消えた', '増えた'):
    print(f'英以外の{kind}：組み直しが合わない {C[kind, "組み直しが合わない"]}・組み直しの符号が生と違う {C[kind, "組み直しの符号が生と違う"]}')
    print('  一つずつ入れ替えて符号が変わる行：' + ' '.join(f'{f}:{flip[kind, f]}' for f in FAC))
    print('  余りを一番大きく動かす因子：' + ' '.join(f'{f}:{win[kind, f]}' for f in FAC))
    print('  国の上位：' + ' '.join(f'{k[1]}:{v}' for k, v in sorted(natd.items(), key=lambda kv: -kv[1]) if k[0] == kind and k[1] != UK)[:400])
