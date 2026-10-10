"""(192) 史実比較の桁の NG の集合（読むだけ）＝ngset184 の直し（反証役 vm1#2・vm1#6・vm2#9・vm2#10・vn2#6）。
python ngset192.py <基準> <比べる>   ／ 各引数＝ rd:<dir>:<タグ>  か  tab:<表のパス>:<タグ>
種類＝navy・rail・steam・fin・hist171・hist・sold・wardead（ngset184 のまま）＋
  law＝平時の徴兵法の割合（an_mil182 の ①＝mil180_<タグ>.txt）を主要10か国の史実の段で両側に読む（ngset184 の mil＝英米の志願だけ を置き換え）＝
       史実の段の割合が 10% 以下＝NG（史実が 100% の側の比 0.1 以下）。史実 0% の段は床 10%（10種の1種ぶん）で割る＝100% で初めて10倍＝
       同じ窓が上の規則で既に NG（数えるのは1セル）。史実の段の表 H は an_lawmatch188 の H に 1939 年以後を足した（英 1939 軍事訓練法→1947 国民役務＝皆兵・
       米 1940 選抜訓練法→1948 選抜徴兵法＝選抜【推】・vm2#10）。
  trade＝貿易/GDP の国ごとの窓（an_trade186 の「国ごと 1904-13」の NG＝README §1-2 の判定の行・vn2#6）。
  warlaw＝戦時に徴兵法を上げた件数を戦争1回につき1で数える（an_warlaw192＝warlaw_<タグ>.txt の NG＝vm1#6）。基準に無ければ数えない（印字に注記）。
直し＝hist の印字「NG（10倍以上）: なし」を1セルに数えない（vm2#9）・hist171（1910-19 の窓）と fin の 1910-1919 の同じ国の同じ量（軍事費・借入）は1セルに数える（vn2#6）。"""
import sys, re, os
KINDS = ['navy', 'rail', 'steam', 'fin', 'hist171', 'hist', 'sold', 'wardead', 'law', 'trade', 'warlaw']
FILE = {'law': 'mil180'}
H = {  # 国: [(この年から, 段 0志願/1選抜/2皆兵)]  an_lawmatch188 の H＋1939 年以後【推】
 'イギリス': [(1783, 0), (1939, 2)], 'アメリカ': [(1783, 0), (1940, 1)],
 'フランス': [(1783, 1), (1798, 2), (1818, 1), (1872, 2)],
 'プロイセン': [(1783, 1), (1814, 2)], 'ロシア': [(1783, 1), (1874, 2)], '日本': [(1783, 0), (1873, 2)],
 '清': [(1783, 0), (1933, 2)], 'オーストリア': [(1783, 1), (1868, 2)], 'スペイン': [(1783, 1), (1912, 2)],
 'オスマン': [(1783, 0), (1843, 1)],
}
LAWN = ['志願', '選抜', '皆兵']
FLOOR = 10.0


def hist_stage(n, y):
    lv = None
    for y0, l in H[n]:
        if y >= y0: lv = l
    return lv


def load(spec):
    kind, rest = spec.split(':', 1); path, tag = rest.rsplit(':', 1)
    out = {}
    if kind == 'rd':
        for k in KINDS:
            p = os.path.join(path, f'{FILE.get(k, k)}_{tag}.txt')
            if os.path.exists(p): out[k] = open(p, encoding='utf-8').read()
    else:
        t = open(path, encoding='utf-8').read()
        for k in KINDS:
            m = re.search(r'^### ' + re.escape(f'{FILE.get(k, k)}_{tag}.txt') + r'[^\n]*\n```\n(.*?)\n```', t, re.S | re.M)
            if m: out[k] = m.group(1)
    return out


def name_of(cell):
    m = re.match(r'^\s*([^\d\s［]*\s?\d{4})', cell)
    if m and re.match(r'^\s*\d{4}', cell): return m.group(1).strip()
    m = re.match(r'^\s*([^\d［]*)', cell)
    return m.group(1).strip()


