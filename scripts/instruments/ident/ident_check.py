# (728) 恒等の判定（測った種で一致）の突き合わせ（読むだけ）。ident.sh check から呼ぶ。使い方は ident.sh の先頭。
#   python -I ident_check.py <ラベル> --root <木> [--work <作業フォルダ>] [--cfg <設定>] [--tree <測った木>] [--save <日付>] [宣言...] [--saveleak] [--ng]
# 判定の規則：宣言の無い違いは全て〈不一致〉。基準の生が無い・出力が無いなど突き合わせられない所は〈未了〉（恒等とは言わない）。
import sys, os, re, csv, hashlib, shutil, subprocess, datetime, difflib
from collections import Counter
from itertools import zip_longest
from concurrent.futures import ThreadPoolExecutor

sys.stdout.reconfigure(encoding='utf-8')
csv.field_size_limit(1 << 26)

def J(*a):   # パスは / で揃える（bash に渡す物が円記号で壊れない）
    return os.path.normpath(os.path.join(*a)).replace(chr(92), '/')


OUT = []        # 報告の行
FAILS = []      # 不一致の理由
MISSING = []    # 未了の理由


def say(s=''):
    OUT.append(s)
    print(s)


def fail(s):
    FAILS.append(s)
    say('  ✗ 不一致：' + s)


def miss(s):
    MISSING.append(s)
    say('  △ 未了：' + s)


def ok(s):
    say('  ○ ' + s)


def parse_args(argv):
    a = {'label': argv[0], 'removed': [], 'added': [], 'grown': [], 'rows_grown': [], 'removed_cols': [], 'known_extra': [], 'cost_allow': [], 'rewritten': [],
         'tests_delta': 0, 'saveleak': False, 'ng': False, 'save': None, 'work': None, 'cfg': None, 'tree': None, 'root': None}
    i = 1
    while i < len(argv):
        k = argv[i]
        if k in ('--saveleak', '--ng'):
            a[k[2:]] = True; i += 1; continue
        v = argv[i + 1]
        if k == '--root': a['root'] = v
        elif k == '--work': a['work'] = v
        elif k == '--cfg': a['cfg'] = v
        elif k == '--tree': a['tree'] = v
        elif k == '--save': a['save'] = v
        elif k == '--removed-tables': a['removed'] = [x for x in v.split(',') if x]
        elif k == '--added-tables': a['added'] = [x for x in v.split(',') if x]
        elif k == '--grown-ok': a['grown'] = [x for x in v.split(',') if x]
        elif k == '--rows-grown-ok': a['rows_grown'] = [x for x in v.split(',') if x]
        elif k == '--removed-cols': a['removed_cols'] = [x for x in v.split(',') if x]   # (786) 表:列（基準にある列を腕で消してよい＝消した計器の列）
        elif k == '--rewritten-tables': a['rewritten'] = [x for x in v.split(',') if x]   # (1303) 表@検算の台本（表の中身が宣言どおり変わった・台本が 0 を返した時だけ基準と違ってよい）
        elif k == '--known-extra': a['known_extra'] = [x for x in v.split(',') if x]
        elif k == '--cost-allow': a['cost_allow'] = [x for x in v.split(',') if x]
        elif k == '--tests-delta': a['tests_delta'] = int(v)
        else:
            print('不明な引数', k); sys.exit(2)
        i += 2
    return a


def load_cfg(p):
    c = {}
    for ln in open(p, encoding='utf-8'):
        ln = ln.rstrip('\r\n')
        if not ln or ln.startswith('#') or '=' not in ln:
            continue
        k, v = ln.split('=', 1)
        c[k.strip()] = v.strip()
    return c


def sha(p):
    h = hashlib.sha256()
    with open(p, 'rb') as f:
        for b in iter(lambda: f.read(1 << 22), b''):
            h.update(b)
    return h.hexdigest()


# ---------- 1 成績表（名乗り行を除く全行バイト一致） ----------
SKIP_PREFIX = ('★係数の上書き'.encode('utf-8'), b'SOURCE-HASH')


def report_lines(p):
    return [l for l in open(p, 'rb').read().split(b'\n') if not l.startswith(SKIP_PREFIX)]


def cmp_report(title, new, base):
    say(f'[{title}] {os.path.basename(new)} 対 {os.path.basename(base)}')
    if not os.path.exists(new):
        miss(f'{title}：出力が無い {new}'); return
    if not os.path.exists(base):
        miss(f'{title}：基準が無い {base}'); return
    a, b = report_lines(new), report_lines(base)
    diff = [(i, x, y) for i, (x, y) in enumerate(zip(a, b)) if x != y]
    if not diff and len(a) == len(b):
        ok(f'全行バイト一致（{len(a)} 行・名乗り行を除く）')
        return
    fail(f'{title}：行数 {len(a)} 対 {len(b)}・違う行 {len(diff)}')
    for i, x, y in diff[:6]:
        say(f'      {i + 1}: 新 {x.decode("utf-8", "replace")[:140]}\n         基 {y.decode("utf-8", "replace")[:140]}')


# ---------- 2 .cost（件数の行・推定・種ごとの忠実度） ----------
def parse_cost(p):
    rows, est, fid = {}, None, []
    on = False
    for ln in open(p, encoding='utf-8').read().split('\n'):
        ln = ln.rstrip('\r')
        if ln.startswith('  名前\t件数'):
            on = True; continue
        if ln.startswith('推定の計算量'):
            on = False
            m = re.search(r'推定の計算量（1試合）: ([\d.]+) 秒', ln)
            est = m.group(1) if m else ln
            continue
        if on and ln.startswith('  '):
            f = ln.strip().split('\t')
            rows[f[0]] = '\t'.join(f[1:])
        if re.match(r'^  seed\d+\texact', ln):
            fid.append(ln)
    return rows, est, fid


