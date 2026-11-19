import sys
def rw(p, f):
    b = open(p, 'rb').read(); nb = f(b)
    assert nb != b, p
    open(p, 'wb').write(nb)
# Colonial.cs :41〜:57 の塊
def colonial(b):
    s = b.index(b'        if (c.ColonialUnrestHomeWeight > 1e-9)\n')
    e = b.index(b'pop.Militancy + homePressure);\n', s)
    e = b.index(b'            }\n        }\n', e) + len(b'            }\n        }\n')
    return b[:s] + b[e:]
rw('PriceOfBread.SimCore/Colonial.cs', colonial)
def coef(b):
    s = b.index(b'    public double ColonialUnrestHomeWeight = 0.0;')
    e = b.index(b'\n', s) + 1
    return b[:s] + b[e:]
rw('PriceOfBread.SimCore/Coefficients.p06_rank_discovery.cs', coef)
def probe(b):
    t = b'        AntiImperialOpinion,\r\n'
    assert b.count(t) == 1
    return b.replace(t, b'')
rw('PriceOfBread.SimCore/MechanismProbe.cs', probe)
def dorm(b):
    s = b.index(b'        "ColonialUnrestHomeWeight",   // (1243)')
    e = b.index(b'\n', s) + 1
    b = b[:s] + b[e:]
    anchor = b'"UnificationAiPursueChance", "UnificationAiCooldownYears", "UnificationAiWarGapYears",'
    i = b.index(anchor); j = b.index(b'\n', i) + 1
    crlf = b[j-2:j] == b'\r\n'
    nl = b'\r\n' if crlf else b'\n'
    add = '        "ColonialUnrestHomeWeight",   // (1243) c126k99：K99 反帝世論（植民地格の州の闘争度→本国の大衆の闘争度・旧 0.006）を腕 B（既定 0）の門の後に塊・係数・探針 AntiImperialOpinion ごと物理で消した（番人は AntiImperialOpinionRemovedTests）'.encode('utf-8') + nl
    return b[:j] + add + b[j:]
rw('PriceOfBread.Tests/DormantIdentityTests.cs', dorm)
print('ok')
