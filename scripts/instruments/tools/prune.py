# (1046) 判定の後の刈り込み・manifest・圧縮（台本だけ・エンジンは 0 行）。
#   python -X utf8 -I prune.py prune <ins フォルダ> <基準の ins フォルダ> --verdict-file <判定を含む出力> --manifest <出力 tsv>
#        [--pre-out <先頭の列の tsv>] [--copy-to <measurements の写し>] [--deleted <消した名の一覧>] [--keep-prefix 表の頭,...]
#        [--heavy] [--heavy-keep 表の頭,...] [--dry] [--base-sums <基準の sha1 の写し tsv>]
#   python -X utf8 -I prune.py guard <パス>        ＝消してよい場所か（0＝よい・3＝拒む）
#   python -X utf8 -I prune.py pack <フォルダ> [--rm]   ＝<フォルダ>.tar.gz に包む（--rm は包めたと確かめた後に元を消す）
#   python -X utf8 -I prune.py unpack <tar.gz> <出力先>
# 規則（measurements/m1046_20261111_prereg_dv.txt）：
#   ・最後の〈判定:〉の行が〈判定: 恒等〉か〈判定: 不一致〉でなければ何も消さない。
#   ・消すのは基準の同名の表と〈行末を LF にそろえた全体の sha1〉が一致する表だけ。食い違い・足した表・宣言した表・KNOWN の表・列が増えた表は残す。
#   ・重い 4 表は --heavy のときだけ消す対象（--heavy-keep の名指しは残す）。
#   ・消せる場所は /d/PoB_work/ident_* と /d/PoB_work/c99dv_work の下だけ。keep_*・INSRAW・シンボリックリンクは拒む。
import sys, os, re, hashlib, shutil, tarfile

HEAVY = ('cpop_s', 'cearn_s', 'subst_s', 'prodcap_s')
ALLOWED_TOP = (re.compile(r'^ident_[^/]+$'), re.compile(r'^c99dv_work$'))
ROOT = '/d/PoB_work'
LF_NORMALIZE = True          # 突然変異 M5 の差し替え点
VERDICT_GATE = True          # 突然変異 M1 の差し替え点


def norm_path(p):
    p = p.replace('\\', '/')
    if not re.match(r'^/[A-Za-z]/', p): p = os.path.abspath(p).replace('\\', '/')
    m = re.match(r'^([A-Za-z]):/(.*)$', p)
    if m: p = '/' + m.group(1).lower() + '/' + m.group(2)
    return p.rstrip('/')


def native(p):
    m = re.match(r'^/([a-z])/(.*)$', p)
    return (m.group(1).upper() + ':/' + m.group(2)) if m else p


def guard(p, protect=()):
    """消してよい場所なら None・拒むなら理由の文字列。"""
    q = norm_path(p)
    if not (q + '/').startswith(ROOT + '/') or q == ROOT:
        return f'/d/PoB_work の外：{q}'
    rel = q[len(ROOT) + 1:]
    parts = rel.split('/')
    if any(x.startswith('keep_') or x in ('branch_backup', 'PoB_play') or x.startswith('wt_') for x in parts):
        return f'保護された場所：{rel}'
    if not any(r.match(parts[0]) for r in ALLOWED_TOP):
        return f'他の線の作業フォルダ：{parts[0]}'
    cur = ROOT
    for x in parts:
        cur += '/' + x
        if os.path.islink(cur):
            return f'シンボリックリンク：{cur}'
    for pr in protect:
        pq = norm_path(pr)
        if q == pq or (q + '/').startswith(pq + '/') or (pq + '/').startswith(q + '/'):
            return f'基準の生と重なる：{pq}'
    return None


def lf_sha1(path):
    """行末を LF にそろえた全体の sha1（\\r\\n → \\n・最後の行に改行が無ければ足す）。"""
    h = hashlib.sha1(); carry = b''; last = b''
    with open(path, 'rb') as f:
        for c in iter(lambda: f.read(1 << 22), b''):
            c = carry + c; carry = b''
            if LF_NORMALIZE:
                if c.endswith(b'\r'): carry = b'\r'; c = c[:-1]
                c = c.replace(b'\r\n', b'\n')
            if c: last = c[-1:]
            h.update(c)
    if carry: h.update(carry); last = carry
    if last and last != b'\n': h.update(b'\n')
    return h.hexdigest()


def header_cols(path):
    with open(path, 'rb') as g:
        return g.readline().rstrip(b'\r\n').split(b'\t')


def pre_sha1(path, k):
    """先頭の k 列だけの sha1（prefix1044.py と同じ形＝各行の先頭 k 列を \\t で結び \\n を付ける）。"""
    h = hashlib.sha1()
    with open(path, 'rb') as g:
        for line in g:
            h.update(b'\t'.join(line.rstrip(b'\r\n').split(b'\t')[:k]) + b'\n')
    return h.hexdigest()


def read_verdict(p):
    if not p or not os.path.isfile(p): return None
    last = ''
    with open(p, encoding='utf-8', errors='replace') as f:
        for l in f:
            if l.startswith('判定:'): last = l.strip()   # 判定の行（--save の後書きの行より前の最後の物）
    return last if last in ('判定: 恒等', '判定: 不一致') else None


