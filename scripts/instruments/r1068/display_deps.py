# (1068) (a) 表示の層 Display と他の部品の読み合い。読むだけ・ビルドしない。
# 使い方: python display_deps.py <展開した木の根> [<別の木の根> ...]
# 木は git archive <commit> PriceOfBread.SimCore PriceOfBread.Runner PriceOfBread.Tests PriceOfBread.App で作る。
# 限界（自己申告）: 正規表現で語を数える＝同名の局所変数・文字列の中の語も数える（上界）。
#   型の宣言は class/struct/record/enum/interface の行。入れ子の型も数える。メンバーの internal は宣言の行の正規表現（上界）。
import os, re, sys, collections

DECL = re.compile(r'^\s*((?:public|internal|private|protected|static|sealed|abstract|readonly|partial|record|ref|file|unsafe|new)\s+)*(class|struct|record|enum|interface)\s+([A-Za-z_][A-Za-z0-9_]*)', re.M)
ACC = re.compile(r'\b(public|internal|private|protected|file)\b')
COMMENT = re.compile(r'//[^\n]*|/\*.*?\*/', re.S)
WORD = re.compile(r'[A-Za-z_][A-Za-z0-9_]*')
MEMBER_INTERNAL = re.compile(r'^\s*(?:\[[^\]]*\]\s*)*internal\s+(?:(?:static|readonly|const|override|virtual|abstract|sealed|new|partial|required|unsafe)\s+)*[A-Za-z_][A-Za-z0-9_<>,\[\]\.\?\s\(\)]*?\s([A-Za-z_][A-Za-z0-9_]*)\s*(\(|\{|=>|=|;)', re.M)

def read(p):
    with open(p, 'rb') as f:
        return f.read().decode('utf-8-sig', errors='replace')

def files(root, proj):
    out = []
    for d, _, fs in os.walk(os.path.join(root, proj)):
        if os.sep + 'obj' in d or os.sep + 'bin' in d:
            continue
        for f in fs:
            if f.endswith('.cs'):
                out.append(os.path.join(d, f))
    return sorted(out)

def decls(text):
    res = []
    for m in DECL.finditer(text):
        mods = m.group(0)
        acc = ACC.search(mods)
        indent = len(m.group(0)) - len(m.group(0).lstrip())
        res.append((m.group(3), acc.group(1) if acc else ('internal(既定)' if indent == 0 else 'private(既定)')))
    return res

