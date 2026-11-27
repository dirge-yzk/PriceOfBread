# (1404) 合流：線 rn の記録の足し（反証役 v・v2）＝①侵略した国ごとの種ごとの数（invade_s の invader）②同盟の天井（alliesCount＝MaxAlliances 2＋AntiHegemonExtra 1＝3）に張り付く国×四半期の割合（rnd_s・1800 年以後）。
# 使い方: python rn_extra.py <ins の生（D:/…）> <種の起点> <種の数> [国,…]
import csv, os, sys
D, SB, N = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
NAT = (sys.argv[4] if len(sys.argv) > 4 else '清,アメリカ,イギリス,フランス,日本,ロシア,スペイン').split(',')
def rows(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    with open(p, encoding='utf-8') as f: return list(csv.DictReader(f, delimiter='\t'))
print('# ① 侵略した国ごとの種ごとの数（invade_s の行の数）')
print('nation\t' + '\t'.join(f's{s}' for s in range(SB, SB + N)) + '\tmedian')
per = {n: [] for n in NAT}
for s in range(SB, SB + N):
    inv = rows('invade', s) or []
    for n in NAT: per[n].append(sum(1 for r in inv if r['invader'] == n))
for n in NAT:
    v = sorted(per[n]); m = (v[len(v)//2 - 1] + v[len(v)//2]) / 2 if len(v) % 2 == 0 else v[len(v)//2]
    print(n + '\t' + '\t'.join(str(x) for x in per[n]) + f'\t{m:g}')
print('# ② 同盟の天井に張り付く国×四半期の割合（rnd_s・year≥1800）')
print('seed\trows\tat3\tshare\tmaxAllies')
T = A = 0
for s in range(SB, SB + N):
    rn = rows('rnd', s)
    if rn is None: print(f'{s}\tNA'); continue
    r2 = [r for r in rn if float(r['year']) >= 1800]
    a3 = sum(1 for r in r2 if float(r['alliesCount']) >= 3); mx = max((float(r['alliesCount']) for r in r2), default=0)
    T += len(r2); A += a3
    print(f'{s}\t{len(r2)}\t{a3}\t{(a3 / len(r2) if r2 else 0):.4f}\t{mx:g}')
print(f'all\t{T}\t{A}\t{(A / T if T else 0):.4f}')
