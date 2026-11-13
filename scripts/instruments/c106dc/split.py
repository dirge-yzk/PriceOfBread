# c106dc 分割：実装ログ・00一覧・CURRENT を〈残り〉と〈書庫〉へ。判定は決定的。
import re, sys, json
sys.path.insert(0, 'D:/PoB_work/wt_c106dc/scripts/instruments/c106dc')
from common import *

def split_log(b):
    L = lines(b)
    mon = None
    cur = '2026-09'
    dest = [None] * len(L)   # 'keep' / '09' / '10'
    sec_start = None
    # 先頭〜最初の「## 」の前＝残り
    state = 'keep'
    for i, l in enumerate(L):
        if l.startswith(b'## '):
            m = re.match(rb'## (20\d\d-\d\d)-\d\d', l)
            if m: cur = m.group(1).decode()
            state = {'2026-09': '09', '2026-10': '10', '2026-11': 'keep'}[cur]
        dest[i] = state
    return L, dest

def split_one(b):
    L = lines(b)
    dest = []
    pat = re.compile(rb'^\s*\|?\s*~~')
    for l in L:
        d = 'keep'
        if pat.match(l):
            ds = re.findall(rb'2026-(\d\d)-(\d\d)', l)
            if ds and (ds[-1][0], ds[-1][1]) < (b'10', b'13'):
                d = 'arc'
        dest.append(d)
    return L, dest

def split_cur(b):
    L = lines(b)
    # 先頭〜「# 1-f.」の前の行（1293 行まで）の〈今〉だけを残し、以降を書庫へ。境は〈# 1-f.〉の見出し行。
    n = next(i for i, l in enumerate(L) if l.startswith('# 1-f.'.encode()))
    return L, ['keep' if i < n else 'arc' for i in range(len(L))]

def build():
    out = {}
    for name, fn, f in (('log', split_log, F_LOG), ('one', split_one, F_ONE), ('cur', split_cur, F_CUR)):
        L, d = fn(base(f))
        out[name] = (L, d)
    return out
