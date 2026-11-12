# (1084) 紙の段 round2 の〈台帳へ写せる〉9 系統の TSV を、台帳 §3 の仮の行（Sxx-Hnnn・行番号つき）へ対応づける（読むだけ）
import re, os
S = 'C:/Users/dirge/AppData/Local/Temp/claude/ee79ebbd2c0731cc/9b9a86e7-43dc-4b00-9074-443f5c41f431/scratchpad'
LED = 'C:/Users/dirge/Documents/PriceOfBread/docs/機能の台帳.md'
W = 'D:/PoB_work/r1084_work'
SYS = ['S03', 'S04', 'S06', 'S09', 'S13', 'S21', 'S26', 'S28', 'S29']
L = open(LED, 'rb').read().decode('utf-8').split('\n')
# 台帳の H 行：id → (台帳の行番号, L の集合, 判定, 12 列目)
H = {}
for i, s in enumerate(L, 1):
    m = re.match(r'\| (S\d\d-H\d+) \|', s)
    if not m:
        continue
    c = [x.strip() for x in s.strip().strip('|').split('|')]
    ls = set(int(x) for x in re.findall(r'L(\d+)', c[3]))
    H[m.group(1)] = (i, ls, c[8], c[11], c[2])
# §1 の系統の表の〈文書の行〉の範囲（例 2033-2048,2158-2256）
RANGE = {}
for s in L:
    m = re.match(r'\| (S\d\d) \| [^|]+ \| ([0-9,\-]+) \|', s)
    if m:
        RANGE[m.group(1)] = [tuple(int(y) for y in x.split('-')) for x in m.group(2).split(',')]
def lnums(t):
    out = set()
    for a, b in re.findall(r'L(\d+)(?:[-〜～](\d+))?', t):
        out.add(int(a))
    return out
rows = open(W + '/round2_rows.tsv', 'w', encoding='utf-8', newline='\n')
rows.write('系統\t新しい行\t機能\t判定\t12列目(先頭80字)\t文書のL\t当たる台帳の仮の行(id@台帳の行)\t列の数\n')
hcov = {}
summ = []
for sy in SYS:
    p = f'{S}/ledger_{sy}_2026-11-12.tsv'
    T = open(p, 'rb').read().decode('utf-8').split('\n')
    T = [t for t in T if t.strip()]
    hdr, body = T[0], T[1:]
    bad = 0
    cnt = {}
    for t in body:
        c = t.split('\t')
        if len(c) != 12 or '|' in t:
            bad += 1
        ls = lnums(c[3]) if len(c) > 3 else set()
        hits = set()
        starts = sorted((min(v[1]), h) for h, v in H.items() if h.startswith(sy) and v[1])
        for x in ls:
            if not any(a <= x <= b for a, b in RANGE.get(sy, [])):
                continue
            cand = [h for st0, h in starts if st0 <= x]
            if cand:
                hits.add(cand[-1])
        hits = sorted(hits)
        for h in hits:
            hcov.setdefault(h, []).append(c[0])
        cnt[c[8] if len(c) > 8 else '?'] = cnt.get(c[8] if len(c) > 8 else '?', 0) + 1
        rows.write('\t'.join([sy, c[0], c[2][:40], c[8], c[11][:80].replace('\t', ' '), ','.join('L%d' % x for x in sorted(ls)),
                              ' '.join(f'{h}@{H[h][0]}' for h in sorted(hits)) or '（仮の行なし）', str(len(c))]) + '\n')
    hs = [h for h in H if h.startswith(sy)]
    unc = [h for h in hs if h not in hcov]
    summ.append(f'{sy}\t新しい行 {len(body)}\t12列でない・縦棒 {bad}\t判定 ' + '・'.join(f'{k} {v}' for k, v in sorted(cnt.items(), key=lambda x: -x[1])) +
                f'\t仮の行 {len(hs)}（当たった {len(hs)-len(unc)}・当たらない {len(unc)}: {" ".join(unc)}）')
rows.close()
st = open(W + '/round2_strike.tsv', 'w', encoding='utf-8', newline='\n')
st.write('台帳の行\t仮の行\t機能\t今の判定\t写しの後の判定の列（打消し＋日付）\t当たる新しい行\n')
for h in sorted(hcov):
    i, ls, j, v12, fn = H[h]
    st.write(f'{i}\t{h}\t{fn[:30]}\t{j}\t~~{j}~~ 2026-11-12 紙の段で門ごとに割った＝{",".join(hcov[h])}（(1087) が §3 の後の節へ写した）\t{",".join(hcov[h])}\n')
st.close()
open(W + '/round2_summary.txt', 'w', encoding='utf-8', newline='\n').write('\n'.join(summ) + '\n')
print('\n'.join(summ))
