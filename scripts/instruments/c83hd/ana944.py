# (944) c83hd プローブの集計（読むだけ・事前登録 measurements/m944_20261107_prereg_hd.txt [12] の ①〜③）
#   python -X utf8 -I ana944.py <probe944 の出力フォルダ> [名,名,…]
import sys, os, csv
out = sys.argv[1]
names = sys.argv[2].split(',') if len(sys.argv) > 2 else ['flag0', 'flag1', 'n1', 'n2']
def tab(p):
    if not os.path.exists(p): return None, []
    with open(p, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); return h, list(rd)
for tag in names:
    d = os.path.join(out, tag)
    print(f'== {tag}')
    tf = os.path.join(out, f'test_{tag}.txt')
    if os.path.exists(tf):
        ls = open(tf, encoding='utf-8').read().splitlines(); print('試験', ls[0][2:], '|', ' '.join(l.strip()[:160] for l in ls[1:4]))
    # ② 国の数と国の名の出入り・パリの持ち主（Q16 まで）
    prev = None; line = []
    sp = os.path.join(d, 'snap.txt')
    for l in (open(sp, encoding='utf-8').read().splitlines() if os.path.exists(sp) else []):
        q, cnt, own, nm = l.split('\t'); qi = int(q[1:]); cur = set(nm.split(','))
        if qi > 16: break
        ch = ''
        if prev is not None:
            add = sorted(cur - prev); rem = sorted(prev - cur)
            if add or rem: ch = f' +{",".join(add)} -{",".join(rem)}'
        line.append(f'{q}:{cnt}:{own}{ch}'); prev = cur
    print('国の数とパリ（Q:数:パリの持ち主 ±出入り）', ' '.join(line))
    # ③ 割譲の事象（Q16 まで）
    h, rows = tab(os.path.join(d, 'cederule_s1789.tsv'))
    if h:
        iq, ih, isd, irc, irg = (h.index(c) for c in ('quarter', 'how', 'sender', 'receiver', 'region'))
        ev = [f'Q{r[iq]} {r[ih]} {r[isd]}→{r[irc]} {r[irg]}' for r in rows if int(r[iq]) <= 16]
        print(f'割譲 Q16 まで {len(ev)} 件：', ' ／ '.join(ev[:30]))
    else: print('割譲の表なし')
    # 戦争と内戦の月（フランス・Q16 まで）
    h, rows = tab(os.path.join(d, 'nation_s1789.tsv'))
    if h:
        iq, inn, iw, ic = (h.index(c) for c in ('quarter', 'nation', 'atWarMonths', 'civilWarMonths'))
        fr = [f'Q{r[iq]}:{r[iw]}/{r[ic]}' for r in rows if r[inn] == 'フランス' and int(r[iq]) <= 16]
        print('フランスの戦争の月/内戦の月', ' '.join(fr))
    # ① フランスの州の estateDebtMovedPerQuarter（Q0〜Q13）とパリの行
    h, rows = tab(os.path.join(d, 'cearn_s1789.tsv'))
    if h:
        iq, inn, irg, im = (h.index(c) for c in ('quarter', 'nation', 'region', 'estateDebtMovedPerQuarter'))
        agg = {}
        for r in rows:
            if r[inn] != 'フランス' or not (0 <= int(r[iq]) <= 13): continue
            v = float(r[im]); s, c = agg.get(r[irg], (0.0, 0)); agg[r[irg]] = (s + v, c + (1 if v > 0 else 0))
        tot = sum(s for s, _ in agg.values()); nz = {k: v for k, v in agg.items() if v[1] > 0}
        print(f'① フランスの州 {len(agg)}・Σ moved Q0〜13 {tot:.6g}・＞0 の州 {len(nz)}：', ' '.join(f'{k}={v[0]:.4g}({v[1]}Q)' for k, v in sorted(nz.items(), key=lambda kv: -kv[1][0])[:15]))
        pr = [f'Q{r[iq]}:{r[inn]}:{float(r[im]):.4g}' for r in rows if r[irg] == 'パリ' and int(r[iq]) <= 19]
        print('パリの行（Q:国:moved）', ' '.join(pr))
    else: print('cearn の表なし')
