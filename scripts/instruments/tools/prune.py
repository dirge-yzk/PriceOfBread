# (1054) 判定の後の刈り込み・manifest・圧縮（台本だけ・エンジンは 0 行）。(1046) の直し：c99dv の反証役 v の高・中・低。
#   python -X utf8 -I prune.py prune <ins フォルダ> <基準の ins フォルダ> --verdict-file <判定を含む出力> --manifest <出力 tsv> --allow <作業フォルダ名>
#        [--pre-spec <表・列数・sha1 の tsv>] [--copy-to <measurements の写し>] [--deleted <消した名の一覧>] [--keep-prefix 表の頭,...]
#        [--heavy] [--heavy-keep 表の頭,...] [--dry]
#   python -X utf8 -I prune.py guard <パス> [--allow 作業フォルダ名] [--any]   ＝消してよい場所か（0＝よい・3＝拒む・--any は ins/ の名を問わない）
#   python -X utf8 -I prune.py pack <フォルダ> [--rm] [--allow 名]   ＝<フォルダ>.tar.gz に包む（--rm は包めたと確かめた後に元を消す）
#   python -X utf8 -I prune.py unpack <tar.gz> <出力先> [--allow 名]
# 規則（measurements/m1054_20261112_prereg_dv.txt）：
#   ・最後の〈判定:〉の行が〈判定: 恒等〉のときだけ刈る（不一致・未了・行が無い・ファイルが無いときは何も消さない）。
#   ・消すかどうかは〈生のバイトの sha1〉が基準の同名の表と一致するかだけで決める（行末はそろえない）。LF にそろえた sha1・先頭の列の sha1 は manifest への記録だけ。
#   ・揺れ・宣言・足した表・列が増えた表・食い違いの表は残す。重い 4 表は --heavy のときだけ対象（--heavy-keep の名指しは残す）。
#   ・消せる場所は /d/PoB_work/<--allow の名>/…/ins だけ。実体のパスを解いてから判定し、keep_*・wt_*・基準の生・リンク・ジャンクションは拒む。
import sys, os, re, hashlib, shutil, tarfile
from concurrent.futures import ThreadPoolExecutor

HEAVY = ('cpop_s', 'cearn_s', 'subst_s', 'prodcap_s')
ROOT = '/d/pob_work'          # 判定は小文字にそろえて行う（Windows は大文字小文字を区別しない）
DEFAULT_ALLOW = ('c100dv_work',)
LF_NORMALIZE = True           # 突然変異 M3 の差し替え点（記録の LF 列だけに効く）
VERDICT_GATE = True           # 突然変異 M1 の差し替え点（判定の行の確認）
ONLY_IDENT = True             # 突然変異 M2 の差し替え点（〈判定: 恒等〉だけを通す）
RAW_KEY = True                # 突然変異 M3 の差し替え点（消す鍵が生の sha1）
CAN_REALPATH = True           # 突然変異 M5 の差し替え点（実体の解決＝/d/ の形・リンクを解く）
COPY_TO = True                # 突然変異 M6 の差し替え点
KEEP_FLAKY = True             # 突然変異 M4 の差し替え点（揺れ・宣言の表を残す）


def norm_path(p):
    """/d/…・D:/…・D:\\… の 3 つの形を /d/… に（小文字・末尾の / なし）。"""
    p = p.replace('\\', '/')
    if p.startswith('//?/'): p = p[4:]
    if not re.match(r'^(/[A-Za-z]/|[A-Za-z]:/)', p): p = os.path.abspath(p).replace('\\', '/')
    m = re.match(r'^([A-Za-z]):/(.*)$', p)
    if m: p = '/' + m.group(1) + '/' + m.group(2)
    return p.rstrip('/').lower()


def native(p):
    p = norm_path(p)
    m = re.match(r'^/([a-z])/(.*)$', p)
    return (m.group(1).upper() + ':/' + m.group(2)) if m else p


GUARD_NATIVE = native         # 突然変異 M5 の差し替え点（実体を調べる側のパスの形）


def resolved(p):
    return norm_path(os.path.realpath(GUARD_NATIVE(p))) if CAN_REALPATH else norm_path(p)


