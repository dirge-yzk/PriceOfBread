# (1228) c124bk 〈機構が狙いどおり働いたか〉の門（事前登録 4-4）と段 0 の数え。paysale_s を種ごとに和る。
#   使い方: python -X utf8 -I work_gate.py <ins のフォルダ> [種の始め=1] [種の終わり=10]
#   門 G1：Σ revBooked ÷ Σ payToProducers が 1±1e-9（種ごと）。門 G2：Σ(revBooked−costBooked) ÷ Σ rentPart が 1±1e-9（種ごと）。旗 0 は revBooked＝0＝落ちる。
#   段 0：財ごとの flipNegToPos・flipPosToNeg・stateFlipNegToPos・negOld・negNew・prodMonths（全ての種の和）。
import sys, os, collections
D = sys.argv[1]; S0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1; S1 = int(sys.argv[3]) if len(sys.argv) > 3 else 10
L = []; ok = True; good = collections.defaultdict(collections.Counter); kinds = collections.Counter(); seen = 0
for s in range(S0, S1 + 1):
    p = os.path.join(D, f'paysale_s{s}.tsv')
    if not os.path.exists(p): L.append(f'種 {s}: 表が無い'); ok = False; continue
    seen += 1; t = collections.Counter(); rows = 0
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            c = l.rstrip('\n').split('\t'); rows += 1
            v = lambda k: float(c[ix[k]])
            for k in ('payToProducers', 'payExport', 'payMint', 'payDomestic', 'rentPart', 'revBooked', 'costBooked'): t[k] += v(k)
            gk = good[c[ix['good']]]
            for k in ('prodMonths', 'negOld', 'negNew', 'flipNegToPos', 'flipPosToNeg', 'stateMonths', 'stateFlipNegToPos', 'payToProducers', 'rentPart'): gk[k] += v(k)
    for k in ('payExport', 'payMint', 'payDomestic'): kinds[k] += t[k]
    g1 = t['revBooked'] / t['payToProducers'] if t['payToProducers'] > 0 else float('nan')
    g2 = (t['revBooked'] - t['costBooked']) / t['rentPart'] if t['rentPart'] > 0 else float('nan')
    p1 = abs(g1 - 1) <= 1e-9; p2 = abs(g2 - 1) <= 1e-9; ok = ok and p1 and p2
    L.append(f'種 {s}: 行 {rows}・代金 {t["payToProducers"]:.6g}（輸出 {t["payExport"]:.4g}・造幣 {t["payMint"]:.4g}・国内 {t["payDomestic"]:.4g}）・G1 {g1:.12g} {"通る" if p1 else "落ちる"}・G2 {g2:.12g} {"通る" if p2 else "落ちる"}')
L.append(f'判定（G1 と G2・{seen} 種）: {"通る" if ok and seen == S1 - S0 + 1 else "落ちる"}')
L.append(f'代金の内訳（全ての種）：輸出 {kinds["payExport"]:.6g}・造幣 {kinds["payMint"]:.6g}・国内 {kinds["payDomestic"]:.6g}')
L.append('== 段 0：財ごと（全ての種の和・生産者×月）＝flipNegToPos の多い順 20')
L.append('財\tprodMonths\tnegOld\tnegNew\tflipNegToPos\tflipPosToNeg\tstateMonths\tstateFlipNegToPos\t代金\t地代')
for g, k in sorted(good.items(), key=lambda x: -x[1]['flipNegToPos'])[:20]:
    L.append('\t'.join([g] + [f'{k[x]:.0f}' for x in ('prodMonths', 'negOld', 'negNew', 'flipNegToPos', 'flipPosToNeg', 'stateMonths', 'stateFlipNegToPos')] + [f'{k["payToProducers"]:.4g}', f'{k["rentPart"]:.4g}']))
tot = collections.Counter()
for k in good.values(): tot.update(k)
L.append('計\t' + '\t'.join(f'{tot[x]:.0f}' for x in ('prodMonths', 'negOld', 'negNew', 'flipNegToPos', 'flipPosToNeg', 'stateMonths', 'stateFlipNegToPos')))
for g in ('砂糖', 'コーヒー', '金'):
    k = good.get(g)
    if k: L.append(f'{g}: 生産者×月 {k["prodMonths"]:.0f}・赤字 旧 {k["negOld"]:.0f}→新 {k["negNew"]:.0f}・負→正 {k["flipNegToPos"]:.0f}・正→負 {k["flipPosToNeg"]:.0f}・国営持分の負→正 {k["stateFlipNegToPos"]:.0f}')
print('\n'.join(L))
