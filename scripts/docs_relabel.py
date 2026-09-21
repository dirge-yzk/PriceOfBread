# -*- coding: utf-8 -*-
# ★2026-09-03（キューD-38）：PYTHONUTF8 なしで起動されても**黙って化けない**ようにする自衛。
#   ⚠実害＝docs 100件が化けた（30%は例外で止まるが70%は黙って通る）。環境変数は作者の設定なのでここでは触らず、
#   スクリプト側で標準入出力を UTF-8 に張り替える（ファイル入出力は各所で encoding を明示しているのでこれで足りる）。
import sys as _sys
try:
    _sys.stdout.reconfigure(encoding='utf-8')
    _sys.stderr.reconfigure(encoding='utf-8')
except Exception:
    pass
"""設計docの棚卸リネーム＋参照追随。

入力: labels.tsv（相対パス<TAB>ラベル）。ラベル「記録」はリネームしない（設計書ではないため）。
処理: docs/<rel> → basename の末尾に .DONE / .PARTIAL / .HOLD / .REVERTED を挿入
      その後、全 .cs / .md をスキャンして旧 basename を新 basename へ置換（参照が死なないように）。
使い方: python scripts/docs_relabel.py labels.tsv [--apply]   （--apply 無しは下見だけ）

⚠ 2026-08-18 の是正2点（100件の文字化けの再発防止）:
  1) ROOT が別PCのユーザー名直書きだった → スクリプト位置から導出
  2) 【ラベル】を付けていた → ASCII ラベルへ。日本語名の直後に ASCII の '.' が来る並びは
     cp932 誤読で不可逆に壊れる（実測 387件中118件が往復不能）。
     ★ ただしこれは緩和にすぎない。真の防波堤は PYTHONUTF8=1（doc 参照）。
"""
import io, os, sys, collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCS = os.path.join(ROOT, 'docs')

# 日本語ラベル → ASCII サフィックス（既存のリネーム済みファイルと同じ語彙）
LABELS = {'完了': '.DONE', '一部': '.PARTIAL', '保留': '.HOLD', '差し戻し': '.REVERTED'}
SUFFIXES = tuple(LABELS.values())


def main():
    if sys.stdout.encoding and sys.stdout.encoding.lower() not in ('utf-8', 'utf8'):
        sys.stdout.reconfigure(encoding='utf-8')   # コンソールが cp932 でも日本語名を出せる
    tsv = sys.argv[1]
    apply = '--apply' in sys.argv
    pairs = []          # (旧絶対パス, 新絶対パス, 旧basename, 新basename)
    skipped = []
    for line in io.open(tsv, encoding='utf-8'):
        line = line.rstrip('\n')
        if not line.strip() or line.startswith('#'):
            continue
        rel, label = line.split('\t')[:2]
        rel = rel.strip().replace('\\', '/')
        label = label.strip()
        src = os.path.join(DOCS, rel.replace('/', os.sep))
        if not os.path.isfile(src):
            skipped.append((rel, 'ファイルが無い'))
            continue
        if label == '記録':
            skipped.append((rel, '記録＝リネーム対象外'))
            continue
        if label not in LABELS:
            skipped.append((rel, f'未知のラベル「{label}」'))
            continue
        base = os.path.basename(src)
        stem, ext = os.path.splitext(base)
        if stem.endswith(SUFFIXES) or stem.endswith('】'):   # 二重付与を防ぐ
            skipped.append((rel, '既にラベルあり'))
            continue
        newbase = f'{stem}{LABELS[label]}{ext}'
        pairs.append((src, os.path.join(os.path.dirname(src), newbase), base, newbase))

    print(f'リネーム対象 {len(pairs)}件 / 対象外 {len(skipped)}件')
    for rel, why in skipped:
        print(f'  skip: {rel}  ({why})')
    for _, _, ob, nb in pairs:
        print(f'  {ob}\n    → {nb}')

    if not apply:
        print('\n（下見のみ。--apply で実行）')
        return

    # 1) リネーム
    for src, dst, _, _ in pairs:
        os.rename(src, dst)
    print(f'\nリネーム完了 {len(pairs)}件')

    # 2) 参照追随（旧basename → 新basename）。長い名前から置換＝部分一致の食い合いを防ぐ
    table = sorted(((ob, nb) for _, _, ob, nb in pairs), key=lambda t: -len(t[0]))
    touched = collections.Counter()
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in ('bin', 'obj', '.git', 'dist', 'PriceOfBread_ver1.3')]
        for fn in filenames:
            if not fn.endswith(('.cs', '.md')):
                continue
            p = os.path.join(dirpath, fn)
            try:
                s = io.open(p, encoding='utf-8-sig').read()
            except Exception:
                continue
            orig = s
            for ob, nb in table:
                if ob in s:
                    s = s.replace(ob, nb)
            if s != orig:
                # 元のBOM有無を保つ（.cs は BOM 付きが混在するため）
                raw = open(p, 'rb').read(3)
                enc = 'utf-8-sig' if raw == b'\xef\xbb\xbf' else 'utf-8'
                io.open(p, 'w', encoding=enc, newline='').write(s)
                touched[os.path.relpath(p, ROOT)] += 1
    print(f'参照を更新したファイル {len(touched)}件')
    for k in sorted(touched):
        print(f'  {k}')


main()
