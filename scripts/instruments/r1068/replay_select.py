# (1068) (b) 〈触った所に関わる試験〉の選び方を、過去の〈フルスイートで拾った予言の外の赤〉に当てて、拾えたかを数える。
# 読むだけ・ビルドしない・試験を回さない。各件の木を git archive で展開して、ソースの字面だけで選ぶ。
# 使い方: python replay_select.py <リポジトリの根> <作業フォルダ>
# 選び方（AI の案・確証なし）:
#   D  ＝依存：触った .cs（SimCore・Runner・App）が宣言する型の名を、語の境で書く試験のファイル（直接の参照だけ・推移は取らない）
#   T  ＝触った試験のファイルそのもの
#   B  ＝その時点の ㉘ の束（README の★からの推定・FullyQualifiedName~ の部分一致で）
#   S  ＝走査の番人の全部（ファイルを読む呼び〔ReadAllText 等〕と、.cs・.py・"scripts" の字の両方を持つ試験のファイル）
#   提案 P＝D∪T∪B∪S。比べ P0＝D∪T∪B（S を足さない）。
# 限界（自己申告）:
#   語の一致は上界（同名の局所の語も拾う）。部分的なクラス（partial）は型の名で選ぶので、World などを触ると広く選ぶ。
#   試験の本数は [Fact]＋[InlineData]＋[MemberData] の字面の数（--list-tests の実数ではない）。時間は本数の割合＝名目。
#   B の時点ごとの中身は README の★の日付からの推定。各件の赤のクラスは実装ログの文から写した（件ごとの出所の行は表に書く）。
import os, re, sys, subprocess, shutil, tarfile, io, collections

REPO, WORK = sys.argv[1], sys.argv[2]
RW = os.path.join(WORK, 'replay')

def git(*a):
    return subprocess.run(['git', '-C', REPO] + list(a), capture_output=True, check=True).stdout.decode('utf-8', 'replace')

def mb(a, b):
    return git('merge-base', a, b).strip()

# (番号, 名, 触った差分の組 [(base, tip)], 試験の木, 赤のクラス, 拾った所, 実装ログの行, 型（ログの文から）)
CASES = [
    (660, 'mon2', [('42e76d01a^', 'c66489a7f')], 'c66489a7f', ['CalendarTests', 'GoodIdGuardTests'], '線（旗0）', 51511, '走査'),
    (665, 'igi3', [('M:8d5f118c3', None)], '772675fbc', ['ScalarDeletionTests'], '合流', 51611, '走査'),
    (694, 'urb2×navf5', [('M:7d2e1c914', None), ('M:8d7f129e2', None)], '7d2e1c914', ['InstrumentsTests'], '合流（組み合わせ）', 51986, '軌道（重ね）'),
    (727, 'dsc2', [('M:5325095d0', None)], '503a58b61', ['ScalarDeletionTests', 'TechLevelRemovalTests'], '合流', 52588, '走査'),
    (810, 'c13fl', [('MB:origin/c13fl', 'origin/c13fl')], 'origin/c13fl', ['IndustrializationReaderTests', 'InstrumentsTests', 'UnitTypeTests'], '線（旗1）', 53601, '走査（㉘の起こり）'),
    (817, 'c14id', [('M:a9f8ae75b', None)], 'cfa36ba08', ['GoodIdGuardTests'], '線', 53720, '走査（台本）'),
    (845, 'c68ui', [('M:3dd969910', None)], '994e80613', ['TechLevelRemovalTests'], '線', 54229, '走査'),
    (863, 'c71ip', [('M:86597d568', None)], 'b0c8d22f0', ['TradeClearingTests'], '線（旗1）', 54534, '軌道（旗1）'),
    (909, 'c78ls', [('MB:origin/c78ls', 'origin/c78ls')], 'origin/c78ls', ['StateFactoryTests', 'CornintInstrumentTests', 'AiFirstWaveTests', 'ReformPathTests', 'InstrumentsTests'], '線（旗1・データ）', 55127, '軌道（データ）'),
    (952, 'c84kp', [('81ad0087c^', '70caed7c6')], '70caed7c6', ['SecessionLaborLawInstrumentTests'], '線（旗1）', 55559, '軌道（旗1・対照の件数）'),
    (956, 'c85hs', [('M:abd62f6ae', None)], '0d1009fa6', ['OpeningBakeTests'], '線', 55606, 'データ（inputHash）'),
    (959, 'c85mw', [('MB:origin/c85mw', 'origin/c85mw')], 'origin/c85mw', ['TransportInstrumentTests', 'WriterRatchetTests'], '線（旗1）', 55624, '軌道（旗1）＋計算量の表'),
    (961, 'c85lt×c85kp', [('G:(958) c85lt', None), ('M:55b910e16', None)], 'a4da57343', ['SecessionLaborLawInstrumentTests'], '合流（組み合わせ）', 55637, '軌道（重ね・対照の件数）'),
    (978, 'c88cs', [('M:3673f4ae9', None)], '050269226', ['GoodIdGuardTests'], '線（旗1）', 55765, '走査（台本）'),
    (1017, 'c94cl', [('M:87e2e5a06', None)], '4a072ad65', ['ScalarDeletionTests'], '合流', 56112, '走査'),
    (1022, 'c95pc', [('M:4e6de7606', None)], '4ce31359d', ['GoodIdGuardTests'], '合流', 56212, '走査（台本）'),
    (1022.5, 'c95wn', [('MB:origin/c95wn', 'origin/c95wn')], 'origin/c95wn', ['OnDoubleReadScanTests', 'InvadeInstrumentTests'], '合流', 56180, '走査＋表の見出し'),
    (1030, 'c97eb', [('MB:origin/c97eb', 'origin/c97eb')], 'origin/c97eb', ['ClearingFillInstrumentTests', 'SecessionLaborLawInstrumentTests', 'EconomyTests', 'RecipeoutInstrumentTests', 'CorncfInstrumentTests'], '線（旗1）', 56321, '軌道（旗1・試験のバグ㊴）'),
    (1038, 'c98wr', [('MB:origin/c98wr', 'origin/c98wr')], 'origin/c98wr', ['ColonyPromotionTests', 'ConservationTests', 'DiplomacyPendingTests', 'InstrumentsTests', 'InvadeInstrumentTests', 'LordstatInstrumentTests', 'MilitaryTests', 'PlayHarnessTests', 'WarInstrumentTests', 'WriterRatchetTests'], '線（旗1）', 56395, '軌道（旗1）＋旧の規則の番人'),
    (1044, 'c98lc', [('M:80ceed4df', None)], '7de8d11af', ['(試験のビルドの失敗)'], '合流', 0, 'ビルド'),
]

