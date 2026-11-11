# (1049) E1 の集計：runs.txt → 事前登録 [2]〜[5] と追記の式（結果の前に書いた）。
#   呼び方：python -X utf8 -I analyze.py [runs.txt]   出力は標準出力（呼び手が analysis.txt へ）
import re, sys
P = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/r1049_work/measurements/m1049_20261111/runs.txt'
M1027, M1044, LIMIT, LC = 231.0, 222.7, 248.5, 5.0
runs = []
for ln in open(P, 'rb').read().decode('utf-8').splitlines():
    m = re.match(r'round=(\d+) label=(\w) .*?wall_s=([\d.]+) cpu_s=([\d.]+) est_s=([\d.]+) fills=(\d+) lbEval=(\d+) otherSum=(\d+) .*other_cpu_s=([\d.]+)', ln)
    if m:
        runs.append(dict(rnd=int(m[1]), cfg=m[2], wall=float(m[3]), cpu=float(m[4]), est=float(m[5]), fills=int(m[6]),
                         lb=int(m[7]), osum=int(m[8]), other=float(m[9])))
print(f'# 読んだ回 {len(runs)}')
# [結果の後の直し 20:58] 台本の other_cpu_s は 0 のまま（PowerShell の '`t' が単一引用符の中で展開されず、撮りが空だった）＝負荷は typeperf の全体の CPU% で読む
import csv, datetime, os
NCPU = 16
csvp = os.path.join(os.path.dirname(P), 'cpu_total.csv')
samples = []
if os.path.exists(csvp):
    for row in list(csv.reader(open(csvp, encoding='utf-8', errors='replace')))[1:]:
        try: samples.append((datetime.datetime.strptime(row[0], '%m/%d/%Y %H:%M:%S.%f'), float(row[1])))
        except (ValueError, IndexError): pass
starts = {}
for ln in open(P, 'rb').read().decode('utf-8').splitlines():
    m = re.match(r'round=(\d+) label=(\w) .*?start=(\d\d:\d\d:\d\d)', ln)
    if m: starts[(int(m[1]), m[2])] = m[3]
print('# 負荷（typeperf・15 秒ごとの全体の CPU%・16 論理コア）：他の負荷のコア＝全体%×16−計り台の平均コア（cpu_s÷wall_s）')
for r in runs:
    st = starts.get((r['rnd'], r['cfg']))
    if not st or not samples: print(f"round={r['rnd']} {r['cfg']}：typeperf の外"); continue
    t0 = datetime.datetime.combine(samples[0][0].date(), datetime.time.fromisoformat(st))
    t1 = t0 + datetime.timedelta(seconds=r['wall'])
    win = [v for t, v in samples if t0 <= t <= t1]
    if not win: print(f"round={r['rnd']} {r['cfg']}：typeperf の外（始め {st}）"); continue
    tot = sum(win) / len(win); bench = r['cpu'] / r['wall']; other = tot / 100 * NCPU - bench
    oth_s = other * r['wall']
    print(f"round={r['rnd']} {r['cfg']}：全体 {tot:.1f}%（{len(win)} 点）・計り台 {bench:.2f} コア・他 {other:.2f} コア＝他の CPU 約 {oth_s:.0f} 秒（計り台の {oth_s/r['cpu']*100:.0f}%・{'静か' if oth_s <= 0.1*r['cpu'] else '静かでない'}）")
for r in runs:
    quiet = '静か' if r['other'] <= 0.10 * r['cpu'] else '静かでない'
    print(f"round={r['rnd']} {r['cfg']} wall={r['wall']:.1f} cpu={r['cpu']:.1f} est={r['est']:.3f} （台本の撮り other_cpu_s は空＝使わない・負荷は上の typeperf）")

# 確かめ
A0 = [r for r in runs if r['cfg'] == 'A']
if A0:
    aos = {r['osum'] for r in A0}
    print(f"# 確かめ：A の軌道の札 {sorted(aos)}（1 つなら A は再現）")
    for r in runs:
        if r['cfg'] == 'B':
            print(f"# 確かめ B round={r['rnd']}: lbEval==0 {r['lb']==0}・軌道の札==A {r['osum'] in aos}・fills==A の lbEval {r['fills'] in {a['lb'] for a in A0}}")
        if r['cfg'] == 'C':
            print(f"# 確かめ C round={r['rnd']}: 軌道の札!=A {r['osum'] not in aos}")

def pick(cfg, key):
    rs = [r for r in runs if r['cfg'] == cfg]
    r12 = [r[key] for r in rs if r['rnd'] >= 1]
    r0 = [r[key] for r in rs if r['rnd'] == 0]
    note = ''
    vals = list(r12)
    if r0:
        v0 = r0[0]
        if len(r12) >= 2:
            lo, hi = min(r12), max(r12); w = hi - lo
            if (v0 < lo and lo - v0 >= w) or (v0 > hi and v0 - hi >= w):
                note = f'周0 {v0:.2f} を外した（周1・2 の範囲 {lo:.2f}〜{hi:.2f} から幅 {w:.2f} 以上離れた）'
            else:
                vals.append(v0)
        else:
            vals.append(v0)
    if not vals: return None, None, 0, 'なし'
    mean = sum(vals) / len(vals)
    spread = (max(vals) - min(vals)) if len(vals) >= 2 else None
    return mean, spread, len(vals), note

