# (890) c75ts 止める線〈名目 GDP の 1 四半期 100 倍以上〉の型の読み手（読むだけ・r833/gdp_jump833.py の写しを型で書き直した版・元の台本は変えない）。
#   ★(885)〈100 倍を国で振り分けない＝型で決める〉。規則は旗 0 とヌル腕の生（keep_885/base_ins・keep_873/base_ins・n1・n2・a11）だけから決めた（事前登録 measurements/m890_20261106_prereg_ts.txt [5]）。
#   区分の順（1 つの跳びは 1 つの型だけ）：
#     1 建国の直後＝その国（nationUid）の系列の最初の行から K＝4 四半期以内（gdp_jump797/833 と同じ・変えない）。
#     2 人口の分母の痩せ＝跳びの四半期の人口の比 popR＝populationK[x]/populationK[p] が ≧THIN か ≦1/THIN（833 と同じ）**かつ** 一人あたりの比 ra÷popR が THIN 未満
#        （人口で説明できない残りが 1 桁未満＝旗 0 の 3 件は 0.66〜2.3）。一人あたりの比が THIN 以上なら 833 で痩せでも ここでは 3 か 4 へ（締める向き）。
#     3 時間の分母の痩せ（新）＝跳びの直前の値が、その前の N 四半期（有限の行）の最大の 1/THIN 以下（dip≦1/THIN＝値の小さい分母）**かつ**
#        跳びの後の値 g が直前 N＋1 四半期（直前の行を含む）の最大の THIN 倍未満（over＝g÷max＜THIN＝直前の数年に持っていた水準から 1 桁を超えて出ない）。
#        旗 0 の dip≦0.1 の 3 件（ジェノヴァ・ライン諸邦・コーカンド＝20 倍以上の全て）は over 0.73〜1.27。ra≧100 で over＜THIN なら dip≦0.1 は自動で立つ（20 倍の探索のために明示した）。
#     4 それ以外＝止める線。
#   THIN＝10（833 の POP_RATIO_THIN と同じ 1 桁）・N＝8 四半期。国の名前・国の過去の跳びは見ない（国で免除しない）。
#   使い方: python -I -X utf8 gdp_jump890.py [--rows] [--thr=<倍>] <名前>=<ins フォルダ> ...   ／   python -I -X utf8 gdp_jump890.py --selftest
import csv, glob, math, os, sys
K = 4; THIN = 10.0; N = 8; JUMP = 100.0
TYPES = ("建国の直後", "人口の分母の痩せ", "時間の分母の痩せ", "それ以外")

def num(s):
    try: return float(s)
    except ValueError: return float('nan')

def pratio(cur, prv):
    if not (math.isfinite(cur) and math.isfinite(prv)): return float('nan')
    if prv > 0: return cur / prv
    return math.inf if cur > 0 else float('nan')

def classify(age, ra, popr, g, recent, country_exempt=None, nation=None):
    """1 つの跳びの型。recent＝跳びの直前の N＋1 行の GDP（直前の行を含む）。country_exempt は自己試験の突然変異（国の集合＝国で免除する誤った形）。"""
    if country_exempt is not None and nation in country_exempt: return "建国の直後"
    if age <= K: return TYPES[0]
    thinp = (math.isfinite(popr) and (popr >= THIN or popr <= 1.0 / THIN)) or popr == math.inf
    if thinp and math.isfinite(popr) and popr > 0 and ra / popr < THIN: return TYPES[1]
    pk = max(recent) if recent else float('nan'); pk0 = max(recent[:-1]) if len(recent) > 1 else float('nan')
    if pk > 0 and g / pk < THIN and pk0 > 0 and recent[-1] / pk0 <= 1.0 / THIN: return TYPES[2]
    return TYPES[3]

