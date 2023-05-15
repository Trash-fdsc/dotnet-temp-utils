#!/bin/bash

# Применение
# sudo bash /Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/exec.sh > /tmp/ex.sh
# sudo bash /tmp/ex.sh
# sudo ausearch -k UsrBinXWatcher
# sudo truncate -s 0 /var/log/audit/audit.log /var/log/audit/audit.log.1 /var/log/audit/audit.log.2 /var/log/audit/audit.log.3 /var/log/audit/audit.log.4

gold=`pwd`

# cd /Arcs/Repos/smalls/dotnet-repos/setMegaR/
# dotnet publish --output ./build -c Release --self-contained false --use-current-runtime true /p:PublishSingleFile=true -p:PublishReadyToRun=false

mr=/Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/build

$mr/setBinExecute $mr/../config.file


cd "$gold"
