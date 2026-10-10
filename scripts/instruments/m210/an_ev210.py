"""(210) 読み手（読むだけ）＝不履行が起きた／起きなくなった国×四半期と、その後の正統性・政体・内戦・領土・国力の差（不履行による正統性の打撃の経路）。
使い方：PYTHONUTF8=1 python an_ev210.py <腕Aの d210> <腕Bの d210> <種の起点> <種の数> [ラベルA ラベルB]
 ① 分かれ目の自然実験＝種ごとに最初に食い違う不履行（それまで2つの世界はビット単位で同じ）＝その国の +4・+20・+40・+80 四半期の状態を2つの腕で並べる
 ② 不履行の組み合わせ＝同じ種・同じ国で ±8 四半期以内に両方の腕にあるか（両方／A だけ／B だけ）・時代ごと
 ③ 腕の中の出来事の研究＝不履行の前（−1 四半期）と後（+4・+20）の正統性・導出政体が変わった割合・内戦に入った割合・州の数の変化・国力の順位
"""
import sys, os, collections, statistics as st

PN = ['絶対', '専制', '幕府', '立憲', '共和', 'ブル独', 'プロ独', '軍政', '無政府']


def load(d, s):
    N = {}   # (q, idx) -> row
    D = []
    rank = collections.defaultdict(dict)   # q -> idx -> 順位
    byq = collections.defaultdict(list)
    with open(os.path.join(d, f'd{s}.txt'), encoding='utf-8') as f:
        for line in f:
            if line[0] == 'N':
                t = line.rstrip('\n').split('|'); q = int(t[1])
                N[(q, t[2])] = t; byq[q].append((float(t[16]), t[2]))
            elif line[0] == 'D':
                t = line.rstrip('\n').split('|'); D.append((int(t[1]), t[2], t[4], int(t[3])))
    for q, lst in byq.items():
        for r, (_, i) in enumerate(sorted(lst, reverse=True)): rank[q][i] = r + 1
    return N, D, rank


def st_of(N, rank, q, idx):
    t = N.get((q, idx))
    if t is None: return None
    return dict(leg=float(t[9]), pol=int(t[5]), cw=int(t[12]), reg=int(t[14]), rank=rank[q].get(idx, 0), suz=t[6], ex=int(t[7]), debt=t[17], dc=int(t[18]))


def fmt(x):
    if x is None: return '—'
    return f'正{x["leg"]:.2f}・{PN[x["pol"]]}・内戦{x["cw"]}・州{x["reg"]}・順位{x["rank"]}' + (f'・宗主{x["suz"]}' if x['suz'] else '')


