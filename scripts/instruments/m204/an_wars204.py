"""(204) 0f 戦争の数を史実の数え方（COW 型）と比べる読み手（読むだけ）＝an_wars201 の直し＝**判定の単位は塊（戦死 1,000 人以上）・組は記録**（推奨で進めた・作者の承認は未確認）。
  理由＝COW（Sarkees & Wayman 2010）の「戦争」は当事国の組でなく、同じ時期に重なる戦闘の全体を1つと数え、戦死 1,000 人以上を線にする＝塊（戦死1,000人以上）が同じ単位。
  組（2国の開戦）は同じ戦争の当事国が多いほど増える＝上限の単位＝記録だけ（(202) 反証役 ve2#9）。内戦は蜂起（分離の札0）と分離独立（札1）にも分けて出す。
python an_wars204.py <out_dir（w171 を含む）> [タグ] [--cow <cow_all.txt>]
入力＝inj_war171（m190/inj_war171n）の w<seed>.txt の WS 行（戦争1本＝2国の組の始まり：通し番号・月・年・段〔同盟の連鎖の深さ・0＝開戦〕・内戦・分離・両者・国か）
     と WE 行（終わり：戦死 A/B〔千人〕）。
数え方：
  組（dyad）＝WS 行の段0（開戦の当事者＝Diplomacy.WarsStarted が数える開戦）。段1以上（同盟・宗主の参戦）は既存の戦争の一部＝数えない。
  塊（COW の「戦争」に近い）＝同じ時期に重なり当事国を共有する組をまとめた連結成分（段1以上の組も含めて繋ぐ）＝下限の見積もり
     （同じ国が別々の2つの戦争を同時に戦うと1つに数える＝少なめ）。
  種類＝国家間（内戦でない・両者とも国）／植民地（内戦でない・片方が国でない＝抽象の標的）／内戦（内戦の札・分離独立を含む）＝塊の種類は塊の中で最初に始まった組の種類。
  戦死の線＝COW の 1,000 人（戦死 1.0 千人）以上の塊だけを数える行も出す（塊の中の組の戦死の和）。
史実＝Sarkees & Wayman, Resort to War: A Data Guide to Inter-State, Extra-State, Intra-state, and Non-State Wars, 1816-2007（CQ Press 2010）の付録A
  （戦争の年表＝scratchpad/cow_all.txt・国家間 95・国家外 163・国家内 334・非国家 62＝本の総数と一致）＝始まった年で数える・非国家（国家でない者どうし）は比べない。
  COW の戦争は 1,000 人の戦死の線を持つ。1783〜1815 は COW の外（比べない）。
判定（README §1-2 の史実比較の桁）＝10種の中央値÷史実（床＝10年に1件）が 10倍以上か 0.1以下＝NG・3〜10倍＝記録。"""
import sys, os, glob, re, statistics as st, collections
args = sys.argv[1:]
cowp = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'cow_all.txt')
if '--cow' in args:
    i = args.index('--cow'); cowp = args[i + 1]; del args[i:i + 2]
D = args[0]; TAG = args[1] if len(args) > 1 else D
KINDS = ['国家間', '植民地', '内戦']
COWK = {'Inter-State': '国家間', 'Extra-State': '植民地', 'Intra-State': '内戦'}
ERAS = [(1784, 1815), (1816, 1849), (1850, 1913), (1914, 1950)]


def fl(x):
    try: return float(x)
    except Exception: return 0.0


# 史実（COW）
# 中南米だけの戦争（ゲームの国が当事者にならない＝開幕の61カ国に中南米の国は無い・建国でしか出ない）を外した数も出す【推】＝名前の語で判定
#   （スペイン・フランス・アメリカ合衆国が当事者の独立戦争・干渉戦争・米墨戦争は残す・語の誤一致〔エストニア・ラトビアの独立戦争・アフガンの反改革戦争〕は除外しない）。
LA = ['Mexic', 'Argentin', 'Brazil', 'Chile', 'Peru', 'Bolivia', 'Colombia', 'Columbia', 'Ecuador', 'Venezuel', 'Uruguay', 'Paraguay', 'Central America', 'Guatemala',
      'Honduras', 'Salvador', 'Nicaragua', 'Costa Ric', 'Haiti', 'Dominican', 'Cuba', 'La Plata', 'Lopez', 'Pacific', 'Buenos Aires', 'Yaqui', 'Maya', 'Cabanos',
      'Farroupilha', 'Sabinada', 'Canudos', 'Cristero', 'Chaco', 'Reform War']
