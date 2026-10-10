"""(210) 読み手（読むだけ）＝(205) の exact −4.6 の帰属。計器 inj_d210 の d<seed>.txt を2つの腕（r202・r207）で同じ種どうし比べる。
使い方：PYTHONUTF8=1 python an_d210.py <腕Aの d210 フォルダ> <腕Bの d210 フォルダ> <種の起点> <種の数> [ラベルA ラベルB]
 ① exact の再現（Q524＝q 523 の N 行・表の国ごとに序列の最も高い同名の国＝Evaluate の Find と同じ）＝帯の報告の「政体exact 分子/61」と突き合わせる
 ② 種ごとの分かれ目＝N 行の四半期ごとの要約が最初に食い違う四半期と、その直前の不履行（D 行）の食い違い
 ③ 1914 年より前の不履行（国ごと・腕ごと）・同じ種で起きた／起きなくなった国×四半期
 ④ exact の差を国×種の組に分け、通り道（自国の不履行の履歴が違う・宗主が違う・導出政体だけが違う）ごとに寄与（pt）を数える
 ⑤ 不履行の瞬間の効き（導出政体が変わった割合・正統性・議会の実効統制・文民制度）とその後（+4・+8・+20 四半期の導出政体が元へ戻った割合）
 ⑥ 1914 年の国の状態の差（腕ごとの平均＝正統性・内戦の四半期・不履行の回数・州の数）＝exact が変わった組と変わらない組
"""
import sys, os, hashlib, collections, statistics as st

PN = ['絶対', '専制', '幕府', '立憲', '共和', 'ブル独', 'プロ独', '軍政', '無政府']
FQ = 523  # Q524 の断面（ループの添字 q＝523 の後）


def load(d, seed):
    p = os.path.join(d, f'd{seed}.txt')
    N = collections.defaultdict(list)   # q -> rows
    H = {}                               # q -> hash
    D = []
    Q = []
    cur = None; h = None
    with open(p, encoding='utf-8') as f:
        for line in f:
            if line[0] == '#': continue
            t = line.rstrip('\n').split('|')
            k = t[0]
            if k == 'N':
                q = int(t[1])
                if q != cur:
                    if cur is not None: H[cur] = h.hexdigest()
                    cur = q; h = hashlib.md5()
                h.update(line.encode())
                if q == FQ or q % 4 == 3:
                    N[q].append(t)
            elif k == 'D':
                D.append(t)
            elif k == 'Q':
                Q.append(t)
    if cur is not None: H[cur] = h.hexdigest()
    return N, H, D, Q


def table_nations(rows):
    return rows


def at1914(N):
    """表の国ごとに序列の最も高い同名の国（ex≥0 の行）。"""
    best = {}
    for t in N.get(FQ, []):
        ex = int(t[7])
        if ex < 0: continue
        base = t[3]; sc = float(t[16])
        if base not in best or sc > float(best[base][16]): best[base] = t
    return best


