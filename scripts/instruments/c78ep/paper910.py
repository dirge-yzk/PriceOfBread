# (910) c78ep 紙の台本（読むだけ・旗 0 の生だけ）：案 B5〈相続の前に、人の居ない POP の債務を返す〉の門 G1′・G2 の旗 0 の値と、
#   〈正しい B5 でも返し切れずに持ち越す行〉の上限の紙（反証役の訂正 1）を取る。
#   python -X utf8 paper910.py <名>=<ins フォルダ> ... [--seeds 1 10]
# 定義（事前登録 measurements/m910_20261106_prereg_ep.txt と同じ）
#   空の商人の行＝cpop_s（植民地格の州だけ）の className＝商人・popK ≦1e-12。
#   G1′ の行＝空の商人の行で debt − inCredit ＞ 1e-9×max(1, debt)（前の四半期から債務を持ち越した行）。比＝G1′ の行 ÷ 空の商人の行。
#   紙の持ち越しの上限 C＝空の商人の行で debt ＞0 かつ inMarket ＜ debt の行（四半期の所得の全てを返しに回しても旗 0 の債務に届かない行）。
#     ＝B5 では月ごとに〈所得で返す→返し切れば次の手形〉なので、四半期の末に持ち越すには四半期の所得の和が持ち越した債務に届かないことが要る（必要条件）。
#     旗 0 の所得は輸入が止まった軌道の所得＝B5 の軌道（輸入が開き、売った代金が入る）より小さい向き＝C は上限の向き（推定・旗 1 の軌道は計っていない）。
#     比＝C ÷ 空の商人の行。
#   G2＝cearn_s の植民地格の州×四半期で importPayerKind＝2 の行のうち importPaid ＞0 の割合。
#   G2 の紙＝(1 − 紙の持ち越しの比) × 商人の居る植民地格の州（importPayerKind＝1）の importPaid ＞0 の割合。
#     持ち越した四半期は手形が出ない＝輸入 0（予算＝貯蓄＝0）・それ以外の四半期は人の居る商人と同じ割合で輸入する、という近似。
import sys, os
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
print('名\t空の商人の行\tうち debt＞0\tG1′ の行\tG1′ の比\t紙の持ち越し C\tC の比\tC÷debt＞0\tkind2 の行\tkind2 で paid＞0\tG2\tkind1 の行\tkind1 で paid＞0\tkind1 の割合\tG2 の紙\t負の debt の行\t人の居る商人の行\tその持ち越しの行\t人の居る商人の持ち越しの比\t種')
for tag, d in [a.split('=', 1) for a in args]:
    emp = empd = g1 = car = neg = lv = lvc = 0; k2 = k2p = k1 = k1p = 0; seeds = 0
    for s in range(s0, s1 + 1):
        p = os.path.join(d, f'cpop_s{s}.tsv'); pc = os.path.join(d, f'cearn_s{s}.tsv')
        if not (os.path.exists(p) and os.path.exists(pc)): print(f'# {tag} 種 {s} 表なし'); continue
        seeds += 1
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            icl, ipk, idb, imk, icr = ix['className'], ix['popK'], ix['debt'], ix['inMarketPerQuarter'], ix['inCreditPerQuarter']
            for line in f:
                x = line.rstrip('\n').split('\t')
                db = fl(x[idb])
                if db < -1e-9: neg += 1
                if x[icl] != '商人': continue
                pk = fl(x[ipk])
                if pk > DEAD:
                    lv += 1
                    if db - fl(x[icr]) > 1e-9 * max(1.0, db): lvc += 1   # 紙の 2：人の居る商人（自分の消費がある＝返しは遅い向き）の持ち越しの比
                    continue
                emp += 1
                if db > 0:
                    empd += 1
                    if fl(x[imk]) < db: car += 1
                if db - fl(x[icr]) > 1e-9 * max(1.0, db): g1 += 1
        with open(pc, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            it, ik, ip = ix['tier'], ix['importPayerKind'], ix['importPaidPerQuarter']
            for line in f:
                x = line.rstrip('\n').split('\t')
                if x[it] != 'Colonial': continue
                if x[ik] == '2':
                    k2 += 1
                    if fl(x[ip]) > 0: k2p += 1
                elif x[ik] == '1':
                    k1 += 1
                    if fl(x[ip]) > 0: k1p += 1
    r = lambda a, b: a / b if b else float('nan')
    cr = r(car, emp); k1r = r(k1p, k1)
    print(f'{tag}\t{emp}\t{empd}\t{g1}\t{r(g1, emp):.4f}\t{car}\t{cr:.4f}\t{r(car, empd):.4f}\t{k2}\t{k2p}\t{r(k2p, k2):.4f}\t{k1}\t{k1p}\t{k1r:.4f}\t{(1 - cr) * k1r:.4f}\t{neg}\t{lv}\t{lvc}\t{r(lvc, lv):.4f}\t{seeds}')
