#!/usr/bin/env bash
# (1191) 合流の読み手（m1184/read1184.sh の型・門は線の台本をそのまま＝c118nc/read_nc.sh と c118ur/read_ur.sh の中身）。
#   使い方: bash read1191.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> <基準の生（D:/ の形）> <門の組 nc,ur か ->
#   種 1〜10 の基準は keep_1184/base_ins・種 11〜20 は keep_1184/a11（m1184 の種 11〜20＝nc と ur の旗 0 そのもの）。ヌル腕は keep_1184/n1・n2（記録）。
#   ur の門の旗 0 の紙は種 1〜10 が measurements/m1188_20261117/paper_M.json・種 11〜20 が paper_A11.json（線が keep_1184/a11 で取った）。
#   (1191) 種 11〜20 の取りこぼしの直し：warcount.py は --seeds=、floor_count.py・secfac.py は種の終わりを引数で渡す（S11 で 0/0 が出たので手で回し直した後に直した）。
#   nc の門 H（magn.py）の基線の mil180 は種 1〜10 が ng1184・種 11〜20 は基準の生 a11 に ng.sh を当てて取る。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; BASE=${6:?基準}; GS=${7:--}
SE=$((SB+9)); K=D:/PoB_work/keep_1184; P=$R/measurements/m1188_20261117
mkdir -p "$OUT"
cd "$R"
case ",$GS," in *,nc,*)
  python -X utf8 -I "$S/c118nc/warcount.py" M=$BASE N1=$K/n1 N2=$K/n2 B=$INS --seeds=$SB-$SE > "$OUT/warcount.txt" 2>&1; echo "warcount rc=$?"
  python -X utf8 -I "$S/c118nc/floor_count.py" "$INS" $SE > "$OUT/floor_B.txt" 2>&1; echo "floor rc=$?"
  python -X utf8 -I "$S/c118nc/floor_count.py" "$BASE" $SE > "$OUT/floor_base.txt" 2>&1; echo "floor base rc=$?"
  python -X utf8 -I "$S/c118nc/secfac.py" "$INS" $SE > "$OUT/secfac_B.txt" 2>&1; echo "secfac rc=$?"
  python -X utf8 -I "$S/m1191/rec1191.py" "$SB" arm=$INS base=$BASE > "$OUT/rec_v2.txt" 2>&1; echo "rec rc=$?" ;;
esac
case ",$GS," in *,ur,*)
  python -X utf8 -I "$S/c118ur/ur.py" paper "$INS" "$SB" "$SE" "$OUT/paper_ur.json" > "$OUT/paper_ur.txt" 2>&1; echo "ur paper rc=$?"
  if [ "$SB" = 1 ]; then F0=$P/paper_M.json; else F0=$P/paper_A11.json; fi
  python -X utf8 -I "$S/c118ur/ur.py" gates "$OUT/paper_ur.json" $F0 > "$OUT/gates_ur.txt" 2>&1; echo "ur gates rc=$?" ;;
esac
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  if [ "$SB" = 1 ]; then BC=$R/measurements/b10_20261116_m1184_ins; else BC=$R/measurements/b10s11_20261116_m1184_ins; fi
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$BC" "$R/measurements/b10_20261116_m1184_n1_ins" "$R/measurements/b10_20261116_m1184_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1184_20261116:m1184 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
case ",$GS," in *,nc,*)
  if [ "$SB" = 1 ]; then MB=$R/measurements/ng1184_20261116/mil180_m1184.txt
  else bash "$S/ng.sh" "$BASE" a11 "$OUT/ng_base" rd:$R/measurements/ng1184_20261116:m1184 > "$OUT/ng_base.txt" 2>&1; MB=$OUT/ng_base/mil180_a11.txt; fi
  python -X utf8 -I "$S/c118nc/magn.py" "$MB" "$OUT/ng/mil180_$TAG.txt" > "$OUT/magn_H.txt" 2>&1; echo "magn rc=$?" ;;
esac
echo READDONE
