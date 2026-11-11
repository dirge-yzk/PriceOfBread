# (1049) 記録（事前登録の外・結果の後に足した読み）：計り台の CPU 秒を〈構成ごとの切片＋共通の傾き×他の負荷のコア〉で当てはめ、同じ負荷での差を読む。
#   入力は analysis.txt の負荷の行（typeperf）と runs.txt。周 0 の A・B は typeperf の外＝使わない。採否や主式には使わない。
import re, sys
D = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/r1049_work/measurements/m1049_20261111'
pts = []
for ln in open(D + '/analysis.txt', 'rb').read().decode('utf-8').splitlines():
    m = re.match(r'round=(\d) (\w)：全体 [\d.]+%.*?計り台 ([\d.]+) コア・他 ([\d.]+) コア', ln)
    if m: pts.append((int(m[1]), m[2], float(m[4])))
cpu = {}
for ln in open(D + '/runs.txt', 'rb').read().decode('utf-8').splitlines():
    m = re.match(r'round=(\d) label=(\w) .*?cpu_s=([\d.]+) est_s=([\d.]+)', ln)
    if m: cpu[(int(m[1]), m[2])] = (float(m[3]), float(m[4]))
rows = [(c, x, cpu[(r, c)][0]) for r, c, x in pts]
for excl in (None, 'B2'):
    use = [rw for (r, c, x), rw in zip(pts, rows) if not (excl == 'B2' and r == 2 and c == 'B')]
    cfgs = sorted({c for c, _, _ in use})
    # 共通の傾き：構成の中で中心化した最小二乗
    mx = {k: sum(x for c, x, _ in use if c == k) / sum(1 for c, _, _ in use if c == k) for k in cfgs}
    my = {k: sum(y for c, _, y in use if c == k) / sum(1 for c, _, _ in use if c == k) for k in cfgs}
    sxy = sum((x - mx[c]) * (y - my[c]) for c, x, y in use); sxx = sum((x - mx[c]) ** 2 for c, x, _ in use)
    beta = sxy / sxx
    x0 = 4.0   # 比べる負荷（他 4 コア＝周 1 の並び）
    at = {k: my[k] + beta * (x0 - mx[k]) for k in cfgs}
    res = [y - (my[c] + beta * (x - mx[c])) for c, x, y in use]
    rms = (sum(e * e for e in res) / max(1, len(res) - len(cfgs) - 1)) ** 0.5
    EA = cpu[(1, 'A')][1]
    print(f"# 除外={excl or 'なし'}・点 {len(use)}・傾き {beta:.1f} CPU 秒/コア・残差の rms {rms:.1f} 秒")
    print('  他 4 コアでの CPU 秒：' + '・'.join(f'{k} {at[k]:.1f}' for k in cfgs))
    if 'A' in at and 'B' in at: print(f"  r_C2=(B−A)/A={(at['B']-at['A'])/at['A']*100:+.2f}%・台帳秒 {(at['B']-at['A'])/at['A']*EA:+.2f}（残差の rms は A の {rms/at['A']*100:.1f}%）")
    if 'A' in at and 'C' in at: print(f"  r_lw=(A−C)/A={(at['A']-at['C'])/at['A']*100:+.2f}%・台帳秒 {(at['A']-at['C'])/at['A']*EA:+.2f}")