def ngs(texts):
    S = {}
    for k, t in texts.items():
        lines = t.split('\n')
        if k in ('navy', 'rail', 'steam'):
            for ln in lines:
                if ':' not in ln or 'NG のセル' in ln or ln.startswith('#'): continue
                head, rest = ln.split(':', 1)
                for c in rest.split('・'):
                    c = c.strip()
                    if c.endswith('NG'):
                        S[(k, head.strip(), name_of(c))] = c
        elif k == 'fin':
            for ln in lines:
                if '←' not in ln: continue
                left, right = ln.split('←', 1)
                p = [x.strip() for x in left.split('|')]
                for c in right.split('・'):
                    c = c.strip()
                    if c.endswith('NG'):
                        S[(k, p[0], p[1], c.split(' ')[0])] = c
        elif k == 'hist171':
            names = ('兵士÷人口', '軍事費÷年GDP', '借入÷年GDP')
            for ln in lines:
                p = [x.strip() for x in ln.split('|')]
                if len(p) != 13: continue
                for i in range(3):
                    if p[4 + 4 * i] == 'NG': S[(k, p[0], names[i])] = f'{p[1 + 4 * i]}［{p[2 + 4 * i]}］{p[3 + 4 * i]}'
        elif k == 'hist':
            for ln in lines:
                if ln.startswith('NG（10倍以上）:'):
                    for c in ln.split(':', 1)[1].split('・'):
                        c = c.strip()
                        if c and c != 'なし': S[(k, ' '.join(c.split(' ')[:3]))] = c
        elif k == 'sold':
            for ln in lines:
                if ln.startswith('NG: ['):
                    for c in re.findall(r"'([^']*)'", ln): S[(k, ' '.join(c.split(' ')[:2]))] = c
        elif k == 'wardead':
            for ln in lines:
                if '＝NG' in ln: S[(k, ln.split(':')[0].strip())] = ln.strip()
        elif k == 'law':
            sec = t.split('## ① 平時の徴兵法の割合%', 1)
            if len(sec) < 2: continue
            body = sec[1].split('\n## ', 1)[0].split('\n')
            years = None
            for ln in body:
                if re.match(r'^\s+1784\s', ln): years = [int(x) for x in ln.split()]; continue
                m = re.match(r'^\s+(\S+):\s+(.*)$', ln)
                if not m or years is None or m.group(1) not in H: continue
                cells = re.findall(r'(\d+)/\s*(\d+)/\s*(\d+)', m.group(2))
                for y, c in zip(years, cells):
                    v = [int(x) for x in c]
                    if sum(v) == 0: continue
                    h = hist_stage(m.group(1), y)
                    if v[h] <= FLOOR: S[(k, m.group(1), str(y))] = f'{LAWN[h]} {v[h]}%［100%］（{"/".join(str(x) for x in v)}）'
        elif k == 'trade':
            for ln in lines:
                if ln.startswith('国ごと 1904-13'):
                    for c in ln.split('：', 1)[1].split('・'):
                        c = c.strip()
                        if c.endswith('（NG）'): S[(k, c.split(' ')[0], '1904-13')] = c
        elif k == 'warlaw':
            for ln in lines:
                if ln.startswith('NG:'):
                    for c in ln.split(':', 1)[1].split('・'):
                        c = c.strip()
                        if c and c != 'なし': S[(k, c.split(' ')[0])] = c
    # hist171 と fin の 1910-1919 の同じ国の同じ量は1セル（vn2#6）
    MAP = {'軍事費÷年GDP': '軍÷GDP', '借入÷年GDP': '借入÷GDP'}
    for key in [x for x in S if x[0] == 'hist171' and x[2] in MAP]:
        if ('fin', key[1], '1910-1919', MAP[key[2]]) in S: del S[key]
    return S


A = load(sys.argv[1]); B = load(sys.argv[2])
a = ngs(A); b = ngs(B)
skip = [k for k in KINDS if (k in A) != (k in B)]
if skip:   # 片側にしか無い種類は両側から外す（同じ規則で数える）
    a = {x: v for x, v in a.items() if x[0] not in skip}; b = {x: v for x, v in b.items() if x[0] not in skip}
print(f'# 基準 {sys.argv[1]}＝NG {len(a)}／比べる {sys.argv[2]}＝NG {len(b)}' + (f'（片側にしか無い種類は外した：{"・".join(skip)}）' if skip else ''))
for title, keys in (('新しい NG（基準では NG でない）', [k for k in b if k not in a]), ('外れた NG（比べる側で NG でない）', [k for k in a if k not in b]),
                    ('前からの NG', [k for k in b if k in a])):
    print(f'## {title}: {len(keys)}')
    by = {}
    for k in keys: by.setdefault(k[0], []).append(k)
    for kind in KINDS:
        if kind not in by: continue
        print(f'  [{kind}] {len(by[kind])}: ' + '・'.join(' '.join(k[1:]) + ' ' + (b.get(k) if k in b else a.get(k)) + ('' if not (k in a and k in b) else f'（基準 {a[k]}）') for k in by[kind]))
