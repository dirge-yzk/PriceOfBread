# (1342) c141k59 段 0（読むだけ）：女性の権利の法の段（scalars_s の womenRightsCode）と、lwk_s の参加率（partShare＝エンジンが使った実効の参加率）を国×年で数える。
#   構造の参加率の近似＝その国×年の partShare の最小（困窮の項 0 の POP）。賃金保持の近似＝(最小−0.4)/0.3。
#   旗 1 で変わる量の近似＝構造の項 0.3×賃金保持（困窮の項は残る）。計器はエンジンの関数を呼ばない（表の列から自分で組む）。
#   python stage0_k59.py <生のフォルダ> <種の始め> <種の数>
import sys, os, csv, statistics as st
d, s0, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
BIG = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', 'オーストリア']
REC = ['清', '日本']
tot_ny = 0; chg_ny = 0; ever = {}; first_year = {}
rows1913 = []
for s in range(s0, s0 + n):
    code = {}
    with open(os.path.join(d, f'scalars_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); iy, inn, ic = h.index('year'), h.index('nation'), h.index('womenRightsCode')
        for x in r:
            code[(x[inn], int(x[iy]))] = int(float(x[ic]))   # 年の最後の四半期が残る
    mn = {}; sm = {}
    if not os.path.exists(os.path.join(d, f'lwk_s{s}.tsv')):   # lwk_s の無い生（ヌル腕・a11）＝法の段だけを数える
        lawny = sum(1 for v in code.values() if v >= 1); print(f'# 種 {s} lwk_s なし・法の段≧1 の国×年 {lawny}/{len(code)}'); continue
    with open(os.path.join(d, f'lwk_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); iy, inn, ip = h.index('year'), h.index('nation'), h.index('partShare')
        for x in r:
            k = (x[inn], int(x[iy])); v = float(x[ip])
            mn[k] = min(mn.get(k, 9), v); a = sm.setdefault(k, [0.0, 0]); a[0] += v; a[1] += 1
    for k, v in mn.items():
        tot_ny += 1
        if v > 0.4 + 1e-6:
            chg_ny += 1; ever.setdefault(k[0], set()).add(s)
            first_year[(s, k[0])] = min(first_year.get((s, k[0]), 9999), k[1])
    for nat in BIG + REC:
        k = (nat, 1913)
        if k in mn:
            rows1913.append((s, nat, code.get(k, -1), mn[k], sm[k][0] / sm[k][1], (mn[k] - 0.4) / 0.3))
        else:
            rows1913.append((s, nat, code.get(k, -1), float('nan'), float('nan'), float('nan')))
print(f'# 生 {d} 種 {s0}..{s0+n-1}')
print(f'国×年の数 {tot_ny}・構造の参加率＞0.4（旗 1 で変わる）{chg_ny}（{chg_ny/max(tot_ny,1):.3f}）')
print(f'構造の参加率＞0.4 になった国の数（どれかの種）{len(ever)}')
for nat in sorted(ever, key=lambda z: -len(ever[z])):
    fy = [first_year[(s, nat)] for s in ever[nat]]
    print(f'  {nat}\t種 {len(ever[nat])}/{n}\t最初の年の中央 {st.median(fy):.0f}')
print('1913 年：種\t国\t法の段\t参加率の最小\t参加率の平均\t賃金保持の近似')
for t in rows1913:
    print('\t'.join([str(t[0]), t[1], str(t[2])] + [f'{v:.3f}' for v in t[3:]]))
for nat in BIG + REC:
    xs = [t for t in rows1913 if t[1] == nat and t[3] == t[3]]
    if xs:
        print(f'中央 {nat}\t法の段≧1 {sum(1 for t in xs if t[2] >= 1)}/{len(xs)}\t最小 {st.median([t[3] for t in xs]):.3f}\t平均 {st.median([t[4] for t in xs]):.3f}\t旗 1 の構造 0.400＝差の近似 {st.median([t[3]-0.4 for t in xs]):.3f}')
