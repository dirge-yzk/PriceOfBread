"""(186) 貿易/GDP の読み手（読むだけ）＝史実の側を「同じ6国の平均」にした比（README §1-2 の式＝シミュ÷max(史実, 床)・10種の中央×10年の窓）。
  python an_trade186.py t <out_dir/t186> [ラベル]     … 計器 inj_t186 の T 行から10年の窓×種の中央（6大国の平均・国ごと）
  python an_trade186.py rejudge <帯の報告> ...          … 印字された「6大国の終局 貿易/GDP 平均」（種の平均・終局の断面）を同じ6国の史実の平均で読み直す
史実（1913・(輸出＋輸入)÷GDP）＝HistoricalReference.TradeOpenness1913（2026-08-15 作者承認の次元の参照表・出典＝Findlay & O'Rourke ほか通説の概数）の同じ6国。
1950 の値は記憶の概数【推】（出典の候補＝Mitchell, International Historical Statistics の貿易額と国民所得・Maddison）＝判定には使わず並べるだけ。"""
import sys, os, re, statistics as st

MAJ = ["イギリス", "フランス", "ロシア", "プロイセン", "清", "アメリカ"]   # SelfPlay.TradeGdpMajorNames と同じ6国
H1913 = {"イギリス": 0.45, "フランス": 0.35, "プロイセン": 0.37, "ロシア": 0.16, "アメリカ": 0.11, "清": 0.08}   # HistoricalReference.TradeOpenness1913
H1950 = {"イギリス": 0.40, "フランス": 0.24, "プロイセン": 0.20, "ロシア": 0.04, "アメリカ": 0.065, "清": 0.06}   # 【推】記憶の概数（西独・ソ連・中華人民共和国）
AVG1913 = sum(H1913[n] for n in MAJ) / len(MAJ)
AVG1950 = sum(H1950[n] for n in MAJ) / len(MAJ)
FLOOR = 0.01   # 0近傍の床（年GDP 比1%）＝README §1-2


def judge(s, h):
    r = s / max(h, FLOOR)
    return r, ('NG' if (r >= 10 or r <= 0.1) else '記録' if (r >= 3 or r <= 1 / 3) else '可')


WIN = [(1783, 1792), (1793, 1802), (1813, 1822), (1843, 1852), (1873, 1882), (1904, 1913), (1920, 1929), (1940, 1949)]

