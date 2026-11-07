# (950) 読むだけ：(b) U2 の中身と、(a)〈相続人が受ける債務から輸入の手形の分を外す〉形の U2 の紙。エンジンの関数を呼ばない・旗を読まない。
#   U2 の定義は gate936.py のまま（cpop_s の州×四半期で、空の商人の行〔商人・popK ≦1e-12〕があり人の居る人数の和 ＞1e-9 の物が分母、人の居る POP の末の債務の和 ＞1e-9 が分子）。
#   分け方：分子の州×四半期を 州の格（cearn_s の tier）・10 年刻み・債務を持つ人の居る POP の階級（行の数と債務の額）で数える。
#   移った額（cearn_s の estateDebtMovedPerQuarter）を、同じ州×四半期の人の居る POP の人数の比（DebtToHeirs の配り方）で階級へ割り、格・10 年で足す。
#   損：同じ州×四半期の 相続人が受けた債務（moved）－空の商人の貯蓄の相続（cpop の空の商人の estateOutPerQuarter）＝相続人の持ち出し（輸入の代金のうち売上で戻らなかった分の目安・記録）。
#   移った債務の出所：空の商人の inCreditPerQuarter（手形）の和と moved の和を並べる（手形の分の割合）。
#   紙 U2(a)：分母は同じ。分子＝人の居る POP の末の債務の和＋空の商人以外の空の POP の末の債務の和 ＞1e-9（空の商人の債務＝輸入の手形は動かさない・他の空の POP の債務は相続人へ移る）。
#     旗 0 の盤面（基準・ヌル腕）で数えると〈(a) の形の 1 回目の近似〉＝軌道の帰還を含まない。旗 1 の盤面（腕）では 空の POP の債務は既に 0＝分子は 人の居る POP の債務から moved の手形の分を引けない（記録）。
#   python -X utf8 -I r950_u2.py <名>=<ins フォルダ> ... [--seeds 1 10]
import sys, os
args = sys.argv[1:]; s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def nz(v): return v if v == v else 0.0
for tag, d in [a.split('=', 1) for a in args]:
    T = dict(u2d=0, u2n=0, u2a=0, mv=0.0, mcred=0.0, mest=0.0, emD_m=0.0, emD_o=0.0, rowsMv=0)
    byTier = {}; byDec = {}; byCls = {}; mvCls = {}; mvTier = {}; mvDec = {}; lossTier = {}; lossDec = {}; emOcls = {}; lossReg = {}
    seeds = 0
    for s in range(s0, s1 + 1):
        pc = os.path.join(d, f'cearn_s{s}.tsv'); pp = os.path.join(d, f'cpop_s{s}.tsv')
        if not (os.path.exists(pc) and os.path.exists(pp)): print(f'# {tag} 種 {s} 表なし'); continue
        seeds += 1
        ce = {}
        with open(pc, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, ir, it, imv = ix['quarter'], ix['region'], ix['tier'], ix['estateDebtMovedPerQuarter']
            for line in f:
                x = line.rstrip('\n').split('\t'); ce[(x[iq], x[ir])] = (x[it], nz(fl(x[imv])))
        with open(pp, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, iy, ir, icl, ipk, icr, idb, ieo = ix['quarter'], ix['year'], ix['region'], ix['className'], ix['popK'], ix['inCreditPerQuarter'], ix['debt'], ix['estateOutPerQuarter']
            cur = None; acc = None
            def flush(acc, key):
                if acc is None: return
                tier, mv = ce.get(key, ('?', 0.0)); dec = (acc['y'] // 10) * 10
                if mv > 0:
                    T['mv'] += mv; T['rowsMv'] += 1; T['mcred'] += acc['mcred']; T['mest'] += acc['mest']
                    mvTier[tier] = mvTier.get(tier, 0.0) + mv; mvDec[dec] = mvDec.get(dec, 0.0) + mv
                    loss = mv - acc['mest']; lossTier[tier] = lossTier.get(tier, 0.0) + loss; lossDec[dec] = lossDec.get(dec, 0.0) + loss
                    lr = lossReg.setdefault((s, key[1]), [0.0, 0.0, 0]); lr[0] += mv; lr[1] += acc['mest']; lr[2] += 1
                    if acc['live'] > 0:
                        for c, sz in acc['sz'].items(): mvCls[c] = mvCls.get(c, 0.0) + mv * sz / acc['live']
                if not acc['em'] or not (acc['live'] > 1e-9): return
                T['u2d'] += 1
                if acc['ldebt'] > 1e-9:
                    T['u2n'] += 1
                    byTier[tier] = byTier.get(tier, 0) + 1; byDec[dec] = byDec.get(dec, 0) + 1
                    for c, v in acc['cd'].items():
                        b = byCls.setdefault(c, [0, 0.0]); b[0] += 1; b[1] += v
                if acc['ldebt'] + acc['emDo'] > 1e-9: T['u2a'] += 1
            for line in f:
                x = line.rstrip('\n').split('\t')
                k = (x[iq], x[ir])
                if k != cur:
                    flush(acc, cur); cur = k; acc = {'em': False, 'live': 0.0, 'ldebt': 0.0, 'cd': {}, 'sz': {}, 'mcred': 0.0, 'mest': 0.0, 'emDo': 0.0, 'y': int(x[iy])}
                pk = fl(x[ipk]); db = nz(fl(x[idb])); cl = x[icl]
                if pk > DEAD:
                    acc['live'] += pk; acc['sz'][cl] = acc['sz'].get(cl, 0.0) + pk
                    if db > 0: acc['ldebt'] += db
                    if db > 1e-9: acc['cd'][cl] = acc['cd'].get(cl, 0.0) + db
                elif cl == '商人':
                    acc['em'] = True; acc['mcred'] += max(0.0, nz(fl(x[icr]))); acc['mest'] += max(0.0, nz(fl(x[ieo])))
                    if db > 0: T['emD_m'] += db
                else:
                    if db > 0: acc['emDo'] += db; T['emD_o'] += db; emOcls[cl] = emOcls.get(cl, 0.0) + db
            flush(acc, cur)
    r = lambda a, b: a / b if b else float('nan')
    print(f'# 名 {tag}・フォルダ {d}・種 {seeds}')
    print(f'U2 分母 {T["u2d"]}・分子 {T["u2n"]}・U2 {r(T["u2n"], T["u2d"]):.6f}｜紙 U2(a)〔人の居る POP の債務＋空の非商人の債務〕分子 {T["u2a"]}・{r(T["u2a"], T["u2d"]):.6f}')
    print(f'Σ moved {T["mv"]:.6g}（＞0 の州×四半期 {T["rowsMv"]}）・同じ州×四半期の空の商人の inCredit（手形）{T["mcred"]:.6g}（moved に対し {r(T["mcred"], T["mv"]):.4f}）・空の商人の貯蓄の相続 {T["mest"]:.6g}')
    print(f'空の POP の末の債務の延べ（州×四半期の行の和）：商人 {T["emD_m"]:.6g}・商人以外 {T["emD_o"]:.6g}　商人以外の階級別 ' + '・'.join(f'{c}:{v:.4g}' for c, v in sorted(emOcls.items(), key=lambda t: -t[1])[:8]))
    print('分子の州の格 ' + '・'.join(f'{k}:{v}' for k, v in sorted(byTier.items(), key=lambda t: -t[1])))
    print('分子の 10 年 ' + '・'.join(f'{k}:{v}' for k, v in sorted(byDec.items())))
    print('分子の債務を持つ階級（行・額）' + '・'.join(f'{c}:{v[0]}/{v[1]:.4g}' for c, v in sorted(byCls.items(), key=lambda t: -t[1][1])[:10]))
    print('移った債務の格 ' + '・'.join(f'{k}:{v:.4g}' for k, v in sorted(mvTier.items(), key=lambda t: -t[1])))
    print('移った債務の 10 年 ' + '・'.join(f'{k}:{v:.4g}' for k, v in sorted(mvDec.items())))
    print('移った債務の相続人の階級（人数の比で割った額）' + '・'.join(f'{c}:{v:.4g}' for c, v in sorted(mvCls.items(), key=lambda t: -t[1])[:10]))
    print('持ち出し（moved－空の商人の貯蓄の相続）の格 ' + '・'.join(f'{k}:{v:.4g}' for k, v in sorted(lossTier.items(), key=lambda t: -t[1])))
    print('持ち出しの 10 年 ' + '・'.join(f'{k}:{v:.4g}' for k, v in sorted(lossDec.items())))
    pos = [(k, v) for k, v in lossReg.items() if v[0] - v[1] > 0]
    print(f'種×州の全期間の和で 持ち出し ＞0（moved ＞空の商人の貯蓄の相続）の州 {len(pos)}／{len(lossReg)}・その持ち出しの和 {sum(v[0]-v[1] for k, v in pos):.4g}・その州の moved の和 {sum(v[0] for k, v in pos):.4g}')
    print('持ち出しの上位（種・州・moved・空の商人の貯蓄の相続・四半期の数）' + '・'.join(f'{k[0]}:{k[1]}:{v[0]:.3g}/{v[1]:.3g}/{v[2]}' for k, v in sorted(pos, key=lambda t: -(t[1][0]-t[1][1]))[:12]))
