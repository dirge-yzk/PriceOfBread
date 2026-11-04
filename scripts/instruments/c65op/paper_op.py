# (820) c65op 紙の台本：旗 0 の盤面（habitcap_s・habitbal_s）で形 H（ロジスティック）の門を取り直す（読むだけ）。
#   python -X utf8 -I scripts/instruments/c65op/paper_op.py <ins のフォルダ> [ラベル]
# 旗 0 の盤面の州×四半期の最後の月から 1 人あたり消費 pc を戻す：new（蓋なしの線形）= s + G*pc - D*s → pc = (new - s*(1-D))/G。
# 門の量（事前登録と同じ定義・旗 1 の腕では habitlg_s から同じ量を出す＝gate_op.py）：
#   L1 定常の州×四半期（その四半期と前 3 四半期の pc の最大÷最小 <= 1.25・平均の pc >= PCMIN）で |s - s_eq(pc)| <= TOL*s_eq の割合（s_eq = G*pc*d/(D*d + G*pc)）
#   L2 実際の在庫 >= 0.995*d の州×四半期（1 種あたり）
#   U  実際の在庫の最大・労働の床（害×在庫 >= 0.5）
# 旗 1 の近似の再生（--replay）：四半期ごとの sigma = pc/(B + s)（B=0.02・困窮の門 g=1 と置く＝近似）を一定として毎月 s を形 H で回す。
import sys, glob, os
from collections import defaultdict
G, D, CAP, B = 0.1, 0.02, 0.2, 0.02
PCMIN = 0.02          # 平均の pc の下限（線形の釣り合い G*pc/D が d/2 以上＝正帰還の効く州）
TOL = (0.3, 0.15)

def seq(pc):
    return G * pc * CAP / (D * CAP + G * pc) if pc > 0 else 0.0

def main():
    d = sys.argv[1]; lab = sys.argv[2] if len(sys.argv) > 2 else d
    caps = sorted(glob.glob(os.path.join(d, 'habitcap_s*.tsv')), key=lambda p: int(os.path.basename(p)[10:-4]))
    print('盤面', lab, '／habitcap_s', len(caps), '本')
    tot = defaultdict(float)
    for f in caps:
        seed = os.path.basename(f)[10:-4]
        fh = open(f, encoding='utf-8'); h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        ser = defaultdict(list)
        rows = pin = 0; umax = 0.0; floor = 0
        for line in fh:
            v = line.rstrip('\n').split('\t')
            q = int(v[ix['quarter']]); s = float(v[ix['stockBeforeLastIndex']]); u = float(v[ix['usedNextLastIndex']]); nw = float(v[ix['newNextLastIndex']])
            pc = max(0.0, (nw - s * (1 - D)) / G)
            ser[(v[ix['nation']], v[ix['region']])].append((q, s, u, pc))
            rows += 1; pin += u >= 0.995 * CAP; umax = max(umax, u); floor += u >= 0.5
        # L1
        n1 = {t: 0 for t in TOL}; den = 0
        rp_n1 = {t: 0 for t in TOL}; rp_den = 0; rp_pin = 0; rp_max = 0.0; rp_rows = 0
        for k, rs in ser.items():
            rs.sort()
            for i in range(3, len(rs)):
                win = rs[i - 3:i + 1]
                if win[-1][0] - win[0][0] != 3: continue
                pcs = [w[3] for w in win]
                if min(pcs) <= 0 or max(pcs) / min(pcs) > 1.25: continue
                pm = sum(pcs) / 4
                if pm < PCMIN: continue
                den += 1; e = seq(pm)
                for t in TOL: n1[t] += abs(rs[i][2] - e) <= t * e
            # 近似の再生（形 H）：最初の行の在庫から、四半期ごとの sigma を一定として 3 か月ずつ回す
            s = min(CAP, rs[0][1]); prevq = None; hist = []
            for (q, s0, u, pc) in rs:
                if prevq is not None and q != prevq + 1: s = min(CAP, s0)
                sig = pc / (B + s0) if (B + s0) > 0 else 0.0
                pcs3 = []
                for _ in range(3):
                    p = sig * (B + s); pcs3.append(p)
                    s = min(CAP, max(0.0, s + G * p * (1 - s / CAP) - D * s))
                hist.append((q, s, sum(pcs3) / 3)); prevq = q
                rp_rows += 1; rp_pin += s >= 0.995 * CAP; rp_max = max(rp_max, s)
            for i in range(3, len(hist)):
                win = hist[i - 3:i + 1]
                if win[-1][0] - win[0][0] != 3: continue
                pcs = [w[2] for w in win]
                if min(pcs) <= 0 or max(pcs) / min(pcs) > 1.25: continue
                pm = sum(pcs) / 4
                if pm < PCMIN: continue
                rp_den += 1; e = seq(pm)
                for t in TOL: rp_n1[t] += abs(hist[i][1] - e) <= t * e
        print(f'種 {seed}：行 {rows}・L2 在庫>=0.199 の行 {pin}・在庫の最大 {umax:.4f}・床 {floor}・L1 の母集合 {den}・'
              + '・'.join(f'許容 {t}：{n1[t]}（{100*n1[t]/max(1,den):.1f}%）' for t in TOL)
              + f'｜再生（近似）：L2 {rp_pin}・在庫の最大 {rp_max:.4f}・L1 の母集合 {rp_den}・'
              + '・'.join(f'許容 {t}：{rp_n1[t]}（{100*rp_n1[t]/max(1,rp_den):.1f}%）' for t in TOL))
        tot['rows'] += rows; tot['pin'] += pin; tot['den'] += den; tot['rp_pin'] += rp_pin; tot['rp_den'] += rp_den
        for t in TOL: tot['n1_%s' % t] += n1[t]; tot['rp_%s' % t] += rp_n1[t]
    ns = max(1, len(caps))
    print(f'計：L2 1 種あたり {tot["pin"]/ns:.1f}・L1 '
          + '・'.join(f'許容 {t} {100*tot["n1_%s"%t]/max(1,tot["den"]):.1f}%' for t in TOL)
          + f'（母集合 1 種あたり {tot["den"]/ns:.0f}）｜再生（近似）L2 1 種あたり {tot["rp_pin"]/ns:.1f}・L1 '
          + '・'.join(f'許容 {t} {100*tot["rp_%s"%t]/max(1,tot["rp_den"]):.1f}%' for t in TOL)
          + f'（母集合 1 種あたり {tot["rp_den"]/ns:.0f}）')

main()
