# (954) 合流：consf_s の旧（keep_947/base_ins）と新（keep_954/base_ins＝c84ca の直しの後）の差の全ての鍵を書き出す（(948) 反証役 中〈32 件の鍵が残っていない〉への答え）。
#   python -X utf8 consf_diff954.py <旧の ins> <新の ins> <出力 tsv>
#   差の鍵＝行の多重集合の差に現れる (種, 国, 四半期)（c84ca の diff_vs_prediction.py と同じ取り方）。
#   区分：q<=160＝cons_s の上限の内（紙の予言の届く所）／q>160 のうち、旧の表で国の (scope→regionsCount) が前の四半期と違う（か前の四半期の行が無い）＝〈regionsCount の変化〉／それ以外＝〈説明できない〉。
#   〈説明できない〉には、新の表だけにある行（activeFlag 0 に当たる出た州の行）の有無と、旧と新で違う列の名を並べる。
import csv, collections, sys
old, new, outp = sys.argv[1:4]
def load(d, s):
    rows = list(csv.reader(open(f'{d}/consf_s{s}.tsv', encoding='utf-8'), delimiter='\t'))
    return rows[0], rows[1:]
tot = collections.Counter(); out = []
for s in range(1, 11):
    h, ro = load(old, s); h2, rn = load(new, s); assert h == h2
    iq, inat, isc, irc = h.index('quarter'), h.index('nation'), h.index('scope'), h.index('regionsCount')
    co, cn = collections.Counter(map(tuple, ro)), collections.Counter(map(tuple, rn))
    d_old, d_new = co - cn, cn - co
    keys = {(r[inat], int(r[iq])) for r in list(d_old) + list(d_new)}
    rc = collections.defaultdict(dict)
    for r in ro: rc[(r[inat], int(r[iq]))][r[isc]] = r[irc]
    for (n, q) in sorted(keys, key=lambda k: (k[1], k[0])):
        if q <= 160: cls = 'q<=160'
        elif (n, q - 1) not in rc or rc[(n, q - 1)] != rc.get((n, q)): cls = 'regionsCount の変化'
        else: cls = '説明できない'
        tot[cls] += 1
        a = [r for r in d_old if r[inat] == n and int(r[iq]) == q]; b = [r for r in d_new if r[inat] == n and int(r[iq]) == q]
        cols = set()
        for x in a:
            for y in b:
                if x[isc] == y[isc]: cols |= {h[i] for i in range(len(h)) if x[i] != y[i]}
        onlynew = sorted({y[isc] for y in b} - {x[isc] for x in a})
        out.append((s, n, q, cls, ','.join(onlynew) or '-', ','.join(sorted(cols)) or '-'))
with open(outp, 'wb') as f:
    f.write('種\t国\t四半期\t区分\t新の表だけの scope\t旧と新で違う列\n'.encode())
    for r in out: f.write(('\t'.join(map(str, r)) + '\n').encode())
print('差の (種,国,四半期)', len(out), dict(tot))