def cmp_cost(title, new, base, allow):
    say(f'[{title}] {os.path.basename(new)} 対 {os.path.basename(base)}')
    if not os.path.exists(new):
        miss(f'{title}：.cost が無い'); return
    if not os.path.exists(base):
        miss(f'{title}：基準の .cost が無い'); return
    ra, ea, fa = parse_cost(new); rb, eb, fb = parse_cost(base)
    bad, excused = [], []
    for k in sorted(set(ra) | set(rb)):
        if ra.get(k) != rb.get(k):
            (excused if any(k.startswith(x) for x in allow) else bad).append((k, ra.get(k), rb.get(k)))
    if not ra:
        miss(f'{title}：件数の行が読めない'); return
    if bad:
        fail(f'{title}：件数の行 {len(bad)} 本が違う')
        for k, x, y in bad[:6]:
            say(f'      {k[:60]}\n         新 {x}\n         基 {y}')
    if excused:
        say(f'  △ 宣言された例外（--cost-allow）{len(excused)} 本：' + '・'.join(k[:30] for k, _, _ in excused))
    if ea != eb:
        if excused and not bad:
            say(f'  △ 推定 新 {ea} 秒 対 基準 {eb} 秒（宣言された例外の分）')
        else:
            fail(f'{title}：推定 新 {ea} 秒 対 基準 {eb} 秒')
    if fa != fb:
        fail(f'{title}：種ごとの忠実度の行が違う')
    if not bad and ea == eb and fa == fb:
        ok(f'件数の行 {len(ra)} 本・推定 {ea} 秒・種ごとの忠実度 {len(fa)} 行が一致' + (f'（例外 {len(excused)} 本を宣言）' if excused else ''))


# ---------- 3 計器の表（バイト一致・違う表は列まで） ----------
TABLE_RE = re.compile(r'^(.+?)_s\d+\.tsv$')


def table_of(f):   # 'clock_s3.tsv' → 'clock_s'（表の名前＝種の番号の前）
    m = TABLE_RE.match(f)
    return m.group(1) + '_s' if m else f


def parse_known(items):   # 'clock_s:worldTotalPerMonth@kind=C' → {'clock_s': {'worldTotalPerMonth': ('kind', 'C')}}・'@' が無ければ全ての行
    k = {}
    for it in items:
        if not it:
            continue
        if ':' not in it:
            print('既知の揺れは 表:列[@列=値] で書く：', it); sys.exit(2)
        t, c = it.split(':', 1)
        flt = None
        if '@' in c:
            c, f = c.split('@', 1)
            flt = tuple(f.split('=', 1))
        k.setdefault(t, {})[c] = flt
    return k


ROWS_ADDED = {}   # --rows-grown-ok の表：{基準のファイル: 腕で増えた行数}
COLS_REMOVED = {}   # --removed-cols の表：{基準のファイル: 腕で消えた列（宣言どおり）}（(786)）


def cols_diff(pa, pb, kmap, rows_grown=False, removed_cols=()):   # pa＝基準・pb＝腕。返す物＝（既知の揺れの列・宣言の無い違い・腕で増えた列）。rows_grown＝基準の行（共通の列）が腕の行の順序つき部分列であれば、腕で増えた行を許す（(781)）
    with open(pa, encoding='utf-8', newline='') as fa, open(pb, encoding='utf-8', newline='') as fb:
        ra = csv.reader(fa, delimiter='\t'); rb = csv.reader(fb, delimiter='\t')
        ha = next(ra, []); hb = next(rb, [])
        extra = [c for c in hb if c not in ha]
        gone = [c for c in ha if c in removed_cols and c not in hb]   # (786) 宣言された消した列（腕に残っていれば普通に比べる）
        if gone:
            COLS_REMOVED[pa] = gone
        ha_eff = [c for c in ha if c not in gone]
        common = [c for c in ha_eff if c in hb]
        ia = {c: ha.index(c) for c in common}; ib = {c: hb.index(c) for c in common}
        real, kn = set(), set()
        if [c for c in hb if c in ha_eff] != ha_eff:
            real.add('HEADER_ORDER_OR_MISSING')
        if rows_grown:
            rows_b = list(rb); j = 0; added = 0
            cmpc = [c for c in common if c not in kmap]   # 既知の揺れの列は比べない
            for xa in ra:
                while j < len(rows_b) and any((xa[ia[c]] if ia[c] < len(xa) else None) != (rows_b[j][ib[c]] if ib[c] < len(rows_b[j]) else None) for c in cmpc):
                    j += 1; added += 1
                if j >= len(rows_b):
                    real.add('ROWS_NOT_SUBSEQUENCE'); break
                j += 1
            else:
                added += len(rows_b) - j
                ROWS_ADDED[pa] = added
            return sorted(kn), sorted(real), extra
        for xa, xb in zip_longest(ra, rb):
            if xa is None or xb is None:   # 行数が違う（片方が尽きた）
                real.add('ROWS'); break
            if len(xa) != len(ha) or len(xb) != len(hb):   # 欄の数が見出しと違う行
                real.add('FIELDS')
            for c in common:
                va = xa[ia[c]] if ia[c] < len(xa) else None
                vb = xb[ib[c]] if ib[c] < len(xb) else None
                if va == vb:
                    continue
                if c not in kmap:
                    real.add(c); continue
                flt = kmap[c]
                if flt is None:
                    kn.add(c); continue
                fc, fv = flt   # 揺れが許されるのは、その行の fc の値が両側とも fv の行だけ
                if fc in ia and ia[fc] < len(xa) and ib[fc] < len(xb) and xa[ia[fc]] == fv and xb[ib[fc]] == fv:
                    kn.add(f'{c}@{fc}={fv}')
                else:
                    real.add(c)
    return sorted(kn), sorted(real), extra


FLOW_LOC = ('sourceFile', 'sourceLine')   # flow_s の呼び出し位置（Instruments.cs の FlowRows はこの順で並べる＝行を動かすと並びも変わる）


def flow_same_but_location(pa, pb):   # 呼び出し位置を除いた行の多重集合が同じか（並べ替え・行の移動・別ファイルへの移動は通る・2 行を 1 行に合わせたら通らない）
    with open(pa, encoding='utf-8', newline='') as fa, open(pb, encoding='utf-8', newline='') as fb:
        ra = list(csv.reader(fa, delimiter='\t')); rb = list(csv.reader(fb, delimiter='\t'))
    if not ra or not rb or ra[0] != rb[0] or any(c not in ra[0] for c in FLOW_LOC):
        return False
    keep = [i for i, c in enumerate(ra[0]) if c not in FLOW_LOC]
    n = len(ra[0])
    if any(len(x) != n for x in ra[1:] + rb[1:]):
        return False
    key = lambda rows: Counter(tuple(x[i] for i in keep) for x in rows[1:])
    return len(ra) == len(rb) and key(ra) == key(rb)