BASE5 = ['IndustrializationReaderTests', 'UnitTypeTests', 'InstrumentsTests', 'WriterRatchetTests', 'DormantIdentityTests']
def bundle_at(n):
    # README の★からの推定。㉘＝波64（(813) の後）・TechLevelRemoval＝★(845)・㊴ Instrument＝作者 2026-11-08（(952) から）・
    # GoodIdGuard＝★(981)・ScalarDeletion＝★(1017)・OnDoubleReadScan と DiplomacyPending＝★(1022)
    b = []
    if n > 813: b += BASE5
    if n > 845: b += ['TechLevelRemovalTests']
    if n >= 952: b += ['Instrument']
    if n > 981: b += ['GoodIdGuardTests']
    if n > 1017: b += ['ScalarDeletionTests']
    if n > 1022: b += ['OnDoubleReadScanTests', 'DiplomacyPendingTests']
    return b
TODAY = BASE5 + ['TechLevelRemovalTests', 'Instrument', 'GoodIdGuardTests', 'ScalarDeletionTests', 'OnDoubleReadScanTests', 'DiplomacyPendingTests']

DECL = re.compile(r'^\s*(?:(?:public|internal|private|protected|static|sealed|abstract|readonly|partial|record|ref|file|unsafe|new)\s+)*(?:class|struct|record|enum|interface)\s+([A-Za-z_][A-Za-z0-9_]*)', re.M)
TOP = re.compile(r'^(?:(?:public|internal|static|sealed|abstract|readonly|partial|record|file)\s+)*(?:class|struct|record|enum|interface)\s+([A-Za-z_][A-Za-z0-9_]*)', re.M)
COMMENT = re.compile(r'//[^\n]*|/\*.*?\*/', re.S)
WORD = re.compile(r'[A-Za-z_][A-Za-z0-9_]*')
READS = re.compile(r'\b(ReadAllText|ReadAllLines|ReadLines|EnumerateFiles|GetFiles|ReadAllBytes|EnumerateDirectories)\s*\(')
SRCPAT = re.compile(r'\.(cs|py)"|"scripts"')

def touched_pairs(spec):
    out = []
    for a, b in spec:
        if a.startswith('M:'):
            m = a[2:]
            p1, p2 = git('rev-parse', m + '^1').strip(), git('rev-parse', m + '^2').strip()
            out.append((mb(p1, p2), p2))
        elif a.startswith('MB:'):
            t = a[3:]
            out.append((mb(t, '9b18137a2'), b))
        else:
            out.append((a, b))
    return out

def extract(rev, dest):
    if os.path.isdir(dest):
        shutil.rmtree(dest)
    os.makedirs(dest)
    data = subprocess.run(['git', '-C', REPO, 'archive', rev, 'PriceOfBread.SimCore', 'PriceOfBread.Tests', 'PriceOfBread.Runner', 'PriceOfBread.App'], capture_output=True, check=True).stdout
    tarfile.open(fileobj=io.BytesIO(data)).extractall(dest, filter='data')

def cs_files(root, proj):
    for d, _, fs in os.walk(os.path.join(root, proj)):
        for f in fs:
            if f.endswith('.cs'):
                yield os.path.join(d, f)

def nmethods(t):
    return t.count('[Fact') + t.count('[InlineData') + t.count('[MemberData')

