#!/usr/bin/env bash
M=/d/PoB_work/c98lc_work/scripts/mut.sh; L=/d/PoB_work/c98lc_work/mut_all.txt; : > $L
bash $M M1 PriceOfBread.SimCore/BilateralMarket.cs 'costC[CostLedger.ClearingSkipsLB] += nEvalLB;' 'costC[CostLedger.ClearingSkipsLB] += nSkipLB;' 'FullyQualifiedName~ClearingLowerBoundTests|FullyQualifiedName~WriterRatchetTests' >> $L 2>&1
bash $M M2 PriceOfBread.SimCore/BilateralMarket.cs 'bool lbOn = c.TransportMargin >= 0 &&' 'bool lbOn = true || c.TransportMargin >= 0 &&' 'FullyQualifiedName~ClearingLowerBoundTests' >> $L 2>&1
bash $M M3 PriceOfBread.SimCore/InstrumentsTradeskip.cs 'else if (tr[s].access <= 1e-12) Add(sn, g, TsCol.OgAcc0);' 'else if (false) Add(sn, g, TsCol.OgAcc0);' 'FullyQualifiedName~TradeSkipC4Tests' >> $L 2>&1
bash $M M4 PriceOfBread.SimCore/BilateralMarket.cs 'xPrice <= xNeed ? Instruments.TsExit.XPriceSmall' 'xPrice >= xNeed ? Instruments.TsExit.XPriceSmall' 'FullyQualifiedName~TradeSkipC4Tests|FullyQualifiedName~TradeInstrumentTests' >> $L 2>&1
bash $M M5 PriceOfBread.SimCore/InstrumentsSellr.cs 'm.LastDemand + m.PrevExport - m.PrevImport' 'm.LastDemand + m.PrevExport + m.PrevImport' 'FullyQualifiedName~TradeSkipC4Tests' >> $L 2>&1
echo fin > /d/PoB_work/c98lc_work/mut.mark
