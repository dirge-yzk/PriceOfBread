# (604) 線 tt：関税の施行値 T の系列を、計器の生の secession_s の stanceIndex（= w.Tariff*2-1）から読む（読むだけ）。
# 使い方：PYTHONUTF8=1 python tariff_series.py <ins フォルダ> [<ins フォルダ> ...]    （各フォルダの中に secession_s<種>.tsv がある）
# 行の単位＝国×四半期（挙げた 9 国は毎四半期・他は年末）。国名は変名で変わる＝同じ名前の連続だけを同じ国として数える。
# 出す量（種ごと→10 種の合計・中央値）：
#   up  ＝同じ名前で 4 四半期離れた対の ΔT ≧ +0.02 の件数（0.02 は TradeTreaties の自分の幅 lo+0.02 と同じ物差し）／down ＝ ΔT ≦ −0.02
#   lockShare1900 ＝ 1900 年以降の国×四半期のうち T ≧ 0.9 の割合（禁止的関税に張り付いた割合）
#   level_<年> ＝ その年の第 1 四半期の国の T の中央値（9 国）
import sys, os, glob, statistics, collections
def read(path):
    rows = {}
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        iq, iy, inat, ist = h.index('quarter'), h.index('year'), h.index('nation'), h.index('stanceIndex')
        for ln in f:
            p = ln.rstrip('\n').split('\t')
            k = (int(p[iq]), p[inat])
            if k in rows: continue
            try: s = float(p[ist])
            except ValueError: continue
            rows[k] = ((s + 1.0) / 2.0, int(p[iy]))
    return rows
def per_seed(rows):
    up = down = 0; hi = tot = 0
    for (q, nm), (t, y) in rows.items():
        if y >= 1900:
            tot += 1
            if t >= 0.9: hi += 1
        o = rows.get((q + 4, nm))
        if o is not None:
            d = o[0] - t
            if d >= 0.02: up += 1
            elif d <= -0.02: down += 1
    lv = {}
    for yr in (1783, 1800, 1850, 1880, 1913, 1949):
        xs = [t for (q, nm), (t, y) in rows.items() if y == yr and q % 4 == 0]
        # 第 1 四半期＝q%4==0 の行（その年に 9 国が毎四半期出る）
        lv[yr] = statistics.median(xs) if xs else float('nan')
    return up, down, (hi / tot if tot else float('nan')), lv
def main():
    for d in sys.argv[1:]:
        print('==', d)
        ups = []; downs = []; locks = []; lvs = collections.defaultdict(list)
        for f in sorted(glob.glob(os.path.join(d, 'secession_s*.tsv')), key=lambda s: int(''.join(c for c in os.path.basename(s) if c.isdigit()))):
            up, down, lock, lv = per_seed(read(f))
            ups.append(up); downs.append(down); locks.append(lock)
            for k, v in lv.items(): lvs[k].append(v)
            print(os.path.basename(f), 'up', up, 'down', down, 'lockShare1900 %.3f' % lock, ' '.join('%d:%.2f' % (k, v) for k, v in lv.items()))
        print('合計 up', sum(ups), 'down', sum(downs), '種ごと down/up の最小', min((d_ / u if u else float('nan')) for u, d_ in zip(ups, downs)), 'lockShare1900 中央値 %.3f 最大 %.3f' % (statistics.median(locks), max(locks)))
        print('年別の国の T の中央値（種の中央値）', ' '.join('%d:%.2f' % (k, statistics.median(v)) for k, v in lvs.items()))
if __name__ == '__main__':
    main()
