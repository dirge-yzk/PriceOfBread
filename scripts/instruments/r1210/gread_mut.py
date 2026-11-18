# (1210) gread.py の selftest の突然変異（4 本）。用法: python -X utf8 -I gread_mut.py <gread.py> <作業フォルダ>
import subprocess, sys, os
src, wd = sys.argv[1], sys.argv[2]
b = open(src, 'rb').read()
muts = {'M1 国の最初の行へ戻す': (b'ser[(nat, reg)][q] = v', b'ser.setdefault((nat, ""), {}).setdefault(q, v)'),
        'M2 州の欠けを名乗らない': (b'if lost: tags.append', b'if False: tags.append'),
        'M3 続いた四半期の条件を外す': (b'if prev is not None and q == prev_q + 1:', b'if prev is not None:'),
        'M4 名の並びの列を数えに戻す': ("('coreRegions', 'integratedRegions', 'colonialRegions'))".encode(), "('coreRegions', 'integratedRegions', 'colonialRegions', 'nonCoreRegions'))".encode())}
for k, (a, c) in muts.items():
    assert a in b, k
    m = os.path.join(wd, 'gread_m.py'); open(m, 'wb').write(b.replace(a, c, 1))
    r = subprocess.run([sys.executable, '-X', 'utf8', '-I', m, 'selftest'], capture_output=True, text=True, encoding='utf-8')
    last = (r.stdout.strip().splitlines() or ['(出力なし) ' + (r.stderr.strip().splitlines() or [''])[-1]])[-1]
    print(k, 'rc=%d' % r.returncode, last, '|', ' / '.join(l for l in r.stdout.splitlines() if l.startswith('FAIL')))
    os.remove(m)
