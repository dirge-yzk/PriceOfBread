# (1075) trx から試験のクラスごとの秒・並列の度合いを読む。読むだけ。
# 使い方: python -X utf8 -I trx_read.py <nonfull.trx> <試験のソースの木 PriceOfBread.Tests> <出力フォルダ>
# 限界（自己申告）: duration は xunit が測った試験の本体の秒（クラスの構築・静的な初期化・Lazy の初回は最初に触った試験に乗る）。
#   壁時計は TestRun の Times の start〜finish（ビルドを含まない）。〈世界を作る〉の判定はソースの字面（new MultiNationSim( 等）＝上界。
import sys, os, re, collections, datetime, xml.etree.ElementTree as ET

TRX, TSRC, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(OUT, exist_ok=True)
NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
root = ET.parse(TRX).getroot()

def ts(s):
    # 2026-11-12T14:46:30.7297021+09:00 → 秒（小数 6 桁へ丸める）
    m = re.match(r'(.*T\d\d:\d\d:\d\d)(\.\d+)?([+-]\d\d:\d\d)', s)
    frac = (m.group(2) or '.0')[:7]
    return datetime.datetime.fromisoformat(m.group(1) + frac + m.group(3)).timestamp()

def dur(s):
    h, m, sec = s.split(':')
    return int(h) * 3600 + int(m) * 60 + float(sec)

times = root.find('t:Times', NS)
W0, W1 = ts(times.get('start')), ts(times.get('finish'))
cls_of = {}
for ut in root.iter('{%s}UnitTest' % NS['t']):
    tm = ut.find('t:TestMethod', NS)
    cls_of[ut.get('id')] = tm.get('className')
rows = []
for r in root.iter('{%s}UnitTestResult' % NS['t']):
    c = cls_of[r.get('testId')]
    rows.append(dict(cls=c.split('.')[-1], name=r.get('testName'), d=dur(r.get('duration', '0:0:0')),
                     s=ts(r.get('startTime')), e=ts(r.get('endTime')), o=r.get('outcome')))

# 試験のソース：クラス→ファイル・コレクション・世界を作るか
DECL = re.compile(r'^\s*(?:(?:public|internal|private|static|sealed|abstract|partial)\s+)*class\s+([A-Za-z_][A-Za-z0-9_]*)', re.M)
COMMENT = re.compile(r'//[^\n]*|/\*.*?\*/', re.S)
WORLD = re.compile(r'\bMultiNationSim\b|WorldData\.|NationConfig\.[A-Z]|WorldLedgerRun\(|\bnew\s+Simulation\(|SaveGame\.Load\(|\.StepQuarter\(|\.StepDay\(')
cls_file, cls_coll, cls_world = {}, {}, {}
for f in sorted(os.listdir(TSRC)):
    if not f.endswith('.cs'):
        continue
    raw = open(os.path.join(TSRC, f), 'rb').read().decode('utf-8-sig', 'replace')
    t = COMMENT.sub('', raw)
    world = bool(WORLD.search(t))
    # クラスごとの属性：[Collection("Instruments")] がクラス宣言の直前にあるか
    for m in DECL.finditer(t):
        c = m.group(1)
        head = t[max(0, m.start() - 400):m.start()]
        cls_file.setdefault(c, f)
        cls_world.setdefault(c, world)
        last_attr = head.rfind('[Collection("Instruments")]')
        prev_close = max(head.rfind('}'), head.rfind(';'))
        cls_coll.setdefault(c, 'Instruments' if last_attr > prev_close else '')

N = len(rows)
tot = sum(r['d'] for r in rows)
wall = W1 - W0
per = collections.defaultdict(lambda: [0, 0.0, 1e18, 0.0])
for r in rows:
    p = per[r['cls']]
    p[0] += 1; p[1] += r['d']; p[2] = min(p[2], r['s']); p[3] = max(p[3], r['e'])

def w(name, lines):
    with open(os.path.join(OUT, name), 'w', encoding='utf-8', newline='\n') as fh:
        fh.write('\n'.join(lines) + '\n')

L = []
L.append(f'件 {N}・クラス {len(per)}・秒の和 {tot:.1f}・壁時計 {wall:.1f}（{wall/60:.2f} 分）・平均の並列度（和÷壁）{tot/wall:.2f}')
L.append(f'outcome: ' + ', '.join(f'{k} {v}' for k, v in collections.Counter(r['o'] for r in rows).items()))
miss = [c for c in per if c not in cls_file]
L.append(f'ソースに宣言が見つからないクラス {len(miss)}: ' + ','.join(miss[:10]))
srt = sorted(per.items(), key=lambda kv: -kv[1][1])
cum = 0.0
L.append('\n== クラスの秒の上位 30（件・秒・割合・累積・最初の開始〜最後の終了〔壁の分・開始からの分〕・集まり・世界）')
for i, (c, p) in enumerate(srt):
    cum += p[1]
    if i < 30:
        L.append(f'{i+1}\t{c}\t{p[0]}\t{p[1]:.1f}\t{p[1]/tot:.3f}\t{cum/tot:.3f}\t{(p[2]-W0)/60:.2f}〜{(p[3]-W0)/60:.2f}\t{cls_coll.get(c, "?")}\t{cls_world.get(c, "?")}')