def main():
    da, db, s0, ns = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
    la, lb = (sys.argv[5], sys.argv[6]) if len(sys.argv) > 6 else ('A', 'B')
    pairs = collections.Counter(); peri = collections.defaultdict(collections.Counter)
    study = {la: collections.defaultdict(list), lb: collections.defaultdict(list)}
    print(f'## ① 分かれ目の自然実験（{la}／{lb}・最初に食い違う不履行の国の状態）')
    for s in range(s0, s0 + ns):
        Na, Da, Ra = load(da, s); Nb, Db, Rb = load(db, s)
        # ① 最初の食い違い
        first = None
        for x, y in zip(Da + [None] * 999, Db + [None] * 999):
            if x != y: first = (x, y); break
        if first:
            x, y = first
            # 早い方
            ev = min([e for e in (x, y) if e is not None], key=lambda e: e[0])
            who = la if ev == x else lb
            q0, idx, nat = ev[0], ev[1], ev[2]
            line = [f'seed{s}: {nat} {ev[3]}年（q {q0}）の不履行は {who} だけ']
            for dq in (4, 20, 40, 80):
                q = q0 + dq
                line.append(f'  +{dq}: {la} {fmt(st_of(Na, Ra, q, idx))}｜{lb} {fmt(st_of(Nb, Rb, q, idx))}')
            print('\n'.join(line))
        # ② 組み合わせ
        def key(D): return [(q, nat) for q, _, nat, _ in D]
        A = key(Da); B = key(Db); usedB = set()
        for q, nat in A:
            m = next((j for j, (qb, nb) in enumerate(B) if j not in usedB and nb == nat and abs(qb - q) <= 8), None)
            era = '1783-1815' if q < 132 else '1816-1849' if q < 268 else '1850-1913' if q < 524 else '1914-1950'
            if m is None: pairs[f'{la}だけ'] += 1; peri[era][f'{la}だけ'] += 1
            else: usedB.add(m); pairs['両方'] += 1; peri[era]['両方'] += 1
        for j, (q, nat) in enumerate(B):
            if j in usedB: continue
            era = '1783-1815' if q < 132 else '1816-1849' if q < 268 else '1850-1913' if q < 524 else '1914-1950'
            pairs[f'{lb}だけ'] += 1; peri[era][f'{lb}だけ'] += 1
        # ③ 出来事の研究
        for lab, N, D, R in ((la, Na, Da, Ra), (lb, Nb, Db, Rb)):
            for q0, idx, nat, y in D:
                pre = st_of(N, R, q0 - 1, idx)
                if pre is None: continue
                for dq in (0, 4, 20):
                    post = st_of(N, R, q0 + dq, idx)
                    if post is None: continue
                    study[lab][dq].append((pre, post))
                # 20 四半期の中で内戦に入ったか・政体が変わったか
                cw = any((st_of(N, R, q0 + k, idx) or {}).get('cw', 0) for k in range(0, 21))
                pc = any((st_of(N, R, q0 + k, idx) or {'pol': pre['pol']})['pol'] != pre['pol'] for k in range(0, 21))
                study[lab]['cw20'].append(cw); study[lab]['pc20'].append(pc)
                # 対照＝同じ国の、不履行の無い20四半期の窓（不履行の前 −40〜−20）で政体が変わった割合・内戦
                if q0 >= 41:
                    cwb = any((st_of(N, R, q0 - 40 + k, idx) or {}).get('cw', 0) for k in range(0, 21))
                    b0 = st_of(N, R, q0 - 41, idx)
                    if b0 is not None:
                        pcb = any((st_of(N, R, q0 - 40 + k, idx) or {'pol': b0['pol']})['pol'] != b0['pol'] for k in range(0, 21))
                        study[lab]['cwb'].append(cwb); study[lab]['pcb'].append(pcb)
    print()
    print('## ② 不履行の組み合わせ（同じ種・同じ国・±8 四半期）')
    print('全期間＝' + '・'.join(f'{k} {v}' for k, v in pairs.most_common()))
    for era in ('1783-1815', '1816-1849', '1850-1913', '1914-1950'):
        print(f'  {era}＝' + '・'.join(f'{k} {v}' for k, v in peri[era].most_common()))
    print()
    print('## ③ 腕の中の出来事の研究（不履行の前 −1 四半期に対して）')
    for lab in (la, lb):
        S = study[lab]
        if not S[0]: continue
        print(f'{lab}（不履行 {len(S[0])}）:')
        for dq in (0, 4, 20):
            L = S[dq]
            if not L: continue
            print(f'  +{dq}: 正統性 {st.mean(a["leg"] for a, b in L):.3f}→{st.mean(b["leg"] for a, b in L):.3f}・導出政体が変わった {sum(1 for a, b in L if a["pol"] != b["pol"])}/{len(L)}・内戦 {sum(1 for a, b in L if b["cw"])}/{len(L)}・州の数の変化の和 {sum(b["reg"] - a["reg"] for a, b in L):+d}・順位の変化の平均 {st.mean(b["rank"] - a["rank"] for a, b in L):+.2f}')
        print(f'  20 四半期の中で 内戦に入った {sum(S["cw20"])}/{len(S["cw20"])}（対照＝同じ国の不履行の前の窓 {sum(S["cwb"])}/{len(S["cwb"])}）・導出政体が変わった {sum(S["pc20"])}/{len(S["pc20"])}（対照 {sum(S["pcb"])}/{len(S["pcb"])}）')


main()