# (835) c67id：cearn_s の経路の列（outTop1Route・outTop2Route・inTop1Route）は〈ファイル:行:向き:区分〉の名を持つ＝金を動かすファイルに行を足すと、動きが同じでも行番号が変わって表が不一致になる。
#   基準の木のコミット（cfg の BASE_COMMIT＝INSRAW を出した木）と測る木の同じファイルの行の対応（difflib・変わらない行どうし）で基準の表の〈:行〉を写してから比べる（cearn_remap.py〔(828)〕と同じ写像）。
#   他の列はバイトで比べる。写像に無い行（消した・書き換えた行）を指す経路・別の行へ移った経路・経路でない列の違いは不一致のまま。BASE_COMMIT が cfg に無ければ使わない（旧のバイト比べ）。flow_s の規則は別（上）。
CEARN_ROUTE_COLS = ('outTop1Route', 'outTop2Route', 'inTop1Route')
CPOP_ROUTE_COLS = ('outOtherTop1Route',)   # (963) c86sm：cpop_s の経路の列（outOtherTop1Route＝同じ〈ファイル:行:向き:区分〉の名）。cearn_same_but_location の route_cols に渡す（cpop_s の宣言の列は known['cpop_s']）
CEARN = {'root': None, 'base': None, 'tree': None, 'arm_commit': None, 'maps': {}, 'paths': None}   # 測る木は tree の作業ファイル（arm_commit を渡せばその木のコミット＝道具の試験用）
_ROUTE_RE = re.compile(r'^([A-Za-z0-9_]+\.cs):(\d+)(.*)$', re.S)


def _git_bytes(root, args):
    r = subprocess.run(['git', '-C', root] + args, capture_output=True)
    return r.stdout if r.returncode == 0 else None


def _cearn_path(name):   # 経路の名のファイル名 → 基準のコミットの PriceOfBread.SimCore/ の下のパス（同名が 2 本あれば None＝写せない）
    c = CEARN
    if c['paths'] is None:
        out = _git_bytes(c['root'], ['ls-tree', '-r', '--name-only', c['base'], '--', 'PriceOfBread.SimCore']) or b''
        d = {}
        for ln in out.decode('utf-8').splitlines():
            if ln.endswith('.cs'):
                d.setdefault(os.path.basename(ln), []).append(ln)
        c['paths'] = d
    ps = c['paths'].get(name, [])
    return ps[0] if len(ps) == 1 else None


_KEY_SKIP = (b'/*', b'@"', b'"""', b'$"')


def _code_key(s):   # (1116) 合流：行の一致の鍵＝文字列・文字リテラルの外の行末の // 注を除き、末尾の空白を落とした字。
    # 注だけを移す恒等の線（(1113) c107hb）が経路の行の行末の注を消すと、行全体の一致では写せない（ha (1112) の申し送り）。
    # /*・逐語・生・補間の文字列を含む行は字句を 1 行で読み切れない＝正規化しない（保守側＝写せなければ ? のまま）。コードの字が 1 字でも違えば鍵も違う。
    if any(t in s for t in _KEY_SKIP):
        return s
    i, n, q = 0, len(s), None
    while i < n:
        c = s[i:i + 1]
        if q:
            if c == b'\\':
                i += 2
                continue
            if c == q:
                q = None
        elif c in (b'"', b"'"):
            q = c
        elif s[i:i + 2] == b'//':
            return s[:i].rstrip()
        i += 1
    return s.rstrip()


def _cearn_map(name):   # ファイル名 → {基準の行: 測る木の行}（変わらない行だけ）。None＝同じファイル（恒等）。{}＝写せない
    c = CEARN
    if name in c['maps']:
        return c['maps'][name]
    m = {}
    path = _cearn_path(name)
    a = _git_bytes(c['root'], ['show', f"{c['base']}:{path}"]) if path else None
    if a is None:
        c['maps'][name] = m
        return m
    if c['arm_commit']:
        bb = _git_bytes(c['root'], ['show', f"{c['arm_commit']}:{path}"])
    else:
        fp = J(c['tree'], path)
        bb = open(fp, 'rb').read() if os.path.exists(fp) else None
    if bb is None:
        c['maps'][name] = m
        return m
    if a == bb:
        c['maps'][name] = None
        return None
    la = [_code_key(x.rstrip(b'\r')) for x in a.split(b'\n')]   # (1116) 行の一致は行末の注を除いたコードの字（_code_key）
    lb = [_code_key(x.rstrip(b'\r')) for x in bb.split(b'\n')]
    for blk in difflib.SequenceMatcher(None, la, lb, autojunk=False).get_matching_blocks():
        for k in range(blk.size):
            m[blk.a + k + 1] = blk.b + k + 1
    c['maps'][name] = m
    return m


def _cearn_remap(s):   # 基準の経路の名の〈:行〉を測る木の行へ。写せない行は末尾に ? を付ける（どの腕の値とも一致しない）
    mo = _ROUTE_RE.match(s)
    if not mo:
        return s
    fn, ln, rest = mo.group(1), int(mo.group(2)), mo.group(3)
    m = _cearn_map(fn)
    if m is None:
        return s
    if ln not in m:
        return s + '?'
    return f'{fn}:{m[ln]}{rest}'


def cearn_same_but_location(pa, pb, allow_grown=False, route_cols=CEARN_ROUTE_COLS):   # 経路の列の〈:行〉を写せば全行が一致するか（経路の外の列はバイトで一致が要る）。(一致か, 写して一致した欄の数, 腕で末尾に増えた列)
    # (852) allow_grown＝--grown-ok に cearn_s がある＝腕の見出しが基準の見出しを頭に含み末尾に列が増えた形も、基準の列（頭の n 列）を同じ規則で比べる（増えた列は比べない＝宣言の増減に数える）。
    #   列の追加と金を動かすファイルの行の移動が同じ木に入ると、旧の形は見出しの不一致で写像を走らせず〈宣言の無い違い〉にしていた（(848) c69eh §9 の道具の限界）。
    # (934) CEARN['known']＝cearn_s の宣言の列（--known-extra と KNOWN・parse_known の形 {列: None か (条件の列, 値)}・cmp_ins が入れる）。経路の外の列でも、宣言の列は値の違いを許す（cols_diff と同じ規則）。空か未設定＝今までと同じ（全ての列がバイトで一致）。呼び出しの引数にしないのは、試験が差し替える関数の形（c70gt）を保つため。
    #   宣言の列だけが違い経路が動かない表は、今までどおり False を返して cols_diff の道へ落とす（判定の文言と内訳の区分を変えない）。
    if not CEARN['base'] or not CEARN['root'] or not (CEARN['tree'] or CEARN['arm_commit']):
        return False, 0, []
    kmap = CEARN.get('known') or {}
    with open(pa, encoding='utf-8', newline='') as fa, open(pb, encoding='utf-8', newline='') as fb:   # (963) 行を並べて読む（cpop_s は 350MB 級＝2 本を list にしない）
        return _cearn_same_rows(csv.reader(fa, delimiter='\t'), csv.reader(fb, delimiter='\t'), allow_grown, route_cols, kmap)


