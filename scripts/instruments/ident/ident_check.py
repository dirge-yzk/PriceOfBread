# (728) 恒等の証明の突き合わせ（読むだけ）。ident.sh check から呼ぶ。使い方は ident.sh の先頭。
#   python -I ident_check.py <ラベル> --root <木> [--work <作業フォルダ>] [--cfg <設定>] [--tree <測った木>] [--save <日付>] [宣言...] [--saveleak] [--ng]
# 判定の規則：宣言の無い違いは全て〈不一致〉。基準の生が無い・出力が無いなど突き合わせられない所は〈未了〉（恒等とは言わない）。
import sys, os, re, csv, hashlib, shutil, subprocess, datetime
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
    a = {'label': argv[0], 'removed': [], 'added': [], 'grown': [], 'known_extra': [], 'cost_allow': [],
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


def cols_diff(pa, pb, kmap):   # pa＝基準・pb＝腕。返す物＝（既知の揺れの列・宣言の無い違い・腕で増えた列）
    with open(pa, encoding='utf-8', newline='') as fa, open(pb, encoding='utf-8', newline='') as fb:
        ra = csv.reader(fa, delimiter='\t'); rb = csv.reader(fb, delimiter='\t')
        ha = next(ra, []); hb = next(rb, [])
        extra = [c for c in hb if c not in ha]
        common = [c for c in ha if c in hb]
        ia = {c: ha.index(c) for c in common}; ib = {c: hb.index(c) for c in common}
        real, kn = set(), set()
        if [c for c in hb if c in ha] != ha:
            real.add('HEADER_ORDER_OR_MISSING')
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


def cmp_ins(new, base, known, removed, added, grown):
    say(f'[計器の表] 腕 {new}  対  基準 {base}')
    if not os.path.isdir(new):
        miss('計器の出力フォルダが無い'); return
    if not os.path.isdir(base):
        miss(f'基準の生が無い（{base}）＝計器の表は突き合わせられない'); return
    fa = sorted(x for x in os.listdir(base) if os.path.isfile(J(base, x)))
    fb = sorted(x for x in os.listdir(new) if os.path.isfile(J(new, x)))
    sb, sa = set(fb), set(fa)

    def pair(f):
        pa, pb = J(base, f), J(new, f)
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
    bad_missing = [f for f in missing if not pref(f, removed)]
    bad_only = [f for f in only if not pref(f, added)]
    known_hit, grown_hit, real = [], [], []
    for f in diffs:
        if not f.endswith('.tsv'):
            real.append((f, ['NONTSV'])); continue
        t = table_of(f)
        if t == 'flow_s' and flow_same_but_location(J(base, f), J(new, f)):
            known_hit.append((f, ['呼び出し位置だけ'])); continue
        kn, d, extra = cols_diff(J(base, f), J(new, f), known.get(t, {}))
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
    say(f'  基準 {len(fa)} 本・腕 {len(fb)} 本・バイト一致 {same} 本')
    if missing:
        say(f'  基準にあって腕に無い {len(missing)} 本（うち宣言 {len(missing) - len(bad_missing)}）')
    if only:
        say(f'  腕にだけある {len(only)} 本（うち宣言 {len(only) - len(bad_only)}）')
    if grown_hit:
        say(f'  列が増えた表（--grown-ok の宣言）{len(grown_hit)} 本：' + '・'.join(f'{f}+{len(e)}列' for f, e in grown_hit[:6]))
    if known_hit:
        by = {}
        for f, d in known_hit:
            by.setdefault(table_of(f) + ':' + ','.join(d), []).append(f)
        say(f'  既知の揺れの列だけが違う表 {len(known_hit)} 本：' + '・'.join(f'{k}×{len(v)}' for k, v in sorted(by.items())))
    if bad_missing:
        fail(f'計器の表：基準にあって腕に無い {len(bad_missing)} 本 {bad_missing[:5]}')
    if bad_only:
        fail(f'計器の表：腕にだけある {len(bad_only)} 本 {bad_only[:5]}')
    if real:
        fail(f'計器の表：宣言の無い違い {len(real)} 本')
        for f, d in real[:10]:
            say(f'      {f}\t{d[:6]}')
    if not (bad_missing or bad_only or real):
        ok(f'計器の表は恒等（バイト一致 {same}・既知の揺れ {len(known_hit)}・宣言された増減 {len(missing) - len(bad_missing) + len(only) - len(bad_only) + len(grown_hit)}）')


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


# ---------- 6 savetest と leakhunt ----------
def cmp_saveleak(W):
    say('[savetest と leakhunt]')
    s, l = J(W, 'savetest.txt'), J(W, 'leakhunt.txt')
    if not (os.path.exists(s) and os.path.exists(l)):
        miss('savetest か leakhunt の出力が無い'); return
    ts, tl = open(s, encoding='utf-8', errors='replace').read(), open(l, encoding='utf-8', errors='replace').read()
    if 'savetest OK' in ts:
        ok('savetest OK')
    else:
        fail('savetest が OK でない')
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
    say(f'=== 恒等の証明 {a["label"]}（{datetime.datetime.now():%F %T}）===')
    tree = norm(a['tree'] or root)
    say(f'測った木：{tree}（HEAD {sh_out(["git", "-C", tree, "rev-parse", "--short", "HEAD"])}・未コミット {len(sh_out(["git", "-C", tree, "status", "--porcelain"]).splitlines())} 本）')
    say(f'基準の設定：{cfgp}')
    say('宣言：' + (' '.join(f'{k}={v}' for k, v in a.items() if k in ('removed', 'added', 'grown', 'known_extra', 'cost_allow') and v) + (f' tests_delta={a["tests_delta"]}' if a['tests_delta'] else '') or 'なし'))
    for ar in ('a', 'b', 't', 's'):
        if ar in arms:
            dp = J(W, 'marks', ar + '.done')
            mk = open(dp, encoding='utf-8').read().strip() if os.path.exists(dp) else ''
            say(f'腕 {ar}：{mk or "印なし（走っていないか途中）"}')
            if not mk:
                miss(f'腕 {ar} の完了の印が無い（走っていないか途中）')
            elif not mk.startswith('rc=0 '):
                miss(f'腕 {ar} の終了コードが 0 でない（{mk}）＝測れていない（腕が満杯の 3・ビルドや measure.sh の失敗）')
    pa, pb = J(W, 'b5_ia_5.txt'), J(W, 'b10_ib_10.txt')
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
    if 'b' in arms:
        say('SOURCE-HASH（計器つき 10 種）' + first_line(pb + '.manifest', 'SOURCE-HASH'))
        cmp_report('計器つき 10 種', pb, base('B10INS'))
        cmp_cost('計器つき 10 種の .cost', pb + '.cost', base('B10INS') + '.cost', a['cost_allow'])
        cmp_ins(J(W, 'out_ib_10', 'ins'), cfg['INSRAW'], known, a['removed'], a['added'], a['grown'])
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
    say(f'判定: {verdict}')
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
