# (1360) c143hr 読む（コミットしない・門にしない・エンジンの関数を呼ばない）：
#   守られた法（ProtectedLaw の 7 軸）の後退と往復を（種・国）ごとに数える（monlaw_s・全ての国）。
#   後退の向き＝時短・児童労働は値が増える向き、他の 5 軸は値が減る向き（Government.Regressive と同じ向きを台本が自分で持つ）。
#   往復＝後退の後に前進がある 1 周（後退の回数と前進の回数の小さい方）。
#   使い方：python q3_count.py <名> <ins> [<名> <ins> ...]
import sys, os, glob, re
AX = ['ShorterHours', 'ChildLabor', 'Suffrage', 'WomenRights', 'SocialSecurity', 'ColonialSuffrage', 'MigrantNativeSuffrage']
UP = {'ShorterHours', 'ChildLabor'}   # 値が増える＝後退
def seed(p): return int(re.search(r'_s(\d+)\.tsv$', os.path.basename(p)).group(1))
args = sys.argv[1:]
print('腕\t軸\t種\t後退1回以上の組\t後退5回以上の組\t往復5周以上の組\t後退の総数\t1国の最大(種 国)\t戦時の後退の割合')
for name, ins in zip(args[0::2], args[1::2]):
    files = sorted(glob.glob(os.path.join(ins, 'monlaw_s*.tsv')), key=seed)
    stat = {a: [] for a in AX}; warb = {a: [0, 0] for a in AX}
    for p in files:
        s = seed(p)
        war = {}
        lr = os.path.join(ins, f'lrlock_s{s}.tsv')
        if os.path.exists(lr):
            with open(lr, 'rb') as fh:
                h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
                for line in fh:
                    c = line.rstrip(b'\r\n').decode('utf-8').split('\t')
                    war[(c[ix['quarter']], c[ix['nationUid']])] = c[ix['atWarFlag']] == '1'
        prev = {}; back = {}; fwd = {}; name_of = {}
        with open(p, 'rb') as fh:
            h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in fh:
                c = line.rstrip(b'\r\n').decode('utf-8').split('\t')
                u = c[ix['nationUid']]; name_of[u] = c[ix['nation']]; q = c[ix['quarter']]
                for a in AX:
                    if a not in ix: continue
                    v = int(float(c[ix[a]]))
                    k = (u, a)
                    if k in prev and v != prev[k]:
                        reg = (v > prev[k]) == (a in UP)
                        d = back if reg else fwd
                        d[k] = d.get(k, 0) + 1
                        if reg:
                            warb[a][1] += 1
                            if war.get((q, u)): warb[a][0] += 1
                    prev[k] = v
        for (u, a), n in back.items():
            stat[a].append((n, min(n, fwd.get((u, a), 0)), s, name_of[u]))
    for a in AX:
        L = stat[a]
        mx = max(L) if L else None
        wb = warb[a]
        print(f'{name}\t{a}\t{len(files)}\t{len(L)}\t{sum(1 for r in L if r[0] >= 5)}\t{sum(1 for r in L if r[1] >= 5)}\t{sum(r[0] for r in L)}\t'
              + (f'{mx[0]} (s{mx[2]} {mx[3]})' if mx else '0') + f'\t{(wb[0] / wb[1]) if wb[1] else 0:.2f}')
