"""(651) an_mon.py の自己試験（事前登録 [6] C1・C2・C4・C5）。python selftest.py [作業フォルダ]  終了コード 0＝全て通る。
  C1 合成の入力（fixtures/make_fixtures.py）に項目ごとに 1 つの極端を仕込み、正しい区分で出ること・仕込まない対照では出ないこと。期待は下の EXPECT／ABSENT（生成側と別に書く）。
  C2 突然変異（環境変数 MON_MUTATE）：各変異で上の検査のどれかが赤になること（読み手の穴の確認）。
  C4 決定性：2 回走らせてバイト一致・種の順を入れ替えても・並列の数を変えても一致。
  C5 限界の申告：LIMIT の節に 10 項目＋共通の全てが出る。"""
import os, sys, subprocess, tempfile, shutil

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, 'fixtures'))
import make_fixtures

READER = os.path.join(HERE, 'an_mon.py')

# 期待（項目, 鍵, n, 区分）。N=6 の分類：先頭 n=6／注視 n≥3／記録 n=2。
EXPECT = [
    ('1', '参政権|段3', 6, '先頭'), ('1', '検閲|全段', 6, '先頭'), ('1', '国有化:全て|段1', 2, '記録'), ('1', '最低賃金|段1', 6, '先頭'),
    ('2', 'N1|パン|short', 6, '先頭'), ('2', '*|パン|short', 6, '先頭'), ('2', 'N1|米|glut', 6, '先頭'), ('2', 'N1|茶|noProd', 6, '先頭'), ('2', 'N1|塩|noProd', 6, '先頭'),
    ('2', 'N1|キニーネ|pxHi', 6, '先頭'), ('2', 'N1|ビール|pxHi', 6, '先頭'), ('2', 'N1|干物|pxLo', 6, '先頭'),
    ('3', 'N1|Pa', 6, '先頭'), ('3', '*|Pa', 6, '先頭'), ('3', 'N2|Pb', 6, '先頭'), ('3', 'N4|Pc|商人', 6, '先頭'),
    ('4', 'D1|R4', 6, '先頭'), ('4', 'D2|R2', 6, '先頭'), ('4', 'D3|N1', 6, '先頭'), ('4', 'D3|N2', 6, '先頭'),
    ('5', 'N1|H1', 6, '先頭'), ('5', 'N1|H2', 6, '先頭'), ('5', 'N2|H3', 6, '先頭'),
    ('6', 'N1', 6, '先頭'),
    ('7', '建設:重工業', 5, '注視'), ('7', '切り下げ', 6, '先頭'), ('7', '運河:suez', 4, '注視'), ('7', '戦争(内戦)', 6, '先頭'),
    ('8', 'N1|B2farm', 6, '先頭'), ('8', 'N2|B3', 6, '先頭'),
    ('9', 'N2', 3, '先頭'), ('9', 'N3', 0, '見逃す'), ('9', 'N4', 5, '先頭'),
]
ABSENT = [
    ('1', '参政権|段1'), ('1', '参政権|段2'),
    ('2', 'N1|塩|glut'), ('2', 'N1|紙|short'), ('2', 'N1|パン|glut'),
    ('3', 'N4|Pa'), ('3', 'N5|Pa'), ('3', 'N3|Pb'), ('3', 'N4|Pc|技師'), ('3', 'N1|Pc|商人'),
    ('4', 'D1|R1'), ('4', 'D2|R3'), ('4', 'D3|N3'), ('4', 'D3|N4'),
    ('5', 'N3|H2'), ('5', 'N4|H2'),
    ('6', 'N2'),
    ('7', '侵略'), ('7', '借入'),
    ('8', 'N3|B3'),
]
NGM9_PRESENT = ['N4']; NGM9_ABSENT = ['N2', 'N3']
# 追加の検査（文字列）：(項目, 鍵, 詳細に含まれる文字)
DETAIL = [('2', 'N1|ビール|pxHi', 'B1(≥50)40行'), ('2', 'N1|キニーネ|pxHi', 'B1(≥50)0行'), ('2', 'N1|干物|pxLo', 'B1(≤0.02)40行'), ('1', '最低賃金|段1', 'R1'),
          ('9', 'N2', '敗戦'), ('9', 'N2', '内戦'), ('9', 'N2', '疫病'), ('9', 'N3', '崩壊')]
MONSUM_FASC = '中央=1903.5 最小=1901 最大=1906'

