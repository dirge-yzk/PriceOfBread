# (1375) rw：内戦の間の親と反乱の国の蜂起の圧 RP＝clamp01(mobilization / max(1, effectiveCoerce))（crownpc の列・Politics.RevolutionPressure と同じ式）
# 使い方: python cwrp.py <base_ins>   読むだけ
import sys, glob, os
d = sys.argv[1]; MODE = sys.argv[2] if len(sys.argv) > 2 else 'rp'
def fl(x):
    try: return float(x)
    except: return float('nan')
def rp(mob, co): return max(0.0, min(1.0, mob / max(1.0, co)))
def kp(mob, co): return 1.0 if not (mob > 0) else co / (co + mob)   # K'=C/(C+X)
starts_p, starts_r, means_p, means_r, first3_p = [], [], [], [], []
nrec = 0
for f in sorted(glob.glob(os.path.join(d, 'cwend_s*.tsv'))):
    seed = f.rsplit('_s', 1)[1].split('.')[0]
    cp = {}
    with open(os.path.join(d, f'crownpc_s{seed}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); iq, iu, im, ic = h.index('quarter'), h.index('nationUid'), h.index('mobilizationIndex'), h.index('effectiveCoerceIndex')
        for ln in fh:
            v = ln.rstrip('\n').split('\t')
            cp[(int(v[iq]), v[iu])] = rp(fl(v[im]), fl(v[ic])) if MODE == 'rp' else kp(fl(v[im]), fl(v[ic]))
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            v = dict(zip(h, ln.rstrip('\n').split('\t'))); nrec += 1
            qe = int(v['quarter']); qs = qe - int(v['months']) // 3
            sp = [cp[(q, v['parentUid'])] for q in range(qs, qe + 1) if (q, v['parentUid']) in cp]
            sr = [cp[(q, v['rebelUid'])] for q in range(qs, qe + 1) if (q, v['rebelUid']) in cp]
            if sp: starts_p.append(sp[0]); means_p.append(sum(sp) / len(sp)); first3_p.append(max(sp[:2]))
            if sr: starts_r.append(sr[0]); means_r.append(sum(sr) / len(sr))
def summ(n, a):
    a = sorted(x for x in a if x == x)
    if not a: print(n, 'none'); return
    q = lambda p: a[min(len(a) - 1, int(p * len(a)))]
    print(n, 'n', len(a), 'p10', round(q(.1), 3), 'med', round(q(.5), 3), 'p90', round(q(.9), 3), 'share>=0.99', round(sum(x >= .99 for x in a) / len(a), 3), 'share<0.5', round(sum(x < .5 for x in a) / len(a), 3), 'share<0.25', round(sum(x < .25 for x in a) / len(a), 3))
print('records', nrec)
summ('RP parent at start quarter', starts_p); summ('RP parent mean over war', means_p)
summ('RP rebel at first quarter', starts_r); summ('RP rebel mean over war', means_r)
