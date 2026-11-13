# (1120) c108sm 書庫の規則の残り。00一覧の行を書庫へ／書庫から移す（行の中身は 1 バイトも変えない）。
#  規則 1：行の中の最も遅い日付が 2026-10-13 以後の行は、書庫に置かない（旧は最後の日付で判定＝(215) の行が 10-14 の確かめを含むのに書庫へ入った）。境（10-13 ちょうど）も残す側。
#  規則 2：行頭が `| ~~` でなくても、題の要素（箇条の印・表の見出しの後ろ・IDの次の欄）が打消しで始まる行は、最も遅い日付が 2026-10-13 より前なら書庫へ。
#  書庫は元の並び（基準 ffb3838 の 00一覧の並び）のまま。移す行は元の位置へ差し込む。
import re, sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'c106dc'))
from common import base, REPO, F_ONE, A_ONE
dt = re.compile(rb'2026-(\d\d)-(\d\d)')
pat = re.compile(rb'^\s*\|?\s*~~')
p2 = re.compile(rb'^\s*(\||[-*])?\s*[^\w~|\x80-\xff]{0,8}~~')   # ⚠ などの印は UTF-8 の多バイト＝下で別に見る
p2u = re.compile(r'^\s*(\||[-*])?\s*[^\w~|]{0,8}~~')
p3 = re.compile(r'^\|\s*[^|~]{0,40}\|\s*~~[^|]*~~\s*\|')
def mx(l):
    d = dt.findall(l); return max(d) if d else None
def lead_struck(l):
    s = l.decode('utf-8', 'replace')
    return bool(pat.match(l) or p2u.match(s) or p3.match(s))
def plan():
    R = open(REPO + '/' + F_ONE, 'rb').read().split(b'\n')
    A = open(REPO + '/' + A_ONE, 'rb').read().split(b'\n')
    O = base(F_ONE).split(b'\n')
    pos = {}
    for i, l in enumerate(O): pos.setdefault(l, i)
    hdr = 2   # 書庫の先頭 2 行（見出し・空行）は行の本体ではない
    body = A[hdr:]
    back = [l for l in body if (mx(l) or (b'00', b'00')) >= (b'10', b'13') and l.strip()]
    toarc = [l for l in R if lead_struck(l) and (mx(l) is not None and mx(l) < (b'10', b'13')) and l not in body]
    return R, A, O, pos, hdr, body, back, toarc
def main(write):
    R, A, O, pos, hdr, body, back, toarc = plan()
    print('A->R', len(back), 'R->A', len(toarc), file=sys.stderr)
    miss = [l[:40] for l in back + toarc if l not in pos]
    print('位置が引けない行', len(miss), file=sys.stderr)
    # 書庫：戻す行を除き、移す行を元の位置で差し込む
    nb = [l for l in body if l not in back]
    keyed = [(pos.get(l, 10**9), 0, l) for l in nb] + [(pos.get(l, 10**9), 1, l) for l in toarc if l in pos]
    # 並びは元の位置の順。ただし書庫の既存の行の並びは崩さない（同じ位置は既存が先）
    arcpos = [pos.get(l, None) if l.strip() else None for l in nb]   # 空行は並びの手掛かりにしない
    out = list(nb)
    for l in toarc:
        p = pos.get(l)
        if p is None: continue
        k = 0
        for j, ap in enumerate(arcpos):
            if ap is not None and ap < p: k = j + 1
        out.insert(k, l); arcpos.insert(k, p)
    # 一覧：戻す行を元の位置の手前の行の次へ、移す行を除く
    Rn = [l for l in R if l not in toarc]
    for l in back:
        p = pos.get(l)
        k = 0
        if p is not None:
            for q in range(p - 1, -1, -1):
                if O[q].strip() and O[q] in Rn: k = Rn.index(O[q]) + 1; break
        Rn.insert(k, l)
    if write:
        open(REPO + '/' + F_ONE, 'wb').write(b'\n'.join(Rn))
        open(REPO + '/' + A_ONE, 'wb').write(b'\n'.join(A[:hdr] + out))
    return Rn, A[:hdr] + out
if __name__ == '__main__':
    main('--write' in sys.argv)