def main():
    da, db, s0, ns = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
    la, lb = (sys.argv[5], sys.argv[6]) if len(sys.argv) > 6 else ('A', 'B')
    seeds = list(range(s0, s0 + ns))
    ex = {la: collections.defaultdict(dict), lb: collections.defaultdict(dict)}   # nation -> seed -> row
    divq = {}
    dsum = {la: [], lb: []}
    cls = collections.Counter(); clsd = collections.defaultdict(float); pairs = []
    flip = {la: collections.Counter(), lb: collections.Counter()}
    after = {la: collections.Counter(), lb: collections.Counter()}
    eff = {la: [], lb: []}
    for s in seeds:
        Na, Ha, Da, Qa = load(da, s)
        Nb, Hb, Db, Qb = load(db, s)
        A, B = at1914(Na), at1914(Nb)
        for nat, t in A.items(): ex[la][nat][s] = t
        for nat, t in B.items(): ex[lb][nat][s] = t
        # ② 分かれ目
        dq = None
        for q in sorted(set(Ha) | set(Hb)):
            if Ha.get(q) != Hb.get(q): dq = q; break
        dla = [(int(t[1]), t[4], t[3]) for t in Da]; dlb = [(int(t[1]), t[4], t[3]) for t in Db]
        first = None
        for x, y in zip(dla + [None] * 999, dlb + [None] * 999):
            if x != y: first = (x, y); break
        divq[s] = (dq, first)
        # ③ 1914 より前の不履行
        for lab, Dl in ((la, Da), (lb, Db)):
            for t in Dl:
                dsum[lab].append((s, int(t[1]), int(t[3]), t[4]))
        def defs(Dl, nat):
            return sorted(int(t[1]) for t in Dl if t[4] == nat and int(t[1]) <= FQ)
        # 宗主の主（宗主の不履行も通り道）
        # ④ 組ごと
        for nat in sorted(set(A) | set(B)):
            if nat not in A or nat not in B: continue
            ta, tb = A[nat], B[nat]
            xa, xb = int(ta[7]), int(tb[7])
            own_a, own_b = defs(Da, nat), defs(Db, nat)
            suz_a, suz_b = ta[6], tb[6]
            dera, derb = int(ta[5]), int(tb[5])
            if dq is None or dq > FQ: c = '分かれ目が1914より後'
            elif own_a != own_b: c = '自国の不履行の履歴が違う'
            elif (suz_a != '') != (suz_b != '') or suz_a != suz_b: c = '宗主が違う（自国の不履行は同じ）'
            elif dera != derb: c = '導出政体だけが違う（自国の不履行も宗主も同じ）'
            else: c = '同じ'
            cls[c] += 1; clsd[c] += (xb - xa)
            pairs.append((s, nat, c, xa, xb, len(own_a), len(own_b), suz_a, suz_b, PN[dera], PN[derb],
                          float(ta[9]), float(tb[9]), int(ta[18]), int(tb[18])))
        # ⑤ 不履行の瞬間とその後
        for lab, Dl, Nn in ((la, Da, Na), (lb, Db, Nb)):
            for t in Dl:
                q = int(t[1]); pre, post = int(t[7]), int(t[8])
                flip[lab]['n'] += 1
                if pre != post: flip[lab]['flip'] += 1; flip[lab][f'{PN[pre]}→{PN[post]}'] += 1
                eff[lab].append((float(t[5]), float(t[6]), float(t[9]), float(t[10]), float(t[11]), float(t[12])))
                idx = t[2]
                for dqq in (4, 8, 20):
                    qq = q + dqq
                    qq = qq - (qq % 4) + 3 if qq % 4 != 3 else qq
                    row = next((r for r in Nn.get(qq, []) if r[2] == idx), None)
                    if row is None: continue
                    after[lab][f'+{dqq}n'] += 1
                    if int(row[5]) == pre: after[lab][f'+{dqq}戻'] += 1
                    if int(row[5]) == post: after[lab][f'+{dqq}留'] += 1
    # ① exact の再現
    print('## ① exact の再現（Q524・表の国ごと・序列の最も高い同名の国）')
    for lab in (la, lb):
        nats = [n for n in ex[lab] if ex[lab][n]]
        tot = sum(sum(int(t[7]) for t in ex[lab][n].values()) / len(ex[lab][n]) for n in nats)
        totn = sum(sum(int(t[8]) for t in ex[lab][n].values()) / len(ex[lab][n]) for n in nats)
        print(f'{lab}: exact {tot:.6f}/{len(nats)}＝{100*tot/len(nats):.2f}%・near {totn:.6f}/{len(nats)}＝{100*totn/len(nats):.2f}%')
    print()
    print('## ② 種ごとの分かれ目（N 行の四半期の要約が最初に食い違う四半期・最初に食い違う不履行）')
    for s in seeds:
        dq, first = divq[s]
        yr = 1783 + (dq + 1) / 4 if dq is not None else None
        print(f'seed{s}: 分かれ目 q={dq}（{yr:.2f}年）・最初に食い違う不履行 {la}={first[0] if first else None} {lb}={first[1] if first else None}' if dq is not None else f'seed{s}: 分かれ目なし')
    print()
    print('## ③ 不履行の数（全ての国・1914 年より前 q≤523・腕ごと）')
    for lab in (la, lb):
        pre = [x for x in dsum[lab] if x[1] <= FQ]
        print(f'{lab}: 全期間 {len(dsum[lab])}・1914 年より前 {len(pre)}')
    print()
    print('## ④ exact の差の通り道（国×種の組・表の61国）')
    tot_pairs = sum(cls.values())
    nnat = len({p[1] for p in pairs})
    print(f'組 {tot_pairs}（国 {nnat}×種 {len(seeds)}）・exact の差＝Σ(Δex)÷(国の数×種の数)×100 pt')
    for c in sorted(cls, key=lambda k: -cls[k]):
        npair = cls[c]; dsumc = clsd[c]
        nchg = sum(1 for p in pairs if p[2] == c and p[3] != p[4])
        print(f'  {c}: 組 {npair}・exact が変わった組 {nchg}（↑{sum(1 for p in pairs if p[2]==c and p[4]>p[3])}・↓{sum(1 for p in pairs if p[2]==c and p[4]<p[3])}）・寄与 {100*dsumc/(nnat*len(seeds)):+.2f} pt')
    print()
    print('### ④-2 国ごとの exact の差（Δ が 0 でない国・↓が先）')
    byn = collections.defaultdict(list)
    for p in pairs: byn[p[1]].append(p)
    rows = []
    for nat, ps in byn.items():
        d = sum(p[4] - p[3] for p in ps)
        if d == 0: continue
        cc = collections.Counter(p[2] for p in ps if p[3] != p[4])
        rows.append((d, nat, len(ps), cc))
    for d, nat, n, cc in sorted(rows):
        print(f'  {nat}: {la}→{lb} Δ {d:+d}/{n}種・変わった組の通り道 ' + '・'.join(f'{k}{v}' for k, v in cc.most_common()))
    print()
    print('### ④-3 exact が変わった組の中身（種・国・通り道・ex・1914 年より前の自国の不履行の回数・宗主・導出政体・正統性・不履行の回数〔全期間の計数〕）')
    for p in sorted(pairs, key=lambda p: (p[1], p[0])):
        if p[3] == p[4]: continue
        print(f'  seed{p[0]} {p[1]}: {p[2]}・ex {p[3]}→{p[4]}・自国の不履行(1914前) {p[5]}→{p[6]}・宗主「{p[7]}」→「{p[8]}」・導出 {p[9]}→{p[10]}・正統性 {p[11]:.2f}→{p[12]:.2f}・計数 {p[13]}→{p[14]}')
    print()
    print('## ⑤ 不履行の瞬間の効き（D 行）とその後（N 行・同じ国の添字）')
    for lab in (la, lb):
        f = flip[lab]; n = f['n']
        if n == 0: print(f'{lab}: 不履行0'); continue
        e = eff[lab]
        print(f'{lab}: 不履行 {n}・導出政体が瞬間に変わった {f["flip"]}（{100*f["flip"]/n:.0f}%）・' + '・'.join(f'{k} {v}' for k, v in f.most_common() if '→' in k))
        print(f'   正統性 {st.mean(x[0] for x in e):.3f}→{st.mean(x[1] for x in e):.3f}・議会の実効統制 {st.mean(x[2] for x in e):.3f}→{st.mean(x[3] for x in e):.3f}・文民制度 {st.mean(x[4] for x in e):.3f}→{st.mean(x[5] for x in e):.3f}（平均）')
        a = after[lab]
        print('   その後の導出政体＝' + '・'.join(f'+{k}四半期 不履行の前と同じ {a[f"+{k}戻"]}/{a[f"+{k}n"]}・直後と同じ {a[f"+{k}留"]}/{a[f"+{k}n"]}' for k in (4, 8, 20)))
    print()
    print('## ⑥ 1914 年の状態（表の国・腕ごとの平均）＝exact が変わった組と変わらない組')
    for name, sel in (('変わった組', lambda p: p[3] != p[4]), ('変わらない組', lambda p: p[3] == p[4])):
        ps = [p for p in pairs if sel(p)]
        if not ps: continue
        print(f'  {name} {len(ps)}: 正統性 {st.mean(p[11] for p in ps):.3f}→{st.mean(p[12] for p in ps):.3f}・不履行の計数 {st.mean(p[13] for p in ps):.2f}→{st.mean(p[14] for p in ps):.2f}・宗主あり {sum(1 for p in ps if p[7])}→{sum(1 for p in ps if p[8])}')


main()