def guard(p, protect=(), allow=DEFAULT_ALLOW, need_ins=True):
    """消してよい場所なら None・拒むなら理由の文字列。"""
    q0 = norm_path(p); q = resolved(p)
    if q != q0: return f'リンクかジャンクションを通る：{q0} → {q}'
    if not (q + '/').startswith(ROOT + '/') or q == ROOT:
        return f'/d/PoB_work の外：{q}'
    parts = q[len(ROOT) + 1:].split('/')
    if any(x.startswith('keep_') or x.startswith('wt_') or x in ('branch_backup', 'pob_play', 'base_ins') for x in parts):
        return f'保護された場所：{q}'
    if parts[0] not in [a.lower() for a in allow]:
        return f'許した作業フォルダの外：{parts[0]}'
    if need_ins and parts[-1] != 'ins': return f'ins/ の外：{q}'
    cur = GUARD_NATIVE(ROOT)
    for x in parts:
        cur += '/' + x
        if os.path.islink(cur) or (hasattr(os.path, 'isjunction') and os.path.isjunction(cur)):
            return f'リンク：{cur}'
    for pr in protect:
        pq = resolved(pr)
        if q == pq or (q + '/').startswith(pq + '/') or (pq + '/').startswith(q + '/'):
            return f'基準の生と重なる：{pq}'
    return None


def hash3(path):
    """（生の sha1・行末を LF にそろえた sha1・バイト数）を 1 回の読みで。"""
    hr, hl = hashlib.sha1(), hashlib.sha1(); carry = b''; last = b''; n = 0
    with open(path, 'rb') as f:
        for c in iter(lambda: f.read(1 << 22), b''):
            n += len(c); hr.update(c)
            c = carry + c; carry = b''
            if LF_NORMALIZE:
                if c.endswith(b'\r'): carry = b'\r'; c = c[:-1]
                c = c.replace(b'\r\n', b'\n')
            if c: last = c[-1:]
            hl.update(c)
    if carry: hl.update(carry); last = carry
    if last and last != b'\n': hl.update(b'\n')
    return hr.hexdigest(), hl.hexdigest(), n


def raw_sha1(path):
    h = hashlib.sha1()
    with open(path, 'rb') as f:
        for c in iter(lambda: f.read(1 << 22), b''): h.update(c)
    return h.hexdigest()


def pre_sha1(path, k):
    """先頭の k 列だけの sha1（prefix1044.py と同じ形＝各行の先頭 k 列を \\t で結び \\n を付ける）。"""
    h = hashlib.sha1()
    with open(path, 'rb') as g:
        for line in g:
            h.update(b'\t'.join(line.rstrip(b'\r\n').split(b'\t')[:k]) + b'\n')
    return h.hexdigest()


def header_cols(path):
    with open(path, 'rb') as g:
        return g.readline().rstrip(b'\r\n').split(b'\t')


def read_verdict(p):
    """最後の判定の行が〈判定: 恒等〉のときだけ返す（不一致・未了・無しは None）。"""
    if not p or not os.path.isfile(p): return None
    last = ''
    with open(p, encoding='utf-8', errors='replace') as f:
        for l in f:
            if l.startswith('判定:'): last = l.strip()
    if ONLY_IDENT: return last if last == '判定: 恒等' else None
    return last if last in ('判定: 恒等', '判定: 不一致') else None


def should_delete(name, same_raw, kept_prefix, heavy_on, heavy_keep, grown):
    """（消すか, 理由）。same_raw＝基準の同名の表と鍵が一致（None＝基準に無い）。"""
    if same_raw is None: return False, '基準に無い（足した表）'
    if KEEP_FLAKY and any(name.startswith(x) for x in kept_prefix): return False, '揺れ・宣言の表'
    isheavy = any(name.startswith(x) for x in HEAVY)
    if isheavy and not heavy_on: return False, '重い表（刈りは切ってある）'
    if isheavy and any(name.startswith(x) for x in heavy_keep): return False, '重い表（名指しで残す）'
    if grown: return False, '列が増えた表'
    if not same_raw: return False, '基準と生が食い違い'
    return True, '基準と生のバイトが一致'


def load_pre(p):
    d = {}
    if p and os.path.isfile(p):
        for l in open(p, encoding='utf-8'):
            x = l.rstrip('\n').split('\t')
            if len(x) >= 2 and not l.startswith('#'): d[x[0]] = int(x[1])
    return d


