# (943) c83ic 紙の台本と門の読み手：矯正講和の王冠を〈敗者の講和の前の形〉から取る（旗 ImposeCrownFromLoser）。
# 使い方: python paper943.py <生のフォルダ>... [--mut M0|NEW|M1..M6]
#   各フォルダの imp_s<seed>.tsv・crownpc_s<seed>.tsv を読む。エンジンの関数を呼ばない（政体の位置・写し替えの表・ApplyRegime の王冠の書き方は台本が自分で書く）。
#   M0＝生そのもの（旧の盤面でも新の盤面でも、腕の生の門の値はこれで読む）。NEW と M1〜M6＝旧の生の矯正の列を、その形で紙の上で辿った予言。
# 門（事前登録 measurements/m943_20261107_prereg_ic.txt）：君主系（絶対・専制・幕府）の勝者の矯正の行で
#   G1  後の王冠が〈月の頭の王冠〉でも〈絶対〉でもない件
#   G1s 後の王冠が写し替えの表（無→絶対・他はそのまま）の王冠と違う件
#   G3  月の頭の王冠が専制で、後が絶対の件
#   GC  同じ月の同じ敗者の 2 本目以降で、後の王冠が写し替えの表と違う件（連鎖の門）
#   GR  矯正の四半期とその次の四半期に、敗者の国で君主系→別の君主系の再ラベル（crownpc_s の relabelFlag）が立った件
#   G2  矯正の件数（10 種の和）
# 月の頭の王冠＝その月の最初の矯正の行の loserCrownBeforeCode。⚠あいまいな四半期（crownpc_s のその四半期の crownPrevCode が最初の矯正の前の王冠と違う
#   ＝同じ四半期の中で矯正より前に別の書き手が王冠を書いた）は G1・G1s・G3・GC から外して件数と名を出す（旧の生 4 腕で 1,599 四半期のうち 1 件）。
# 限界（自己申告）：①imp_s は門を通った矯正だけを持つ＝紙の予言は旧の生で門を通った列を辿るだけ（新で途中の政体が変わって新たに門を通る講和は見えない）。
#   ②GR の旧の生の値は 0 しか出せない（形 (ii) の誤りは旧の生では起きない）＝M5 の予言は〈後の政体と後の王冠の食い違い〉の件数で数える。
import sys, os, glob, collections

POS = {0: 0.0, 2: 0.0, 1: 0.15, 7: 0.2, 5: 0.35, 3: 0.45, 4: 0.60, 6: 1.0, 8: 0.50}   # 政体の符号（Model.cs の列挙の順）→ イデオロギー位置
GAP = 0.4
MONARCH = {0, 1, 2}           # 絶対・専制・幕府
CROWN_TO_POL = {0: 0, 1: 0, 2: 1, 3: 2}   # 王冠 None→絶対・絶対→絶対・専制→専制・幕府→幕府
POL_TO_CROWN = {0: 1, 1: 2, 2: 3}
CNAME = {0: '無', 1: '絶対', 2: '専制', 3: '幕府'}

def mapped_crown(head): return 1 if head == 0 else head

def crown_after(pol_target, crown_now):
    if pol_target in (4, 6): return 0
    if pol_target == 3: return 1 if crown_now == 0 else crown_now
    if pol_target in MONARCH: return POL_TO_CROWN[pol_target]
    return crown_now

