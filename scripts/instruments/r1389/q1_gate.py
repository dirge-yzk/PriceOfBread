# (1389) 問い 1・2：開戦の門の厭戦を国難に替える形 G1（消す）・G2（a か b が国難で閉じる）・G2s（両方が国難の時だけ閉じる＝宣戦できる側が 1 つでもあれば開く）
import sys; sys.path.insert(0, 'D:/PoB_work/r1389_work/scripts')
from load import *; from align import match
R = {k: 0 for k in ('closedW_peace','closedW_war','G2keep_peace','G2keep_war','G2skeep_peace','G2skeep_war')}
runs_out = {m: {'peace': 0, 'war': 0} for m in ('G1', 'G2', 'G2s')}
st = {'starts': 0, 'submits': 0, 'start_any_crisis': 0, 'start_both_crisis': 0, 'start_any_crisis_prev': 0, 'start_both_crisis_prev': 0, 'start_civil_side': 0, 'start_known': 0,
      'sub_any_crisis': 0, 'go_months': 0, 'go_any_crisis': 0, 'go_both_crisis': 0}
wb = {'start_main': 0, 'submits': 0}
atw = {'n': 0, 'cr': 0, 'nw': 0, 'crw': 0, 'parts': [0, 0, 0, 0]}
for s in SEEDS:
    cm = crisis_map(s)
    sc = {(int(v['quarter']), v['nation']): (fl(v['warExhaustion']), fl(v['atWar'])) for v in rows('scalars', s)}
    for v in rows('warband', s): wb['start_main'] += int(v['start_main']); wb['submits'] += int(v['submits'])
    for (q, nm), (c, parts, aw) in cm.items():
        if not aw: continue
        atw['n'] += 1; atw['cr'] += c
        for i in range(4): atw['parts'][i] += parts[i]
        e = sc.get((q, nm))
        if e is not None and e[0] > 0.5: atw['nw'] += 1; atw['crw'] += c
    o, _ = match(s); iv = {}
    for b, a in o:
        e = int(b['month']); iv.setdefault(frozenset((a['sideA'], a['sideB'])), []).append((e - int(a['months']), e))
    runs = {m: {} for m in runs_out}
    def cr(q, nm): return cm.get((q, nm), (False,))[0]
    for v in rows('warpair', s):
        q = int(v['quarter']); ge, go = int(v['monthsGe140']), int(v['gateOpenMonths']); A, B = v['nationA'], v['nationB']; k = frozenset((A, B))
        ns, nsub = int(v['starts']), int(v['submits'])
        ca, cb = cr(q, A), cr(q, B); cpa, cpb = cr(q - 1, A), cr(q - 1, B)
        if ns:
            st['starts'] += ns
            if (q, A) in cm and (q, B) in cm:
                st['start_known'] += ns; st['start_any_crisis'] += ns * (ca or cb); st['start_both_crisis'] += ns * (ca and cb)
                st['start_civil_side'] += ns * (cm[(q, A)][1][0] or cm[(q, B)][1][0])
            if (q - 1, A) in cm and (q - 1, B) in cm:
                st['start_any_crisis_prev'] += ns * (cpa or cpb); st['start_both_crisis_prev'] += ns * (cpa and cpb)
        if nsub: st['submits'] += nsub; st['sub_any_crisis'] += nsub * (ca or cb)
        if go: st['go_months'] += go; st['go_any_crisis'] += go * (ca or cb); st['go_both_crisis'] += go * (ca and cb)
        kind = None
        if ge > go and not any(a0 <= q * 3 + 2 and q * 3 <= e0 for a0, e0 in iv.get(k, [])):
            xa, xb = sc.get((q, A)), sc.get((q, B))
            if xa and xb:
                w = [x for x in (xa, xb) if x[0] > 0.5]
                if w:
                    kind = 'war' if any(x[1] > 0 for x in w) else 'peace'; n = ge - go
                    R['closedW_' + kind] += n
                    if ca or cb: R['G2keep_' + kind] += n
                    if ca and cb: R['G2skeep_' + kind] += n
        opens = {'G1': kind is not None, 'G2': kind is not None and not (ca or cb), 'G2s': kind is not None and not (ca and cb)}
        for m in runs_out:
            r = runs[m].get(k)
            if opens[m] and r and r[0] == kind and r[1] == q - 1: runs[m][k] = (kind, q, r[2] + ge - go)
            else:
                if r and r[2] >= 6: runs_out[m][r[0]] += 1
                runs[m][k] = (kind, q, ge - go) if opens[m] else None
    for m in runs_out:
        for r in runs[m].values():
            if r and r[2] >= 6: runs_out[m][r[0]] += 1
print('厭戦で閉じた月（scalars の 22 国どうし・緊張>=140・組は交戦していない）', R)
print('6 か月以上の塊（門が開く見込み）', runs_out)
print('warband', wb, ' 開戦の率 start_main/(start_main+submits)', f"{wb['start_main']/(wb['start_main']+wb['submits']):.3f}")
print('warpair の開戦と屈服', st)
print('交戦中の国×四半期', atw['n'], '国難', f"{atw['cr']/atw['n']:.1%}", '内訳(内戦・食・支払い・正統性)', [f'{x/atw["n"]:.1%}' for x in atw['parts']], ' 厭戦>0.5 の交戦中（22 国）', atw['nw'], '国難', f"{atw['crw']/max(1,atw['nw']):.1%}")
