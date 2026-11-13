#!/bin/bash
cd /d/PoB_work/c106im_work/mut
dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c106im_mutart --filter "FullyQualifiedName~FoodLedgerTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~InstrumentsPlacementTests|FullyQualifiedName~WriterRatchetTests" > ../mut.log 2>&1
echo "exit=$?" >> ../mut.log
touch ../mut.done
