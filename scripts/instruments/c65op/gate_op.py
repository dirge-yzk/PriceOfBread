# (820) c65op 門の読み手：計器の出力フォルダの habitlg_s*.tsv から事前登録 [4] の門 L1・L2・U1・U2・U3 と記録を出す（読むだけ）。
#   python -X utf8 -I scripts/instruments/c65op/gate_op.py <ins のフォルダ> [ラベル]
# 定数は事前登録の値で固定（G=0.1・D=0.02・d=0.2）＝表の係数の列は読まない（d を取り違えた突然変異が表に別の値を書いても門は動かない）。
import sys, glob, os
from collections import defaultdict
G, D, DD = 0.1, 0.02, 0.2
PCMIN, RATIO, TOL, TOL2 = 0.02, 1.25, 0.3, 0.15

def seq(pc):
    return G * pc * DD / (D * DD + G * pc) if pc > 0 else 0.0

def main():
    d = sys.argv[1]; lab = sys.argv[2] if len(sys.argv) > 2 else d
    fs = sorted(glob.glob(os.path.join(d, 'habitlg_s*.tsv')), key=lambda p: int(os.path.basename(p)[9:-4]))
    print('盤面', lab, '／habitlg_s', len(fs), '本')
    if not fs: print('判定: 未了（habitlg_s が無い）'); return
    T = defaultdict(float); smax_all = 0.0; ratios = []
    for f in fs:
        seed = os.path.basename(f)[9:-4]
        fh = open(f, encoding='utf-8'); h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        ser = defaultdict(list); rows = pin = floor = viol = trans = logmis = oldmis = overd = pinm = 0; smax = 0.0; flags = set()
        for line in fh:
            v = line.rstrip('\n').split('\t')
            g = lambda c: float(v[ix[c]])
            q = int(v[ix['quarter']]); sl = g('stockLastIndex'); sm = g('stockMaxIndex'); pm = g('pcMeanPerMonth')
            ser[(v[ix['nation']], v[ix['region']])].append((q, sl, pm))
            rows += 1; pin += sm >= 0.199; smax = max(smax, sm)
            floor += int(v[ix['floorMonthsCount']]); viol += int(v[ix['lgViolMonthsCount']]); trans += int(v[ix['transCount']])
            logmis += int(v[ix['logMismatchCount']]); oldmis += int(v[ix['oldMismatchCount']]); overd += int(v[ix['overDMonthsCount']]); pinm += int(v[ix['pinMonthsCount']])
            flags.add(v[ix['logisticFlag']])
            o = g('oldNextLastIndex'); l = g('logNextLastIndex')
            if o > 1e-6: ratios.append(l / o)
        den = n1 = n2 = 0
        for k, rs in ser.items():
            rs.sort()
            for i in range(3, len(rs)):
                win = rs[i - 3:i + 1]
                if win[-1][0] - win[0][0] != 3: continue
                pcs = [w[2] for w in win]
                if min(pcs) <= 0 or max(pcs) / min(pcs) > RATIO: continue
                p = sum(pcs) / 4
                if p < PCMIN: continue
                den += 1; e = seq(p)
                n1 += abs(rs[i][1] - e) <= TOL * e; n2 += abs(rs[i][1] - e) <= TOL2 * e
        print(f'種 {seed}：行 {rows}・旗 {sorted(flags)}・L1 母集合 {den}・許容 0.3 {n1}（{100*n1/max(1,den):.1f}%）・許容 0.15 {n2}（{100*n2/max(1,den):.1f}%）・L2 在庫≧0.199 の行 {pin}・在庫の最大 {smax:.6f}・床の月 {floor}・釣り合いの破り {viol}・遷移 {trans}・新と不一致 {logmis}・旧と不一致 {oldmis}・挟みが効いた月 {overd}・pin の月 {pinm}')
        for k, x in (('rows', rows), ('den', den), ('n1', n1), ('n2', n2), ('pin', pin), ('floor', floor), ('viol', viol), ('trans', trans), ('logmis', logmis), ('oldmis', oldmis), ('overd', overd)): T[k] += x
        smax_all = max(smax_all, smax)
    ns = len(fs)
    l1 = T['n1'] / max(1, T['den']); l1b = T['n2'] / max(1, T['den'])
    ok = {
        'L1': l1 >= 0.70 and T['den'] / ns >= 100,
        'L2': T['pin'] / ns <= 10,
        'U1': smax_all <= 0.2 + 1e-9,
        'U2': T['floor'] == 0,
        'U3': T['viol'] == 0,
    }
    ratios.sort()
    qn = lambda p: ratios[min(len(ratios) - 1, int(p * len(ratios)))] if ratios else float('nan')
    print(f'計（{ns} 種）：L1 {100*l1:.2f}%（門 ≧70%・母集合 1 種あたり {T["den"]/ns:.1f}〔門 ≧100〕）＝{"通る" if ok["L1"] else "落ちる"}・記録 許容 0.15 {100*l1b:.2f}%')
    print(f'  L2 在庫≧0.199 の州×四半期 1 種あたり {T["pin"]/ns:.1f}（門 ≦10）＝{"通る" if ok["L2"] else "落ちる"}')
    print(f'  U1 在庫の最大 {smax_all:.9f}（門 ≦0.2+1e-9）＝{"通る" if ok["U1"] else "落ちる"}')
    print(f'  U2 床の月 {int(T["floor"])}（門 =0）＝{"通る" if ok["U2"] else "落ちる"}')
    print(f'  U3 釣り合いの破り {int(T["viol"])}（門 =0）＝{"通る" if ok["U3"] else "落ちる"}')
    print(f'  記録：遷移 {int(T["trans"])}・新と不一致 {int(T["logmis"])}・旧と不一致 {int(T["oldmis"])}・挟みが効いた月 {int(T["overd"])}・同じ盤面の新÷旧（最後の月・旧>1e-6 の行 {len(ratios)}）の 5%・50%・95% 分位 {qn(0.05):.4f}・{qn(0.5):.4f}・{qn(0.95):.4f}')
    print('門：' + '・'.join(f'{k} {"緑" if v else "赤"}' for k, v in ok.items()))
    print('判定: ' + ('全ての門を通る' if all(ok.values()) else '落ちた門あり ' + ','.join(k for k, v in ok.items() if not v)))

main()