def read_tsv(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for ln in f:
            yield dict(zip(h, ln.rstrip('\n').split('\t')))

def simulate(rows, mut):
    """戻り値＝1 件ごとの (勝者の政体, 前の王冠, 後の王冠, 押し付けた政体 or None/'skip', 月の頭の王冠, 順番, 後の政体と王冠の食い違い)"""
    out = []
    head = int(rows[0]['loserCrownBeforeCode'])
    pol = int(rows[0]['loserPolityBeforeCode'])
    crown = head
    for k, r in enumerate(rows):
        W = int(r['winnerPolityCode'])
        if mut == 'M0':
            out.append((W, int(r['loserCrownBeforeCode']), int(r['loserCrownAfterCode']), int(r['imposedPolityCode']), head, k + 1, False)); continue
        if not (W != pol and abs(POS[W] - POS[pol]) >= GAP):
            out.append((W, crown, crown, None, head, k + 1, False)); continue
        if W in MONARCH:
            if mut == 'M1': tgt = W
            elif mut == 'M2': tgt = 0
            elif mut == 'M3': out.append((W, crown, crown, None, head, k + 1, False)); continue
            elif mut == 'M4': tgt = 0 if head == 0 else W
            elif mut == 'M6': tgt = CROWN_TO_POL[crown]
            else: tgt = CROWN_TO_POL[head]
            if mut == 'M5':
                c2 = mapped_crown(head)
                out.append((W, crown, c2, W, head, k + 1, POL_TO_CROWN[W] != c2)); pol = W; crown = c2; continue
            if tgt == pol:
                out.append((W, crown, crown, 'skip', head, k + 1, False)); continue
        else:
            tgt = W
        c2 = crown_after(tgt, crown)
        out.append((W, crown, c2, tgt, head, k + 1, False)); pol = tgt; crown = c2
    return out

def main():
    args = sys.argv[1:]; mut = 'M0'
    if '--mut' in args:
        i = args.index('--mut'); mut = args[i + 1]; del args[i:i + 2]
    print(f'# paper943 mut={mut}')
    for d in args:
        seeds = sorted(int(os.path.basename(p)[5:-4]) for p in glob.glob(os.path.join(d, 'imp_s*.tsv')))
        tot = collections.Counter(); per_seed_n = []; trans = collections.Counter(); amb_names = []; viol = []
        for s in seeds:
            rows = list(read_tsv(os.path.join(d, f'imp_s{s}.tsv')))
            grp = collections.OrderedDict()
            for r in rows: grp.setdefault((r['month'], r['loser']), []).append(r)
            for v in grp.values(): v.sort(key=lambda r: int(r['orderInMonthIndex']))
            # crownpc_s：矯正の四半期の crownPrevCode（あいまいさ）と、矯正の四半期・次の四半期の再ラベル
            firstq = {}
            for (m, loser), v in grp.items():
                k = (int(m) // 3, loser)
                if k not in firstq or int(m) < firstq[k][0]: firstq[k] = (int(m), v[0]['loserCrownBeforeCode'])
            cpinfo = {}
            cp = os.path.join(d, f'crownpc_s{s}.tsv')
            want = {(q + dq, l) for (q, l) in firstq for dq in (0, 1)}
            if os.path.exists(cp):
                for r in read_tsv(cp):
                    key = (int(r['quarter']), r['nation'])
                    if key in want: cpinfo[key] = r
                    if mut == 'M0' and r['nation'] == 'イギリス':
                        if r['polityCode'] == '1': tot['rec_UK_despoticQ'] += 1
                        if r['crownCode'] == '2': tot['rec_UK_crownDespQ'] += 1
                        if r['polityPrevCode'] == '3' and r['polityCode'] in ('0', '1', '2'): tot['rec_UK_constToMonarch'] += 1
            amb = {k for k, (m, cb) in firstq.items() if k in cpinfo and cpinfo[k]['crownPrevCode'] != cb}
            n_imp = 0; impq = set()
            for (m, loser), v in grp.items():
                q = int(m) // 3
                if len(v) >= 2: tot['chainGroups'] += 1
                isamb = (q, loser) in amb
                for (W, cb, ca, tgt, head, order, mism) in simulate(v, mut):
                    if tgt is None: tot['gateClosedOnPaper'] += 1; continue
                    if tgt == 'skip': tot['skip'] += 1; continue
                    n_imp += 1; impq.add((q, loser))
                    if mism: tot['M5_mismatch'] += 1
                    if W not in MONARCH: continue
                    tot['monarchWinner'] += 1
                    if isamb: tot['ambiguousRows'] += 1; amb_names.append(f's{s}:m{m}:{loser}'); continue
                    trans[(head, ca)] += 1
                    if ca != head and ca != 1: tot['G1'] += 1; viol.append(('G1', s, m, loser, CNAME[head], CNAME[ca]))
                    if ca != mapped_crown(head): tot['G1s'] += 1; viol.append(('G1s', s, m, loser, CNAME[head], CNAME[ca]))
                    if head == 2 and ca == 1: tot['G3'] += 1
                    if order >= 2 and ca != mapped_crown(head): tot['GC'] += 1
                    if head == 1 and ca == 2: tot['absToDesp'] += 1; tot[f'absToDesp_order{"2+" if order >= 2 else "1"}'] += 1
            for (q, l) in impq:
                for dq in (0, 1):
                    r = cpinfo.get((q + dq, l))
                    if r and r.get('relabelFlag') == '1':
                        tot['rec_relabelAfterImp'] += 1
                        if mut == 'M0' and r['polityPrevCode'] in ('0', '1', '2') and r['polityCode'] in ('0', '1', '2') and r['polityPrevCode'] != r['polityCode']:
                            tot['GR'] += 1; viol.append(('GR', s, q + dq, l, r['polityPrevCode'], r['polityCode']))
            per_seed_n.append(n_imp); tot['G2'] += n_imp
        for k in ('G1', 'G1s', 'G3', 'GC', 'GR', 'G2'): tot.setdefault(k, 0)
        print(f'{d}\tseeds={len(seeds)}\timpPerSeed={per_seed_n}')
        print('  gates: ' + '  '.join(f'{k}={tot[k]}' for k in ('G1', 'G1s', 'G3', 'GC', 'GR', 'G2')))
        print('  other: ' + '  '.join(f'{k}={v}' for k, v in sorted(tot.items()) if k not in ('G1', 'G1s', 'G3', 'GC', 'GR', 'G2')))
        print('  trans(head->after,monarchWinner): ' + ' '.join(f'{CNAME[a]}->{CNAME[b]}:{n}' for (a, b), n in sorted(trans.items())))
        if amb_names: print('  ambiguous(excluded): ' + ' '.join(amb_names[:10]))
        if viol and mut == 'M0': print('  violations(first 8): ' + ' | '.join(map(str, viol[:8])))

if __name__ == '__main__':
    main()