def _cearn_same_rows(ra, rb, allow_grown, route_cols, kmap):
    CEARN['actual'] = []   # (963) 宣言の列のうち実際に値が違った列（呼び出しの後に cmp_ins が札へ併記する）
    act = set()
    h0, hb = next(ra, None), next(rb, None)
    if not h0 or not hb:
        return False, 0, []
    h = h0
    n = len(h)
    if hb == h:
        extra = []
    elif allow_grown and len(hb) > n and hb[:n] == h:
        extra = hb[n:]
    else:
        return False, 0, []
    nb = len(hb)
    rc = [i for i, c in enumerate(h) if c in route_cols]
    if not rc:
        return False, 0, []
    moved = 0
    kdiff = False
    for xa, xb in zip_longest(ra, rb):
        if xa is None or xb is None:   # 行の数が違う
            return False, 0, []
        if len(xa) != n or len(xb) != nb:
            return False, 0, []
        for i in range(n):
            if i in rc:
                if xa[i] == xb[i]:
                    if _cearn_remap(xa[i]) != xa[i]:   # 同じ字でも基準の行が動いた（または消えた）行を指す＝別の呼び出しの行を指している
                        return False, 0, []
                    continue
                if _cearn_remap(xa[i]) != xb[i]:
                    return False, 0, []
                moved += 1
            elif xa[i] != xb[i]:
                flt = kmap.get(h[i], False)
                if flt is False:
                    return False, 0, []
                if flt is not None:   # 条件つきの宣言＝その行の条件の列の値が両側とも指定の値のときだけ許す（cols_diff と同じ）
                    fc, fv = flt
                    if fc not in h:
                        return False, 0, []
                    j = h.index(fc)
                    if xa[j] != fv or xb[j] != fv:
                        return False, 0, []
                kdiff = True
                act.add(h[i])
    if kdiff and moved == 0:
        return False, 0, []
    CEARN['actual'] = sorted(act)
    return True, moved, extra


def _actual():   # (963) 直前の cearn_same_but_location で実際に違った宣言の列（無ければ空）
    a = CEARN.get('actual') or []
    return '（実際に違った列 ' + ','.join(a) + '）' if a else ''


def _decl(known, t):   # (949) 既知の揺れの札に、その表の宣言の列の名を併記する（宣言が無ければ空＝今までの札と同じ）
    return '（宣言の列 ' + ','.join(sorted(known[t])) + '）' if known.get(t) else ''


def sha1(p):
    h = hashlib.sha1()
    with open(p, 'rb') as f:
        for b in iter(lambda: f.read(1 << 22), b''):
            h.update(b)
    return h.hexdigest()


def pruned_ok(new, base, fa, have):
    """(1054) prune.py（PRUNE=1）が判定の後に消した表のうち、manifest の生の sha1・大きさが基準の同名の表と一致する物の名。manifest が無ければ空（今までと同じ）。"""
    mp = J(os.path.dirname(os.path.dirname(os.path.abspath(new))), 'prune_manifest.tsv')
    if not os.path.isfile(mp): return []
    ok = []
    for l in open(mp, encoding='utf-8'):
        x = l.rstrip('\n').split('\t')
        if l.startswith('#') or len(x) < 8 or not x[7].startswith('消す') or x[0] in have or x[0] not in fa: continue
        bp = J(base, x[0])
        if os.path.getsize(bp) == int(x[1]) and sha1(bp) == x[2]: ok.append(x[0])
    if ok: say(f'  刈り済みの表 {len(ok)} 本は manifest の生の sha1 と基準が一致（バイト一致に数える）')
    return ok


def _rw_abs(x, tree, root):   # (1303) 表@台本 の台本のパスを、測った木（無ければ根）の下の物へ
    t, s = x.split('@', 1) if '@' in x else (x, '')
    if s and not os.path.isabs(s) and not re.match(r'^[a-zA-Z]:', s):
        s = J(tree, s) if os.path.isfile(J(tree, s)) else J(root, s)
    return t + '@' + s


def run_rewritten(script, pa, pb):
    """(1303) 検算の台本を基準の生と腕の生で走らせる。返す物＝（rc, 台本の最初の出力の行）。台本が無い・起動できない＝rc 99。"""
    if not os.path.isfile(script): return 99, '台本が無い ' + script
    try:
        r = subprocess.run([sys.executable, '-X', 'utf8', '-I', script, pa, pb], capture_output=True, text=True, encoding='utf-8', timeout=900)
    except Exception as e:
        return 99, '起動できない ' + str(e)[:80]
    ls = [x for x in r.stdout.splitlines() if x.strip()]
    return r.returncode, (ls[0] if ls else '')