KEEP = ['Spanish', 'Franco', 'Mexican-American', 'Liberation of', 'Bolivar', 'Bolívar', 'Independence', 'Estonian', 'Latvian', 'Afghan']
cow = collections.defaultdict(lambda: collections.defaultdict(int))
cowx = collections.defaultdict(lambda: collections.defaultdict(int))
txt = open(cowp, encoding='utf-8').read()
for y, nm, ty, no in re.findall(r'(\d{4})\s+(.+?)\s+(Inter-State|Extra-State|Intra-State|Non-State)\s+War\s+#\s*(\d+)', txt):
    y = int(y)
    if y > 1950 or ty == 'Non-State': continue
    cow[y][COWK[ty]] += 1
    if not (any(k in nm for k in LA) and not any(k in nm for k in KEEP)): cowx[y][COWK[ty]] += 1


def cow_count(y0, y1, k=None, x=False):
    src = cowx if x else cow
    if isinstance(k, tuple): return sum(cow_count(y0, y1, kk, x) for kk in k)
    return sum((src[y][k] if k else sum(src[y].values())) for y in range(y0, y1 + 1))


seeds = []
dy = {}      # seed -> list of dyad dicts
for f in sorted(glob.glob(os.path.join(D, 'w171', 'w*.txt'))):
    sd = os.path.basename(f)[1:-4]
    ws = {}; we = {}
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] == 'WS':
            ws[t[1]] = dict(t0=int(t[2]), y0=int(t[3]), depth=int(t[4]), civil=int(t[5]), sec=int(t[6]), A=t[7], B=t[8], an=int(t[9]), bn=int(t[10]))
        elif t[0] == 'WE':
            we[t[1]] = dict(ended=int(t[2]), yEnd=int(t[3]), months=int(t[4]), dead=fl(t[16]) + fl(t[17]))
    L = []
    for k, w in ws.items():
        e = we.get(k, dict(ended=0, yEnd=1950, months=0, dead=0.0))
        w.update(e); w['id'] = k
        w['t1'] = w['t0'] + max(1, w['months'])
        w['kind'] = '内戦' if w['civil'] else ('国家間' if (w['an'] and w['bn']) else '植民地')
        L.append(w)
    L.sort(key=lambda w: (w['t0'], int(w['id'])))
    dy[sd] = L; seeds.append(sd)

if not seeds:
    print('# w171 の行が無い'); sys.exit(0)

# 塊＝union-find（当事国を共有し時期が重なる組）
def episodes(L):
    par = list(range(len(L)))
    def fd(i):
        while par[i] != i:
            par[i] = par[par[i]]; i = par[i]
        return i
    act = collections.defaultdict(list)   # 国 -> 組の添字（時期で比べる）
    for i, w in enumerate(L):
        for side in (w['A'], w['B']):
            for j in act[side]:
                v = L[j]
                if v['t0'] <= w['t0'] < v['t1'] or w['t0'] <= v['t0'] < w['t1']:
                    a, b = fd(i), fd(j)
                    if a != b: par[a] = b
            act[side].append(i)
    g = collections.defaultdict(list)
    for i in range(len(L)): g[fd(i)].append(i)
    E = []
    for mem in g.values():
        mem.sort(key=lambda i: (L[i]['t0'], int(L[i]['id'])))
        first = next((L[i] for i in mem if L[i]['depth'] == 0), L[mem[0]])
        E.append(dict(y0=L[mem[0]]['y0'], kind=first['kind'], sec=first['sec'], dead=sum(L[i]['dead'] for i in mem), n=len(mem)))
    return E


