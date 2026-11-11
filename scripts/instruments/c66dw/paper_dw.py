import sys, glob, os, math
# 紙の台本（エンジンを呼ばない）：cpwd_s から利札の落としの片側の書き込みを数える
d = sys.argv[1]
tot = 0; ev = 0; mism = 0; absorbed = 0; small = 0; maxabs = 0.0; maxrel = 0.0
flight_rows = 0; flight_zero_f0pos = 0
per = {}
for p in sorted(glob.glob(os.path.join(d, 'cpwd_s*.tsv'))):
    s = os.path.basename(p)[len('cpwd_s'):-4]
    with open(p, 'rb') as f:
        hdr = f.readline().decode().rstrip('\r\n').split('\t'); ix = {h:i for i,h in enumerate(hdr)}
        for line in f:
            r = line.decode().rstrip('\r\n').split('\t')
            tot += 1
            e = float(r[ix['fxRate']]); f1 = float(r[ix['fciAtCoupon']]); woE = float(r[ix['woExpectedIndex']]); woA = float(r[ix['woActualIndex']])
            un = float(r[ix['unpaidIndex']])
            if woE > 0:
                ev += 1
                diff = abs(woE - woA) * e
                if woE != woA:
                    mism += 1
                    maxabs = max(maxabs, diff); maxrel = max(maxrel, abs(woE-woA)/woE)
                if woA == 0:
                    absorbed += 1; per.setdefault(s, []).append((r[ix['quarter']], r[ix['year']], r[ix['nation']], f1, woE))
                if woE <= 1e-6: small += 1
            elif woA != 0:
                flight_rows += 1
print('rows', tot, 'events(woExp>1e-9)', ev, 'mismatch(woExp!=woAct)', mism, 'absorbed(woAct==0)', absorbed, 'woExp<=1e-6', small)
print('max |woExp-woAct|*e', repr(maxabs), 'max rel', repr(maxrel))
print('rows woExp==0 but woAct!=0', flight_rows)
for s,v in sorted(per.items()):
    for x in v: print('absorbed', 's'+s, *x)