def cmp_ins(new, base, known, removed, added, grown, rows_grown=(), removed_cols=(), only=None, bypass=(), rewritten=()):
    say(f'[計器の表] 腕 {new}  対  基準 {base}' + (f'  ※段 1＝集合 {len(only)} 表だけ' if only is not None else ''))
    if not os.path.isdir(new):
        miss('計器の出力フォルダが無い'); return
    if not os.path.isdir(base):
        miss(f'基準の生が無い（{base}）＝計器の表は突き合わせられない'); return
    fa = sorted(x for x in os.listdir(base) if os.path.isfile(J(base, x)))
    fb = sorted(x for x in os.listdir(new) if os.path.isfile(J(new, x)))
    if only is not None:   # (1061) 段 1：集合の中の表だけを比べる（基準の側も腕の側も）・集合の外のファイルは 0 本のはず（Open を通らない表 bypass を除く）
        outside = [x for x in fb if table_of(x) not in only and table_of(x) not in bypass]
        if outside: fail(f'段 1：選ばない表のファイルが {len(outside)} 本出ている（{outside[:3]}）')
        else: ok(f'段 1：選ばない表のファイルは 0 本（Open を通らない表 {len(bypass)} 種は除く）')
        fa = [x for x in fa if table_of(x) in only]; fb = [x for x in fb if table_of(x) in only]
        if not fa: miss('段 1：基準の生に集合の表が 1 本も無い（空の比べで恒等にしない）')
    PR = pruned_ok(new, base, fa, set(fb))   # (1054) 刈り済みの表（manifest の生の sha1 が基準の生と一致した物）は〈バイト一致〉に数える
    fb = sorted(fb + PR)
    sb, sa = set(fb), set(fa)

    def pair(f):
        pa, pb = J(base, f), J(new, f)
        if f in PR: return f, 'same'
        if os.path.getsize(pa) == os.path.getsize(pb) and sha(pa) == sha(pb):
            return f, 'same'
        return f, 'diff'
    both = [f for f in fa if f in sb]
    with ThreadPoolExecutor(8) as ex:
        res = list(ex.map(pair, both))
    same = sum(1 for _, s in res if s == 'same')
    diffs = [f for f, s in res if s == 'diff']
    missing = [f for f in fa if f not in sb]
    only = [f for f in fb if f not in sa]
    pref = lambda f, L: any(f.startswith(x) for x in L)
    CEARN['known'] = known.get('cearn_s', {})   # (934)
    bad_missing = [f for f in missing if not pref(f, removed)]
    bad_only = [f for f in only if not pref(f, added)]
    known_hit, grown_hit, real, rows_hit, cols_hit = [], [], [], [], []
    RW = dict(x.split('@', 1) for x in rewritten if '@' in x)   # (1303) 表の名前（arrears_s）→ 台本のパス
    rw_hit = []
    for f in diffs:
        if not f.endswith('.tsv'):
            real.append((f, ['NONTSV'])); continue
        t = table_of(f)
        if t in RW:   # (1303) --rewritten-tables：台本が 0 を返した時だけ基準と違ってよい（他の表・宣言の無い時は通らない）
            rc, msg = run_rewritten(RW[t], J(base, f), J(new, f))
            if rc == 0:
                rw_hit.append((f, msg)); continue
            real.append((f, [f'検算の台本が非 0（rc={rc}）：{msg}'])); continue
        if t == 'flow_s' and flow_same_but_location(J(base, f), J(new, f)):
            known_hit.append((f, ['呼び出し位置だけ' + _decl(known, 'flow_s')])); continue
        if t == 'cearn_s':
            okc, nmv, ext = cearn_same_but_location(J(base, f), J(new, f), allow_grown=pref(f, grown))
            if okc and not ext:
                known_hit.append((f, [f'経路の行番号だけ（写して一致 {nmv} 欄）' + _decl(known, 'cearn_s') + _actual()])); continue
            if okc and ext:   # (852) 基準の列は経路を写して一致・末尾に増えた列は --grown-ok の宣言（宣言の増減と既知の揺れの両方に名乗る）
                grown_hit.append((f, ext)); known_hit.append((f, [f'経路の行番号だけ（写して一致 {nmv} 欄・列が増えた表）' + _decl(known, 'cearn_s') + _actual()])); continue
        if t == 'cpop_s':   # (963) cpop_s も経路の行番号の移動を写す（宣言の列は known['cpop_s']・cearn_s の宣言とは別）
            CEARN['known'] = known.get('cpop_s', {})
            try:
                okc, nmv, ext = cearn_same_but_location(J(base, f), J(new, f), allow_grown=pref(f, grown), route_cols=CPOP_ROUTE_COLS)
            finally:
                CEARN['known'] = known.get('cearn_s', {})
            if okc and not ext:
                known_hit.append((f, [f'経路の行番号だけ（写して一致 {nmv} 欄）' + _decl(known, 'cpop_s') + _actual()])); continue
            if okc and ext:
                grown_hit.append((f, ext)); known_hit.append((f, [f'経路の行番号だけ（写して一致 {nmv} 欄・列が増えた表）' + _decl(known, 'cpop_s') + _actual()])); continue
        rcs = [x.split(':', 1)[1] for x in removed_cols if ':' in x and f.startswith(x.split(':', 1)[0])]
        kn, d, extra = cols_diff(J(base, f), J(new, f), known.get(t, {}), rows_grown=pref(f, rows_grown), removed_cols=rcs)
        if J(base, f) in COLS_REMOVED and not d and not extra and not kn and not (pref(f, rows_grown) and ROWS_ADDED.get(J(base, f), 0) > 0):
            cols_hit.append((f, COLS_REMOVED[J(base, f)])); continue
        if pref(f, rows_grown) and not d and J(base, f) in ROWS_ADDED and ROWS_ADDED[J(base, f)] > 0:
            rows_hit.append((f, ROWS_ADDED[J(base, f)]))
            if not extra and not kn:
                continue
        if extra:
            if pref(f, grown):
                grown_hit.append((f, extra))
            else:
                real.append((f, ['列が増えた:' + ','.join(extra)]))
        if d:
            real.append((f, d))
        elif kn:
            known_hit.append((f, kn))
        elif not extra:
            real.append((f, ['BYTES_ONLY']))
    # 既知の揺れだけが違う表は known_hit、それ以外が real。増えた列で real に入った表は上で入れ済み
    # (853) 表示の内訳：共通の表を 1 つの区分にだけ数える（優先＝宣言の無い違い・列が増えた・行が増えた・列が消えた・既知の揺れ）。判定（bad_missing・bad_only・real）には触れない。
    gf, rf, cf, kf = ({f for f, _ in x} for x in (grown_hit, rows_hit, cols_hit, known_hit))
    realf = {f for f, _ in real}
    rwf = {f for f, _ in rw_hit}
    part = Counter('real' if f in realf else 'grown' if f in gf else 'rows' if f in rf else 'cols' if f in cf else 'known' if f in kf else 'rw' if f in rwf else 'other' for f in diffs)
    say(f'  基準 {len(fa)} 本・腕 {len(fb)} 本・バイト一致 {same} 本')
    if missing:
        say(f'  基準にあって腕に無い {len(missing)} 本（うち宣言 {len(missing) - len(bad_missing)}）')
    if only:
        say(f'  腕にだけある {len(only)} 本（うち宣言 {len(only) - len(bad_only)}）')
    if grown_hit:
        say(f'  列が増えた表（--grown-ok の宣言）{len(grown_hit)} 本：' + '・'.join(f'{f}+{len(e)}列' for f, e in grown_hit[:6]))
    if rows_hit:
        say(f'  行が増えた表（--rows-grown-ok の宣言・基準の行は順序つき部分列として全て残る）{len(rows_hit)} 本・増えた行 {sum(n for _, n in rows_hit)}：' + '・'.join(f'{table_of(f)}+{n}' for f, n in rows_hit[:6]))
    if rw_hit:   # (1303)
        say(f'  表の中身が宣言どおり変わった表（--rewritten-tables・検算の台本が 0）{len(rw_hit)} 本：' + '・'.join(f'{f}［{m}］' for f, m in rw_hit[:3]))
    if cols_hit:
        say(f'  列が消えた表（--removed-cols の宣言・残りの列は全行バイト一致）{len(cols_hit)} 本：' + '・'.join(sorted(set(table_of(f) + ':' + ','.join(c) for f, c in cols_hit))))
    if known_hit:
        by = {}
        for f, d in known_hit:
            if f in gf or f in rf or f in cf or f in realf:
                continue   # (853) 列が増えた表・行が増えた表・列が消えた表として数え済み（二重に数えない）／(860) 宣言の無い違いの表（real）も数え済み（part は real を先に数える）
            by.setdefault(table_of(f) + ':' + ','.join(d), []).append(f)
        say(f'  既知の揺れの列だけが違う表 {part["known"]} 本：' + '・'.join(f'{k}×{len(v)}' for k, v in sorted(by.items())))
    if bad_missing:
        fail(f'計器の表：基準にあって腕に無い {len(bad_missing)} 本 {bad_missing[:5]}')
    if bad_only:
        fail(f'計器の表：腕にだけある {len(bad_only)} 本 {bad_only[:5]}')
    if real:
        fail(f'計器の表：宣言の無い違い {len(real)} 本')
        for f, d in real[:10]:
            say(f'      {f}	{d[:6]}')
    if not (bad_missing or bad_only or real):
        ok(f'計器の表は恒等（バイト一致 {same}・既知の揺れ {part["known"]}・宣言された増減 {len(missing) - len(bad_missing) + len(only) - len(bad_only) + part["grown"] + part["rows"] + part["cols"]}）' + (f'・検算の台本で通った表 {part["rw"]}' if part['rw'] else ''))
    # (853) 内訳の和＝共通の表の数（表示だけ・合わなければ注意の行を出す）
    tot = same + sum(part.values())
    say(f'  内訳：共通の表 {len(both)} 本＝バイト一致 {same}＋既知の揺れ {part["known"]}＋列が増えた {part["grown"]}＋行が増えた {part["rows"]}＋列が消えた {part["cols"]}＋検算の台本で通った表 {part["rw"]}＋宣言の無い違い {part["real"]}＋その他 {part["other"]}（和 {tot}）・1 表は優先順〔宣言の無い違い→列が増えた→行が増えた→列が消えた→既知の揺れ→検算の台本〕の最初の区分にだけ数える'
        + ('' if tot == len(both) else '　※和が合わない'))