def scan_file(path, thr, country_exempt=None):
    """1 種の nation_s。戻り [(型, 行の組)]。行の組＝(種?, 国, 年, 四半期, 最初の行からの四半期, 比, 人口の比, 一人あたりの比, over, dip)。"""
    out = []
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        out = scan_rows(([x[ix['nationUid']], x[ix['nation']], int(x[ix['year']]), int(x[ix['quarter']]), num(x[ix['nominalGdpPerYear']]), num(x[ix['populationK']])] for x in r), thr, country_exempt)
    return out

def scan_rows(rows, thr, country_exempt=None):
    hist = {}; first = {}; ppop = {}; out = []; j1e10 = 0; nonf = 0
    for u, nm, y, q, g, pop in rows:
        if u not in first: first[u] = q
        if not math.isfinite(g): nonf += 1; continue
        H = hist.setdefault(u, [])
        if H and H[-1] > 0:
            ra = g / H[-1]
            if ra >= 1e10: j1e10 += 1
            if ra >= thr:
                popr = pratio(pop, ppop[u]); recent = H[-(N + 1):]
                t = classify(q - first[u], ra, popr, g, recent, country_exempt, nm)
                pk = max(recent)
                out.append((t, (nm, y, q, q - first[u], round(ra, 1), round(popr, 4), round(ra / popr, 3) if math.isfinite(popr) and popr > 0 else float('nan'),
                                round(g / pk, 3), round(H[-1] / max(H[-(N + 1):-1]) if len(H) > 1 and max(H[-(N + 1):-1]) > 0 else float('nan'), 4))))
        H.append(g); ppop[u] = pop
    return out, j1e10, nonf

