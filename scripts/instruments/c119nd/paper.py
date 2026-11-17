"""(1193) c119nd 紙の台本と帰属の読み手。
信任帝政の床（Politics.cs の mandateFloor）が立ちうる四半期を基線の生から数え、腕 B が基線と最初に分かれる四半期と並べる。
  条件 A（厳密・scalars だけ）＝polityCode==3（立憲君主）・crown!=0・dynMandate>=0.6（RedefinedMandateMandateMin）。
  床（近似）＝条件 A かつ 国民意識>=0.5。国民意識は scalars に列が無い＝secession_s の州の consciousnessMeanIndex を popK で
  加重平均した近似（secession_s は首都と兵を除く＝近似と名乗る・floor_count.py と同じ）。
  床の張り付き（近似）＝床（近似）かつ |legitimacy − 0.6×dynMandate×(1−defeatShock)| <= 0.01・床+0.05以下＝床（近似）かつ legitimacy <= 床の値＋0.05。
  政体の遷移＝同じ国の続く 2 行で polityCode が変わった回数。立憲の割合＝polityCode==3 の行÷全ての行。
帰属の門（腕を渡したとき）：種ごとに、腕の scalars が基線と最初に違う四半期 ≥ 基線の条件 A の最初の四半期（厳密の下限）。
  前なら 赤（恒等の部分の誤り）。床（近似）の最初の四半期とも並べる（記録）。
使い方: python paper.py <基線の生> <種の起点> <種の終点> [腕の生]"""
import sys, os, collections

base = sys.argv[1]; s0 = int(sys.argv[2]); s1 = int(sys.argv[3]); arm = sys.argv[4] if len(sys.argv) > 4 else None


def nc_proxy(d, s):
    nc = collections.defaultdict(lambda: [0.0, 0.0])
    se = os.path.join(d, f'secession_s{s}.tsv')
    if not os.path.exists(se): return None
    with open(se, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        if 'consciousnessMeanIndex' not in h: return None
        iq, inn, ip, ic = h.index('quarter'), h.index('nation'), h.index('popK'), h.index('consciousnessMeanIndex')
        for line in f:
            a = line.rstrip('\n').split('\t')
            try: p = float(a[ip]); v = float(a[ic])
            except ValueError: continue
            if v < 0 or not p > 0: continue
            k = (a[iq], a[inn]); nc[k][0] += p * v; nc[k][1] += p
    return nc


def read_scalars(d, s):
    p = os.path.join(d, f'scalars_s{s}.tsv')
    if not os.path.exists(p): return None, None
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        rows = [line.rstrip('\n').split('\t') for line in f]
    return h, rows


tot = collections.Counter(); first_div_red = 0; div_rows = []
for s in range(s0, s1 + 1):
    h, rows = read_scalars(base, s)
    if h is None: print(f's{s} 基線に scalars なし'); continue
    iq, inn, ipo, icr, idm, ilg, ids = (h.index(x) for x in ('quarter', 'nation', 'polityCode', 'crown', 'dynMandate', 'legitimacy', 'defeatShock'))
    nc = nc_proxy(base, s)
    c = collections.Counter(); firstA = None; firstF = None; prev = {}
    for a in rows:
        c['rows'] += 1
        q = int(a[iq]); n = a[inn]
        if n in prev and prev[n] != a[ipo]: c['transitions'] += 1
        prev[n] = a[ipo]
        if a[ipo] == '3': c['constMon'] += 1
        try: dm = float(a[idm]); lg = float(a[ilg]); ds = float(a[ids])
        except ValueError: continue
        if a[ipo] == '3' and a[icr] != '0' and dm >= 0.6:
            c['condA'] += 1
            if firstA is None or q < firstA: firstA = q
            k = (a[iq], n)
            if nc is not None and k in nc and nc[k][1] > 0 and nc[k][0] / nc[k][1] >= 0.5:
                c['floor'] += 1
                if firstF is None or q < firstF: firstF = q
                if abs(lg - 0.6 * dm * (1 - min(max(ds, 0), 1))) <= 0.01: c['floorPinned'] += 1
                if lg <= 0.6 * dm * (1 - min(max(ds, 0), 1)) + 0.05: c['floorNear'] += 1
    share = c['constMon'] / c['rows'] if c['rows'] else 0
    line = f's{s} rows {c["rows"]} 遷移 {c["transitions"]} 立憲の割合 {share:.4f} 条件A {c["condA"]} 床(近似) {c["floor"]} 張り付き(近似) {c["floorPinned"]} 床+0.05以下 {c["floorNear"]} 条件Aの最初 {firstA} 床の最初 {firstF}'
    if arm:
        ha, ra = read_scalars(arm, s)
        if ha is None: line += ' 腕に scalars なし'
        else:
            jq = ha.index('quarter'); jn = ha.index('nation')
            bm = {(a[iq], a[inn]): a for a in rows}
            am = {(a[jq], a[jn]): a for a in ra}
            common = [x for x in h if x in ha]
            qs = sorted({int(k[0]) for k in set(bm) | set(am)})
            fd = None
            for q in qs:
                keys = {k for k in bm if int(k[0]) == q} | {k for k in am if int(k[0]) == q}
                for k in keys:
                    if k not in bm or k not in am: fd = q; break
                    if any(bm[k][h.index(col)] != am[k][ha.index(col)] for col in common): fd = q; break
                if fd is not None: break
            ok = fd is None or (firstA is not None and fd >= firstA)
            if not ok: first_div_red += 1
            line += f' 最初に分かれる四半期 {fd} 帰属 {"緑" if ok else "赤"}'
            div_rows.append((s, fd, firstA, firstF))
    print(line); tot.update(c)
share = tot['constMon'] / tot['rows'] if tot['rows'] else 0
print(f'計 rows {tot["rows"]} 遷移 {tot["transitions"]} 立憲の割合 {share:.4f} 条件A {tot["condA"]} 床(近似) {tot["floor"]} 張り付き(近似) {tot["floorPinned"]} 床+0.05以下 {tot["floorNear"]}')
if arm: print(f'帰属: {"赤" if first_div_red else "緑"}（赤 {first_div_red} 種）')