# ---------- 4 注視の表（タグの行以外） ----------
def mon_lines(p):
    return [l for l in open(p, 'rb').read().split(b'\n')
            if not (l.startswith(b'MONHEAD') and '読み手 an_mon.py'.encode('utf-8') in l)]


def cmp_mon(new, base):
    say(f'[注視の表] {os.path.basename(new)} 対 {os.path.basename(base)}（タグの行を除く）')
    if not os.path.exists(new):
        miss('注視の表が無い'); return
    if not os.path.exists(base):
        miss(f'基準の注視の表が無い {base}'); return
    a, b = mon_lines(new), mon_lines(base)
    diff = [(i, x, y) for i, (x, y) in enumerate(zip(a, b)) if x != y]
    if not diff and len(a) == len(b):
        ok(f'タグの行以外が一致（{len(a)} 行）'); return
    fail(f'注視の表：行数 {len(a)} 対 {len(b)}・違う行 {len(diff)}')
    for i, x, y in diff[:5]:
        say(f'      {i + 1}: 新 {x.decode("utf-8", "replace")[:140]}\n         基 {y.decode("utf-8", "replace")[:140]}')


# ---------- 5 テストの数 ----------
def count_listed(p):   # --list-tests の出力の、テスト名の行（先頭が空白 4 つ）の数
    if not os.path.exists(p):
        return -1
    return sum(1 for l in open(p, encoding='utf-8', errors='replace') if re.match(r'^ {4}\S', l))


def cmp_tests(W, cfg, delta):
    say('[テストの数] --list-tests')
    nf, fu = count_listed(J(W, 'tests_list_nonfull.txt')), count_listed(J(W, 'tests_list_full.txt'))
    bn, bf = int(cfg['TESTS_NONFULL']), int(cfg['TESTS_FULL'])
    if nf <= 0 or fu < 0:
        miss('テストの数の出力が無いか読めない'); return
    if nf == bn + delta and fu == bf:
        ok(f'Category!=FullLength {nf}（基準 {bn}' + (f'{delta:+d} を宣言' if delta else '') + f'）・FullLength {fu}（基準 {bf}）')
    else:
        fail(f'テストの数：Category!=FullLength {nf}（基準 {bn}・宣言 {delta:+d}）・FullLength {fu}（基準 {bf}）')
    # (1426) 2 本に分けた形（重い組 H と残り R）の --list-tests：H と R の名は重ならず、和が Tests の Category!=FullLength と一致する（出力が無い木＝分けない形の古い run は読み飛ばす）
    names = {}
    for k in ('T', 'H', 'R'):
        pth = J(W, f'tests_list_split_{k}.txt')
        if os.path.exists(pth):
            names[k] = [l.strip() for l in open(pth, encoding='utf-8', errors='replace') if re.match(r'^ {4}\S', l)]
    if len(names) == 3:
        h, r_, t_ = names['H'], names['R'], names['T']
        both = len(set(h) & set(r_))
        if len(h) > 0 and both == 0 and len(h) + len(r_) == len(t_):
            ok(f'分けた形：H {len(h)}＋R {len(r_)}＝{len(h) + len(r_)}（Tests の Category!=FullLength {len(t_)}・重なり 0）')
        else:
            fail(f'分けた形：H {len(h)}＋R {len(r_)}≠{len(t_)} か H が空か重なり {both}＝filters.sh の H と R の filter を確かめる（scripts/instruments/suite/filters.sh）')


