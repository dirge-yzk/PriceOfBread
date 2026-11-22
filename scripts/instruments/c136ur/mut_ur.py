# (1312) c136ur 突然変異（㉚）：使い捨ての木で 1 本ずつ入れ、関連の試験を回して赤の試験の名前を書く（c134f1/mut_f1.py の型）。
#   使い方: python -X utf8 -I mut_ur.py <使い捨ての木> <ビルドの置き場（D:/…）> <出力フォルダ> [名前,...]
#   木は旗 1（p06:116 を 1）と T1〜T5・計器 U0 の番人を入れた形を前提にする。1 本ごとに元のバイトへ戻す。
import sys, os, subprocess
T, ART, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
ONLY = set(sys.argv[4].split(',')) if len(sys.argv) > 4 else None
os.makedirs(OUT, exist_ok=True)
PD = 'PriceOfBread.SimCore/PopDecision.cs'
UX = 'PriceOfBread.SimCore/Instruments/InstrumentsUrx.cs'
MV_IF = b'        if (coef.UrbanFlowReal > 0)\n        {   // (1312)'
RUN_IF = b'        if (coef.UrbanFlowReal > 0)\n        {\n            double g = MigrationValues'
VU = b'vuRaw = ell * pl * (1.0 - urban.UnemploymentRate.Value) - housing;'
VR = b'vr = (vrw > 0 ? vrw : 0.0) + (sub > 0 ? sub : 0.0);'
MUT = {
    'B_none': [],
    'M0_flag0_path': [(PD, MV_IF, b'        if (false)\n        {   // (1312)'), (PD, RUN_IF, b'        if (false)\n        {\n            double g = MigrationValues')],
    'M1_vu_earned': [(PD, VU, b'vuRaw = (urban.Size > Eps ? urban.EarnedPerMonth.Value / urban.Size : 0.0) - housing;')],
    'M2_vr_earned': [(PD, VR, b'vr = (rural.Size > Eps ? rural.EarnedPerMonth.Value / rural.Size : 0.0) + (sub > 0 ? sub : 0.0);')],
    'M3_empty_only': [(PD, MV_IF, b'        if (coef.UrbanFlowReal > 0 && !(urban.Size > Eps))\n        {   // (1312)')],
    'M4_no_unemp': [(PD, VU, b'vuRaw = ell * pl - housing;')],
    'M5_sender_swap': [(PD, b'flow = g > 0 ? coef.Mobility * g * rural.Size : coef.Mobility * g * urban.Size;', b'flow = g > 0 ? coef.Mobility * g * urban.Size : coef.Mobility * g * rural.Size;')],
    'M6_ignore_flag': [(PD, MV_IF, b'        if (true)\n        {   // (1312)')],
    'Mi1_stuck_add_off': [(UX, b'if ((pv.Bits & 1) != 0) ux.MkStuck++;', b'if ((pv.Bits & 1) != 0) { }')],
    'Mi2_flow_cond_off': [(UX, b'if (FlowOf(gA) > 1e-9) bits |= 1;', b'bits |= 1;')],
}
FILTER = 'FullyQualifiedName~EmptyUrbanValueTests|FullyQualifiedName~UrbanFlowRealTests|FullyQualifiedName~RdhInstrumentTests|FullyQualifiedName~UrxInstrumentTests|FullyQualifiedName~FamineGateReturnsRuralTests'
summary = []
for name, edits in MUT.items():
    if ONLY and name not in ONLY: continue
    saved = {}
    for f, a, b in edits:
        p = os.path.join(T, f); src = saved.get(p) or open(p, 'rb').read(); saved[p] = src
        assert src.count(a) == 1, (name, f, a)
    for p, src in saved.items():
        cur = src
        for f, a, b in edits:
            if os.path.join(T, f) == p: cur = cur.replace(a, b)
        open(p, 'wb').write(cur)
    try:
        log = os.path.join(OUT, f'{name}.log')
        with open(log, 'wb') as lf:
            rc = subprocess.call(['dotnet', 'test', os.path.join(T, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', ART, '--filter', FILTER], stdout=lf, stderr=subprocess.STDOUT)
        txt = open(log, encoding='utf-8', errors='replace').read()
        fails = sorted(set(l.strip().split(' [')[0].replace('失敗 ', '') for l in txt.splitlines() if l.strip().startswith('失敗 PriceOfBread')))
        tot = [l.strip() for l in txt.splitlines() if '合計:' in l]
        summary.append(f'{name}\trc={rc}\t赤 {len(fails)}\t{tot[-1] if tot else "合計なし"}')
        for x in fails: summary.append(f'  {x}')
    finally:
        for p, src in saved.items(): open(p, 'wb').write(src)
with open(os.path.join(OUT, 'mut_summary.txt'), 'wb') as f: f.write(('\n'.join(summary) + '\n').encode('utf-8'))
print('\n'.join(summary))
