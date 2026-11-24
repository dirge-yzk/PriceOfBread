# (1343) c141wd 記録の読み手：開幕（ゲームの最初の四半期＝quarter 0）の国営工場の着工（aib_s の startedCount の和）・総動員の国（nation_s mobilizedFlag）・
#   最初の 4 四半期の国営工場の着工の和を、腕と基準で並べる（門にしない）。使い方: python opening_wd.py <腕の ins> <基準の ins> <種の起点> <種の数>
import sys, csv, os
csv.field_size_limit(1 << 30)
arm, base, sb, n = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
def rows(p):
    if not os.path.exists(p): return None
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r: yield dict(zip(h, x))
def fnum(s):
    try: return float(s)
    except Exception: return 0.0
def read(ins, s):
    aib = rows(f'{ins}/aib_s{s}.tsv'); nat = rows(f'{ins}/nation_s{s}.tsv')
    st0 = st4 = 0.0; mob0 = 0; ok = [aib is not None, nat is not None]
    if aib is not None:
        for x in aib:
            q = int(fnum(x.get('quarter', '-1')))
            if q == 0: st0 += fnum(x.get('startedCount', '0'))
            if 0 <= q < 4: st4 += fnum(x.get('startedCount', '0'))
    if nat is not None:
        for x in nat:
            if int(fnum(x.get('quarter', '-1'))) == 0 and fnum(x.get('mobilizedFlag', '0')) > 0: mob0 += 1
    return ok, st0, st4, mob0
print('種\t側\t表\t開幕の着工\t最初の4四半期の着工\t開幕の総動員の国')
for s in range(sb, sb + n):
    for tag, ins in (('arm', arm), ('base', base)):
        ok, a, b, c = read(ins, s)
        print(f'{s}\t{tag}\t{"aib" if ok[0] else "-"}{"/nat" if ok[1] else "/-"}\t{a:g}\t{b:g}\t{c}')