def analyze(case):
    n, name, spec, tree, fails, caught, logline, kind = case
    pairs = touched_pairs(spec)
    touched = set()
    for a, b in pairs:
        if a.startswith('G:'):   # 線の自分のコミット（merge を除く・件名の grep）が触ったファイルの和（分かれ目が main の祖先になった線の代わり）
            out = git('log', '--all', '--no-merges', '--format=', '--name-only', '--grep=' + a[2:])
        else:
            out = git('diff', '--name-only', a, b)
        touched |= set(x for x in out.split('\n') if x and not x.startswith(('docs/', 'measurements/')))
    dest = os.path.join(RW, str(n).replace('.', '_'))
    extract(tree, dest)
    tests = {}
    for p in cs_files(dest, 'PriceOfBread.Tests'):
        raw = open(p, 'rb').read().decode('utf-8-sig', 'replace')
        tests[os.path.relpath(p, dest).replace(os.sep, '/')] = (raw, COMMENT.sub('', raw))
    classes = {}
    for f, (raw, t) in tests.items():
        for c in DECL.findall(t):
            classes.setdefault(c, f)
    total = sum(nmethods(r) for r, _ in tests.values())
    # 触った .cs が宣言する型（トップの型だけ）
    tnames = set()
    for f in touched:
        if f.endswith('.cs') and not f.startswith('PriceOfBread.Tests/'):
            p = os.path.join(dest, f)
            if os.path.exists(p):
                tnames |= set(TOP.findall(COMMENT.sub('', open(p, 'rb').read().decode('utf-8-sig', 'replace'))))
    D, T, S, B, Bt = set(), set(), set(), set(), set()
    for f, (raw, t) in tests.items():
        ws = set(WORD.findall(t))
        if ws & tnames: D.add(f)
        if f in touched: T.add(f)
        if READS.search(t) and SRCPAT.search(raw): S.add(f)
        cls = DECL.findall(t)
        if any(any(k in c for k in bundle_at(n)) for c in cls): B.add(f)
        if any(any(k in c for k in TODAY) for c in cls): Bt.add(f)
    # データ（SimCore の埋め込みの .json・.bin）を触ったら全部を選ぶ規則 R（AI の案）
    data = any(x.startswith('PriceOfBread.SimCore/') and x.endswith(('.json', '.bin')) for x in touched)
    P = D | T | B | S
    P0 = D | T | B
    Pt = D | T | Bt | S
    cnt = lambda s: sum(nmethods(tests[f][0]) for f in s)
    rows = []
    for c in fails:
        f = classes.get(c)
        if f is None:
            rows.append((c, '木に無い', '', '', '', '', ''))
            continue
        rows.append((c, f.split('/')[-1], 'D' if f in D else '-', 'T' if f in T else '-', 'B' if f in B else '-', 'S' if f in S else '-', 'P' if f in P else '×'))
    shutil.rmtree(dest)
    PR = set(tests) if data else P
    return dict(tn=tnames, PR=cnt(PR), data=data, n=n, name=name, caught=caught, logline=logline, kind=kind, touched=len(touched), tnames=len(tnames), total=total,
                D=cnt(D), P=cnt(P), P0=cnt(P0), Pt=cnt(Pt), S=cnt(S), rows=rows, nS=len(S))

os.makedirs(RW, exist_ok=True)
print('件', '名', '拾った所', 'ログの行', '型', '触ったファイル', '触った型', '本数の全体', 'D', 'S', 'P0', 'P', 'P（今日の束）', 'データを触った', 'P+R', 'P+R の割合', '触った型の名', sep='\t')
allrows = []
for c in CASES:
    r = analyze(c)
    print(r['n'], r['name'], r['caught'], r['logline'], r['kind'], r['touched'], r['tnames'], r['total'], r['D'], r['S'], r['P0'], r['P'], r['Pt'], r['data'], r['PR'], f"{r['PR'] / r['total']:.2f}", ','.join(sorted(r['tn'])[:12]), sep='\t')
    for row in r['rows']:
        print('', *row, sep='\t')
    allrows.append(r)
os.rmdir(RW)
caught_P = sum(1 for r in allrows for row in r['rows'] if row[-1] == 'P')
caught_P0 = sum(1 for r in allrows for row in r['rows'] if 'D' in row[2:5] or 'T' in row[2:5] or 'B' in row[2:5])
caught_B = sum(1 for r in allrows for row in r['rows'] if row[4] == 'B')
caught_PR = sum(1 for r in allrows for row in r['rows'] if row[-1] == 'P' or r['data'])
print('赤のクラスの延べ', sum(len(r['rows']) for r in allrows), 'P で拾う', caught_P, 'P+R で拾う', caught_PR, 'P0（S なし）で拾う', caught_P0, 'その時点の束 B だけで拾う', caught_B)
shares = sorted(r['PR'] / r['total'] for r in allrows)
print('P+R の割合（名目＝本数の割合）: 最小', f'{shares[0]:.2f}', '中央', f'{shares[len(shares) // 2]:.2f}', '最大', f'{shares[-1]:.2f}', '・0.5 未満の件', sum(1 for s in shares if s < 0.5), '/', len(shares))
