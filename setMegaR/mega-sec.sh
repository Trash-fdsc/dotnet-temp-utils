#!/bin/bash
gold=`pwd`
# VisualStudioCode:w3geMDnvA18SaWNhn4e5@
# set +m # disable job control in order to allow lastpipe
# shopt -s lastpipe

# cd /Arcs/Repos/smalls/dotnet-repos/setMegaR/
# dotnet publish --output ./build -c Release --self-contained false --use-current-runtime true /p:PublishSingleFile=true -p:PublishReadyToRun=false

mr=/Arcs/Repos/smalls/dotnet-repos/setMegaR/build

cd /A/Mega

sudo chown -R --from=:mega :arcs-read .
sudo chown -R --from=:root :arcs-read .
sudo setfacl -R -x mega .

sudo $mr/setMegaR /A/Mega mega


cd /Arcs/A/Ya-disk/

sudo chown -R --from=:yandex-disk :arcs-read .
sudo chown -R --from=:root :arcs-read .
sudo setfacl -R -x yandex-disk .

sudo $mr/setMegaR /Arcs/A/Ya-disk/ yandex-disk


cd "$gold"