def cmd_prune(a):
    ins, base = a['pos'][0], a['pos'][1]
    v = read_verdict(a.get('verdict-file'))
    if VERDICT_GATE and v is None:
        print('刈らない：最後の判定の行が〈判定: 恒等〉でない'); return 0
    if not a.get('dry'):
        why = guard(ins, protect=(base,), allow=a.get('allow', ','.join(DEFAULT_ALLOW)).split(','))
        if why: print('拒む：' + why); return 3
    if not os.path.isdir(ins) or not os.path.isdir(base):
        print('刈らない：フォルダが無い'); return 0
    heavy_keep = [x for x in a.get('heavy-keep', '').split(',') if x]
    kept_prefix = [x for x in a.get('keep-prefix', '').split(',') if x]
    pre = load_pre(a.get('pre-spec'))
    names = [n for n in sorted(os.listdir(ins)) if os.path.isfile(os.path.join(ins, n)) and not os.path.islink(os.path.join(ins, n))]

    def one(n):
        p = os.path.join(ins, n); bp = os.path.join(base, n)
        hr, hl, size = hash3(p)
        same = None; grown = False; k = '-'; ps = '-'; braw = '-'
        if os.path.isfile(bp):
            braw = raw_sha1(bp) if os.path.getsize(bp) == size else '(大きさが違う)'
            same = (braw == hr) if RAW_KEY else (hash3(bp)[1] == hl)
            ha, hb = header_cols(p), header_cols(bp)
            if ha != hb and ha[:len(hb)] == hb: grown = True
        kk = pre.get(n)
        if kk is None and grown: kk = len(header_cols(bp))
        if kk is not None: k = kk; ps = pre_sha1(p, kk)
        d, r = should_delete(n, same, kept_prefix, bool(a.get('heavy')), heavy_keep, grown)
        return (n, size, hr, hl, k, ps, braw, ('消す' if d else '残す') + '：' + r), d, p, r
    with ThreadPoolExecutor(6) as ex: res = list(ex.map(one, names))
    rows = [r[0] for r in res]; dele = [(r[2], r[3]) for r in res if r[1]]
    out = ['#name\tbytes\tsha1_raw\tsha1_full_LF\tcols\tsha1_prefix_LF\tbase_sha1_raw\taction']
    out += ['\t'.join(str(x) for x in r) for r in rows]
    dsts = [a['manifest']] + ([a['copy-to']] if COPY_TO and a.get('copy-to') else [])
    for dst in dsts:
        os.makedirs(os.path.dirname(os.path.abspath(dst)), exist_ok=True)
        with open(dst, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
    if not a.get('dry'):
        for p, r in dele: os.remove(p)
        if a.get('deleted'):
            with open(a['deleted'], 'a', encoding='utf-8', newline='\n') as f:
                for p, r in dele: f.write(f'{norm_path(p)} 消した（{r}・sha1 は manifest）\n')
    nbytes = sum(r[1] for r in rows if r[7].startswith('消す'))
    print(f'刈り込み：判定={v}・表 {len(rows)} 本・消した {0 if a.get("dry") else len(dele)} 本（{nbytes / 1e9:.2f}GB）・残した {len(rows) - len(dele)} 本'
          + ('（--dry＝消していない）' if a.get('dry') else ''))
    return 0


def cmd_pack(a):
    d = a['pos'][0]
    why = guard(d, allow=a.get('allow', ','.join(DEFAULT_ALLOW)).split(','), need_ins=False)
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
    why = guard(dst, allow=a.get('allow', ','.join(DEFAULT_ALLOW)).split(','), need_ins=False)
    if why: print('拒む：' + why); return 3
    os.makedirs(dst, exist_ok=True)
    with tarfile.open(tgz, 'r:gz') as t: t.extractall(dst, filter='data')
    print('解いた：' + dst); return 0


def parse(argv):
    a = {'pos': []}; i = 0
    flags = {'dry', 'heavy', 'rm', 'any'}
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
        w = guard(a['pos'][0], allow=a.get('allow', ','.join(DEFAULT_ALLOW)).split(','), need_ins=not a.get('any')); print(w or 'ok'); return 3 if w else 0
    return {'prune': cmd_prune, 'pack': cmd_pack, 'unpack': cmd_unpack}[cmd](a)


if __name__ == '__main__':
    sys.exit(main())
