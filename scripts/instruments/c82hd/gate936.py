# (936) c82hd 門の台本（読むだけ・エンジンの関数を呼ばない・旗を読まない・事前登録 measurements/m936_20261107_prereg_hd.txt [4] の定義をそのまま）
#   python -X utf8 -I gate936.py <名>=<ins フォルダ> ... [--seeds 1 10]
#   W1 ＝cearn_s の tier＝Colonial・importPayerKind＝2（人の居ない商人が払い手）の行のうち importPaidPerQuarter ＞0 の割合（gate910 の G2 と同じ定義）
#   W2 ＝cpop_s の空の商人の行（className＝商人・popK ≦1e-12）のうち inCreditPerQuarter ＞0 の割合（read917_dust の empty の〈全ての行で inCredit ＞0〉と同じ定義）
#   U1 ＝cearn_s の emptyDebtAfterEstate の和（相続の段の直後に人数 ≦1e-12 で債務 ≠0 の POP の数の月の和）
#   U2 ＝cpop_s の州×四半期のうち、空の商人の行があり人の居る人数の和 ＞1e-9 の物（分母）で、人の居る POP（popK ＞1e-12）の末の債務の和 ＞1e-9 の割合（記録）
#   記録：kind1 の輸入の割合（人の居る商人が払い手＝需要の頻度の目安）・Σ estateDebtMovedPerQuarter・Σ heirDebtSettledPerQuarter・Σ billBlockedClearings・
#         相続人の無い州×四半期で空の商人の末の債務 ＞0 の数・人の居る POP の塵の行（0＜debt≦1e-12）の数・種ごとの W1・W2
import sys, os
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
r = lambda a, b: a / b if b else float('nan')
for tag, d in [a.split('=', 1) for a in args]:
    T = dict(k2=0, k2p=0, k1=0, k1p=0, u1=0, u1rows=0, moved=0.0, settled=0.0, blocked=0, emp=0, empc=0, u2d=0, u2n=0, noheir=0, dust=0, liverows=0)
    per = []; seeds = 0
    for s in range(s0, s1 + 1):
        pc = os.path.join(d, f'cearn_s{s}.tsv'); pp = os.path.join(d, f'cpop_s{s}.tsv')
        if not (os.path.exists(pc) and os.path.exists(pp)): print(f'# {tag} 種 {s} 表なし'); continue
        seeds += 1
        a = dict(k2=0, k2p=0, emp=0, empc=0)
        with open(pc, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            it, ik, ip = ix['tier'], ix['importPayerKind'], ix['importPaidPerQuarter']
            iu1, imv, ihs, ibb = ix['emptyDebtAfterEstate'], ix['estateDebtMovedPerQuarter'], ix['heirDebtSettledPerQuarter'], ix['billBlockedClearings']
            for line in f:
                x = line.rstrip('\n').split('\t')
                if x[it] == 'Colonial':
                    if x[ik] == '2':
                        a['k2'] += 1
                        if fl(x[ip]) > 0: a['k2p'] += 1
                    elif x[ik] == '1':
                        T['k1'] += 1
                        if fl(x[ip]) > 0: T['k1p'] += 1
                n = int(x[iu1]); T['u1'] += n
                if n: T['u1rows'] += 1
                mv = fl(x[imv]); T['moved'] += mv if mv == mv else 0.0
                hs = fl(x[ihs]); T['settled'] += hs if hs == hs else 0.0
                T['blocked'] += int(x[ibb])
        with open(pp, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, ir, icl, ipk, icr, idb = ix['quarter'], ix['region'], ix['className'], ix['popK'], ix['inCreditPerQuarter'], ix['debt']
            cur = None; acc = None
            def flush(acc):
                if acc is None or not acc['em']: return
                if not (acc['live'] > 1e-9):
                    if acc['D'] > 0: T['noheir'] += 1
                    return
                T['u2d'] += 1
                if acc['ldebt'] > 1e-9: T['u2n'] += 1
            for line in f:
                x = line.rstrip('\n').split('\t')
                k = (x[iq], x[ir])
                if k != cur:
                    flush(acc); cur = k; acc = {'em': False, 'D': 0.0, 'live': 0.0, 'ldebt': 0.0}
                pk = fl(x[ipk]); db = fl(x[idb])
                if pk > DEAD:
                    acc['live'] += pk; acc['ldebt'] += max(0.0, db); T['liverows'] += 1
                    if 0 < db <= 1e-12 and x[icl] != '商人': T['dust'] += 1
                elif x[icl] == '商人':
                    acc['em'] = True; a['emp'] += 1
                    if db > 0: acc['D'] += db
                    if fl(x[icr]) > 0: a['empc'] += 1
            flush(acc)
        for k in ('k2', 'k2p', 'emp', 'empc'): T[k] += a[k]
        per.append((s, r(a['k2p'], a['k2']), r(a['empc'], a['emp'])))
    print(f'# 名 {tag}・フォルダ {d}・種 {seeds}')
    print(f'W1 kind2 の行 {T["k2"]}・paid＞0 {T["k2p"]}・W1 {r(T["k2p"], T["k2"]):.4f}')
    print(f'W2 空の商人の行 {T["emp"]}・inCredit＞0 {T["empc"]}・W2 {r(T["empc"], T["emp"]):.4f}')
    print(f'U1 Σ emptyDebtAfterEstate {T["u1"]}（＞0 の行 {T["u1rows"]}）')
    print(f'U2 分母 {T["u2d"]}・相続人の債務 ＞1e-9 {T["u2n"]}・U2 {r(T["u2n"], T["u2d"]):.6f}')
    print(f'記録 kind1 の行 {T["k1"]}・paid＞0 {T["k1p"]}（{r(T["k1p"], T["k1"]):.4f}）・Σ moved {T["moved"]:.6g}・Σ heirSettled {T["settled"]:.6g}・Σ billBlocked {T["blocked"]}・相続人なしで空の商人の債務＞0 {T["noheir"]}・人の居る非商人の塵の行 {T["dust"]}／{T["liverows"]}')
    print('種ごと（種・W1・W2）：' + '・'.join(f'{s}:{w1:.4f}/{w2:.4f}' for s, w1, w2 in per))
