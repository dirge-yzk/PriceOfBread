# (1056) c100ic 門の台本（事前登録 measurements/m1056_20261112_prereg_ic.txt §3）。
# 使い方: python gate_ic.py <腕の計器の出力フォルダ> <基線の計器の出力フォルダ（keep_1053/base_ins）> [seeds=1-10]
# G1 働いた：cg を通った矯正の、同じ（月・敗者・集合）の 2 本目以降（飛ばした行を除く）＝0。
# G2 消しすぎない：events_cg（事象の最初の矯正が cg を通った数）が [71, 107] の中・0 でない。
# G3 割譲と賠償は変えない：種ごとに最初の飛ばした行の月より前の cpeace の全行が基線と一致。飛ばした月の（月・敗者）の行の cedeRealCount・repRealIndex の差は記録。
# 腕に新しい列が無い（旗 0 の基線）ときは cpeace の nGrouped と shares で cg を近似する（paper_ic.py と同じ）。
import sys, os, collections
arm, base = sys.argv[1], sys.argv[2]
a, b = (sys.argv[3].split('-') if len(sys.argv) > 3 else ('1', '10'))
seeds = range(int(a), int(b) + 1)
G2LO, G2HI = 71, 107
UK = 'イギリス'

def rd(p):
    if not os.path.exists(p): return None, []
    L = open(p, encoding='utf-8').read().splitlines()
    return {k: i for i, k in enumerate(L[0].split('\t'))}, [r for r in L[1:] if r]

g1 = 0; g2 = 0; g3bad = 0; g3rec = []; rec = collections.Counter(); seeds_uk = 0; missing = []
for s in seeds:
    ih, irows = rd(os.path.join(arm, f'imp_s{s}.tsv'))
    ch, crows = rd(os.path.join(arm, f'cpeace_s{s}.tsv'))
    bh, brows = rd(os.path.join(base, f'cpeace_s{s}.tsv'))
    if ih is None or ch is None or bh is None: missing.append(s); continue
    new = 'groupIdIndex' in ih
    grp = {}
    for r in crows:
        f = r.split('\t')
        if int(f[ch['nGrouped']]) >= 2: grp[(f[ch['month']], f[ch['loser']])] = set(x.split(':')[0] for x in f[ch['shares']].split('/') if x)
    seen_g = collections.Counter(); ev = {}; prevpol = {}; skipm = None; skiplos = set(); uk = False
    for r in irows:
        f = r.split('\t'); m, lo, wi = f[ih['month']], f[ih['loser']], f[ih['winner']]
        if new and f[ih['skippedByGroupFlag']] == '1':
            rec['skipped'] += 1; rec['skipped_uk'] += lo == UK
            if skipm is None or int(m) < skipm: skipm = int(m); skiplos = {lo}
            elif int(m) == skipm: skiplos.add(lo)
            continue
        rec['imp'] += 1
        if lo == UK: rec['imp_uk'] += 1; uk = True
        incg = (f[ih['groupIdIndex']] not in ('0',)) if new else ((m, lo) in grp and wi in grp[(m, lo)])
        gk = (m, lo, f[ih['groupIdIndex']] if new else 'x')
        if incg:
            seen_g[gk] += 1
            if seen_g[gk] >= 2: g1 += 1
        else:
            rec['nocg'] += 1
        key = (m, lo)
        if key not in ev:
            ev[key] = incg
            prevpol[key] = f[ih['loserPolityBeforeCode']]
        elif f[ih['imposedPolityCode']] == prevpol[key]: rec['roundtrip'] += 1
    rec['events'] += len(ev); rec['events_uk'] += sum(1 for k in ev if k[1] == UK)
    g2 += sum(1 for v in ev.values() if v); seeds_uk += uk
    # G3
    lim = skipm if skipm is not None else 10 ** 9
    pa = [r for r in crows if int(r.split('\t')[0]) < lim]
    pb = [r for r in brows if int(r.split('\t')[0]) < lim]
    if pa != pb: g3bad += 1; g3rec.append(f'seed {s}: 飛ばす前の行が不一致（腕 {len(pa)} 行・基線 {len(pb)} 行）')
    if skipm is not None:
        for lo in sorted(skiplos):
            fa = [r.split('\t') for r in crows if r.split('\t')[0] == str(skipm) and r.split('\t')[2] == lo]
            fb = [r.split('\t') for r in brows if r.split('\t')[0] == str(skipm) and r.split('\t')[2] == lo]
            if fa and fb:
                da = (fa[0][ch['cedeRealCount']], fa[0][ch['repRealIndex']]); db = (fb[0][bh['cedeRealCount']], fb[0][bh['repRealIndex']])
                g3rec.append(f'seed {s}: 飛ばした月 {skipm} {lo} cedeReal/repReal 腕 {da} 基線 {db} {"一致" if da == db else "差（記録）"}')
    # 記録：王冠の書き手 2・戦争の本数
    cph, cprows = rd(os.path.join(arm, f'crownpc_s{s}.tsv'))
    if cph and 'crownRegisterSourceCode' in cph: rec['crown2'] += sum(1 for r in cprows if r.split('\t')[cph['crownRegisterSourceCode']] == '2')
    wh, wrows = rd(os.path.join(arm, f'wars_s{s}.tsv'))
    if wh is not None: rec['wars'] += len(wrows)
print(f'G1 cg の同じ集合の 2 本目以降 = {g1} → {"PASS" if g1 == 0 else "FAIL"}')
print(f'G2 events_cg = {g2}（[{G2LO}, {G2HI}]・0 でない）→ {"PASS" if G2LO <= g2 <= G2HI else "FAIL"}')
print(f'G3 飛ばす前の cpeace の不一致の種 = {g3bad} → {"PASS" if g3bad == 0 else "FAIL"}')
for x in g3rec: print('  ' + x)
print('記録: ' + ' '.join(f'{k}={rec.get(k, 0)}' for k in ['imp', 'events', 'skipped', 'nocg', 'roundtrip', 'imp_uk', 'events_uk', 'skipped_uk', 'crown2', 'wars']) + f' seeds_uk={seeds_uk}')
if missing: print('欠けた種: ' + ','.join(map(str, missing)))
