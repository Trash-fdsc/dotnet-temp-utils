#!/bin/bash

# Применение
# sudo bash /Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/exec.sh > /tmp/ex.sh
# sudo bash /tmp/ex.sh

gold=`pwd`
# VisualStudioCode:w3geMDnvA18SaWNhn4e5@
# set +m # disable job control in order to allow lastpipe
# shopt -s lastpipe

# cd /Arcs/Repos/smalls/dotnet-repos/setMegaR/
# dotnet publish --output ./build -c Release --self-contained false --use-current-runtime true /p:PublishSingleFile=true -p:PublishReadyToRun=false

mr=/Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/build

# whitelist=/Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/usr.bin.whitelist
# $mr/setBinExecute "/usr/bin" "g:noaccess_sbin" "$whitelist"

# whitelist=/Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/usr.bin.deluge.whitelist
# $mr/setBinExecute "/usr/bin" "g:noaccess_sbin_deluge" "$whitelist"

# whitelist=/Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/usr.bin.mega.whitelist
$mr/setBinExecute $mr/../config.file


cd "$gold"
