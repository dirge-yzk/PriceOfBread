#!/usr/bin/env python
# (623) 線 mgp の読み手（読むだけ）。事前登録 measurements/mgp623_20261028_prereg.txt [3] の定義のとおり。
# 使い方：PYTHONUTF8=1 python scripts/instruments/mgp623/g_lines.py --arm <腕 B の ins フォルダ> [--base <基線 m617_ins>] [--null <N1> <N2>] [--armA <腕 A の ins フォルダ>]
#   各フォルダには mgp_s<seed>.tsv がある（INS=1 batch の出力・列は InstrumentsMegaMat.cs の先頭の注）。基線・ヌル腕の生（m617）には mgp_s が無い（旗 0 の値＝基線の値とする）ので、
#   --base／--null は mgp_s が在るときだけ読む（無ければ記録の欄に〈列なし〉と出す）。
# G1：腕 B の全ての行で startedMaterialMissingCount ＝ 0。
# G2：腕 B の (i) refusedMaterialCount が正の行 ≧1、(ii) unstartedMaterialMissingCount が正の行 ≧1（(iii) は G1 と同じ内容なので外した＝事前登録 [10]）。
# G2′（事前登録 [10]）：unstartedImportLeadCount が正の行 ≧1（旧の発見の門より先に材料の述語が輸入で開いた個体がある）。0 行なら旗 1 は旗 0 と同じ着工の集合＝空の門。列は旗に依らない＝腕 A（旗 0）の mgp_s で読める。
# 記録：startedIds の並び（個体・国・四半期・年）を 1783〜1785 年の件数と合わせて出す（旗 0 の腕 A と並べる）。
import argparse, csv, collections, glob, os, statistics

SEVEN = ['dover_calais_cable', 'transatlantic_cable', 'anglo_indian_cable', 'muscat_cable_station', 'fuzhou_navy_yard', 'ba_son_arsenal', 'daimler_works']

def rows(arm):
    out = []
    for p in sorted(glob.glob(os.path.join(arm, 'mgp_s*.tsv'))):
        s = os.path.basename(p)[len('mgp_s'):-4]
        with open(p, encoding='utf-8') as f:
            for d in csv.DictReader(f, delimiter='\t'):
                d['seed'] = s; out.append(d)
    return out

def starts(rs):
    ev = []
    for d in rs:
        for i in d['startedIds'].split(';'):
            if i: ev.append((d['seed'], d['nation'], int(d['quarter']), int(d['year']), i))
    return ev

def completes(rs):
    ev = []
    for d in rs:
        for i in d['completedIds'].split(';'):
            if i: ev.append((d['seed'], d['nation'], int(d['quarter']), int(d['year']), i))
    return ev

def arm_report(name, arm):
    rs = rows(arm)
    if not rs:
        print(f'[{name}] mgp_s が無い（列なし）：{arm}'); return None
    n = len(rs)
    g1_bad = sum(1 for d in rs if int(d['startedMaterialMissingCount']) > 0)
    ref_mat = sum(int(d['refusedMaterialCount']) for d in rs)
    ref_meth = sum(int(d['refusedMethodCount']) for d in rs)
    unm = sum(1 for d in rs if int(d['unstartedMaterialMissingCount']) > 0)
    st = starts(rs); cp = completes(rs)
    print(f'[{name}] 行 {n}・着工 {len(st)}・完成 {len(cp)}・G1（材料の欠けで着工した行）{g1_bad}・refusedMaterial 合計 {ref_mat}（正の行 {sum(1 for d in rs if int(d["refusedMaterialCount"])>0)}）・refusedMethod 合計 {ref_meth}・unstartedMaterialMissing が正の行 {unm}')
    early = collections.Counter(e[4] for e in st if e[3] <= 1785)
    print(f'  1783〜1785 年の着工 {sum(early.values())} 件（個体の種類 {len(early)}）')
    lead = sum(1 for d in rs if int(d['unstartedImportLeadCount']) > 0)
    lag = sum(1 for d in rs if int(d['unstartedMaterialLagCount']) > 0)
    net_pos = sum(1 for d in rs if int(d['unstartedLegacyUnmetCount']) - int(d['unstartedMethodUnmetCount']) - int(d['unstartedMaterialMissingCount']) > 0)
    net_neg = sum(1 for d in rs if int(d['unstartedLegacyUnmetCount']) - int(d['unstartedMethodUnmetCount']) - int(d['unstartedMaterialMissingCount']) < 0)
    print(f'  G2′（既存の列の差・列の定義に依らない）Legacy−Method−Material が正の行 {net_pos}（=lead−lag>0）・負の行 {net_neg}')
    print(f'  （参考・(624) の修正前のビルドの生では材料の無い個体も数えた＝判定に使わない）G2′ の列：unstartedImportLeadCount（旧の発見の門が閉じ材料の述語が真）が正の行 {lead}・unstartedMaterialLagCount（旧の門が開き材料の述語が偽）が正の行 {lag}')
    return dict(rs=rs, st=st, cp=cp, g1_bad=g1_bad, ref_mat=ref_mat, unm=unm, lead=lead, lag=lag, net_pos=net_pos)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arm', required=True); ap.add_argument('--base'); ap.add_argument('--null', nargs='*', default=[]); ap.add_argument('--armA'); ap.add_argument('--label', default='腕 B')
    a = ap.parse_args()
    B = arm_report(a.label, a.arm)
    if a.armA: arm_report('腕 A', a.armA)
    if a.base: arm_report('基線', a.base)
    for i, nl in enumerate(a.null, 1): arm_report(f'ヌル N{i}', nl)
    if B is None: return
    print('--- 判定 ---')
    print('G1', 'PASS' if B['g1_bad'] == 0 else f'FAIL（{B["g1_bad"]} 行）')
    print('G2', 'PASS' if (B['ref_mat'] > 0 and B['unm'] > 0) else f'FAIL（refusedMaterial 合計 {B["ref_mat"]}・unstartedMaterialMissing の正の行 {B["unm"]}）＝働いていない')
    print('G2′', 'PASS' if B['net_pos'] > 0 else f'FAIL（unstartedImportLeadCount が正の行 {B["lead"]}・既存の列の差が正の行 {B["net_pos"]}）＝空の門（旗 1 は旗 0 と着工の集合が同じ）')
    # 記録：着工の年代と国（個体ごとの最初の着工の四半期の中央値）
    first = collections.defaultdict(list)
    for e in B['st']: first[e[4]].append((e[3], e[1]))
    for i in SEVEN:
        ys = [y for y, _ in first.get(i, [])]
        print(f'  {i}: 着工 {len(ys)} 回（種×国）・年の中央値 {statistics.median(ys) if ys else "-"}')

if __name__ == '__main__':
    main()
