# -*- coding: utf-8 -*-
"""(715) prd3 の読み手の共通部（読むだけ・シミュを回さない）。prds_s<種>.tsv と prdf_s<種>.tsv を読む。"""
import csv, os

POL = {0: '絶対王政', 1: '専制', 2: '幕府', 3: '立憲君主制', 4: '共和制', 5: 'ブルジョワ独裁', 6: 'プロ独裁', 7: '軍事独裁', 8: '無政府'}
LAW = {0: '非合法', 1: '統制', 2: '容認'}
PD = 6
UK = 'イギリス'
# 先進国の定義（推）：核＝英仏普米／広＝核＋墺・日（prd2 の shadow_n.py と同じ）
ADV_WIDE = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', 'オーストリア', '日本']
# 基線 m702 と全ての比べる側の置き場（keep_711・読み終えても消さない）
KEEP = 'D:/PoB_work/keep_711'
ARMS = {'base': (KEEP + '/base_ins', 1), 'n1': (KEEP + '/n1', 1), 'n2': (KEEP + '/n2', 1), 'a11': (KEEP + '/a11', 11)}


def path(root, stem, s):
    for p in (os.path.join(root, f'{stem}_s{s}.tsv'), os.path.join(root, 'ins', f'{stem}_s{s}.tsv')):
        if os.path.exists(p):
            return p
    return None


def read(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        rows = [x for x in r if len(x) == len(h)]
    return ix, rows


def seeds(s0, n=10):
    return range(s0, s0 + n)
