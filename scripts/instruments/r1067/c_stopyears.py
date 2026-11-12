"""(1067) C：止める線の読み手の出力（measurements の stop*.txt と popjump*.txt）に出た事象の年の分布（読むだけ）。
拾う物：①gdpjump の事象の組 (種, 国, 年, 四半期, …) の前の札（建国の直後・それ以外・分母の痩せ ほか）
        ②gdp_jump1002 と popjump の〈行〉（四半期 q → 年＝ラベルがあればラベル、無ければ 1783＋(q＋1)//4）
        ③hol の 1/10・1/4 の事象（落ちた年＝→の後の (年)）
数え方：ファイルごとの延べ と 異なる事象（種類・国か州・年）。1914 年より前／以後。
python -X utf8 c_stopyears.py <measurements> <出力>"""
import sys, os, re, glob, collections
root, out = sys.argv[1], sys.argv[2]
files = sorted(set(glob.glob(os.path.join(root, '**', 'stop*.txt'), recursive=True)) | set(glob.glob(os.path.join(root, '**', '*popjump*.txt'), recursive=True)))
ev = []   # (file, kind, label, key, year)
RT = re.compile(r"^\s+(\S[^(]*?)\s*\((\d+), '([^']+)', (\d{4}), (\d+)")
RROW = re.compile(r"^\s+行\t(?:種 )?(\d+)\t(\S+)\tq(\d+)\t(.*)$")
RHOL = re.compile(r"^\s+\[([^\]・]+)・(1/10|1/4)([^\]]*)\]\s+s(\d+)\s+(\S+?)（[^）]*）\s+[\d.]+\((\d{4})\)→[\d.]+\((\d{4})\)")
for f in files:
    sect = ''
    try: lines = open(f, encoding='utf-8', errors='replace').read().split('\n')
    except Exception: continue
    for ln in lines:
        if ln.startswith('== '): sect = ln[3:].split('（')[0].strip(); continue
        m = RT.match(ln)
        if m: ev.append((f, 'gdpjump:' + sect, m.group(1).strip(), (m.group(2), m.group(3)), int(m.group(4)))); continue
        m = RROW.match(ln)
        if m:
            q = int(m.group(3)); lab = re.search(r'ラベル (\d{4})', m.group(4)); cls = m.group(4).split('\t')[0]
            y = int(lab.group(1)) if lab else 1783 + (q + 1) // 4
            k = 'popjump' if 'popjump' in os.path.basename(f) else 'rows:' + sect
            ev.append((f, k, cls if k != 'popjump' else '人口の比', (m.group(1), m.group(2)), y)); continue
        m = RHOL.match(ln)
        if m: ev.append((f, 'hol' + m.group(2), m.group(1) + m.group(3), (m.group(4), m.group(5)), int(m.group(7))))
with open(out, 'w', encoding='utf-8') as fo:
    fo.write(f'# 読んだファイル {len(files)}・事象の延べ {len(ev)}\n')
    c = collections.Counter((e[1], e[2], 'pre' if e[4] < 1914 else 'post') for e in ev)
    fo.write('## 延べ（種類・札・1914 年より前 pre／以後 post）\n')
    for (k, lab, p), n in sorted(c.items()): fo.write(f'{k}\t{lab}\t{p}\t{n}\n')
    d = {}
    for e in ev: d.setdefault((e[1], e[2], e[3][1], e[4]), e)
    cd = collections.Counter(('止める線の区分' if (('それ以外' in k[1]) or (k[0].startswith('hol1/10') and '例外' not in k[1]) or k[0] == 'popjump') else '記録の区分', 'pre' if k[3] < 1914 else 'post') for k in d)
    fo.write(f'## 異なる事象（種類・札・国か州・年）{len(d)}： ' + '  '.join(f'{a}/{b}:{n}' for (a, b), n in sorted(cd.items())) + '\n')
    dec = collections.Counter((k[3] // 10) * 10 for k in d)
    fo.write('## 異なる事象の年代（10 年刻み）: ' + '  '.join(f'{y}:{dec[y]}' for y in sorted(dec)) + '\n')
    stopd = [k for k in d if ('それ以外' in k[1]) or (k[0].startswith('hol1/10') and '例外' not in k[1]) or k[0] == 'popjump']
    dec2 = collections.Counter((k[3] // 10) * 10 for k in stopd)
    fo.write('## うち止める線の区分（それ以外・hol 1/10・popjump）の年代: ' + '  '.join(f'{y}:{dec2[y]}' for y in sorted(dec2)) + '\n')
    fo.write('## 止める線の区分の 1914 年以後の事象\n')
    for k in sorted(stopd, key=lambda x: x[3]):
        if k[3] >= 1914: fo.write(f'  {k[3]}\t{k[0]}\t{k[1]}\t{k[2]}\t{os.path.relpath(d[k][0], root)}\n')
print('files', len(files), 'events', len(ev))
