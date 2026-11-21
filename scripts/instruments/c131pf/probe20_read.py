# (1277) c131pf 先読みの読み手（事前登録 [2]）：種 20 のレイキャヴィクの人口の最大・最小・終わり・守備兵と技師の入りを、旗 0・旗 1・基準 a11 で並べる。
#   旗 0 の表が a11 とバイト一致かも出す（器の恒等の種 20 の確かめ）。エンジンの関数を呼ばない。
# 使い方: python -X utf8 -I probe20_read.py <旗 0 の ins> <旗 1 の ins> <a11>
import sys, os, hashlib
P0, P1, A11 = sys.argv[1:4]
REG = 'レイキャヴィク'; S = 20

def sha(p):
    return hashlib.sha1(open(p, 'rb').read()).hexdigest() if os.path.exists(p) else '(無い)'

for t in ('popclose_s', 'region_s', 'nation_s', 'migration_s', 'pxmonth_s'):
    a, b = sha(os.path.join(P0, f'{t}{S}.tsv')), sha(os.path.join(A11, f'{t}{S}.tsv'))
    print(f'一致 {t}{S}: 旗0 {a[:12]} a11 {b[:12]} {"同じ" if a == b else "違う"}')

def fl(x):
    try: return float(x)
    except Exception: return float('nan')

def read(d):
    p = os.path.join(d, f'popclose_s{S}.tsv')
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        rs = [l.rstrip('\n').split('\t') for l in f]
    rs = [r for r in rs if r[ix['region']] == REG]
    pop = [(fl(r[ix['populationK']]), int(r[ix['year']]), int(r[ix['quarter']]), r[ix['nation']]) for r in rs]
    pk = max(pop); after = [x for x in pop if x[2] >= pk[2]]; mn = min(after)
    gin = sum(fl(r[ix['garrisonInK']]) for r in rs); gmax = max(((fl(r[ix['garrisonInK']]), int(r[ix['year']]), int(r[ix['quarter']])) for r in rs))
    sin = sum(fl(r[ix['specialistInK']]) for r in rs)
    cin = sum(fl(r[ix['crossBorderInK']]) for r in rs); cout = sum(fl(r[ix['crossBorderOutK']]) for r in rs)
    return pk, mn, pop[-1], gin, gmax, sin, cin, cout

print('腕\tピーク(千人@年・国)\tピーク後の最小\t比\t終わり\t守備兵の入りの和\t守備兵の入りの最大(@年 q)\t技師の入りの和\t越境の入り\t越境の出')
for nm, d in (('旗0', P0), ('旗1', P1), ('a11', A11)):
    try:
        pk, mn, end, gin, gmax, sin, cin, cout = read(d)
        print(f'{nm}\t{pk[0]:.1f}@{pk[1]}・{pk[3]}\t{mn[0]:.1f}@{mn[1]}・{mn[3]}\t{mn[0] / pk[0]:.3f}\t{end[0]:.1f}@{end[1]}・{end[3]}\t{gin:.1f}\t{gmax[0]:.1f}@{gmax[1]} q{gmax[2]}\t{sin:.1f}\t{cin:.1f}\t{cout:.1f}')
    except Exception as e:
        print(f'{nm}\t(読めない {e})')