def analyze(root):
    disp_dir = os.path.join('PriceOfBread.SimCore', 'Display')
    sim = files(root, 'PriceOfBread.SimCore')
    disp = [p for p in sim if disp_dir in p]
    eng = [p for p in sim if disp_dir not in p]
    txt = {p: COMMENT.sub('', read(p)) for p in sim + files(root, 'PriceOfBread.Runner') + files(root, 'PriceOfBread.Tests') + files(root, 'PriceOfBread.App')}
    raw = {p: read(p) for p in txt}
    dtypes = collections.OrderedDict()
    for p in disp:
        for n, a in decls(txt[p]):
            dtypes.setdefault(n, (a, os.path.basename(p)))
    etypes = {}
    for p in eng:
        for n, a in decls(txt[p]):
            etypes.setdefault(n, a)
    clash = sorted(set(dtypes) & set(etypes))
    print('== 木', root)
    print('Display のファイル', len(disp), '行', sum(raw[p].count('\n') for p in disp), '型', len(dtypes), '（public', sum(1 for v in dtypes.values() if v[0] == 'public'), '）')
    print('エンジンの型と同名の Display の型（名前だけでは向きが決まらない）:', clash)
    # global using / using の確かめ
    gl = [p for p in txt if re.search(r'global\s+using\s+PriceOfBread\.SimCore\.Display', txt[p])]
    print('global using Display の件数', len(gl))
    groups = {'エンジン（SimCore の Display 以外）': eng, 'Runner': files(root, 'PriceOfBread.Runner'), 'Tests': files(root, 'PriceOfBread.Tests'), 'App': files(root, 'PriceOfBread.App')}
    names = [n for n in dtypes if n not in clash]
    for g, fs in groups.items():
        using = [p for p in fs if re.search(r'using\s+PriceOfBread\.SimCore\.Display\s*;', txt[p]) or re.search(r'\bDisplay\.[A-Z]', txt[p])]
        cnt = collections.Counter(); filesWith = collections.Counter()
        for p in using:
            ws = collections.Counter(WORD.findall(txt[p]))
            for n in dtypes:
                if ws[n]:
                    cnt[n] += ws[n]; filesWith[n] += 1
        print(f'-- {g}: ファイル {len(fs)}・Display を using する {len(using)}・使う Display の型 {len(cnt)}・呼びの数 {sum(cnt.values())}')
        if cnt:
            print('   ', ' '.join(f'{n}:{cnt[n]}/{filesWith[n]}f' for n, _ in cnt.most_common(40)))
        if using and g != 'App':
            print('    ファイル:', ' '.join(os.path.basename(p) for p in using))
    # Display -> エンジン
    ecnt = collections.Counter(); efile = collections.Counter()
    for p in disp:
        ws = collections.Counter(WORD.findall(txt[p]))
        for n in etypes:
            if n in dtypes:
                continue
            if ws[n]:
                ecnt[n] += ws[n]; efile[n] += 1
    internal_types = [n for n in ecnt if etypes[n].startswith('internal')]
    print(f'-- Display がエンジンの型を読む: 型 {len(ecnt)}・呼び {sum(ecnt.values())}・うち internal の型 {len(internal_types)} {sorted(internal_types)}')
    print('   多い順:', ' '.join(f'{n}:{c}' for n, c in ecnt.most_common(30)))
    # internal のメンバー
    imem = collections.defaultdict(set)
    for p in eng:
        for m in MEMBER_INTERNAL.finditer(txt[p]):
            imem[m.group(1)].add(os.path.basename(p))
    used = set()
    for p in disp:
        t = txt[p]
        for n in imem:
            if re.search(r'\.' + re.escape(n) + r'\b', t) or re.search(r'\b' + re.escape(n) + r'\s*\(', t):
                used.add(n)
    print(f'-- エンジンの internal のメンバー（宣言 {len(imem)} 名）のうち Display が名で呼ぶ（上界）: {len(used)}')
    print('   ', ' '.join(sorted(used)))
    PUB = re.compile(MEMBER_INTERNAL.pattern.replace('internal', 'public'), re.M)
    pubn = set()
    for p in eng:
        for m in PUB.finditer(txt[p]):
            pubn.add(m.group(1))
    only = sorted(n for n in used if n not in pubn and n not in etypes)
    print(f'   うち同じ名の public の宣言がエンジンに無い（internal だけの名）: {len(only)}', ' '.join(f'{n}@{"/".join(sorted(imem[n]))}' for n in only))
    # Display の internal の型・メンバーを外が読むか
    dint = [n for n, v in dtypes.items() if not v[0] == 'public']
    print('-- Display の public でない型:', dint)
    dmem = collections.defaultdict(set)
    for p in disp:
        for m in MEMBER_INTERNAL.finditer(txt[p]):
            dmem[m.group(1)].add(os.path.basename(p))
    outside = collections.Counter()
    for g, fs in groups.items():
        if g.startswith('エンジン'):
            continue
        for p in fs:
            for n in dmem:
                if re.search(r'\.' + re.escape(n) + r'\b', txt[p]):
                    outside[(g, n)] += 1
    print(f'-- Display の internal のメンバー {len(dmem)} 名・外（Runner・Tests・App）が名で呼ぶ組 {len(outside)}:', ' '.join(f'{g}:{n}' for (g, n) in sorted(outside)))

for r in sys.argv[1:]:
    analyze(r)