# ---------- 6 savetest と leakhunt ----------
def cmp_saveleak(W):
    say('[savetest と leakhunt]')
    s, l = J(W, 'savetest.txt'), J(W, 'leakhunt.txt')
    if not (os.path.exists(s) and os.path.exists(l)):
        miss('savetest か leakhunt の出力が無い'); return
    ts, tl = open(s, encoding='utf-8', errors='replace').read(), open(l, encoding='utf-8', errors='replace').read()
    # (1380) 最終行が 'savetest OK' で始まる時だけ OK（途中の行に OK が混ざっても通さない）。'値の違い' が 0 でない行があれば不一致。
    tl_lines = [x for x in ts.splitlines() if x.strip()]
    last = tl_lines[-1] if tl_lines else ''
    bad_vals = [x for x in tl_lines if '値の違い' in x and not re.search(r'値の違い 0・', x)]
    if last.startswith('savetest OK') and not bad_vals:
        ok('savetest OK')
    else:
        fail('savetest 不一致（最終行が savetest OK で始まらないか、値の違いが 0 でない）：' + last[:60])
    if '保存則違反なし' in tl and '人口 0四半期' in tl and '基礎貨幣 0四半期' in tl:
        ok('leakhunt 完走・保存則違反なし・世界の台帳の説明できない増減 0')
    else:
        fail('leakhunt が保存則違反なしで終わっていない')


# ---------- 7 NG の集合（任意） ----------
def run_ng(W, cfg, root):   # root＝基準のフォルダの根
    say('[NG の集合] ng.sh（読み手の出力は D:/ の形で渡す）')
    ins = J(W, 'out_ib_10', 'ins')
    outd = W + '/ng'
    base = cfg.get('NG_BASE', '')
    if base.startswith('rd:') and not re.match(r'^rd:([a-zA-Z]:|/)', base):
        base = 'rd:' + J(root, base[3:]).replace(chr(92), '/')
    if not os.path.isdir(ins) or not base:
        miss('NG：計器の出力か基準が無い'); return
    r = subprocess.run([shutil.which('bash') or 'bash', J(os.path.dirname(os.path.abspath(__file__)), '..', 'ng.sh'), ins, 'ident', outd, base],
                       capture_output=True, text=True, encoding='utf-8', errors='replace', env={**os.environ, 'PYTHONUTF8': '1'})
    txt = r.stdout
    m1 = re.search(r'＝NG (\d+)／比べる .*＝NG (\d+)', txt)
    m2 = re.search(r'## 新しい NG.*?: (\d+)', txt); m3 = re.search(r'## 外れた NG.*?: (\d+)', txt)
    if not (m1 and m2 and m3):
        miss('NG：集合の差が読めない\n' + txt[-300:]); return
    if m2.group(1) == '0' and m3.group(1) == '0' and m1.group(1) == m1.group(2):
        ok(f'NG {m1.group(2)} 件・新しい 0・外れた 0（基準 {base}）')
    else:
        fail(f'NG：基準 {m1.group(1)}・比べる {m1.group(2)}・新しい {m2.group(1)}・外れた {m3.group(1)}')


def sh_out(args, cwd=None):
    try:
        return subprocess.run(args, capture_output=True, text=True, encoding='utf-8', errors='replace', cwd=cwd).stdout.strip()
    except Exception:
        return '?'


def first_line(p, key):
    try:
        for l in open(p, encoding='utf-8'):
            if l.startswith(key):
                return l.strip()
    except OSError:
        pass
    return '（無い）'


def norm(p):   # MSYS の /d/… も Windows の D:\… も D:/… に揃える
    p = p.replace(chr(92), '/')
    return (p[1].upper() + ':/' + p[3:]) if re.match(r'^/[a-zA-Z]/', p) else p


STAGE = 2   # (1061) 判定の段（main が marks/stage.txt から決める）