def should_delete(name, base_full, full, kept_prefix, heavy_on, heavy_keep, grown):
    """（消すか, 理由）。"""
    if base_full is None: return False, '基準に無い（足した表）'
    if any(name.startswith(x) for x in kept_prefix): return False, '揺れ・宣言の表'
    isheavy = any(name.startswith(x) for x in HEAVY)
    if isheavy and not heavy_on: return False, '重い表（刈りは切ってある）'
    if isheavy and any(name.startswith(x) for x in heavy_keep): return False, '重い表（名指しで残す）'
    if grown: return False, '列が増えた表'
    if full != base_full: return False, '基準と食い違い'
    return True, '基準と全体が一致'


def load_sums(p):
    d = {}
    if p and os.path.isfile(p):
        for l in open(p, encoding='utf-8'):
            x = l.rstrip('\n').split('\t')
            if len(x) >= 2 and not l.startswith('#'): d[x[0]] = x[1]
    return d


def cmd_prune(a):
    ins, base = a['pos'][0], a['pos'][1]
    why = None
    v = read_verdict(a.get('verdict-file'))
    if VERDICT_GATE and v is None:
        print('刈らない：判定の行（判定: 恒等／不一致）が無い'); return 0
    if not a.get('dry'):
        why = guard(ins, protect=(base,))
        if why: print('拒む：' + why); return 3
    if not os.path.isdir(ins) or not os.path.isdir(base):
        print('刈らない：フォルダが無い'); return 0
    heavy_keep = [x for x in a.get('heavy-keep', '').split(',') if x]
    kept_prefix = [x for x in a.get('keep-prefix', '').split(',') if x]
    cache = load_sums(a.get('base-sums'))
    rows = []; dele = []
    for n in sorted(os.listdir(ins)):
        p = os.path.join(ins, n)
        if not os.path.isfile(p): continue
        bp = os.path.join(base, n)
        full = lf_sha1(p); size = os.path.getsize(p)
        bfull = None; k = '-'; pre = '-'; grown = False
        if os.path.isfile(bp):
            bfull = cache.get(n) or lf_sha1(bp)
            ha, hb = header_cols(p), header_cols(bp)
            if ha != hb and ha[:len(hb)] == hb:
                grown = True; k = len(hb); pre = pre_sha1(p, k)
        d, r = should_delete(n, bfull, full, kept_prefix, bool(a.get('heavy')), heavy_keep, grown)
        rows.append((n, size, full, k, pre, bfull or '-', ('消す' if d else '残す') + '：' + r))
        if d: dele.append((n, p, r))
    out = ['#name\tbytes\tsha1_full_LF\tcols_base\tsha1_prefix_LF\tbase_sha1_full_LF\taction']
    out += ['\t'.join(str(x) for x in r) for r in rows]
    for dst in (a['manifest'], a.get('copy-to')):
        if dst:
            os.makedirs(os.path.dirname(os.path.abspath(dst)), exist_ok=True)
            with open(dst, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
    if a.get('pre-out'):
        with open(a['pre-out'], 'w', encoding='utf-8', newline='\n') as f:
            for r in rows:
                if r[3] != '-': f.write(f'{r[0]}\t{r[3]}\t{r[4]}\n')
    if not a.get('dry'):
        for n, p, r in dele: os.remove(p)
        if a.get('deleted'):
            with open(a['deleted'], 'a', encoding='utf-8', newline='\n') as f:
                for n, p, r in dele: f.write(f'{norm_path(p)} 消した（{r}・sha1 は manifest）\n')
    nbytes = sum(r[1] for r in rows if r[6].startswith('消す'))
    print(f'刈り込み：判定={v}・表 {len(rows)} 本・消した {0 if a.get("dry") else len(dele)} 本（{nbytes / 1e9:.2f}GB）・残した {len(rows) - len(dele)} 本'
          + ('（--dry＝消していない）' if a.get('dry') else ''))
    return 0


def cmd_pack(a):
    d = a['pos'][0]
    why = guard(d)
    if why: print('拒む：' + why); return 3
    q = native(norm_path(d)); out = q + '.tar.gz'
    with tarfile.open(out, 'w:gz', compresslevel=6) as t:
        t.add(q, arcname=os.path.basename(q))
    nfiles = sum(len(fs) for _, _, fs in os.walk(q))
    with tarfile.open(out, 'r:gz') as t:
        got = sum(1 for m in t.getmembers() if m.isfile())
    if got != nfiles: print(f'包みの検算が合わない：{got}/{nfiles}'); os.remove(out); return 4
    print(f'包んだ：{out}（{nfiles} 本）')
    if a.get('rm'): shutil.rmtree(q); print('元を消した：' + q)
    return 0


def cmd_unpack(a):
    tgz, dst = a['pos'][0], a['pos'][1]
    why = guard(dst)
    if why: print('拒む：' + why); return 3
    os.makedirs(dst, exist_ok=True)
    with tarfile.open(tgz, 'r:gz') as t: t.extractall(dst, filter='data')
    print('解いた：' + dst); return 0


def parse(argv):
    a = {'pos': []}; i = 0
    flags = {'dry', 'heavy', 'rm'}
    while i < len(argv):
        x = argv[i]
        if x.startswith('--'):
            k = x[2:]
            if k in flags: a[k] = True; i += 1
            else: a[k] = argv[i + 1]; i += 2
        else: a['pos'].append(x); i += 1
    return a


def main():
    cmd = sys.argv[1]; a = parse(sys.argv[2:])
    if cmd == 'guard':
        w = guard(a['pos'][0]); print(w or 'ok'); return 3 if w else 0
    return {'prune': cmd_prune, 'pack': cmd_pack, 'unpack': cmd_unpack}[cmd](a)


if __name__ == '__main__':
    sys.exit(main())
