# (1061) stage.py と ident.cfg の自己試験。期待値はここに直書き（stage.py や cfg から作らない）。
#   python -X utf8 -I stage_selftest.py      ＝ 0 で緑・1 で赤
import os, sys, importlib.util
HERE = os.path.dirname(os.path.abspath(__file__))


def load(p, n):
    s = importlib.util.spec_from_file_location(n, p); m = importlib.util.module_from_spec(s); s.loader.exec_module(m); return m


def check(st, cfg_text):
    bad = []
    cases = [   # (ファイル一覧, 期待する段)
        (['PriceOfBread.SimCore/Instruments/Instruments.cs'], 2), (['PriceOfBread.SimCore/Instruments/InstrumentsX.cs'], 2), (['PriceOfBread.SimCore/Instruments.cs'], 2), (['PriceOfBread.SimCore/InstrumentsNew.cs'], 2), (['PriceOfBread.SimCore/Display/Pages.cs'], 2),
        (['scripts/instruments/batch.sh'], 2), (['scripts/instruments/ident/ident_check.py'], 2), (['scripts/instruments/tools/prune.py'], 2),
        (['scripts/instruments/mon/an_mon.py'], 2), (['measure.sh'], 2),
        (['PriceOfBread.SimCore/Fiscal.cs', 'PriceOfBread.Tests/FiscalTests.cs'], 1), (['scripts/instruments/c101py/paper.py'], 1), (['docs/x.md', 'measurements/m1.txt'], 1),
        ([], 1), (['PriceOfBread.SimCore/Fiscal.cs', 'PriceOfBread.SimCore/Instruments/Instruments.cs'], 2),
    ]
    for files, want in cases:
        got = st.stage_of(files)[0]
        if got != want: bad.append(f'段が違う {files} 期待 {want} 実際 {got}')
    import tempfile, subprocess
    d = tempfile.mkdtemp(prefix='stagetest_')   # 差が測れない場合（基準のコミットが無い・git の木でない）は段 2
    if st.decide(d, 'deadbeef')[0] != 2: bad.append('git の木でない場所で段 2 にならない（fail-open）')
    subprocess.run(['git', '-C', d, 'init', '-q'], capture_output=True)
    if st.decide(d, 'deadbeef')[0] != 2: bad.append('基準のコミットが引けないのに段 2 にならない（fail-open）')
    # (1080) ident.cfg の差：基準の鍵だけ→段 1／SMALL の差→段 2／注と基準の鍵と BYPASS が混ざる→段 2（使い捨ての git の木で実際の差を測る）
    base_cfg = '# 注\nB5=a\nB10INS=b\nMON=c\nNG_BASE=n\nINSRAW=i\nBASE_COMMIT=1\nSTAGE_BASE=1\nTESTS_NONFULL=1\nTESTS_FULL=1\nSMALL=nation_s,stock_s\nBYPASS=x_s\n'
    cases2 = [
        ('基準の鍵と注だけ', base_cfg.replace('B5=a', 'B5=z').replace('# 注', '# 注2').replace('TESTS_NONFULL=1', 'TESTS_NONFULL=9').replace('STAGE_BASE=1', 'STAGE_BASE=2'), 1),
        ('SMALL の差', base_cfg.replace('nation_s,stock_s', 'nation_s'), 2),
        ('注と基準の鍵と BYPASS', base_cfg.replace('# 注', '# 注2').replace('B5=a', 'B5=z').replace('BYPASS=x_s', 'BYPASS=y_s'), 2),
        ('未知の鍵の追加', base_cfg + 'NEWKEY=1\n', 2),
    ]
    for name, newtext, want in cases2:
        d = tempfile.mkdtemp(prefix='stagecfg_')
        cp = os.path.join(d, 'scripts', 'instruments', 'ident'); os.makedirs(cp)
        subprocess.run(['git', '-C', d, 'init', '-q'], capture_output=True)
        open(os.path.join(cp, 'ident.cfg'), 'wb').write(base_cfg.encode('utf-8'))
        subprocess.run(['git', '-C', d, 'add', '.'], capture_output=True)
        subprocess.run(['git', '-C', d, '-c', 'user.name=t', '-c', 'user.email=t@t', 'commit', '-q', '-m', 'b'], capture_output=True)
        open(os.path.join(cp, 'ident.cfg'), 'wb').write(newtext.encode('utf-8'))
        got, why, hit = st.decide(d, 'HEAD')
        if got != want: bad.append(f'ident.cfg の例 {name} 期待 {want} 実際 {got}（{why}）')
    small = []
    for l in cfg_text.splitlines():
        if l.startswith('SMALL='): small = [x.strip() for x in l[6:].split(',') if x.strip()]
    for t in ('nation_s', 'stock_s', 'wars_s', 'casualty_s'):   # NG の読み手（ng.sh）
        if t not in small: bad.append(f'SMALL に {t} が無い（NG の読み手の表）')
    for t in ('region_s', 'migration_s', 'pxmonth_s', 'scalars_s', 'monlaw_s', 'mongood_s', 'monai_s', 'lawgate_s', 'reform_s', 'sbu_s', 'sf_s', 'fiscal_s'):   # 止める線・注視の読み手（an_mon.py の名簿が読む表も）
        if t not in small: bad.append(f'SMALL に {t} が無い')
    # an_mon.py が読む表（名簿の第 3 要素・P('…')・need('…')・tcols の鍵）は全て SMALL に在る＝読み手のソースから独立に数える（i・ban・rev は辞書の鍵なので除く）
    import re
    src = open(os.path.join(HERE, '..', 'mon', 'an_mon.py'), encoding='utf-8').read()
    used = set(re.findall(r"\('[^']*', \d, '([a-z0-9]+)'", src)) | set(re.findall(r"P\('([a-z0-9]+)'\)", src)) | set(re.findall(r"need\('([a-z0-9]+)'\)", src))
    tc = re.search(r"tcols = \{(.*?)\}\s*\n\s*for ", src, re.S) or re.search(r"tcols = \{(.*?)'sf': \['quarter', 'nation'\]\}", src, re.S)
    if tc: used |= set(re.findall(r"'([a-z0-9]+)':", tc.group(1)))
    for t in sorted(used - {'i', 'ban', 'rev'}):
        if t + '_s' not in small: bad.append(f'SMALL に an_mon.py が読む {t}_s が無い')
    if len(used) < 15: bad.append('an_mon.py から読む表を拾えていない（正規表現が壊れた）')
    keep = []
    for l in cfg_text.splitlines():
        if l.startswith('PRUNE_KEEP='): keep = [x.strip() for x in l[11:].split(',') if x.strip()]
    for t in ('nation_s', 'stock_s', 'wars_s', 'casualty_s'):
        if t not in keep: bad.append(f'PRUNE_KEEP に {t} が無い（NG の読み手の 4 表は刈らない）')
    return bad


