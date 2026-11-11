#!/usr/bin/env python
# (1018) c95wn 門 G-W1〜G-W3 と 床 F1〜F5 の予言値（記録）を計器つきの生から数える。使い方：python wn_gates.py <ins のフォルダ> [<種の数=10>] [<バッチの報告 b10_*.txt>]
import sys, os, csv, collections, re
d = sys.argv[1]; nseed = int(sys.argv[2]) if len(sys.argv) > 2 else 10
rep = sys.argv[3] if len(sys.argv) > 3 else None
def rd(name, s):
    p = os.path.join(d, f"{name}_s{s}.tsv")
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); return h, list(r)
JOIN = ('ally', 'suzerain', 'calltoarms')
tot = collections.Counter(); out = []
mx = []; ge3 = []; pr = []
for s in range(1, nseed + 1):
    # G-W3 q の対応：nation_s の (quarter, year)
    hn, rn = rd('nation', s); qi, yi = hn.index('quarter'), hn.index('year')
    yq = {}
    for r in rn: yq.setdefault(int(r[qi]), int(r[yi]))
    hw, rw = rd('wars', s); ws = collections.Counter(); bad3 = 0; badq = 0
    for r in rw:
        q = int(r[hw.index('q')]); y = int(r[0]); code = r[hw.index('entryCode')]
        if code in ('unknown', 'none'): tot['wars_unknown_code'] += 1
        if q not in yq or yq[q] != y: bad3 += 1
        if code not in JOIN:
            ws[frozenset((r[2], r[3]))] += 1
    mx.append(max(ws.values()) if ws else 0); ge3.append(sum(1 for v in ws.values() if v >= 3)); pr.append(len(ws))
    hi, ri = rd('invade', s); badi = sum(1 for r in ri if int(r[-1]) not in yq or yq[int(r[-1])] != int(r[0]))
    # G-W2
    he, re_ = rd('warentry', s); c = {n: i for i, n in enumerate(he)}
    r0, r1 = c['r_ok'], c['r_other']; s0, s1 = c['sh_pass'], c['sh_canned']
    bad2 = 0; sums = collections.Counter()
    for r in re_:
        ev = int(r[c['evals']]); rs = sum(int(x) for x in r[r0:r1 + 1]); sh = sum(int(x) for x in r[s0:s1 + 1])
        if ev != rs: bad2 += 1
        if sh != int(r[c['r_weak']]): bad2 += 1
        if int(r[c['sh_projPass']]) + int(r[c['sh_projFail']]) > int(r[c['sh_pass']]): bad2 += 1
        for k in ('evals', 'r_ok', 'r_weak', 'r_will', 'r_mil', 'r_ship', 'r_sea', 'r_canned', 'r_atWar', 'r_dependent', 'r_noLand', 'r_maxDep', 'sh_pass', 'sh_projPass', 'sh_projFail', 'sh_calls', 'tauPos', 'tauPosWeak', 'tgtRankMissing', 'tgtNonMinor', 'calls', 'rCalls', 'scLessCalls'):
            sums[k] += int(r[c[k]])
        sums['sumTau2'] += float(r[c['sumTau2']])
    hp, rp = rd('warpair', s); cp = {n: i for i, n in enumerate(hp)}
    sub = sum(int(r[cp['submits']]) for r in rp); pe = sum(int(r[cp['peaceResets']]) for r in rp); se = sum(int(r[cp['secessionResets']]) for r in rp)
    hb, rb = rd('warband', s); cb = {n: i for i, n in enumerate(hb)}
    bsk = sum(int(r[cb['pairsSkipped']]) for r in rb); bw = sum(int(r[cb['pairsWritten']]) for r in rb)
    bsub = sum(int(r[cb['submits']]) for r in rb)
    out.append(f"種{s}: 戦争 {len(rw)} 件・G-W3 の食い違い 戦争 {bad3}・侵略 {badi}・G-W2 の食い違い {bad2}｜評価 {sums['evals']}・通過 {sums['r_ok']}・弱小国で落ち {sums['r_weak']}（影の通過 {sums['sh_pass']}・投射も通過 {sums['sh_projPass']}）・余剰戦力不足 {sums['r_will']}｜τ>0 {sums['tauPos']}（弱小国で落ちた分 {sums['tauPosWeak']}）｜屈服(組) {sub}＝band {bsub}・侵略行 {len(ri)}・置き直し 講和{pe}/分離{se}｜標的の格が _rank に無い {sums['tgtRankMissing']}・非 Minor の標的 {sums['tgtNonMinor']}｜S_C′<S_A の呼び {sums['scLessCalls']}/{sums['calls']}")
    tot['bad3'] += bad3 + badi; tot['bad2'] += bad2
    for k, v in sums.items(): tot[k] += v
    tot['subs'] += sub; tot['invrows'] += len(ri)
print("\n".join(out))
print(f"合計: G-W3 食い違い {tot['bad3']}・G-W2 食い違い {tot['bad2']}・札 unknown {tot['wars_unknown_code']}")
print(f"G-W1（名前で数える近似）: 参戦以外の札で数えた 最多の再戦 平均 {sum(mx)/len(mx):.1f}・3回以上の組 平均 {sum(ge3)/len(ge3):.1f}・交戦ペア 平均 {sum(pr)/len(pr):.1f}")
if rep and os.path.exists(rep):
    for l in open(rep, encoding='utf-8'):
        if '同一ペアの再戦' in l: print("バッチの報告:", l.strip()[:200])
print(f"F1 余剰戦力不足の評価 {tot['r_will']}・F2 通った評価 {tot['r_ok']}（侵略 {tot['invrows']} 行）・F3 影の通過 {tot['sh_pass']}／投射も通過 {tot['sh_projPass']}・F4 τ>0 {tot['tauPos']}（弱小国の門で落ちた分 {tot['tauPosWeak']}・Σ τ² {tot['sumTau2']:.1f}）・F5 AI の屈服(組) {tot['subs']} と侵略 {tot['invrows']}")