def term(key):
    out = {}
    for c in 'ABC':
        out[c] = pick(c, key)
        m, s, n, note = out[c]
        if m is None: print(f'[{key}] {c}: 回なし'); continue
        print(f"[{key}] {c}: 平均 {m:.2f}・揺れ {'不明' if s is None else f'{s:.2f}'}・本数 {n} {note}")
    return out

E = {c: pick(c, 'est')[0] for c in 'ABC'}
print(f"# 台帳（種 1）E_A={E['A']} E_B={E['B']} E_C={E['C']}")
if E['A'] is not None and E['B'] is not None: print(f"# 台帳の C2 の正味（E_B−E_A）={E['B']-E['A']:.3f}（台帳が数える〈飛ばした充填の減り−評価の数え〉）")
if E['A'] is not None and E['C'] is not None: print(f"# 台帳の lw の軌道（E_A−E_C）={E['A']-E['C']:.3f}")

for key in ('cpu', 'wall'):
    print(f'\n==== 主な量 {key}（CPU 秒が主・実時間は併記） ====')
    t = term(key)
    (a, sa, na, _), (b, sb, nb, _), (c, sc, nc, _) = t['A'], t['B'], t['C']
    EA = E['A']
    if a is None or EA is None: print('A が無い＝計算できない'); continue
    def rel_noise(s1, s2):
        return None if (s1 is None or s2 is None) else max(s1, s2)
    # C2
    xC2 = 0.0; c2note = ''
    if b is not None:
        rC2 = (b - a) / a; xC2raw = rC2 * EA; nz = rel_noise(sa, sb)
        inside = nz is None or abs(b - a) <= nz
        c2note = '雑音の内' if inside else '雑音の外'
        print(f"C2：r_C2=(B−A)/A={rC2*100:+.2f}%・x_C2={xC2raw:+.2f} 台帳秒・|B−A|={abs(b-a):.2f} 対 揺れ {('不明' if nz is None else f'{nz:.2f}')} ＝{c2note}")
        xC2 = 0.0 if inside else xC2raw
        dC2 = (0 if nz is None else nz) / a * EA
    else:
        print('C2：B が無い＝雑音の内と同じ扱い'); dC2 = 0.0; c2note = 'B なし'
    # lw
    lwpaper_lo, lwpaper_hi = 0.7 + 1.0, 0.7 + 3.0
    if c is not None:
        rlw = (a - c) / a; xlwraw = rlw * EA; nz = rel_noise(sa, sc)
        inside = nz is None or abs(a - c) <= nz
        lwnote = '雑音の内' if inside else '雑音の外'
        traj = (E['A'] - E['C']) if E['C'] is not None else float('nan')
        print(f"lw：r_lw=(A−C)/A={rlw*100:+.2f}%・x_lw={xlwraw:+.2f} 台帳秒（内訳の記録：軌道〔台帳〕{traj:+.2f}・ループ {xlwraw-traj:+.2f}）・|A−C|={abs(a-c):.2f} 対 揺れ {('不明' if nz is None else f'{nz:.2f}')} ＝{lwnote}")
        dlw = (0 if nz is None else nz) / a * EA
        if inside: xlw_lo, xlw_hi = lwpaper_lo, lwpaper_hi
        else: xlw_lo, xlw_hi = xlwraw - dlw, xlwraw + dlw
        xlw_mid = xlwraw if not inside else (lwpaper_lo + lwpaper_hi) / 2
    else:
        print('lw：C が無い＝紙の値'); xlw_lo, xlw_hi = lwpaper_lo, lwpaper_hi; xlw_mid = 2.7; traj = float('nan')
    # 余り
    def R(xc2, xlw, L): return LIMIT - (M1027 - xc2 + xlw + L)
    c2lo, c2hi = (xC2 - dC2, xC2 + dC2) if c2note == '雑音の外' else (0.0, 0.0)
    Rmid = R(xC2, xlw_mid, 0); Rlo = R(c2lo, xlw_hi, 0); Rhi = R(c2hi, xlw_lo, 0)
    print(f"余りの真値（主式・L=0）R={Rmid:.2f}（幅 {Rlo:.2f}〜{Rhi:.2f}）")
    print(f"余りの真値（記録・L=+5）R={Rmid-LC:.2f}（幅 {Rlo-LC:.2f}〜{Rhi-LC:.2f}）")
    if E['B'] is not None and E['C'] is not None and c2note == '雑音の外':
        loop = xlw_mid - traj if c is not None else 2.0
        R2 = LIMIT - (M1044 + (E['B'] - E['A']) - xC2 + loop)
        print(f"照合（m1044 の台帳から）R2={R2:.2f}")
    elif E['B'] is not None:
        # [結果の後の直し 20:58] lw が雑音の内なら、ループは紙の値（1〜3 の中 2.0）＝旧は〈紙の x_lw の中−台帳の種 1 の軌道〉を引いて負になっていた
        loop = (xlw_mid - traj) if (c is not None and traj == traj and lwnote == '雑音の外') else 2.0
        R2 = LIMIT - (M1044 + (E['B'] - E['A']) - 0.0 + loop)
        print(f"照合（m1044 の台帳から・C2 を雑音の内＝実の減り 0 とした）R2={R2:.2f}")
    v = '切る' if Rhi < 5 else ('切りうる' if Rlo < 5 else '切らない')
    print(f"5 秒の判定（主式）：{v}")
