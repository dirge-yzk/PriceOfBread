# (1283) c132fg 紙の台本：飢えの門 PopDecision.cs:76 が掛かる州×四半期と、その中の g＜0 の割合（読むだけ）
# 入力: 計器つき 10 種の生のフォルダ（cpop_s*.tsv・urxpd_s*.tsv）
# 近似: cpop は四半期末の値・稼ぎは earnedSettlePerQuarter÷3・自給の値は subsistenceValuePerQuarter÷3
#       住居の費用は cpop に無い＝0 と置く（住居は都市の値打ちを下げるだけ＝g＜0 の割合はこの近似で下限）
#       対＝popIndex 1（農民）と 2（工場労働者）＝(1272) pd_gate.py と同じ読み
# 旗 0（今の規則）: 門が掛かれば流れ 0。旗 1（形 A）: 門が掛かり g＜0 なら 流れ＝Mobility×g×都市の規模（月）
# 根の側（UrbanFlowReal=1・門は残る）: 流れ＝g＞0 ? Mobility×g×農村 : Mobility×g×都市・門は g＞0 の向きだけ止める
import sys, csv, glob, os, math, collections
T, MOB, GATE = 0.15, 0.02, 0.8
d = sys.argv[1]
out = open(sys.argv[2], 'w', encoding='utf-8', newline='\n')
tot = collections.Counter(); fl = collections.defaultdict(float)
per_seed = []
for f in sorted(glob.glob(os.path.join(d, 'cpop_s*.tsv')), key=lambda p: int(p.rsplit('_s', 1)[1][:-4])):
    seed = int(f.rsplit('_s', 1)[1][:-4])
    rows = collections.defaultdict(dict)
    with open(f, encoding='utf-8') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            if r['popIndex'] in ('1', '2'):
                rows[(r['quarter'], r['nation'], r['region'])][r['popIndex']] = r
    c = collections.Counter(); s = collections.defaultdict(float)
    for k, pr in rows.items():
        R = pr.get('1'); U = pr.get('2')
        if not R or not U: continue
        sr, su = float(R['popK']), float(U['popK'])
        if sr <= 0 or su <= 0: continue
        ir = float(R['earnedSettlePerQuarter']) / 3 / sr; iu = float(U['earnedSettlePerQuarter']) / 3 / su
        sub = float(R['subsistenceValuePerQuarter']) / 3 / sr
        z = max(-40, min(40, (iu - ir) / T)); tgt = 1 / (1 + math.exp(-z)); cur = su / (sr + su)
        flow0 = MOB * (tgt - cur) * (sr + su)
        fr, fu = float(R['foodFill']), float(U['foodFill'])
        vr = max(ir, 0) + max(sub, 0); vu = max(iu, 0)
        g = 2 * (vu - vr) / (vr + vu) if vr + vu > 1e-9 else 0.0
        c['regionQ'] += 1
        famine = fu < GATE and fu < fr
        gate = flow0 > 0 and famine
        if gate:
            c['gate'] += 1
            if g < 0:
                c['gate_gneg'] += 1; s['returnA_K_perMonth'] += -MOB * g * su
        # 根の側
        flowR = MOB * g * (sr if g > 0 else su)
        if flowR > 0 and famine:
            c['root_gate'] += 1
        elif flowR < 0:
            c['root_toRural'] += 1; s['root_toRural_K_perMonth'] += -flowR
        if flow0 < 0: s['old_toRural_K_perMonth'] += -flow0
        if flow0 > 0 and not gate: s['old_toUrban_K_perMonth'] += flow0
        if flowR > 0 and not famine: s['root_toUrban_K_perMonth'] += flowR
    share = c['gate_gneg'] / c['gate'] if c['gate'] else float('nan')
    per_seed.append((seed, c, s, share))
    tot.update(c)
    for kk, v in s.items(): fl[kk] += v
out.write('seed\tregionQ\tgate\tgate_gneg\tgnegShare\treturnA_K_perMonth\told_toRural_K_perMonth\told_toUrban_K_perMonth\troot_gate\troot_toRural\troot_toRural_K_perMonth\troot_toUrban_K_perMonth\n')
for seed, c, s, share in per_seed:
    out.write(f"{seed}\t{c['regionQ']}\t{c['gate']}\t{c['gate_gneg']}\t{share:.4f}\t{s['returnA_K_perMonth']:.3f}\t{s['old_toRural_K_perMonth']:.3f}\t{s['old_toUrban_K_perMonth']:.3f}\t{c['root_gate']}\t{c['root_toRural']}\t{s['root_toRural_K_perMonth']:.3f}\t{s['root_toUrban_K_perMonth']:.3f}\n")
share = tot['gate_gneg'] / tot['gate'] if tot['gate'] else float('nan')
out.write(f"ALL\t{tot['regionQ']}\t{tot['gate']}\t{tot['gate_gneg']}\t{share:.4f}\t{fl['returnA_K_perMonth']:.3f}\t{fl['old_toRural_K_perMonth']:.3f}\t{fl['old_toUrban_K_perMonth']:.3f}\t{tot['root_gate']}\t{tot['root_toRural']}\t{fl['root_toRural_K_perMonth']:.3f}\t{fl['root_toUrban_K_perMonth']:.3f}\n")
# urxpd の famineGated（月の数・計器の国だけ・正確な値）
ug = 0; um = 0
for f in glob.glob(os.path.join(d, 'urxpd_s*.tsv')):
    with open(f, encoding='utf-8') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            ug += int(r['famineGated']); um += int(r['regionMonths'])
out.write(f"# urxpd: famineGated 州×月 {ug} / regionMonths {um} = {ug / um if um else float('nan'):.5f}\n")
out.close()
print('done', tot['regionQ'], tot['gate'], tot['gate_gneg'], round(share, 4), ug, um)
