#!/usr/bin/env bash
# (1311) c136kb 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚・c135k47/mut_k47.sh の写し）：bash mut_kb.sh <項 2 のコミット> <項 1 のコミット> <出力（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、M ごとに 1 か所だけ書き換えて LampOilDuplicateRecipeTests・SecondRecipeContentsTests・DiscoveryEngineerTests を回す。
#   M0＝何もしない実装（SimCore を main 1c8873228 に戻し、試験は項 2 の物）・M1＝石油ケロシンを建てない・M2＝「石油灯油」の発見の行を戻す・
#   M3＝「石油灯油」の行とレシピ（旗なし）を戻す・Mv＝項 1 の木（名簿と Terms の Assert が残る）で Discovery.cs の `if (!spread && rp is null) continue;` を外す（記録）。
REV=${1:?}; REV1=${2:?}; OUT=${3:?}; R=D:/PoB_work/wt_c136kb; W=D:/PoB_work/c136kb_work/mut
export TEMP=D:/PoB_work/c136kb_work/tmp TMP=D:/PoB_work/c136kb_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP $W
RP="python -I $R/scripts/instruments/tools/replace1.py"
# M3 の新しい文字列（main の Simulation.cs の「石油灯油」の Add の 2 行を旗なしで）
git -C $R show 1c8873228:PriceOfBread.SimCore/Simulation.cs | awk 'NR==1225||NR==1226' > $W/m3_add.txt
grep -q '"石油灯油"' $W/m3_add.txt || { echo "M3 の行が取れない" >> $OUT/mut.txt; exit 1; }
printf '        // 圧縮帯（自動車1.50' > $W/m3_old.txt
cat $W/m3_add.txt $W/m3_old.txt > $W/m3_new.txt
: > $OUT/mut.txt
for MU in M0 M1 M2 M3 Mv; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  case $MU in
    M0) git -C $R archive 1c8873228 PriceOfBread.SimCore | tar -x -C $T && git -C $R archive $REV PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; } ;;
    Mv) git -C $R archive $REV1 PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; } ;;
    *)  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; } ;;
  esac
  case $MU in
    M0) RC0=0 ;;
    M1) $RP $T/PriceOfBread.SimCore/Simulation.cs 'if (keroDefault is not null)   // 灯油を作らない世界' 'if (keroDefault is null)   // 灯油を作らない世界' > $OUT/$MU.replace.txt 2>&1; RC0=$? ;;
    M2) $RP $T/PriceOfBread.SimCore/Catalog.cs '        new("石油ケロシン", LampOilId, "石油ケロシン", DiscoveryField.Process, 1850),' '        new("石油灯油", LampOilId, "石油灯油", DiscoveryField.Process, 1850),\n        new("石油ケロシン", LampOilId, "石油ケロシン", DiscoveryField.Process, 1850),' > $OUT/$MU.replace.txt 2>&1; RC0=$? ;;
    M3) $RP $T/PriceOfBread.SimCore/Catalog.cs '        new("石油ケロシン", LampOilId, "石油ケロシン", DiscoveryField.Process, 1850),' '        new("石油灯油", LampOilId, "石油灯油", DiscoveryField.Process, 1850),\n        new("石油ケロシン", LampOilId, "石油ケロシン", DiscoveryField.Process, 1850),' > $OUT/$MU.replace.txt 2>&1 \
          && $RP $T/PriceOfBread.SimCore/Simulation.cs x x --file-old $W/m3_old.txt --file-new $W/m3_new.txt >> $OUT/$MU.replace.txt 2>&1; RC0=$? ;;
    Mv) $RP $T/PriceOfBread.SimCore/Discovery.cs '            if (!spread && rp is null) continue;' '            // (突然変異 Mv) 代表の生産者の無い製法の行の continue を外した' > $OUT/$MU.replace.txt 2>&1; RC0=$? ;;
  esac
  [ $RC0 = 0 ] || { echo "$MU REPLACEFAIL" >> $OUT/mut.txt; continue; }
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $W/art_$MU -v q -nologo > $OUT/$MU.build.txt 2>&1 || { echo "$MU BUILDFAIL" >> $OUT/mut.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $W/art_$MU --no-build --filter "FullyQualifiedName~LampOilDuplicateRecipeTests|FullyQualifiedName~SecondRecipeContentsTests|FullyQualifiedName~DiscoveryEngineerTests" > $OUT/$MU.test.txt 2>&1; RC=$?
  echo "$MU rc=$RC $(grep -E '合計|成功!|失敗!' $OUT/$MU.test.txt | tail -1)" >> $OUT/mut.txt
  grep -E "^\s+失敗 " $OUT/$MU.test.txt | awk '{$1=$1; print "    " $0}' >> $OUT/mut.txt
  rm -rf $T $W/art_$MU
done
echo "MUTDONE $(date '+%F %T')" >> $OUT/mut.txt