EP = {sd: episodes(dy[sd]) for sd in seeds}


def med(v):
    return st.median(v) if v else float('nan')


def kin(kind, k):
    return k is None or (kind in k if isinstance(k, tuple) else kind == k)


def cnt(sd, y0, y1, k, mode):
    if mode == 'dyad':
        return sum(1 for w in dy[sd] if w['depth'] == 0 and y0 <= w['y0'] <= y1 and kin(w['kind'], k))
    th = 1.0 if mode == 'ep1000' else -1
    return sum(1 for e in EP[sd] if y0 <= e['y0'] <= y1 and kin(e['kind'], k) and e['dead'] >= th)


def jl(lab, j):
    return f'判定 {j}' if lab == '塊1000人' else f'記録の単位（読み {j}）'   # 判定は塊（戦死1,000人以上）だけ・組と塊（全て）は記録


def judge(sim, hist, floor=1.0):
    r = sim / max(hist, floor)
    lab = 'NG' if (r >= 10 or (r <= 0.1 and max(sim, hist) >= floor)) else ('記録' if (r >= 3 or r <= 1 / 3) else '可')
    return r, lab


print(f'######## {TAG}（種 {len(seeds)}）＝戦争の数（10種の中央値）と史実（COW・Sarkees & Wayman 2010 付録A・始まった年・非国家を除く）')
tot = [len(dy[sd]) for sd in seeds]
d0 = [sum(1 for w in dy[sd] if w['depth'] == 0) for sd in seeds]
print(f'## 組の総数（WS 行・1試合）中央 {med(tot):.0f}〔{min(tot)}〜{max(tot)}〕・うち段0（開戦＝WarsStarted が数える）中央 {med(d0):.0f}・塊 中央 {med([len(EP[s]) for s in seeds]):.0f}・戦死1,000人以上の塊 中央 {med([sum(1 for e in EP[s] if e["dead"] >= 1.0) for s in seeds]):.0f}')
print('## 時代ごと（期間の件数・10種の中央値）｜史実（COW）｜比＝シミュ÷max(史実, 床＝10年に1件×年数/10)・**判定は塊1000人だけ**（組・塊〔全て〕は記録）')
for (y0, y1) in ERAS:
    yrs = y1 - y0 + 1; fl_ = yrs / 10.0
    for k in [None] + KINDS + [('国家間', '植民地')]:
        kn = k if isinstance(k, str) else ('国家間＋植民地' if k else '計')
        row = []
        for mode, lab in (('dyad', '組'), ('ep', '塊'), ('ep1000', '塊1000人')):
            v = med([cnt(s, y0, y1, k, mode) for s in seeds])
            row.append((lab, v))
        if y0 < 1816:
            print(f'{y0}-{y1} | {kn} | ' + '・'.join(f'{lab} {v:.0f}' for lab, v in row) + ' | 史実 COW の外')
            continue
        h = cow_count(y0, y1, k); hx = cow_count(y0, y1, k, True)
        s = '・'.join(f'{lab} {v:.0f}（{judge(v, h, fl_)[0]:.1f}倍 {jl(lab, judge(v, h, fl_)[1])}｜中南米抜き {judge(v, hx, fl_)[0]:.1f}倍 {jl(lab, judge(v, hx, fl_)[1])}）' for lab, v in row)
        print(f'{y0}-{y1} | {kn} | {s} | 史実 {h}（中南米だけの戦争を抜くと {hx}）')
print('## 10年ごと（10種の中央値）＝組／塊／塊1000人 ｜史実（COW）｜比（塊1000人÷max(史実,1)）')
for d in range(1780, 1950, 10):
    y0, y1 = max(d, 1783), d + 9
    parts = []
    for k in KINDS:
        a = med([cnt(s, y0, y1, k, 'dyad') for s in seeds]); b = med([cnt(s, y0, y1, k, 'ep') for s in seeds]); c = med([cnt(s, y0, y1, k, 'ep1000') for s in seeds])
        if d >= 1810:
            h = cow_count(max(y0, 1816), y1, k)
            r, lab = judge(c, h)
            parts.append(f'{k} {a:.0f}/{b:.0f}/{c:.0f}［{h}］{r:.1f}{"＝NG" if lab == "NG" else ""}')
        else:
            parts.append(f'{k} {a:.0f}/{b:.0f}/{c:.0f}')
    print(f'{d}年代 | ' + ' | '.join(parts))