def selftest():
    """合成の盤面（両側・恒真でない・国で免除の突然変異が見える）。"""
    ok = True; L = []
    def chk(name, got, want):
        nonlocal ok
        ok = ok and got == want; L.append("%s：%s（期待 %s）→ %s" % (name, got, want, "期待どおり" if got == want else "**外れ**"))
    def ser(vals, pops=None, uid="A", nm="甲国", q0=0):
        return [(uid, nm, 1800 + (q0 + i) // 4, q0 + i, v, (pops[i] if pops else 100.0)) for i, v in enumerate(vals)]
    # 盤面 1：同じ国に建国の直後の跳び（2 四半期目・500 倍）と、33 四半期目の落ち込みからの跳び（直前の最大の 20 倍へ）
    v = [1.0, 2.0, 1000.0] + [1500.0] * 23 + [1517.0, 1128.0, 648.0, 407.0, 227.0, 167.0, 45.9, 30808.0, 21181.0]
    out, _, _ = scan_rows(ser(v), JUMP)
    chk("盤面 1：跳びの数", len(out), 2)
    chk("盤面 1：2 四半期目の 500 倍は建国の直後", out[0][0] if out else None, "建国の直後")
    chk("盤面 1：落ち込みから直前の最大の 20 倍へ跳ぶ型（670 倍）はそれ以外", out[1][0] if len(out) > 1 else None, "それ以外")
    outm, _, _ = scan_rows(ser(v), JUMP, country_exempt={"甲国"})
    chk("突然変異〈国で免除〉：同じ盤面の 670 倍がそれ以外に残らない（＝突然変異が見える）", (outm[1][0] if len(outm) > 1 else None) != "それ以外", True)
    # 盤面 2：落ち込みから直前の水準へ戻るだけ（50 四半期目・180 倍・over 0.9）＝時間の分母の痩せ（免除されるべき＝規則は恒真に厳しくない）
    v2 = [1000.0] * 45 + [600.0, 200.0, 50.0, 5.0, 900.0]
    out2, _, _ = scan_rows(ser(v2, uid="B", nm="乙国"), JUMP)
    chk("盤面 2：落ち込みから直前の水準へ戻る 180 倍は時間の分母の痩せ", out2[0][0] if out2 else None, "時間の分母の痩せ")
    # 盤面 3：人口の比 20 で 150 倍（一人あたり 7.5）＝人口の分母の痩せ・人口の比 20 で 5000 倍（一人あたり 250）＝それ以外（833 なら痩せ）
    v3 = [1000.0] * 10 + [150000.0]; p3 = [100.0] * 10 + [2000.0]
    out3, _, _ = scan_rows(ser(v3, p3, uid="C", nm="丙国"), JUMP)
    chk("盤面 3a：人口の比 20・150 倍は人口の分母の痩せ", out3[0][0] if out3 else None, "人口の分母の痩せ")
    v3b = [1000.0] * 10 + [5.0e6]
    out3b, _, _ = scan_rows(ser(v3b, p3, uid="D", nm="丁国"), JUMP)
    chk("盤面 3b：人口の比 20・5000 倍（一人あたり 250）はそれ以外（締める向き）", out3b[0][0] if out3b else None, "それ以外")
    # 盤面 4：人口が 1/20 に落ちた四半期の 200 倍＝それ以外（833 では痩せ）
    v4 = [1000.0] * 10 + [2.0e5]; p4 = [100.0] * 10 + [5.0]
    out4, _, _ = scan_rows(ser(v4, p4, uid="E", nm="戊国"), JUMP)
    chk("盤面 4：人口の比 1/20 の 200 倍はそれ以外", out4[0][0] if out4 else None, "それ以外")
    # 盤面 5：平らな系列から 200 倍＝それ以外
    v5 = [1000.0] * 20 + [2.0e5]
    out5, _, _ = scan_rows(ser(v5, uid="F", nm="己国"), JUMP)
    chk("盤面 5：平らな系列からの 200 倍はそれ以外", out5[0][0] if out5 else None, "それ以外")
    # 盤面 7（20 倍の探索の閾値）：直前が最大の 0.3（痩せていない）で 20 倍・over 6＝それ以外
    v7 = [1000.0] * 10 + [300.0, 6000.0]
    out7, _, _ = scan_rows(ser(v7, uid="H", nm="辛国"), 20.0)
    chk("盤面 7：痩せていない分母（dip 0.3）からの 20 倍はそれ以外", out7[0][0] if out7 else None, "それ以外")
    # 盤面 6：直前の最大が N＋1 四半期より前にしか無い戻り（10 四半期前が最大）＝それ以外（窓の外は見ない）
    v6 = [1000.0] * 10 + [1.0] * 10 + [900.0]
    out6, _, _ = scan_rows(ser(v6, uid="G", nm="庚国"), JUMP)
    chk("盤面 6：最大が窓 N＋1 の外の戻り（900 倍）はそれ以外", out6[0][0] if out6 else None, "それ以外")
    return ok, L

def main():
    a = sys.argv[1:]
    if a and a[0] == '--selftest':
        ok, L = selftest(); print("\n".join(L)); print("自己試験：%s" % ("全て期待どおり" if ok else "外れあり")); return 0 if ok else 1
    rows_out = '--rows' in a; thr = JUMP
    for x in a:
        if x.startswith('--thr='): thr = float(x[6:])
    print(f'# gdp_jump890 K={K} THIN={THIN:g} N={N} 閾値={thr:g}（型：建国の直後・人口の分母の痩せ〔一人あたりの比＜THIN〕・時間の分母の痩せ〔over＜THIN〕・それ以外）')
    for arg in [x for x in a if not x.startswith('--')]:
        name, d = arg.split('=', 1)
        seeds = sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
        cnt = {t: 0 for t in TYPES}; rows = []; j1e10 = nonf = 0
        for s in seeds:
            out, j, nf = scan_file(os.path.join(d, f'nation_s{s}.tsv'), thr)
            j1e10 += j; nonf += nf
            for t, row in out: cnt[t] += 1; rows.append((t, (s,) + row))
        print(f'{name}\t種 {len(seeds)}\t{thr:g} 倍以上 {sum(cnt.values())}（' + '・'.join(f'{t} {cnt[t]}' for t in TYPES) + f'）\t1e10 倍以上 {j1e10}\t非有限 {nonf}')
        if rows_out:
            for t, row in rows: print('   %s\t%s' % (t, row))
    return 0

if __name__ == '__main__':
    sys.exit(main())