if sys.argv[1] == 't':
    d = sys.argv[2]; lab = sys.argv[3] if len(sys.argv) > 3 else d
    per = {}   # seed -> {(q,yr): {name: tg}}
    for fn in sorted(os.listdir(d)):
        m = re.match(r't(\d+)\.txt$', fn)
        if not m: continue
        seed = int(m.group(1)); rows = {}
        for ln in open(os.path.join(d, fn), encoding='utf-8'):
            if not ln.startswith('T|'): continue
            p = ln.rstrip('\n').split('|')
            q, yr, name = int(p[1]), int(p[2]), p[4]
            ex, im, ng, cpy = float(p[5]), float(p[6]), float(p[7]), float(p[8])
            tg = (ex + im) * cpy / max(1.0, ng * 12)
            rows.setdefault((q, yr), {})[name] = tg
        per[seed] = rows
    seeds = sorted(per)
    print(f'# {lab}：貿易/GDP（(輸出＋輸入)×清算/年÷年の名目GDP＝SelfPlay.MajorsTradeGdp と同じ物差し）・{len(seeds)}種')
    print(f'# 史実＝同じ6国の平均 1913 {AVG1913 * 100:.1f}%（HistoricalReference.TradeOpenness1913）・1950 {AVG1950 * 100:.1f}%【推】・床 {FLOOR * 100:.0f}%')
    # 終局の断面（印字の値の検算）
    term = []
    for s in seeds:
        qmax = max(k[0] for k in per[s]); row = next(v for k, v in per[s].items() if k[0] == qmax)
        xs = [row[n] for n in MAJ if n in row]
        term.append(sum(xs) / len(xs) if xs else float('nan'))
    print(f'終局の断面 6国の平均：種の平均 {st.mean(term) * 100:.2f}%（帯の報告の印字と同じはず）・種の中央 {st.median(term) * 100:.2f}%・種ごと ' + ' '.join(f'{x * 100:.2f}' for x in term))
    print('窓 | 6国の平均（種の中央）| 種の範囲 | 史実の6国の平均 | 比 | 判定 | 国ごと（種の中央：英 仏 普 露 米 清）')
    for (y0, y1) in WIN:
        vals = []; pern = {n: [] for n in MAJ}
        for s in seeds:
            qs = [v for k, v in per[s].items() if y0 <= k[1] <= y1]
            if not qs: continue
            avgs = []
            for row in qs:
                xs = [row[n] for n in MAJ if n in row]
                if xs: avgs.append(sum(xs) / len(xs))
            if avgs: vals.append(sum(avgs) / len(avgs))
            for n in MAJ:
                xn = [row[n] for row in qs if n in row]
                if xn: pern[n].append(sum(xn) / len(xn))
        if not vals: continue
        med = st.median(vals)
        h = AVG1913 if (y0, y1) == (1904, 1913) else AVG1950 if (y0, y1) == (1940, 1949) else None
        hs = f'{h * 100:.1f}%' + ('' if (y0, y1) == (1904, 1913) else '【推】') if h else '—'
        if h:
            r, j = judge(med, h)
            rj = f'{r:.3f} | {j}' + ('' if (y0, y1) == (1904, 1913) else '（【推】の錨＝判定に使わない）')
        else:
            rj = '— | —'
        cn = ' '.join(f'{(st.median(pern[n]) * 100 if pern[n] else float("nan")):.2f}' for n in ["イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "清"])
        print(f'{y0}-{y1} | {med * 100:.2f}% | {min(vals) * 100:.2f}〜{max(vals) * 100:.2f} | {hs} | {rj} | {cn}')
    # 国ごとの 1904-13（史実 1913 の表と）
    print('国ごと 1904-13（種の中央÷史実 1913）：', end='')
    out = []
    for n in ["イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "清"]:
        xs = []
        for s in seeds:
            qs = [v[n] for k, v in per[s].items() if 1904 <= k[1] <= 1913 and n in v]
            if qs: xs.append(sum(qs) / len(qs))
        if xs:
            med = st.median(xs); r, j = judge(med, H1913[n]); out.append(f'{n} {med * 100:.2f}%÷{H1913[n] * 100:.0f}%＝{r:.3f}（{j}）')
    print('・'.join(out))
elif sys.argv[1] == 'rejudge':
    print(f'# 印字された「6大国の終局 貿易/GDP 平均」（種の平均・終局の断面）を同じ6国の史実の平均で読み直す（README §1-2 の式）')
    print(f'# 史実＝1913 の同じ6国の平均 {AVG1913 * 100:.1f}%（参照表）・旧の読み＝英1国 1913 40%・参考＝1950 の6国の平均 {AVG1950 * 100:.1f}%【推】')
    print('帯 | 印字の値 | 旧の比（÷英 40%）| 旧の判定 | 新の比（÷6国 1913）| 新の判定 | 参考（÷6国 1950【推】）')
    for path in sys.argv[2:]:
        t = open(path, encoding='utf-8').read()
        m = re.search(r'6大国の終局 貿易/GDP 平均 ([\d.]+) %', t)
        if not m: print(f'{os.path.basename(path)} | — |'); continue
        s = float(m.group(1)) / 100
        r0, j0 = judge(s, 0.40); r1, j1 = judge(s, AVG1913); r2, j2 = judge(s, AVG1950)
        print(f'{os.path.basename(path)} | {s * 100:.2f}% | {1 / r0:.1f}分の1 | {j0} | {1 / r1:.1f}分の1 | {j1} | {1 / r2:.1f}分の1（{j2}）')
