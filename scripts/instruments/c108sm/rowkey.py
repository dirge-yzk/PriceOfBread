# (1146) c108sm の表の札：README の表の行を引く鍵。make_short.py（札を作る）と archive_gate.py（札が README にちょうど 1 行あるか）が同じ関数を使う。
# 鍵は 2 種。単独＝第 1 欄（★⚠ を除いた名）の頭一致。組＝第 1 欄（★ を残す）・第 2 欄（印を除く）。数字だけの第 1 欄は別の表と重なる（★★★1 は 3 表）ので組で名乗る。
import re


def cells(x):
    return [y.strip() for y in x.strip().strip('|').split('|')] if x.lstrip().startswith('|') else []


def name1(x):
    c = cells(x)
    return re.sub(r'[*★⚠`~]', '', c[0]).strip() if c else ''


def pair(x):
    c = cells(x)
    return (re.sub(r'[*`~]', '', c[0]).strip() + '・' + re.sub(r'[*`~]', '', c[1]).strip()) if len(c) > 1 else ''


def count(cur, key):
    """key に当たる行の数。単独の頭一致か組の頭一致。空の鍵は 0。"""
    if not key: return 0
    return sum(1 for x in cur if x.lstrip().startswith('|') and (name1(x).startswith(key) or pair(x).startswith(key)))


def simple_key(t, cur):
    """単独の鍵（第 1 欄の名の頭 24 字）。ちょうど 1 行に当たらない・ASCII の数字だけなら None。"""
    n1 = re.split('[（`]', name1(t))[0].strip()[:24]
    return n1 if n1 and not re.fullmatch('[0-9]+', n1) and count(cur, n1) == 1 else None


def pair_key(t, cur):
    """組の鍵（第 1 欄＋第 2 欄の頭 30 字）。ちょうど 1 行に当たらなければ None。"""
    k = pair(t)[:30]
    return k if count(cur, k) == 1 else None