MUTATIONS = {   # 変異 → 赤になるはずの検査の説明（項目別・事前登録 追記 2 ②）
    'law_inv': '①', 'ye_off': '①②③⑤⑥(年末の断面揃え)', 'r2_off': '①R2', 'r1_off': '①R1', 'short_inv': '②', 'floor_rel': '②(相対の床・noProd と glut の分離)',
    'chronic_years': '③(慢性の年数)', 'pc_drop': '③Pc', 'pb_war': '③Pb', 'key_one': '③⑤⑧(鍵の一般の段)', 'd2_one': '④D2', 'd3_zero': '④D3', 'h2_one': '⑤H2', 'freeze_one': '⑥',
    'verb_one': '⑦', 'b2_inv': '⑧', 'window_zero': '⑨(窓)', 'ev_defeat': '⑨敗戦', 'ev_civil': '⑨内戦', 'ev_epi': '⑨疫病', 'ev_collapse': '⑨崩壊', 'fasc_last': '⑩',
}

def run(d, extra=None, env=None, seeds='1-6'):
    e = dict(os.environ); e['PYTHONUTF8'] = '1'
    e.pop('MON_MUTATE', None)
    if env: e.update(env)
    p = subprocess.run([sys.executable, '-I', READER, d, '--seeds', seeds, '--workers', '4'] + (extra or []), capture_output=True, env=e)
    if p.returncode != 0: raise RuntimeError(p.stderr.decode('utf-8', 'replace')[-800:])
    return p.stdout

def parse(out):
    rows = {}; ngm9 = set(); sums = []
    for line in out.decode('utf-8').split('\n'):
        p = line.split('\t')
        if p[0] == 'MONROW' and len(p) >= 6: rows[(p[1], p[2])] = (int(p[3]), p[4], p[5])
        elif p[0] == 'NGM9': ngm9.add(p[1])
        elif p[0] == 'MONSUM': sums.append(line)
    return rows, ngm9, sums, out.decode('utf-8')

def check(out):
    """検査に落ちた説明のリストを返す（空＝全部通る）。"""
    rows, ngm9, sums, txt = parse(out)
    bad = []
    for it, k, n, c in EXPECT:
        if (it, k) not in rows: bad.append('欠け %s %s' % (it, k))
        elif rows[(it, k)][0] != n or rows[(it, k)][1] != c: bad.append('値違い %s %s 期待=%d/%s 実際=%d/%s' % (it, k, n, c, rows[(it, k)][0], rows[(it, k)][1]))
    for it, k in ABSENT:
        if (it, k) in rows: bad.append('出てはいけない %s %s' % (it, k))
    for k in NGM9_PRESENT:
        if k not in ngm9: bad.append('NGM9 欠け ' + k)
    for k in NGM9_ABSENT:
        if k in ngm9: bad.append('NGM9 出てはいけない ' + k)
    for it, k, s in DETAIL:
        if (it, k) not in rows or s not in rows[(it, k)][2]: bad.append('詳細に %s が無い %s %s' % (s, it, k))
    if not any(MONSUM_FASC in x for x in sums): bad.append('⑩ MONSUM の値違い')
    return bad

def main():
    work = sys.argv[1] if len(sys.argv) > 1 else tempfile.mkdtemp(prefix='mon_fx_')
    fx = make_fixtures.gen(work)
    ok = True
    base = run(fx)
    bad = check(base)
    print('C1 合成の入力：%s' % ('通る' if not bad else '赤 ' + '; '.join(bad[:6]))); ok &= not bad
    # C4 決定性
    again = run(fx)
    rev = run(fx, seeds='6,5,4,3,2,1')
    w1 = run(fx, extra=['--workers', '1'])
    c4 = (base == again) and (base == rev) and (base == w1)
    print('C4 決定性（2 回・種の順・並列の数）：%s' % ('通る' if c4 else '赤')); ok &= c4
    # C5 限界
    txt = base.decode('utf-8')
    tags = ['①', '②', '③', '④', '⑤', '⑥', '⑦', '⑧', '⑨', '⑩', '共通']
    miss = [t for t in tags if ('LIMIT\t%s\t' % t) not in txt]
    c5 = not miss and 'UNREAD\t' in txt
    print('C5 限界の申告：%s' % ('通る' if c5 else '赤 欠け=' + ','.join(miss))); ok &= c5
    # C2 突然変異
    print('C2 突然変異（赤になれば合格）：')
    for m, what in MUTATIONS.items():
        try:
            o = run(fx, env={'MON_MUTATE': m})
            b = check(o)
        except Exception as ex:
            b = ['例外 ' + str(ex)[:80]]
        red = bool(b)
        print('  %-14s %-28s %s %s' % (m, what, '赤(合格)' if red else '通ってしまった(不合格)', ('｜' + b[0]) if b else ''))
        ok &= red
    print('自己試験：%s' % ('全て通る' if ok else '不合格あり'))
    if len(sys.argv) <= 1: shutil.rmtree(work, ignore_errors=True)
    sys.exit(0 if ok else 1)

if __name__ == '__main__':
    main()