if __name__ == '__main__':
    st = load(os.path.join(HERE, 'stage.py'), 'stage')
    cfg = open(os.path.join(HERE, 'ident.cfg'), encoding='utf-8').read()
    ok = True
    g = check(st, cfg); print('正しい実装：' + ('緑' if not g else '赤 ' + '; '.join(g))); ok &= not g
    # 突然変異
    m8 = load(os.path.join(HERE, 'stage.py'), 'stage8'); _cf = m8.changed_files; m8.changed_files = lambda t, bs: (_cf(t, bs) or [])   # 失敗を空の差に読み替える（fail-open）
    r = check(m8, cfg); print('MU8 git の失敗を空の差と読む：' + ('赤を確認（' + str(len(r)) + ' 件）' if r else '緑のまま（穴）')); ok &= bool(r)
    m4 = load(os.path.join(HERE, 'stage.py'), 'stage4'); m4.STAGE2_RX = [x for x in m4.STAGE2_RX if 'Instruments[^/]' not in x]
    r = check(m4, cfg); print('MU4 段 2 の条件から Instruments*.cs を外す：' + ('赤を確認（' + str(len(r)) + ' 件）' if r else '緑のまま（穴）')); ok &= bool(r)
    m1 = load(os.path.join(HERE, 'stage.py'), 'stage1'); _c1 = m1.changed_files; m1.changed_files = lambda t, bs: (lambda r: None if r is None else [f for f in r if not f.endswith('ident.cfg')])(_c1(t, bs))
    r = check(m1, cfg); print('MU1 ident.cfg を丸ごと外す：' + ('赤を確認（' + str(len(r)) + ' 件）' + ' / '.join(x[:40] for x in r) if r else '緑のまま（穴）')); ok &= bool(r)
    m2 = load(os.path.join(HERE, 'stage.py'), 'stage2'); m2.BASELINE_KEYS = m2.BASELINE_KEYS + ('SMALL',)
    r = check(m2, cfg); print('MU2 基準の鍵の一覧に SMALL を混ぜる：' + ('赤を確認（' + str(len(r)) + ' 件）' + ' / '.join(x[:40] for x in r) if r else '緑のまま（穴）')); ok &= bool(r)
    lines = [(l.replace('nation_s,', '').replace(',nation_s', '') if l.startswith('SMALL=') else l) for l in cfg.splitlines()]
    r = check(st, '\n'.join(lines)); print('MU5 SMALL から nation_s を落とす：' + ('赤を確認（' + str(len(r)) + ' 件）' if r else '緑のまま（穴）')); ok &= bool(r)
    sys.exit(0 if ok else 1)