def main():
    a = parse_args(sys.argv[1:])
    root = norm(a['root'])
    cfgp = a['cfg'] or J(root, 'scripts', 'instruments', 'ident', 'ident.cfg')
    cfg = load_cfg(cfgp)
    W = norm(a['work'] or f'/d/PoB_work/ident_{a["label"]}')
    arms_p = J(W, 'marks', 'arms.txt')
    arms = open(arms_p, encoding='utf-8').read().split() if os.path.exists(arms_p) else ['a', 'b', 't']
    known = parse_known(cfg.get('KNOWN', '').split(',') + a['known_extra'])
    base = lambda k: cfg[k] if os.path.isabs(cfg[k]) or re.match(r'^[a-zA-Z]:', cfg[k]) else J(root, cfg[k])
    say(f'=== 恒等の判定（測った種で一致） {a["label"]}（{datetime.datetime.now():%F %T}）===')
    tree = norm(a['tree'] or root)
    CEARN.update(root=root, base=cfg.get('BASE_COMMIT') or None, tree=tree)   # (835) cearn_s の経路の列の規則（BASE_COMMIT が無ければ使わない）
    say(f'測った木：{tree}（HEAD {sh_out(["git", "-C", tree, "rev-parse", "--short", "HEAD"])}・未コミット {len(sh_out(["git", "-C", tree, "status", "--porcelain"]).splitlines())} 本）')
    say(f'基準の設定：{cfgp}')
    say('宣言：' + (' '.join(f'{k}={v}' for k, v in a.items() if k in ('removed', 'added', 'grown', 'rows_grown', 'removed_cols', 'known_extra', 'cost_allow', 'rewritten') and v) + (f' tests_delta={a["tests_delta"]}' if a['tests_delta'] else '') or 'なし'))
    for ar in ('a', 'b', 't', 's'):
        if ar in arms:
            dp = J(W, 'marks', ar + '.done')
            mk = open(dp, encoding='utf-8').read().strip() if os.path.exists(dp) else ''
            say(f'腕 {ar}：{mk or "印なし（走っていないか途中）"}')
            if not mk:
                miss(f'腕 {ar} の完了の印が無い（走っていないか途中）')
            elif not re.match(r'^rc=(0|1|2) ' if ar in ('a', 'b') else r'^rc=0 ', mk):
                # (867) 注だけの直し（判定の規則と上の受け入れの正規表現は変えない）。batch.sh の終了コード（(860) 以後）：0＝成績表が出て最後の行に〈判定:〉がある（ハーネス不合格でも 0）・2＝INS=1 で計器の国が空・3＝腕が満杯で走っていない（ARMFULL）・4＝BATCHFAIL（ビルドの失敗か成績表が無い／途中で切れた）。
                # 受け入れの 1 は旧の木（(860) の前の batch.sh はハーネス不合格を 1 で返した）の基準を読む場合と、ident.sh の prep の失敗（&& で run まで行かず印に rc が書かれない）のための物。prep の失敗は出力の欠けで下の突き合わせの未了に落ちる。3 と 4 は受け入れない。
                # (880) 注だけの追記（判定の行・正規表現・メッセージの文字列は 1 字も変えない）：受け入れの 2（INS=1 で計器の国が空）を印の段で落とさないのは、その走りは計器の表が出ず、下の計器の表の突き合わせ（基準にある表が腕に無い）で不一致に落ちるから。受け入れの組 0|1|2 は今の batch.sh の終了コード 0・2・3・4 のうち 0 と 2 を受け 3 と 4 を受けない（1 は上の旧の木の場合だけ）。
                miss(f'腕 {ar} の終了コードが測れた走りの物でない（{mk}）＝腕が満杯の 3・ビルドや measure.sh の失敗')
    pa, pb = J(W, 'b5_ia_5.txt'), J(W, 'b10_ib_10.txt')
    global STAGE
    stp = J(W, 'marks', 'stage.txt')   # (1061) ident.sh start が stage.py で決めた段（無ければ段 2＝全表）
    STAGE = 1 if os.path.exists(stp) and open(stp, encoding='utf-8').read().splitlines()[0].strip() == '段=1' else 2
    if STAGE == 1 and not cfg.get('SMALL'): miss('段 1 の集合 SMALL が cfg に無い')
    say(f'判定の段：段 {STAGE}（' + ('素の 5 種の全行＋.cost の件数の行＋計器つき 10 種の小さい表の集合 ' + str(len([x for x in cfg.get("SMALL", "").split(",") if x])) + ' 表' if STAGE == 1 else '全表') + '）')
    # 測った木と出力の SOURCE-HASH を突き合わせる（ident.sh start が測る木から数えた値＝measure.sh の src_manifest と同じ数え方）
    thp = J(W, 'marks', 'tree_hash.txt')
    th = open(thp, encoding='utf-8').read().strip() if os.path.exists(thp) else ''
    if not th:
        miss('測る木の SOURCE-HASH の印（marks/tree_hash.txt）が無い')
    for ar, p in (('a', pa), ('b', pb)):
        if ar in arms:
            h = first_line(p + '.manifest', 'SOURCE-HASH').replace('SOURCE-HASH:', '').strip()
            if th and h != th:
                miss(f'腕 {ar} のマニフェストの SOURCE-HASH {h} が測る木の {th} と違う＝この木を測った出力ではない')
    say(f'測る木の SOURCE-HASH：{th or "（無い）"}')
    say()
    if 'a' in arms:
        say('SOURCE-HASH（素の 5 種）' + first_line(pa + '.manifest', 'SOURCE-HASH'))
        cmp_report('素の 5 種', pa, base('B5'))
        cmp_cost('素の 5 種の .cost', pa + '.cost', base('B5') + '.cost', a['cost_allow'])
    if STAGE == 2 and 'b' not in arms:   # (1434) 腕 b が無い＝計器の表を見ていない（--no-ins の走りが〈恒等〉と出さない）
        miss('段 2 で腕 b〔計器つき 10 種〕の出力が無い＝計器の表を見ていない')
    if 'b' in arms:
        say('SOURCE-HASH（計器つき 10 種）' + first_line(pb + '.manifest', 'SOURCE-HASH'))
        cmp_report('計器つき 10 種', pb, base('B10INS'))
        cmp_cost('計器つき 10 種の .cost', pb + '.cost', base('B10INS') + '.cost', a['cost_allow'])
        cmp_ins(J(W, 'out_ib_10', 'ins'), cfg['INSRAW'], known, a['removed'], a['added'], a['grown'], a['rows_grown'], a['removed_cols'],
                only=set(x for x in cfg.get('SMALL', '').split(',') if x) if STAGE == 1 else None, bypass=tuple(x for x in cfg.get('BYPASS', '').split(',') if x),
                rewritten=tuple(_rw_abs(x, tree, root) for x in a['rewritten']))
        cmp_mon(J(W, 'mon_ib_10.txt'), base('MON'))
        if a['ng']:
            run_ng(W, {**cfg}, root)
    if 't' in arms:
        cmp_tests(W, cfg, a['tests_delta'])
    if 's' in arms or a['saveleak']:
        cmp_saveleak(W)
    say()
    verdict = '不一致' if FAILS else ('未了' if MISSING else '恒等')
    if FAILS:
        say(f'不一致の理由 {len(FAILS)} 件：')
        for f in FAILS:
            say('  - ' + f)
    if MISSING:
        say(f'未了 {len(MISSING)} 件：')
        for m in MISSING:
            say('  - ' + m)
    if STAGE == 1:
        say('段 1 は集合の外の計器の表を見ていない' + ('＝不一致は --stage2 を付けて全表の段 2 を回して読む' if verdict != '恒等' else ''))
    say('試験の腕は件数だけ（--list-tests）＝赤の有無は見ていない・赤の有無は ㉘ とフルスイートで見る（(1206)）')
    say(f'判定: {verdict}' + ('（段 1）' if STAGE == 1 else ''))
    if a['save']:
        d, mdir = a['save'], J(root, 'measurements')
        lab = a['label']
        for src, dst in ((pa, f'b5_{d}_{lab}'), (pb, f'b10_{d}_{lab}_ins')):
            for suf in ('', '.manifest', '.cost'):
                if os.path.exists(src + suf):
                    shutil.copyfile(src + suf, J(mdir, dst + suf))
        mp = J(W, 'mon_ib_10.txt')
        if os.path.exists(mp):
            shutil.copyfile(mp, J(mdir, f'mon_{d}_{lab}.txt'))
        with open(J(mdir, f'ident_{d}_{lab}.txt'), 'wb') as o:
            o.write(('\n'.join(OUT) + '\n').encode('utf-8'))
        print(f'成績表・マニフェスト・.cost・注視の表・この報告を {mdir} に残した（{d}_{lab}）')
    sys.exit({'恒等': 0, '不一致': 1, '未了': 2}[verdict])


if __name__ == "__main__":
    main()
