#!/bin/bash

pushd /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/
/usr/bin/python3 /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/setrestrict.py 1

echo 'aaa' >  /inRamA/111
popd

exit 0