# 分布
L.append('\n== クラスの秒の分布（区間・クラス数・秒の和）')
bins = [0, 0.1, 1, 5, 10, 30, 60, 120, 300, 1e9]
for a, b in zip(bins, bins[1:]):
    cs = [p[1] for _, p in per.items() if a <= p[1] < b]
    L.append(f'[{a},{b})\t{len(cs)}\t{sum(cs):.1f}')
for k in (10, 30, 50, 100):
    L.append(f'上位 {k} クラスの割合 {sum(p[1] for _, p in srt[:k])/tot:.3f}')
L.append('\n== 試験 1 本の秒の上位 30（秒・クラス・名の頭 80 字）')
for r in sorted(rows, key=lambda r: -r['d'])[:30]:
    L.append(f"{r['d']:.1f}\t{r['cls']}\t{r['name'].split('.')[-1][:80] if '(' not in r['name'] else r['name'][len('PriceOfBread.Tests.'+r['cls'])+1:][:80]}")
L.append('\n== 試験 1 本の秒の分布（区間・本数・秒の和）')
bins = [0, 0.01, 0.1, 1, 5, 10, 30, 60, 1e9]
for a, b in zip(bins, bins[1:]):
    ds = [r['d'] for r in rows if a <= r['d'] < b]
    L.append(f'[{a},{b})\t{len(ds)}\t{sum(ds):.1f}')
w('q1_classes.txt', L)

# 問い 2：世界を作る・集まり・並列の度合い
L = []
g = collections.defaultdict(lambda: [0, 0, 0.0])
for c, p in per.items():
    k = ('Instruments（直列）' if cls_coll.get(c) == 'Instruments' else '並列') + '・' + ('世界を作る' if cls_world.get(c) else '作らない')
    g[k][0] += 1; g[k][1] += p[0]; g[k][2] += p[1]
L.append('== 区分（集まり×世界を作るかの字面）：クラス・件・秒・割合')
for k, v in sorted(g.items()):
    L.append(f'{k}\t{v[0]}\t{v[1]}\t{v[2]:.1f}\t{v[2]/tot:.3f}')
# 時刻の掃き出し：各時刻に走っている試験の数
ev = []
for r in rows:
    if r['e'] > r['s']:
        ev.append((r['s'], 1)); ev.append((r['e'], -1))
ev.sort()
cur, last, area = 0, ev[0][0], collections.Counter()
for t, d in ev:
    area[cur] += t - last
    cur += d; last = t
L.append('\n== 同時に走る試験の数ごとの壁の秒（掃き出し・試験の開始〜終了の区間だけ）')
for k in sorted(area):
    L.append(f'{k}\t{area[k]:.1f}')
busy = sum(v for k, v in area.items() if k > 0)
L.append(f'試験が 1 本以上走る壁の秒 {busy:.1f}・壁時計 {wall:.1f}・試験が 0 本の秒（開始前・終了後を含む）{wall-busy:.1f}')
# 直列の集まりの区間
ins = [r for r in rows if cls_coll.get(r['cls']) == 'Instruments']
par = [r for r in rows if cls_coll.get(r['cls']) != 'Instruments']
for nm, rs in (('Instruments（直列）', ins), ('並列', par)):
    if rs:
        s0, e1 = min(r['s'] for r in rs), max(r['e'] for r in rs)
        L.append(f'{nm}: 件 {len(rs)}・秒の和 {sum(r["d"] for r in rs):.1f}・最初の開始 {(s0-W0)/60:.2f} 分・最後の終了 {(e1-W0)/60:.2f} 分（壁の始まりから）')
# 1 分ごとの並列度
L.append('\n== 1 分ごとの平均の並列度（走っている試験の秒÷60）と、その分に走った直列の集まりの秒')
nb = int(wall // 60) + 1
busy_m = [0.0] * nb; ins_m = [0.0] * nb
for r in rows:
    a, b = r['s'] - W0, r['e'] - W0
    for m in range(int(a // 60), int(b // 60) + 1):
        if 0 <= m < nb:
            ov = min(b, (m + 1) * 60) - max(a, m * 60)
            if ov > 0:
                busy_m[m] += ov
                if cls_coll.get(r['cls']) == 'Instruments':
                    ins_m[m] += ov
for m in range(nb):
    L.append(f'{m}\t{busy_m[m]/60:.2f}\t{ins_m[m]:.1f}')
w('q2_parallel.txt', L)

# 次の台本へ：クラスごとの秒の表
with open(os.path.join(OUT, 'class_secs.tsv'), 'w', encoding='utf-8', newline='\n') as fh:
    fh.write('cls\tfile\tcoll\tworld\tn\tsecs\tfirst_start_min\tlast_end_min\n')
    for c, p in srt:
        fh.write(f'{c}\t{cls_file.get(c, "")}\t{cls_coll.get(c, "")}\t{cls_world.get(c, "")}\t{p[0]}\t{p[1]:.3f}\t{(p[2]-W0)/60:.3f}\t{(p[3]-W0)/60:.3f}\n')
print('done', N, len(per), f'{tot:.1f}', f'{wall:.1f}')