# 組の当事国（段0）の上位
cc = collections.Counter()
for s in seeds:
    for w in dy[s]:
        if w['depth'] == 0: cc[w['A']] += 1; cc[w['B']] += 1
print('## 段0 の組の当事国（10種の和・上位15）：' + '・'.join(f'{n} {c}' for n, c in cc.most_common(15)))
# 国ごとの塊の数（その国が当事者の組を1つでも含む塊）
EPN = {}
for s_ in seeds:
    L = dy[s_]
    # 塊ごとの当事国を作り直す（episodes と同じ union-find）
    par = list(range(len(L)))
    def fd(i):
        while par[i] != i:
            par[i] = par[par[i]]; i = par[i]
        return i
    act = collections.defaultdict(list)
    for i, w in enumerate(L):
        for side in (w['A'], w['B']):
            for j in act[side]:
                v = L[j]
                if v['t0'] <= w['t0'] < v['t1'] or w['t0'] <= v['t0'] < w['t1']:
                    a, b = fd(i), fd(j)
                    if a != b: par[a] = b
            act[side].append(i)
    mem = collections.defaultdict(set)
    for i, w in enumerate(L):
        if w['y0'] >= 1816: mem[fd(i)].update((w['A'], w['B']))
    c = collections.Counter()
    for ms in mem.values():
        for n in ms: c[n] += 1
    EPN[s_] = c
print('## 国ごとの塊の数（1816〜1950・その国が当事者の組を含む塊・10種の中央値）［史実【推】＝COW で英が当事者の戦争は国家間 約6・国家外 約40］：'
      + '・'.join(f'{n} {med([EPN[s_][n] for s_ in seeds]):.0f}' for n in ['イギリス', 'フランス', 'ロシア', 'オスマン', 'オーストリア', 'プロイセン', 'アメリカ', 'スペイン', '清', '日本']))
# 塊の大きさ
szs = [e['n'] for s in seeds for e in EP[s]]
print(f'## 塊の組の数：1 が {sum(1 for x in szs if x == 1)}・2〜4 が {sum(1 for x in szs if 2 <= x <= 4)}・5以上 {sum(1 for x in szs if x >= 5)}（10種の和）・最大 {max(szs) if szs else 0}')
dd = [e['dead'] for s in seeds for e in EP[s]]
if dd:
    q = sorted(dd)
    print(f'## 塊の戦死（千人）：中央 {med(q):.2f}・p90 {q[int(0.9 * (len(q) - 1))]:.1f}・1.0 未満の割合 {sum(1 for x in q if x < 1.0) / len(q):.2f}')

# (204) 内戦の内訳＝塊の最初の組の分離の札（蜂起＝0・分離独立＝1）・戦死1,000人以上の塊・10種の中央値
print('## 内戦の内訳（塊1000人・10種の中央値）＝蜂起／分離独立｜史実（COW 国家内・中南米抜き）')
for (y0, y1) in ERAS:
    rv = med([sum(1 for e in EP[s_] if y0 <= e['y0'] <= y1 and e['kind'] == '内戦' and not e['sec'] and e['dead'] >= 1.0) for s_ in seeds])
    sc = med([sum(1 for e in EP[s_] if y0 <= e['y0'] <= y1 and e['kind'] == '内戦' and e['sec'] and e['dead'] >= 1.0) for s_ in seeds])
    print(f'{y0}-{y1} | 蜂起 {rv:.0f}・分離独立 {sc:.0f}' + ('' if y0 < 1816 else f' | 史実 {cow_count(y0, y1, "内戦")}（中南米抜き {cow_count(y0, y1, "内戦", True)}）'))
