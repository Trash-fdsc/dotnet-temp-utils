#!/bin/bash
gold=`pwd`
# VisualStudioCode:w3geMDnvA18SaWNhn4e5@
# set +m # disable job control in order to allow lastpipe
# shopt -s lastpipe

# cd /Arcs/Repos/smalls/dotnet-repos/setMegaR/
# dotnet publish --output ./build -c Release --self-contained false --use-current-runtime true /p:PublishSingleFile=true -p:PublishReadyToRun=false

mr=/Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/build
whitelist=/Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/usr.bin.whitelist


$mr/setBinExecute "/usr/bin" "g:noaccess_sbin" "$whitelist"


cd "$gold"
