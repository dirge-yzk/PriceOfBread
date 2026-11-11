import sys
p, m = sys.argv[1], sys.argv[2]
b = open(p, 'rb').read()
R = {
 'M1': [(b'if (cg is not null) cg.ImposeLeft = 0;', b'if (cg is not null) { }')],
 'M2': [(b'CedeLeft = 1, ImposeLeft = 1 }', b'CedeLeft = 1, ImposeLeft = 0 }')],
 'M3': [(b'cg is not null && cg.ImposeLeft <= 0)', b'cg is not null && cg.CedeLeft <= 0)'), (b'if (cg is not null) cg.ImposeLeft = 0;', b'if (cg is not null) cg.CedeLeft = 0;')],
 'M4': [(b'c.ImposeOncePerCollapse > 0 && cg is not null && cg.ImposeLeft', b'false && cg is not null && cg.ImposeLeft')],
}[m]
for a, z in R:
    if b.count(a) != 1: sys.exit(f'{m}: {a!r} の数 {b.count(a)}')
    b = b.replace(a, z)
open(p, 'wb').write(b)
