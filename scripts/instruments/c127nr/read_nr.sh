#!/usr/bin/env bash
# (1249) c127nr 腕 B の読み手（c124nr/read_nr.sh の写し・基線とヌル腕と NG の基準は m1233〔keep_1233/base_ins・n1〕・NG は ng1233・ヌル腕は N1 の 1 本）。英の H3 の記録 h3uk.py と uk_seedtab.py を足した。
#   足した物：狙い W1 と記録（c122nr/w1.py）・統一の成立（c122nr/formations.py）。
#   門＝止める線（stoplines1002・plvl_seeds・popjump1017・an_h2 -v4・warcount の 10 倍・.cost）＋史実の桁 H1・H2（paper.py）・H3（magn.py）＋帰属 A1（paper.py）。他は記録。
#   使い方: bash read_nc.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}
K=D:/PoB_work/keep_1233; BASE=$K/base_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c118nc/warcount.py" M=$BASE N1=$K/n1 B=$INS > "$OUT/warcount.txt" 2>&1; echo "warcount rc=$?"
python -X utf8 -I "$S/c119nd/paper.py" "$BASE" 1 10 "$INS" > "$OUT/paper_B.txt" 2>&1; echo "paper rc=$?"
python -X utf8 -I "$S/c119nd/paper.py" "$INS" 1 10 > "$OUT/paper_Bself.txt" 2>&1; echo "paper self rc=$?"   # H1・H2＝腕の遷移と立憲の割合（M の値は paper_B.txt の基線の節）
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
for n in n1; do python -X utf8 -I "$S/brk693/plvl_seeds.py" "$K/$n" > "$OUT/plvl_$n.txt" 2>&1; echo "plvl $n rc=$?"; done
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261119_m1233_ins" "$R/measurements/b10_20261119_m1233_n1_ins" "$R/measurements/b10_20261119_m1233_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1233_20261119:m1233 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c118nc/magn.py" "$R/measurements/ng1233_20261119/mil180_m1233.txt" "$OUT/ng/mil180_$TAG.txt" > "$OUT/magn_H.txt" 2>&1; echo "magn rc=$?"
python -X utf8 -I "$S/c124nr/w1.py" "$BASE" "$INS" > "$OUT/w1.txt" 2>&1; echo "w1 rc=$?"
python -X utf8 -I "$S/c124nr/formations.py" M=$BASE N1=$K/n1 B=$INS > "$OUT/formations.txt" 2>&1; echo "formations rc=$?"
R25=$S/r1225
for t in M=$BASE B=$INS; do n=${t%%=*}; d=${t#*=}; echo "== $n"; python -X utf8 -I $R25/wars_entry.py "$d" 1 10; done > "$OUT/wars_entry.txt" 2>&1; echo "wars_entry rc=$?"
python -X utf8 -I $R25/declare_first.py "$INS" 1 10 > "$OUT/declare_first.txt" 2>&1; echo "declare_first rc=$?"
python -X utf8 -I "$S/c124nr/unif_read.py" "$BASE" "$INS" > "$OUT/unif_read.txt" 2>&1; echo "unif_read rc=$?"
for t in M=$BASE N1=$K/n1 B=$INS; do n=${t%%=*}; d=${t#*=}; python -X utf8 -I "$D0/h3uk.py" "$d" 1 10 $n; done > "$OUT/h3uk.txt" 2>&1; echo "h3uk rc=$?"
for t in M=$BASE N1=$K/n1 B=$INS; do n=${t%%=*}; d=${t#*=}; python -X utf8 -I "$S/r1238/uk_seedtab.py" "$d" 1 10 $n; done > "$OUT/uk_seedtab.txt" 2>&1; echo "uk_seedtab rc=$?"
python -X utf8 -I "$D0/tw_read.py" Z0=D:/PoB_work/c127nr_work/z0_tables B=$INS > "$OUT/tw_read.txt" 2>&1; echo "tw_read rc=$?"
echo READDONE
